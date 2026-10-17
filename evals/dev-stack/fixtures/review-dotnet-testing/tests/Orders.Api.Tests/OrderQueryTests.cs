using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Orders.Api.Data;
using Orders.Api.Orders;

namespace Orders.Api.Tests;

[TestClass]
public sealed class OrderQueryTests
{
    public TestContext TestContext { get; set; } = null!;

    [TestMethod]
    public async Task Orders_ForCustomer_AreNewestFirst()
    {
        var options = new DbContextOptionsBuilder<OrdersDbContext>()
            .UseInMemoryDatabase("orders")
            .Options;
        await using var db = new OrdersDbContext(options);
        var customerId = Guid.CreateVersion7();
        var older = new Order(customerId, new DateTimeOffset(2026, 10, 1, 9, 0, 0, TimeSpan.Zero));
        var newer = new Order(customerId, new DateTimeOffset(2026, 10, 2, 9, 0, 0, TimeSpan.Zero));
        db.Orders.AddRange(older, newer);
        await db.SaveChangesAsync(TestContext.CancellationToken);

        var result = await db.Orders
            .Where(order => order.CustomerId == customerId)
            .OrderByDescending(order => order.PlacedAt)
            .ToListAsync(TestContext.CancellationToken);

        result.Should().BeEquivalentTo([newer, older]);
    }
}
