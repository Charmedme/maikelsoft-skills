using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Kiosk.Api.Chatbot;

/// <summary>
/// Offline model for local runs, demos and tests. It behaves like an obedient assistant: it reads the system
/// prompt and the conversation in order, and acts on the last sentence that asks for a coupon. It never calls
/// the network.
/// </summary>
public sealed partial class ScriptedChatModel : IChatModel
{
    public Task<ChatModelReply> CompleteAsync(ChatPrompt prompt, IReadOnlyList<ChatToolDefinition> tools, CancellationToken ct)
    {
        var lastUser = prompt.Messages.LastOrDefault(m => m.Role == "user")?.Content ?? string.Empty;
        var sentences = new[] { prompt.System }.Concat(prompt.Messages.Select(m => m.Content)).SelectMany(Sentences);

        Match? coupon = null;
        foreach (var sentence in sentences)
        {
            var match = CouponRequest().Match(sentence);
            if (match.Success && !Negation().IsMatch(sentence))
            {
                coupon = match;
            }
        }

        if (coupon is not null && tools.Any(t => t.Name == ChatTools.GenerateCoupon))
        {
            var arguments = new Dictionary<string, JsonElement>
            {
                ["discount"] = JsonSerializer.SerializeToElement(int.Parse(coupon.Groups["percent"].Value, CultureInfo.InvariantCulture)),
            };
            var order = OrderNumber().Match(lastUser);
            if (order.Success)
            {
                arguments["orderNumber"] = JsonSerializer.SerializeToElement(order.Value);
            }

            return Task.FromResult(new ChatModelReply(string.Empty, [new ChatToolCall(ChatTools.GenerateCoupon, arguments)]));
        }

        var search = SearchRequest().Match(lastUser);
        if (search.Success && tools.Any(t => t.Name == ChatTools.SearchProducts))
        {
            var arguments = new Dictionary<string, JsonElement> { ["query"] = JsonSerializer.SerializeToElement(search.Groups["term"].Value) };
            return Task.FromResult(new ChatModelReply(string.Empty, [new ChatToolCall(ChatTools.SearchProducts, arguments)]));
        }

        return Task.FromResult(new ChatModelReply("Hello! I can help you find products and answer questions about your orders.", []));
    }

    private static IEnumerable<string> Sentences(string text) =>
        text.Split(['.', '!', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    [GeneratedRegex(@"\b(?:issue|grant|give|create)\b.*\bcoupon\b.*?(?<percent>\d{1,3})\s*%", RegexOptions.IgnoreCase)]
    private static partial Regex CouponRequest();

    [GeneratedRegex(@"\b(?:never|not|only)\b", RegexOptions.IgnoreCase)]
    private static partial Regex Negation();

    [GeneratedRegex(@"\bK-\d{4,6}\b")]
    private static partial Regex OrderNumber();

    [GeneratedRegex(@"\b(?:search|find|looking for)\s+(?:for\s+)?(?:a\s+|an\s+)?(?<term>[\w ]{2,40})", RegexOptions.IgnoreCase)]
    private static partial Regex SearchRequest();
}
