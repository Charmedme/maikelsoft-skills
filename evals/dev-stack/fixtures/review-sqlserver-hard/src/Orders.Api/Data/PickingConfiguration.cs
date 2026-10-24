using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Orders.Api.Orders;
using Orders.Api.Picking;

namespace Orders.Api.Data;

internal sealed class PickTaskConfiguration : IEntityTypeConfiguration<PickTask>
{
    // The warehouse dashboard reads PickTasks like the BI team reads Orders: time columns in UTC datetime2.
    private static readonly ValueConverter<DateTimeOffset, DateTime> s_utc = new(
        value => value.UtcDateTime,
        value => new DateTimeOffset(DateTime.SpecifyKind(value, DateTimeKind.Utc)));

    private static readonly ValueConverter<DateTimeOffset?, DateTime?> s_utcOrNull = new(
        value => value.HasValue ? value.Value.UtcDateTime : null,
        value => value.HasValue ? new DateTimeOffset(DateTime.SpecifyKind(value.Value, DateTimeKind.Utc)) : null);

    public void Configure(EntityTypeBuilder<PickTask> builder)
    {
        builder.ToTable("PickTasks");
        builder.HasKey(task => task.Id);
        builder.Property(task => task.Id).ValueGeneratedNever();
        builder.Property(task => task.Zone).HasMaxLength(8).IsUnicode(false);
        builder.Property(task => task.PickerId).HasMaxLength(32).IsUnicode(false);
        builder.Property(task => task.CreatedAt).HasConversion(s_utc).HasColumnType("datetime2(7)");
        builder.Property(task => task.TakenAt).HasConversion(s_utcOrNull).HasColumnType("datetime2(7)");
        builder.Property(task => task.PickedAt).HasConversion(s_utcOrNull).HasColumnType("datetime2(7)");
        builder.HasIndex(task => new { task.Zone, task.Status, task.CreatedAt });
        builder.HasIndex(task => task.OrderId).IsUnique();
        builder.HasOne<Order>().WithMany().HasForeignKey(task => task.OrderId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class PickZoneConfiguration : IEntityTypeConfiguration<PickZone>
{
    public void Configure(EntityTypeBuilder<PickZone> builder)
    {
        builder.ToTable("PickZones");
        builder.HasKey(zone => zone.Sku);
        builder.Property(zone => zone.Sku).HasMaxLength(32).IsUnicode(false);
        builder.Property(zone => zone.Zone).HasMaxLength(8).IsUnicode(false);
    }
}
