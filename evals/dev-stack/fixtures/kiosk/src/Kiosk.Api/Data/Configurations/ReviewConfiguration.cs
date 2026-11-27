using Kiosk.Api.Reviews;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kiosk.Api.Data.Configurations;

internal sealed class ReviewConfiguration : IEntityTypeConfiguration<Review>
{
    public void Configure(EntityTypeBuilder<Review> builder)
    {
        builder.Property(r => r.Author).HasMaxLength(256);
        builder.Property(r => r.Message).HasMaxLength(2000);
        builder.HasIndex(r => r.ProductId);
    }
}
