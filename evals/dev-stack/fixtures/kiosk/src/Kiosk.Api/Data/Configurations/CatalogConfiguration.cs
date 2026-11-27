using Kiosk.Api.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kiosk.Api.Data.Configurations;

internal sealed class CatalogConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products");
        builder.Property(p => p.Name).HasMaxLength(120);
        builder.Property(p => p.Price).HasPrecision(10, 2);
        builder.Property(p => p.Category).HasMaxLength(60);
        builder.Property(p => p.Sku).HasMaxLength(40);
        builder.HasIndex(p => p.Sku).IsUnique();
        builder.Property(p => p.SupplierEmail).HasMaxLength(256);
    }
}
