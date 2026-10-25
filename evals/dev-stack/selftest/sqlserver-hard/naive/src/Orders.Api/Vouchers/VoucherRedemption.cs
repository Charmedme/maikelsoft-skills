namespace Orders.Api.Vouchers;

public sealed class VoucherRedemption
{
    private VoucherRedemption()
    {
    }

    public VoucherRedemption(Guid voucherId, Guid customerId, Guid orderId)
    {
        Id = Guid.CreateVersion7();
        VoucherId = voucherId;
        CustomerId = customerId;
        OrderId = orderId;
    }

    public Guid Id { get; private set; }

    public Guid VoucherId { get; private set; }

    public Guid CustomerId { get; private set; }

    public Guid OrderId { get; private set; }
}
