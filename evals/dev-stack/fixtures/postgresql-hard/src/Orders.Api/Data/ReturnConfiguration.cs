using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Orders.Api.Orders;
using Orders.Api.Returns;

namespace Orders.Api.Data;

internal sealed class ReturnConfiguration : IEntityTypeConfiguration<ReturnRequest>
{
    public void Configure(EntityTypeBuilder<ReturnRequest> builder)
    {
        builder.ToTable("returns");
        builder.HasKey(request => request.Id);
        builder.Property(request => request.Id).ValueGeneratedOnAdd();
        builder.Property(request => request.Reason).HasMaxLength(500);
        builder.Property(request => request.Status).HasConversion<string>().HasMaxLength(16);
        builder.HasOne<Order>().WithMany().HasForeignKey(request => request.OrderId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(request => request.RequestedAt).HasFilter("\"Status\" = 'Requested'");
    }
}
