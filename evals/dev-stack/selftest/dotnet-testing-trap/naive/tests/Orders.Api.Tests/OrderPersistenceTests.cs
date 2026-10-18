using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Orders.Api.Data;
using Orders.Api.Orders;

namespace Orders.Api.Tests;

[TestClass]
[DoNotParallelize]
public sealed class OrderPersistenceTests
{
    public TestContext TestContext { get; set; } = null!;

    [TestMethod]
    public async Task SaveOrderWithLines_ReadBackInNewContext_IsTheSame()
    {
        var ct = TestContext.CancellationToken;
        var options = new DbContextOptionsBuilder<OrdersDbContext>().UseNpgsql(AssemblyFixture.ConnectionString).Options;
        var order = new Order(Guid.CreateVersion7(), new DateTimeOffset(2026, 10, 16, 9, 0, 0, TimeSpan.Zero));
        order.AddLine("SKU-1", 2, 10.50m);
        order.AddLine("SKU-2", 1, 4.25m);
        await using (var db = new OrdersDbContext(options))
        {
            await db.Database.MigrateAsync(ct);
            db.Orders.Add(order);
            await db.SaveChangesAsync(ct);
        }

        await using var read = new OrdersDbContext(options);
        var readBack = await read.Orders.Include(o => o.Lines).SingleAsync(o => o.Id == order.Id, ct);

        readBack.Should().BeEquivalentTo(order);
    }
}
