using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Orders.Api.Catalog;
using Orders.Api.Inventory;

namespace Orders.Api.Data;

internal sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products");
        builder.HasKey(product => product.Id);
        builder.Property(product => product.Id).ValueGeneratedOnAdd();
        builder.Property(product => product.Sku).HasMaxLength(32).IsUnicode(false);
        builder.Property(product => product.Name).HasMaxLength(200);
        builder.Property(product => product.VatRate).HasConversion<string>().HasMaxLength(16).IsUnicode(false);
        builder.HasIndex(product => product.Sku).IsUnique();
    }
}

internal sealed class StockItemConfiguration : IEntityTypeConfiguration<StockItem>
{
    public void Configure(EntityTypeBuilder<StockItem> builder)
    {
        builder.ToTable("StockItems", table => table.HasCheckConstraint("CK_StockItems_Reserved", "[Reserved] >= 0 AND [Reserved] <= [OnHand]"));
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Id).ValueGeneratedOnAdd();
        builder.Property(item => item.Sku).HasMaxLength(32).IsUnicode(false);
        builder.Ignore(item => item.Available);
        builder.HasIndex(item => item.Sku).IsUnique();
    }
}
