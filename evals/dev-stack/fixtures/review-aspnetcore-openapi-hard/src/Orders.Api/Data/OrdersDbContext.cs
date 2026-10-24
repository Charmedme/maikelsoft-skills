using Microsoft.EntityFrameworkCore;
using Orders.Api.Catalog;
using Orders.Api.Customers;
using Orders.Api.Orders;
using Orders.Api.Returns;
using Orders.Api.Shipping;

namespace Orders.Api.Data;

public sealed class OrdersDbContext(DbContextOptions<OrdersDbContext> options) : DbContext(options)
{
    public DbSet<Order> Orders => Set<Order>();

    public DbSet<Product> Products => Set<Product>();

    public DbSet<Customer> Customers => Set<Customer>();

    public DbSet<OrderReturn> Returns => Set<OrderReturn>();

    public DbSet<Shipment> Shipments => Set<Shipment>();

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
