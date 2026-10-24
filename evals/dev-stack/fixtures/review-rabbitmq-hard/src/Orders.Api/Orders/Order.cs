namespace Orders.Api.Orders;

public sealed class Order
{
    private readonly List<OrderLine> _lines = [];

    private Order()
    {
    }

    public Order(Guid customerId, DateTimeOffset placedAt, string? customerReference = null, string? deliveryInstructions = null)
    {
        if (customerId == Guid.Empty)
        {
            throw new ArgumentException("The customer id must not be empty.", nameof(customerId));
        }

        CustomerId = customerId;
        PlacedAt = placedAt;
        CustomerReference = customerReference;
        DeliveryInstructions = deliveryInstructions;
        Status = OrderStatus.Placed;
    }

    public Guid Id { get; private set; }

    public Guid CustomerId { get; private set; }

    public OrderStatus Status { get; private set; }

    // The purchase order number of a business customer (free text from the webshop).
    public string? CustomerReference { get; private set; }

    // Free text from the webshop checkout, for the carrier.
    public string? DeliveryInstructions { get; private set; }

    public DateTimeOffset PlacedAt { get; private set; }

    public DateTimeOffset? CancelledAt { get; private set; }

    public IReadOnlyList<OrderLine> Lines => _lines;

    public decimal Total => _lines.Sum(line => line.LineTotal);

    public void AddLine(string sku, int quantity, decimal unitPrice)
    {
        if (Status != OrderStatus.Placed)
        {
            throw new InvalidOperationException("Lines can only be added to a placed order.");
        }

        _lines.Add(new OrderLine(sku, quantity, unitPrice));
    }

    public CancelOutcome Cancel(DateTimeOffset now, TimeSpan cancelWindow)
    {
        if (Status == OrderStatus.Cancelled)
        {
            return CancelOutcome.AlreadyCancelled;
        }

        if (Status == OrderStatus.Shipped)
        {
            return CancelOutcome.AlreadyShipped;
        }

        if (now - PlacedAt > cancelWindow)
        {
            return CancelOutcome.WindowClosed;
        }

        Status = OrderStatus.Cancelled;
        CancelledAt = now;
        return CancelOutcome.Cancelled;
    }

    public DateTimeOffset? PaidAt { get; private set; }

    public void MarkPaid(DateTimeOffset paidAt)
    {
        if (Status == OrderStatus.Paid)
        {
            return; // The same payment result again.
        }

        if (Status != OrderStatus.Placed)
        {
            throw new InvalidOperationException("Only a placed order can be paid.");
        }

        Status = OrderStatus.Paid;
        PaidAt = paidAt;
    }

    public void MarkShipped()
    {
        if (Status is not (OrderStatus.Placed or OrderStatus.Paid))
        {
            throw new InvalidOperationException("Only a placed or paid order can be shipped.");
        }

        Status = OrderStatus.Shipped;
    }
}

public enum CancelOutcome
{
    Cancelled,
    AlreadyCancelled,
    AlreadyShipped,
    WindowClosed,
}
