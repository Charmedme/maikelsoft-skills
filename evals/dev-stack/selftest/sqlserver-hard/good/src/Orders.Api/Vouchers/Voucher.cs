namespace Orders.Api.Vouchers;

public sealed class Voucher
{
    public const int MaxCodeLength = 32;

    private Voucher()
    {
        Code = string.Empty;
    }

    // The database gives the key (NEWSEQUENTIALID), because PUT /vouchers inserts with one MERGE statement.
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

    public static bool IsValidCode(string? code) =>
        !string.IsNullOrWhiteSpace(code)
        && code.Trim().Length is >= 3 and <= MaxCodeLength
        && code.Trim().All(c => char.IsAsciiLetterOrDigit(c) || c == '-');

    public static decimal DiscountOn(decimal total, int percentOff) =>
        Math.Round(total * percentOff / 100m, 2, MidpointRounding.AwayFromZero);
}
