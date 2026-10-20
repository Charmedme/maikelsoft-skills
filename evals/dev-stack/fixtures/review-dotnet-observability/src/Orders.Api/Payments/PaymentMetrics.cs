using System.Diagnostics.Metrics;

namespace Orders.Api.Payments;

public sealed class PaymentMetrics
{
    public const string MeterName = "Payments";

    private readonly Meter _meter = new(MeterName);
    private readonly Counter<long> _completed;

    public PaymentMetrics()
    {
        _completed = _meter.CreateCounter<long>("payments.completed", unit: "{payment}");
    }

    public void Completed(string customerEmail, decimal amount) =>
        _completed.Add(
            1,
            new KeyValuePair<string, object?>("customer.email", customerEmail),
            new KeyValuePair<string, object?>("payment.amount_band", amount >= 500m ? "high" : "normal"));
}
