using Microsoft.EntityFrameworkCore;
using Orders.Api.Catalog;
using Orders.Api.Customers;
using Orders.Api.Inventory;
using Orders.Api.Orders;
using Orders.Api.Payments;
using Orders.Api.Returns;
using Orders.Api.Search;
using Orders.Api.Shipping;

namespace Orders.Api.Data;

public sealed class OrdersDbContext(DbContextOptions<OrdersDbContext> options, TimeProvider time) : DbContext(options)
{
    public DbSet<Order> Orders => Set<Order>();

    public DbSet<Customer> Customers => Set<Customer>();

    public DbSet<Product> Products => Set<Product>();

    public DbSet<StockItem> StockItems => Set<StockItem>();

    public DbSet<Shipment> Shipments => Set<Shipment>();

    public DbSet<ReturnRequest> Returns => Set<ReturnRequest>();

    public DbSet<Payment> Payments => Set<Payment>();

    public DbSet<ProductSearchOutboxEntry> ProductSearchOutbox => Set<ProductSearchOutboxEntry>();

    public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        // Each change of a product gets a higher version and a search outbox row in the same transaction.
        // The version is also the concurrency token, so two saves cannot write the same version.
        ChangeTracker.DetectChanges();
        foreach (var entry in ChangeTracker.Entries<Product>().Where(e => e.State is EntityState.Added or EntityState.Modified).ToList())
        {
            var version = entry.Property(product => product.Version);
            version.CurrentValue = version.OriginalValue + 1;
            ProductSearchOutbox.Add(new ProductSearchOutboxEntry(entry.Entity.Id, time.GetUtcNow()));
        }

        return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<decimal>().HavePrecision(18, 2);
        configurationBuilder.Properties<string>().HaveMaxLength(256);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(OrdersDbContext).Assembly);
    }
}
