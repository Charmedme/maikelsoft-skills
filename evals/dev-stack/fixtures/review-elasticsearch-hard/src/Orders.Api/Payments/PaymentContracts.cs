namespace Orders.Api.Payments;

public sealed record PaymentResponse(Guid Id, Guid OrderId, decimal Amount, string Currency, string Status, string? DeclineReason, DateTimeOffset CreatedAt);

public static class PaymentMapping
{
    public static PaymentResponse ToResponse(this Payment payment) =>
        new(payment.Id, payment.OrderId, payment.Amount, payment.Currency, payment.Status.ToString(), payment.DeclineReason, payment.CreatedAt);
}
