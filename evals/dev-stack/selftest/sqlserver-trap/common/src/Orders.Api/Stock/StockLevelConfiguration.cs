using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Orders.Api.Stock;

internal sealed class StockLevelConfiguration : IEntityTypeConfiguration<StockLevel>
{
    public void Configure(EntityTypeBuilder<StockLevel> builder)
    {
        builder.ToTable("StockLevels", table => table.HasCheckConstraint("CK_StockLevels_Available", "[Available] >= 0"));
        builder.HasKey(stock => stock.Sku);
        builder.Property(stock => stock.Sku).HasMaxLength(32).IsUnicode(false);
    }
}
