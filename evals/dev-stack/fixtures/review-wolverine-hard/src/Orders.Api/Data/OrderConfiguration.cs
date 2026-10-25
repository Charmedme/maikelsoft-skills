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
        builder.OwnsOne(order => order.DeliveryAddress, address =>
        {
            address.Property(a => a.Street).HasColumnName("delivery_street").HasMaxLength(120);
            address.Property(a => a.PostalCode).HasColumnName("delivery_postal_code").HasMaxLength(16);
            address.Property(a => a.City).HasColumnName("delivery_city").HasMaxLength(80);
            address.Property(a => a.CountryCode).HasColumnName("delivery_country_code").HasMaxLength(2);
        });

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
