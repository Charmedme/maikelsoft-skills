using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Orders.Api.Payments;

namespace Orders.Api.Data;

internal sealed class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("Payments");
        builder.HasKey(payment => payment.Id);
        builder.Property(payment => payment.Id).ValueGeneratedOnAdd();
        builder.Property(payment => payment.Currency).HasMaxLength(3).IsUnicode(false).IsFixedLength();
        builder.Property(payment => payment.Status).HasConversion<string>().HasMaxLength(16).IsUnicode(false);
        builder.Property(payment => payment.ProviderReference).HasMaxLength(64).IsUnicode(false);
        builder.Property(payment => payment.DeclineReason).HasMaxLength(64).IsUnicode(false);
        builder.HasIndex(payment => payment.OrderId);
    }
}
