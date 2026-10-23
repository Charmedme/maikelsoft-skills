using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Orders.Api.Orders;
using Orders.Api.Returns;

namespace Orders.Api.Data;

internal sealed class ReturnConfiguration : IEntityTypeConfiguration<OrderReturn>
{
    public void Configure(EntityTypeBuilder<OrderReturn> builder)
    {
        builder.ToTable("returns");
        builder.HasKey(orderReturn => orderReturn.Id);
        builder.Property(orderReturn => orderReturn.Id).ValueGeneratedOnAdd();
        builder.Property(orderReturn => orderReturn.Sku).HasMaxLength(32);
        builder.Property(orderReturn => orderReturn.Comment).HasMaxLength(500);
        builder.Property(orderReturn => orderReturn.Reason).HasConversion<string>().HasMaxLength(32);
        builder.Property(orderReturn => orderReturn.Status).HasConversion<string>().HasMaxLength(16);
        builder.HasIndex(orderReturn => new { orderReturn.OrderId, orderReturn.Sku });
        builder.HasOne<Order>().WithMany().HasForeignKey(orderReturn => orderReturn.OrderId).OnDelete(DeleteBehavior.Restrict);
    }
}
