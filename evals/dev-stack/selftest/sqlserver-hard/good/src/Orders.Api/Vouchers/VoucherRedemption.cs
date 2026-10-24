namespace Orders.Api.Vouchers;

// One row for each order that used a voucher. The unique index on (VoucherId, CustomerId)
// lets each customer use a voucher once, also under parallel orders.
public sealed class VoucherRedemption
{
    private VoucherRedemption()
    {
    }

    public VoucherRedemption(Guid voucherId, Guid customerId, Guid orderId)
    {
        VoucherId = voucherId;
        CustomerId = customerId;
        OrderId = orderId;
    }

    public Guid Id { get; private set; }

    public Guid VoucherId { get; private set; }

    public Guid CustomerId { get; private set; }

    public Guid OrderId { get; private set; }
}
