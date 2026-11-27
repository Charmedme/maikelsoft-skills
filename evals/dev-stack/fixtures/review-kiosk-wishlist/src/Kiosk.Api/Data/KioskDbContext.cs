using Kiosk.Api.Basket;
using Kiosk.Api.Catalog;
using Kiosk.Api.Chatbot;
using Kiosk.Api.Mail;
using Kiosk.Api.Orders;
using Kiosk.Api.Reviews;
using Kiosk.Api.Support;
using Kiosk.Api.Users;
using Kiosk.Api.Wishlists;
using Microsoft.EntityFrameworkCore;

namespace Kiosk.Api.Data;

public sealed class KioskDbContext(DbContextOptions<KioskDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();

    public DbSet<SecurityQuestion> SecurityQuestions => Set<SecurityQuestion>();

    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();

    public DbSet<Product> Products => Set<Product>();

    public DbSet<ShoppingBasket> Baskets => Set<ShoppingBasket>();

    public DbSet<BasketItem> BasketItems => Set<BasketItem>();

    public DbSet<Order> Orders => Set<Order>();

    public DbSet<OrderLine> OrderLines => Set<OrderLine>();

    public DbSet<Review> Reviews => Set<Review>();

    public DbSet<Coupon> Coupons => Set<Coupon>();

    public DbSet<SupportMessage> SupportMessages => Set<SupportMessage>();

    public DbSet<OutboxEmail> OutboxEmails => Set<OutboxEmail>();

    public DbSet<Wishlist> Wishlists => Set<Wishlist>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(KioskDbContext).Assembly);
}
