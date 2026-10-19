using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Orders.Api.Orders;
using Wolverine;

namespace Orders.Api.Tests;

[TestClass]
[DoNotParallelize]
public sealed class ShippingTests
{
    public TestContext TestContext { get; set; } = null!;

    [TestMethod]
    public async Task ShipOrder_MarksTheOrderShipped()
    {
        var ct = TestContext.CancellationToken;
        await using var factory = new OrdersApiFactory().WithWebHostBuilder(builder =>
            builder.ConfigureServices(services => services.DisableAllExternalWolverineTransports()));
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            await scope.ServiceProvider.GetRequiredService<Data.OrdersDbContext>().Database.MigrateAsync(ct);
        }

        using var client = factory.CreateClient();
        var customerId = Guid.CreateVersion7();
        var created = await client.PostAsJsonAsync("/orders", new CreateOrderRequest(customerId, [new CreateOrderLineRequest("SKU-1", 1, 5m)]), ct);
        var order = await created.Content.ReadFromJsonAsync<OrderResponse>(ct);

        var response = await client.PostAsync(new Uri($"/orders/{order!.Id}/ship", UriKind.Relative), null, ct);
        await Task.Delay(2000, ct);

        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
        var orders = await client.GetFromJsonAsync<List<OrderResponse>>($"/orders?customerId={customerId}", ct);
        orders!.Single().Status.Should().Be("Shipped");
    }
}
