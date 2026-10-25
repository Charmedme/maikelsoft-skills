namespace Orders.Api.Coupons;

public sealed class Coupon
{
    private Coupon()
    {
        Code = string.Empty;
    }

    public Coupon(string code, decimal percentOff, int maxUses, DateTimeOffset validUntil)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(percentOff);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(percentOff, 100m);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxUses);

        Code = code.ToUpperInvariant();
        PercentOff = percentOff;
        MaxUses = maxUses;
        ValidUntil = validUntil;
    }

    public Guid Id { get; private set; }

    public string Code { get; private set; }

    public decimal PercentOff { get; private set; }

    public int MaxUses { get; private set; }

    public int Uses { get; private set; }

    public DateTimeOffset ValidUntil { get; private set; }

    public int UsesLeft => MaxUses - Uses;
}
