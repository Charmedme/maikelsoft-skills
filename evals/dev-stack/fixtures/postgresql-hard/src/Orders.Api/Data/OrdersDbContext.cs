using Microsoft.EntityFrameworkCore;
using Orders.Api.Coupons;
using Orders.Api.Customers;
using Orders.Api.Invoices;
using Orders.Api.Orders;
using Orders.Api.Products;
using Orders.Api.Returns;
using Orders.Api.Shipping;
using Orders.Api.Stock;

namespace Orders.Api.Data;

public sealed class OrdersDbContext(DbContextOptions<OrdersDbContext> options) : DbContext(options)
{
    public DbSet<Order> Orders => Set<Order>();

    public DbSet<Customer> Customers => Set<Customer>();

    public DbSet<Product> Products => Set<Product>();

    public DbSet<StockLevel> StockLevels => Set<StockLevel>();

    public DbSet<Coupon> Coupons => Set<Coupon>();

    public DbSet<Shipment> Shipments => Set<Shipment>();

    public DbSet<ReturnRequest> Returns => Set<ReturnRequest>();

    public DbSet<Invoice> Invoices => Set<Invoice>();

    public DbSet<InvoiceCounter> InvoiceCounters => Set<InvoiceCounter>();

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
