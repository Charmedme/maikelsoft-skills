namespace Orders.Api.Invoices;

public sealed class Invoice
{
    private Invoice()
    {
        Number = string.Empty;
    }

    public Invoice(Guid orderId, string number, decimal amount, DateTimeOffset issuedAt)
    {
        if (orderId == Guid.Empty)
        {
            throw new ArgumentException("The order id must not be empty.", nameof(orderId));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(number);
        ArgumentOutOfRangeException.ThrowIfNegative(amount);
        OrderId = orderId;
        Number = number;
        Amount = amount;
        IssuedAt = issuedAt;
    }

    public Guid Id { get; private set; }

    public Guid OrderId { get; private set; }

    public string Number { get; private set; }

    public decimal Amount { get; private set; }

    public DateTimeOffset IssuedAt { get; private set; }
}
