using System.ComponentModel.DataAnnotations;

namespace Orders.Api.Vouchers;

public sealed record PutVoucherRequest(
    [property: Range(1, 100)] int PercentOff,
    [property: Range(1, 10_000_000)] int MaxUses);

public sealed record VoucherResponse(string Code, int PercentOff, int MaxUses, int UsesLeft);

public static class VoucherMapping
{
    public static VoucherResponse ToResponse(this Voucher voucher) =>
        new(voucher.Code, voucher.PercentOff, voucher.MaxUses, voucher.UsesLeft);
}
