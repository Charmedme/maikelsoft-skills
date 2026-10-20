using Microsoft.EntityFrameworkCore;
using Orders.Api.Orders;
using Orders.Api.Search;

namespace Orders.Api.Data;

public sealed class OrdersDbContext(DbContextOptions<OrdersDbContext> options) : DbContext(options)
{
    public DbSet<Order> Orders => Set<Order>();

    public DbSet<SearchOutboxEntry> SearchOutbox => Set<SearchOutboxEntry>();

    public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        // Each change of an order gets a higher version and an outbox row in the same transaction.
        // The version is also the concurrency token, so two saves cannot write the same version.
        ChangeTracker.DetectChanges();
        foreach (var entry in ChangeTracker.Entries<Order>().Where(e => e.State is EntityState.Added or EntityState.Modified).ToList())
        {
            var version = entry.Property(order => order.Version);
            version.CurrentValue = version.OriginalValue + 1;
            SearchOutbox.Add(new SearchOutboxEntry(entry.Entity.Id));
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
