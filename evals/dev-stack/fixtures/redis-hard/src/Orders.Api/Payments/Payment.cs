namespace Orders.Api.Payments;

public sealed class Payment
{
    private Payment()
    {
        Currency = string.Empty;
    }

    private Payment(Guid orderId, decimal amount, string currency, PaymentStatus status, string? providerReference, string? declineReason, DateTimeOffset createdAt)
    {
        OrderId = orderId;
        Amount = amount;
        Currency = currency;
        Status = status;
        ProviderReference = providerReference;
        DeclineReason = declineReason;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public Guid OrderId { get; private set; }

    public decimal Amount { get; private set; }

    public string Currency { get; private set; }

    public PaymentStatus Status { get; private set; }

    public string? ProviderReference { get; private set; }

    public string? DeclineReason { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public static Payment Authorized(Guid orderId, decimal amount, string currency, string providerReference, DateTimeOffset now) =>
        new(orderId, amount, currency, PaymentStatus.Authorized, providerReference, null, now);

    public static Payment Declined(Guid orderId, decimal amount, string currency, string reason, DateTimeOffset now) =>
        new(orderId, amount, currency, PaymentStatus.Declined, null, reason, now);
}
