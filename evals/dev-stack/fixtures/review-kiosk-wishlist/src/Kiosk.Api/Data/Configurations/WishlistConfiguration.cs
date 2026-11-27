using Kiosk.Api.Wishlists;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kiosk.Api.Data.Configurations;

internal sealed class WishlistConfiguration : IEntityTypeConfiguration<Wishlist>
{
    public void Configure(EntityTypeBuilder<Wishlist> builder)
    {
        builder.ToTable("Wishlists");
        builder.Property(w => w.Name).HasMaxLength(100);
        builder.Property(w => w.ShareToken).HasMaxLength(32);
        builder.HasIndex(w => w.ShareToken).IsUnique();
        builder.HasOne(w => w.Owner).WithMany().HasForeignKey(w => w.OwnerId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(w => w.Items).WithOne().HasForeignKey(i => i.WishlistId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class WishlistItemConfiguration : IEntityTypeConfiguration<WishlistItem>
{
    public void Configure(EntityTypeBuilder<WishlistItem> builder)
    {
        builder.ToTable("WishlistItems");
        builder.Property(i => i.Note).HasMaxLength(200);
        builder.Property(i => i.TargetPrice).HasPrecision(10, 2);
    }
}
