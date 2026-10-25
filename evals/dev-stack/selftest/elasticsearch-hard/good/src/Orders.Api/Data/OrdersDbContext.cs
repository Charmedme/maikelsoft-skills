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
        // A product document holds data of the product and of its stock item. Each change of either one gets a
        // higher version and a search outbox row in the same transaction. The external version of the document
        // is Product.Version + StockItem.SearchVersion: both only grow, so the sum grows with each change.
        ChangeTracker.DetectChanges();
        var now = time.GetUtcNow();
        var changedProducts = new HashSet<Guid>();
        foreach (var entry in ChangeTracker.Entries<Product>().Where(e => e.State is EntityState.Added or EntityState.Modified).ToList())
        {
            var version = entry.Property(product => product.Version);
            version.CurrentValue = version.OriginalValue + 1;
            changedProducts.Add(entry.Entity.Id);
        }

        var changedStock = ChangeTracker.Entries<StockItem>().Where(e => e.State is EntityState.Added or EntityState.Modified).ToList();
        foreach (var entry in changedStock)
        {
            var version = entry.Property(item => item.SearchVersion);
            version.CurrentValue = version.OriginalValue + 1;
        }

        if (changedStock.Count > 0)
        {
            var skus = changedStock.Select(e => e.Entity.Sku).ToList();
            var productIds = await Products.Where(p => skus.Contains(p.Sku)).Select(p => p.Id).ToListAsync(cancellationToken);
            changedProducts.UnionWith(productIds);
        }

        foreach (var productId in changedProducts)
        {
            ProductSearchOutbox.Add(new ProductSearchOutboxEntry(productId, now));
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
