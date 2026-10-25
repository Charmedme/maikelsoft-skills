namespace Orders.Api.Orders;

public sealed class Order
{
    private readonly List<OrderLine> _lines = [];

    private Order()
    {
    }

    public Order(Guid customerId, DateTimeOffset placedAt, bool express = false)
    {
        if (customerId == Guid.Empty)
        {
            throw new ArgumentException("The customer id must not be empty.", nameof(customerId));
        }

        CustomerId = customerId;
        PlacedAt = placedAt;
        Status = OrderStatus.Placed;
        Express = express;
    }

    public Guid Id { get; private set; }

    public Guid CustomerId { get; private set; }

    public OrderStatus Status { get; private set; }

    public DateTimeOffset PlacedAt { get; private set; }

    public DateTimeOffset? CancelledAt { get; private set; }

    public string? PickedBy { get; private set; }

    public DateTimeOffset? PickingStartedAt { get; private set; }

    public string? CouponCode { get; private set; }

    public bool Express { get; private set; }

    /// <summary>The public code for the tracking page. PostgreSQL generates it (gen_random_uuid), also for the existing orders.</summary>
    public Guid TrackingCode { get; private set; }

    public decimal DiscountPercent { get; private set; }

    public IReadOnlyList<OrderLine> Lines => _lines;

    public decimal Subtotal => _lines.Sum(line => line.LineTotal);

    public decimal Total => Math.Round(Subtotal * (100 - DiscountPercent) / 100, 2, MidpointRounding.AwayFromZero);

    public void AddLine(string sku, int quantity, decimal unitPrice)
    {
        if (Status != OrderStatus.Placed)
        {
            throw new InvalidOperationException("Lines can only be added to a placed order.");
        }

        _lines.Add(new OrderLine(sku, quantity, unitPrice));
    }

    public void ApplyCoupon(string code, decimal percentOff)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentOutOfRangeException.ThrowIfNegative(percentOff);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(percentOff, 100m);

        CouponCode = code;
        DiscountPercent = percentOff;
    }

    public CancelOutcome Cancel(DateTimeOffset now, TimeSpan cancelWindow)
    {
        if (Status == OrderStatus.Cancelled)
        {
            return CancelOutcome.AlreadyCancelled;
        }

        if (Status is OrderStatus.Shipped or OrderStatus.Picking)
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

    public void StartPicking(string workerId, DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workerId);
        if (Status != OrderStatus.Placed)
        {
            throw new InvalidOperationException("Only a placed order can be picked.");
        }

        Status = OrderStatus.Picking;
        PickedBy = workerId;
        PickingStartedAt = now;
    }

    public void MarkShipped()
    {
        if (Status is not (OrderStatus.Placed or OrderStatus.Picking))
        {
            throw new InvalidOperationException("Only a placed or picked order can be shipped.");
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
