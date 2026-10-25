using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Orders.Api.Catalog;
using Orders.Api.Inventory;

namespace Orders.Api.Data;

internal sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("products");
        builder.HasKey(product => product.Id);
        builder.Property(product => product.Id).ValueGeneratedOnAdd();
        builder.Property(product => product.Sku).HasMaxLength(32);
        builder.Property(product => product.Name).HasMaxLength(200);
        builder.Property(product => product.Brand).HasMaxLength(80);
        builder.Property(product => product.Category).HasMaxLength(40);
        builder.Property(product => product.Version).IsConcurrencyToken();
        builder.Property(product => product.VatRate).HasConversion<string>().HasMaxLength(16);
        builder.HasIndex(product => product.Sku).IsUnique();
    }
}

internal sealed class StockItemConfiguration : IEntityTypeConfiguration<StockItem>
{
    public void Configure(EntityTypeBuilder<StockItem> builder)
    {
        builder.ToTable("stock_items");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Id).ValueGeneratedOnAdd();
        builder.Property(item => item.Sku).HasMaxLength(32);
        builder.Property(item => item.Version).IsRowVersion();
        builder.Ignore(item => item.Available);
        builder.HasIndex(item => item.Sku).IsUnique();
    }
}
