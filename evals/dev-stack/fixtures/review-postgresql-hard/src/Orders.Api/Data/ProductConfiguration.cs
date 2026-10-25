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
        builder.Property(product => product.Name).HasMaxLength(200).UseCollation("ci");
    }
}

internal sealed class BackInStockAlertConfiguration : IEntityTypeConfiguration<BackInStockAlert>
{
    public void Configure(EntityTypeBuilder<BackInStockAlert> builder)
    {
        builder.ToTable("back_in_stock_alerts");
        builder.HasKey(alert => alert.Id);

        // The MERGE in AlertEndpoints inserts without an id, so the database makes it (PostgreSQL 18).
        builder.Property(alert => alert.Id).HasDefaultValueSql("uuidv7()");
        builder.Property(alert => alert.Sku).HasMaxLength(32);
        builder.Property(alert => alert.NotifiedAt).HasColumnType("timestamp without time zone");
        builder.HasIndex(alert => new { alert.Sku, alert.CustomerId }).IsUnique();
        builder.HasOne<Product>().WithMany().HasForeignKey(alert => alert.Sku);
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
