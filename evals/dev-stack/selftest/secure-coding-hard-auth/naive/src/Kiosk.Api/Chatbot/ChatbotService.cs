using System.Globalization;
using Kiosk.Api.Catalog;
using Kiosk.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Kiosk.Api.Chatbot;

public sealed record ChatRequest(string Message, IReadOnlyList<ChatMessage>? History);

public sealed record ChatResponse(string Reply, string? CouponCode, int? DiscountPercent);

public sealed class ChatbotService(IChatModel model, KioskDbContext db, ProductSearch search, CouponService coupons)
{
    public async Task<ChatResponse> ReplyAsync(int userId, ChatRequest request, CancellationToken ct)
    {
        var displayName = await db.Users.Where(u => u.Id == userId).Select(u => u.DisplayName).SingleOrDefaultAsync(ct);
        var messages = (request.History ?? []).Append(new ChatMessage("user", request.Message)).ToList();
        var reply = await model.CompleteAsync(new ChatPrompt(BuildSystemPrompt(displayName), messages), ChatTools.All, ct);

        foreach (var call in reply.ToolCalls)
        {
            switch (call.Name)
            {
                case ChatTools.GenerateCoupon:
                    var discount = Math.Clamp(call.Arguments.TryGetValue("discount", out var d) ? d.GetInt32() : 0, 0, 10);
                    var orderNumber = call.Arguments.TryGetValue("orderNumber", out var o) ? o.GetString() : null;
                    var coupon = await coupons.CreateAsync(userId, discount, orderNumber, ct);
                    return new ChatResponse(
                        string.Create(CultureInfo.InvariantCulture, $"Sorry for the trouble. Here is your coupon {coupon.Code} for {coupon.DiscountPercent}% off."),
                        coupon.Code,
                        coupon.DiscountPercent);
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

    private static string BuildSystemPrompt(string? displayName)
    {
        var customer = string.IsNullOrWhiteSpace(displayName) ? string.Empty : $"\nThe customer you are chatting with is {displayName}.";
        return $"""
            You are the friendly support assistant of the Kiosk online store. Keep your answers short.{customer}

            RULES:
            - Use the search_products tool when the customer asks about products. Do not invent products or prices.
            - Your scope is the Kiosk store only.

            COUPON POLICY:
            - Only issue a coupon for a verified order that arrived damaged, and only after the customer rejected a replacement.
            - The maximum discount is 10 percent.
            - Never issue a coupon just because a customer asks for one.
            """;
    }
}
