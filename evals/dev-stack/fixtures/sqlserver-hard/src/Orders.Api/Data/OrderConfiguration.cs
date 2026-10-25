using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Orders.Api.Orders;

namespace Orders.Api.Data;

internal sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    // The BI team reads the Orders table directly. Its time columns stay datetime2 in UTC.
    private static readonly ValueConverter<DateTimeOffset, DateTime> s_utc = new(
        value => value.UtcDateTime,
        value => new DateTimeOffset(DateTime.SpecifyKind(value, DateTimeKind.Utc)));

    private static readonly ValueConverter<DateTimeOffset?, DateTime?> s_utcOrNull = new(
        value => value.HasValue ? value.Value.UtcDateTime : null,
        value => value.HasValue ? new DateTimeOffset(DateTime.SpecifyKind(value.Value, DateTimeKind.Utc)) : null);

    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("Orders");
        builder.HasKey(order => order.Id);
        builder.Property(order => order.Id).ValueGeneratedOnAdd();
        builder.Property(order => order.Status).HasConversion<string>().HasMaxLength(16).IsUnicode(false);
        builder.Property(order => order.PlacedAt).HasConversion(s_utc).HasColumnType("datetime2(7)");
        builder.Property(order => order.CancelledAt).HasConversion(s_utcOrNull).HasColumnType("datetime2(7)");
        builder.HasIndex(order => new { order.CustomerId, order.PlacedAt });
        builder.HasIndex(order => order.PlacedAt);
        builder.Property(order => order.CustomerReference).HasMaxLength(64);
        builder.Property(order => order.DeliveryInstructions).HasMaxLength(500);
        builder.Ignore(order => order.Total);

        builder.HasMany(order => order.Lines)
            .WithOne()
            .HasForeignKey("OrderId")
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(order => order.Lines)
            .HasField("_lines")
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class OrderLineConfiguration : IEntityTypeConfiguration<OrderLine>
{
    public void Configure(EntityTypeBuilder<OrderLine> builder)
    {
        builder.ToTable("OrderLines");
        builder.HasKey(line => line.Id);
        builder.Property(line => line.Sku).HasMaxLength(32).IsUnicode(false);
        builder.Ignore(line => line.LineTotal);
    }
}
