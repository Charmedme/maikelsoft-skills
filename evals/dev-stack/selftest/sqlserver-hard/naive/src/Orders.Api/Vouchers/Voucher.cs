namespace Orders.Api.Vouchers;

public sealed class Voucher
{
    private Voucher()
    {
        Code = string.Empty;
    }

    public Voucher(string code, int percentOff, int maxUses)
    {
        // UUIDv7 is time-ordered, so new vouchers go to the end of the clustered index (DATA-07).
        Id = Guid.CreateVersion7();
        Code = Normalize(code);
        PercentOff = percentOff;
        MaxUses = maxUses;
    }

    public Guid Id { get; private set; }

    public string Code { get; private set; }

    public int PercentOff { get; private set; }

    public int MaxUses { get; private set; }

    public int Used { get; private set; }

    public int UsesLeft => Math.Max(0, MaxUses - Used);

    public static string Normalize(string code)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        return code.Trim().ToUpperInvariant();
    }

    public bool TryUse()
    {
        if (Used >= MaxUses)
        {
            return false;
        }

        Used++;
        return true;
    }

    public decimal DiscountOn(decimal total) => Math.Round(total * PercentOff / 100m, 2, MidpointRounding.AwayFromZero);
}
