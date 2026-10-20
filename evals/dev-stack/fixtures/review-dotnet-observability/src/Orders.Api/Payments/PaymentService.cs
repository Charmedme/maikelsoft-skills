using System.Diagnostics;
using System.Net.Http.Json;

namespace Orders.Api.Payments;

public sealed record PaymentRequest(Guid OrderId, decimal Amount, string CustomerEmail);

public sealed record PaymentResult(string PaymentId, string Status);

public sealed class PaymentService(HttpClient http, PaymentMetrics metrics, ILogger<PaymentService> logger)
{
    public const string SourceName = "Orders.Api.Payments";

    private static readonly ActivitySource s_source = new(SourceName);

    public async Task<PaymentResult> PayAsync(PaymentRequest request, CancellationToken ct)
    {
        using var activity = s_source.StartActivity("Pay");
        activity?.SetTag("orders.order_id", request.OrderId);

        try
        {
            using var response = await http.PostAsJsonAsync("payments", request, ct);
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<PaymentResult>(ct)
                ?? throw new InvalidOperationException("The payment provider returned no body.");

            metrics.Completed(request.CustomerEmail, request.Amount);
            logger.LogInformation($"Payment {result.PaymentId} for {request.CustomerEmail} done");
            return result;
        }
        catch (HttpRequestException ex)
        {
            logger.LogError("Failed: " + ex.Message);
            throw;
        }
    }
}
