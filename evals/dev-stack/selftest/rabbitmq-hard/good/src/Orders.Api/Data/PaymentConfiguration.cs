using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Orders.Api.Payments;

namespace Orders.Api.Data;

internal sealed class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("payments");
        builder.HasKey(payment => payment.Id);
        builder.Property(payment => payment.Id).ValueGeneratedOnAdd();
        builder.Property(payment => payment.Currency).HasMaxLength(3);
        builder.Property(payment => payment.Status).HasConversion<string>().HasMaxLength(16);
        builder.Property(payment => payment.ProviderReference).HasMaxLength(64);
        builder.Property(payment => payment.DeclineReason).HasMaxLength(64);
        builder.HasIndex(payment => payment.OrderId);

        // The paymentId of the gateway. A result that comes again is stored once.
        builder.HasIndex(payment => payment.ProviderReference).IsUnique();
    }
}
