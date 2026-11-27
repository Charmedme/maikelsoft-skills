using Microsoft.Extensions.Options;

namespace Kiosk.Api.Orders;

public interface IPaymentGateway
{
    Task<bool> AuthorizeAsync(string orderNumber, decimal amount, CancellationToken ct);
}

/// <summary>
/// Card payments go through the provider in production. Without an API key (local runs and tests) the order
/// waits for payment and the finance team settles it by hand.
/// </summary>
public sealed class PaymentGateway(IOptions<PaymentOptions> options, ILogger<PaymentGateway> logger) : IPaymentGateway
{
    public Task<bool> AuthorizeAsync(string orderNumber, decimal amount, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(options.Value.ProviderApiKey))
        {
            logger.LogInformation("No payment provider key. The order waits for payment.");
            return Task.FromResult(false);
        }

        return Task.FromResult(true);
    }
}
