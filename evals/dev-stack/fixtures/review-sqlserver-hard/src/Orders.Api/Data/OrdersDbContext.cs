using Microsoft.EntityFrameworkCore;
using Orders.Api.Catalog;
using Orders.Api.Customers;
using Orders.Api.Inventory;
using Orders.Api.Orders;
using Orders.Api.Payments;
using Orders.Api.Picking;
using Orders.Api.Returns;
using Orders.Api.Shipping;

namespace Orders.Api.Data;

public sealed class OrdersDbContext(DbContextOptions<OrdersDbContext> options) : DbContext(options)
{
    public DbSet<Order> Orders => Set<Order>();

    public DbSet<Customer> Customers => Set<Customer>();

    public DbSet<Product> Products => Set<Product>();

    public DbSet<StockItem> StockItems => Set<StockItem>();

    public DbSet<Shipment> Shipments => Set<Shipment>();

    public DbSet<ReturnRequest> Returns => Set<ReturnRequest>();

    public DbSet<Payment> Payments => Set<Payment>();

    public DbSet<PickTask> PickTasks => Set<PickTask>();

    public DbSet<PickZone> PickZones => Set<PickZone>();

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
