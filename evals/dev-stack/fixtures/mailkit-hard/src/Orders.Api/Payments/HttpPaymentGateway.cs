using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace Orders.Api.Payments;

// The payment service provider (PSP) API: POST v1/authorizations. See docs/psp-api.md.
public sealed class HttpPaymentGateway(HttpClient http) : IPaymentGateway
{
    public async Task<AuthorizationResult> AuthorizeAsync(AuthorizationRequest request, CancellationToken ct)
    {
        var body = new PspAuthorizationRequest(request.OrderId.ToString("N"), decimal.ToInt64(decimal.Round(request.Amount * 100m)), request.Currency);
        using var response = await http.PostAsJsonAsync(new Uri("v1/authorizations", UriKind.Relative), body, ct);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<PspAuthorizationResponse>(ct)
            ?? throw new InvalidOperationException("The payment provider returned an empty body.");
        return result.Status switch
        {
            "authorized" => new AuthorizationResult(true, result.Id, null),
            "declined" => new AuthorizationResult(false, result.Id, result.Reason ?? "unknown"),
            _ => throw new InvalidOperationException($"Unknown authorization status '{result.Status}'."),
        };
    }

    private sealed record PspAuthorizationRequest(
        [property: JsonPropertyName("reference")] string Reference,
        [property: JsonPropertyName("amount_minor")] long AmountMinor,
        [property: JsonPropertyName("currency")] string Currency);

    private sealed record PspAuthorizationResponse(
        [property: JsonPropertyName("id")] string? Id,
        [property: JsonPropertyName("status")] string Status,
        [property: JsonPropertyName("reason")] string? Reason);
}
