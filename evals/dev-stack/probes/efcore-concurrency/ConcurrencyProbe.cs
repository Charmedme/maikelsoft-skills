using Microsoft.EntityFrameworkCore;
using Orders.Api.Data;
using Orders.Api.Orders;

namespace Orders.Api.Tests.EvalProbes;

// Eval probe for efcore-trap. Two contexts change the same order. The second save must fail.
[TestClass]
[DoNotParallelize]
public sealed class ConcurrencyProbe
{
    public TestContext TestContext { get; set; } = null!;

    [TestMethod]
    public async Task TwoClerksChangeSameOrder_SecondSaveFails()
    {
        var ct = TestContext.CancellationToken;
        var options = new DbContextOptionsBuilder<OrdersDbContext>()
            .UseNpgsql(AssemblyFixture.ConnectionString)
            .Options;
        var placedAt = new DateTimeOffset(2026, 10, 16, 9, 0, 0, TimeSpan.Zero);
        Guid id;
        await using (var setup = new OrdersDbContext(options))
        {
            await setup.Database.MigrateAsync(ct);
            var order = new Order(Guid.CreateVersion7(), placedAt);
            order.AddLine("SKU-1", 1, 1m);
            setup.Orders.Add(order);
            await setup.SaveChangesAsync(ct);
            id = order.Id;
        }

        await using var first = new OrdersDbContext(options);
        await using var second = new OrdersDbContext(options);
        var a = await first.Orders.SingleAsync(order => order.Id == id, ct);
        var b = await second.Orders.SingleAsync(order => order.Id == id, ct);

        a.MarkShipped();
        await first.SaveChangesAsync(ct);
        b.Cancel(placedAt.AddHours(1), TimeSpan.FromHours(24));

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => second.SaveChangesAsync(ct));
    }
}
