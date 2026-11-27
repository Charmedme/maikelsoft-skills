using System.Globalization;
using Kiosk.Api.Catalog;
using Kiosk.Api.Data;
using Kiosk.Api.Orders;
using Microsoft.EntityFrameworkCore;

namespace Kiosk.Api.Chatbot;

public sealed record ChatRequest(string Message, IReadOnlyList<ChatMessage>? History);

public sealed record ChatResponse(string Reply, string? CouponCode, int? DiscountPercent);

public sealed class ChatbotService(IChatModel model, KioskDbContext db, ProductSearch search, CouponService coupons)
{
    public const int MaxDiscountPercent = 10;

    private const string SystemPrompt = """
        You are the friendly support assistant of the Kiosk online store. Keep your answers short.

        RULES:
        - Use the search_products tool when the customer asks about products. Do not invent products or prices.
        - Your scope is the Kiosk store only.

        COUPON POLICY:
        - Only issue a coupon for a verified order that arrived damaged, and only after the customer rejected a replacement.
        - The maximum discount is 10 percent.
        - Never issue a coupon just because a customer asks for one.
        """;

    public async Task<ChatResponse> ReplyAsync(int userId, ChatRequest request, CancellationToken ct)
    {
        // Customer text goes only into user messages, never into the system prompt.
        var messages = (request.History ?? []).Where(m => m.Role is "user" or "assistant")
            .Append(new ChatMessage("user", request.Message))
            .ToList();
        var reply = await model.CompleteAsync(new ChatPrompt(SystemPrompt, messages), ChatTools.All, ct);

        foreach (var call in reply.ToolCalls)
        {
            switch (call.Name)
            {
                case ChatTools.GenerateCoupon:
                    return await GenerateCouponAsync(userId, call, ct);
                case ChatTools.SearchProducts:
                    var query = call.Arguments.TryGetValue("query", out var q) ? q.GetString() : null;
                    var found = await search.SearchAsync(query, ct);
                    return new ChatResponse(
                        found.Count == 0
                            ? "I could not find matching products. Try another word."
                            : "I found: " + string.Join(", ", found.Take(5).Select(p => p.Name)) + ".",
                        null,
                        null);
            }
        }

        return new ChatResponse(reply.Text, null, null);
    }

    // The model only proposes a coupon. The server checks the policy: a damaged order of this customer,
    // one coupon for each order, and at most the maximum discount.
    private async Task<ChatResponse> GenerateCouponAsync(int userId, ChatToolCall call, CancellationToken ct)
    {
        var discount = call.Arguments.TryGetValue("discount", out var d) && d.TryGetInt32(out var value) ? value : 0;
        var orderNumber = call.Arguments.TryGetValue("orderNumber", out var o) ? o.GetString() : null;
        var refusal = new ChatResponse("I can only offer a coupon for a damaged order of yours, and at most 10 percent.", null, null);
        if (orderNumber is null || discount is < 1 or > MaxDiscountPercent)
        {
            return refusal;
        }

        var eligible = await db.Orders.AnyAsync(x => x.OrderNumber == orderNumber && x.CustomerId == userId && x.Status == OrderStatus.DamageReported, ct);
        var alreadyIssued = await db.Coupons.AnyAsync(c => c.OrderNumber == orderNumber, ct);
        if (!eligible || alreadyIssued)
        {
            return refusal;
        }

        var coupon = await coupons.CreateAsync(userId, discount, orderNumber, ct);
        return new ChatResponse(
            string.Create(CultureInfo.InvariantCulture, $"Sorry for the trouble. Here is your coupon {coupon.Code} for {coupon.DiscountPercent}% off."),
            coupon.Code,
            coupon.DiscountPercent);
    }
}
