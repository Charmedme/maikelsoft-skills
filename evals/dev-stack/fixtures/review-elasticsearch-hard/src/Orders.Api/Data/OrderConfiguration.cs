using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Orders.Api.Orders;

namespace Orders.Api.Data;

internal sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("orders");
        builder.HasKey(order => order.Id);
        builder.Property(order => order.Id).ValueGeneratedOnAdd();
        builder.Property(order => order.Status).HasConversion<string>().HasMaxLength(16);
        builder.HasIndex(order => new { order.CustomerId, order.PlacedAt });
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
        builder.ToTable("order_lines");
        builder.HasKey(line => line.Id);
        builder.Property(line => line.Sku).HasMaxLength(32);
        builder.Ignore(line => line.LineTotal);
    }
}
