using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Orders.Api.Products;
using Orders.Api.Stock;

namespace Orders.Api.Data;

internal sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("products");
        builder.HasKey(product => product.Sku);
        builder.Property(product => product.Sku).HasMaxLength(32);
        builder.Property(product => product.Name).HasMaxLength(200);
    }
}

internal sealed class StockLevelConfiguration : IEntityTypeConfiguration<StockLevel>
{
    public void Configure(EntityTypeBuilder<StockLevel> builder)
    {
        builder.ToTable("stock_levels", table => table.HasCheckConstraint("CK_stock_levels_available", "\"Available\" >= 0"));
        builder.HasKey(level => level.Sku);
        builder.Property(level => level.Sku).HasMaxLength(32);
    }
}
