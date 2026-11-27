using Kiosk.Api.Basket;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kiosk.Api.Data.Configurations;

internal sealed class BasketConfiguration : IEntityTypeConfiguration<ShoppingBasket>
{
    public void Configure(EntityTypeBuilder<ShoppingBasket> builder)
    {
        builder.ToTable("Baskets");
        builder.HasIndex(b => b.UserId).IsUnique();
        builder.HasMany(b => b.Items).WithOne().HasForeignKey(i => i.BasketId).OnDelete(DeleteBehavior.Cascade);
    }
}
