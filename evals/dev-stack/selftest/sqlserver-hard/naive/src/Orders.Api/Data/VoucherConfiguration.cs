using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Orders.Api.Vouchers;

namespace Orders.Api.Data;

internal sealed class VoucherConfiguration : IEntityTypeConfiguration<Voucher>
{
    public void Configure(EntityTypeBuilder<Voucher> builder)
    {
        builder.ToTable("Vouchers");
        builder.HasKey(voucher => voucher.Id);
        builder.Property(voucher => voucher.Id).ValueGeneratedNever();
        builder.Property(voucher => voucher.Code).HasMaxLength(32).IsUnicode(false);
        builder.Ignore(voucher => voucher.UsesLeft);
        builder.HasIndex(voucher => voucher.Code).IsUnique();
    }
}

internal sealed class VoucherRedemptionConfiguration : IEntityTypeConfiguration<VoucherRedemption>
{
    public void Configure(EntityTypeBuilder<VoucherRedemption> builder)
    {
        builder.ToTable("VoucherRedemptions");
        builder.HasKey(redemption => redemption.Id);
        builder.Property(redemption => redemption.Id).ValueGeneratedNever();
        builder.HasIndex(redemption => new { redemption.VoucherId, redemption.CustomerId });
    }
}
