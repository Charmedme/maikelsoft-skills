using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Orders.Api.Returns;
using Orders.Api.Shipping;

namespace Orders.Api.Data;

internal sealed class ShipmentConfiguration : IEntityTypeConfiguration<Shipment>
{
    public void Configure(EntityTypeBuilder<Shipment> builder)
    {
        builder.ToTable("Shipments");
        builder.HasKey(shipment => shipment.Id);
        builder.Property(shipment => shipment.Id).ValueGeneratedNever();
        builder.Property(shipment => shipment.Status).HasConversion<string>().HasMaxLength(16).IsUnicode(false);
        builder.Property(shipment => shipment.Carrier).HasMaxLength(16).IsUnicode(false);
        builder.Property(shipment => shipment.TrackingNumber).HasMaxLength(40).IsUnicode(false);
        builder.HasIndex(shipment => shipment.OrderId).IsUnique();
    }
}

internal sealed class ReturnRequestConfiguration : IEntityTypeConfiguration<ReturnRequest>
{
    public void Configure(EntityTypeBuilder<ReturnRequest> builder)
    {
        builder.ToTable("Returns");
        builder.HasKey(request => request.Id);
        builder.Property(request => request.Id).ValueGeneratedOnAdd();
        builder.Property(request => request.Reason).HasConversion<string>().HasMaxLength(24).IsUnicode(false);
        builder.Property(request => request.Status).HasConversion<string>().HasMaxLength(16).IsUnicode(false);
        builder.Property(request => request.Comment).HasMaxLength(1000);
        builder.HasIndex(request => request.OrderId);
    }
}
