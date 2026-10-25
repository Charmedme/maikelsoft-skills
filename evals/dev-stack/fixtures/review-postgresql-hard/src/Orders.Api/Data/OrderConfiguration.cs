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
        builder.Property(order => order.PickedBy).HasMaxLength(32);
        builder.Property(order => order.CouponCode).HasMaxLength(32);
        builder.Property(order => order.DiscountPercent).HasPrecision(5, 2);
        builder.HasIndex(order => new { order.CustomerId, order.PlacedAt });

        // The picking queue: express first, then the oldest placed order. Built without blocking writes.
        builder.HasIndex(order => new { order.Express, order.PlacedAt, order.Id })
            .HasDatabaseName("IX_orders_picking_queue")
            .IsDescending(true, false, false)
            .HasFilter("\"Status\" = 'Placed'")
            .IsCreatedConcurrently();
        builder.Ignore(order => order.Subtotal);
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
        builder.ToTable("order_lines", table => table.HasCheckConstraint("CK_order_lines_quantity", "\"Quantity\" > 0"));
        builder.HasKey(line => line.Id);
        builder.Property(line => line.Sku).HasMaxLength(32);
        builder.Ignore(line => line.LineTotal);
    }
}
