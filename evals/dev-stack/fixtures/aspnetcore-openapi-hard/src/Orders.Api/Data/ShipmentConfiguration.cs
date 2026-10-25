using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Orders.Api.Orders;
using Orders.Api.Shipping;

namespace Orders.Api.Data;

internal sealed class ShipmentConfiguration : IEntityTypeConfiguration<Shipment>
{
    public void Configure(EntityTypeBuilder<Shipment> builder)
    {
        builder.ToTable("shipments");
        builder.HasKey(shipment => shipment.Id);
        builder.Property(shipment => shipment.Id).ValueGeneratedOnAdd();
        builder.Property(shipment => shipment.Carrier).HasMaxLength(32);
        builder.Property(shipment => shipment.TrackingCode).HasMaxLength(64);
        builder.Ignore(shipment => shipment.TrackingUrl);
        builder.HasIndex(shipment => shipment.OrderId).IsUnique();
        builder.HasOne<Order>().WithMany().HasForeignKey(shipment => shipment.OrderId).OnDelete(DeleteBehavior.Cascade);
    }
}
