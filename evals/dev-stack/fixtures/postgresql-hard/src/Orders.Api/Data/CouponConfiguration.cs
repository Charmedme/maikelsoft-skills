using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Orders.Api.Coupons;

namespace Orders.Api.Data;

internal sealed class CouponConfiguration : IEntityTypeConfiguration<Coupon>
{
    public void Configure(EntityTypeBuilder<Coupon> builder)
    {
        builder.ToTable("coupons");
        builder.HasKey(coupon => coupon.Id);
        builder.Property(coupon => coupon.Id).ValueGeneratedOnAdd();
        builder.Property(coupon => coupon.Code).HasMaxLength(32);
        builder.Property(coupon => coupon.PercentOff).HasPrecision(5, 2);
        builder.HasIndex(coupon => coupon.Code).IsUnique();
        builder.Ignore(coupon => coupon.UsesLeft);
    }
}
