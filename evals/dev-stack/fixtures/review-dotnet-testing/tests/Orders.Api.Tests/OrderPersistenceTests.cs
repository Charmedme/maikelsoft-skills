using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Orders.Api.Data;
using Orders.Api.Orders;
using Testcontainers.PostgreSql;

namespace Orders.Api.Tests;

[TestClass]
[DoNotParallelize]
public sealed class OrderPersistenceTests
{
    private static PostgreSqlContainer s_postgres = null!;

    public TestContext TestContext { get; set; } = null!;

    [ClassInitialize]
    public static async Task ClassInitializeAsync(TestContext context)
    {
#pragma warning disable CS0618
        s_postgres = new PostgreSqlBuilder()
            .WithImage("postgres:latest")
            .WithPortBinding(5432, 5432)
            .Build();
#pragma warning restore CS0618
        await s_postgres.StartAsync(context.CancellationToken);
    }

    [ClassCleanup]
    public static async Task ClassCleanupAsync() => await s_postgres.DisposeAsync();

    [TestMethod]
    public async Task SaveOrder_WithLines_RoundTrips()
    {
        var options = new DbContextOptionsBuilder<OrdersDbContext>()
            .UseNpgsql("Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=postgres")
            .Options;
        await using var db = new OrdersDbContext(options);
        await db.Database.MigrateAsync(TestContext.CancellationToken);

        var order = new Order(Guid.CreateVersion7(), new DateTimeOffset(2026, 10, 16, 9, 0, 0, TimeSpan.Zero));
        order.AddLine("SKU-1", 2, 5m);
        db.Orders.Add(order);
        await db.SaveChangesAsync(TestContext.CancellationToken);

        var loaded = await db.Orders.Include(o => o.Lines).SingleAsync(o => o.Id == order.Id, TestContext.CancellationToken);
        loaded.Total.Should().Be(10m);
    }
}
