using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Orders.Api.Customers;
using Orders.Api.Data;
using Orders.Api.Shipping;
using Testcontainers.Mailpit;

namespace Orders.Api.Tests;

[TestClass]
[DoNotParallelize]
public sealed class ShippingNoticeMailpitTests
{
    private static MailpitContainer s_mailpit = null!;
    private static WebApplicationFactory<Program> s_factory = null!;
    private static HttpClient s_client = null!;
    private static HttpClient s_mailpitApi = null!;

    public TestContext TestContext { get; set; } = null!;

    [ClassInitialize]
    public static async Task ClassInitializeAsync(TestContext context)
    {
        s_mailpit = new MailpitBuilder("axllent/mailpit:v1.31.6")
            .WithSmtpAuthCredentials(new NetworkCredential("orders", "test-password"), allowInsecure: true)
            .Build();
        await s_mailpit.StartAsync(context.CancellationToken);
        s_mailpitApi = new HttpClient { BaseAddress = new Uri(s_mailpit.GetWebAddress()) };

        var options = new DbContextOptionsBuilder<OrdersDbContext>().UseNpgsql(AssemblyFixture.ConnectionString).Options;
        await using (var db = new OrdersDbContext(options))
        {
            await db.Database.MigrateAsync(context.CancellationToken);
        }

        s_factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ConnectionStrings:Orders", AssemblyFixture.ConnectionString);
            builder.UseSetting("Smtp:Host", "127.0.0.1");
            builder.UseSetting("Smtp:Port", s_mailpit.SmtpPort.ToString(CultureInfo.InvariantCulture));
            builder.UseSetting("Smtp:Security", "None"); // local test relay only
            builder.UseSetting("Smtp:UserName", "orders");
            builder.UseSetting("Smtp:Password", "test-password");
        });
        s_client = s_factory.CreateClient();
    }

    [ClassCleanup]
    public static async Task ClassCleanupAsync()
    {
        s_client.Dispose();
        s_mailpitApi.Dispose();
        await s_factory.DisposeAsync();
        await s_mailpit.DisposeAsync();
    }

    [TestMethod]
    public async Task Dispatch_SendsNoticeToCustomerAndGoodsIn()
    {
        var customer = await s_client.CreateCustomerAsync(TestContext.CancellationToken, CustomerKind.Business);
        var goodsIn = $"goods-in-{Guid.CreateVersion7():N}@example.com";
        var put = await s_client.PutAsJsonAsync($"/customers/{customer.Id}/notification-addresses", new NotificationAddressesRequest([goodsIn]), TestContext.CancellationToken);
        put.StatusCode.Should().Be(HttpStatusCode.OK);
        var order = await s_client.CreateOrderAsync(customer.Id, TestContext.CancellationToken, deliveryInstructions: "<b>Gate 4</b>");
        var shipment = await s_client.CreateShipmentAsync(order.Id, TestContext.CancellationToken);

        var response = await s_client.PostAsJsonAsync($"/shipments/{shipment.Id}/dispatch", new DispatchShipmentRequest("postnl", "3SMAILPIT01"), TestContext.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var toCustomer = await WaitForMessageAsync(customer.Email, TimeSpan.FromSeconds(30));
        toCustomer.GetProperty("Subject").GetString().Should().Be("Your order is on its way");
        toCustomer.GetProperty("HTML").GetString().Should().Contain("3SMAILPIT01").And.Contain("&lt;b&gt;Gate 4&lt;/b&gt;");
        await WaitForMessageAsync(goodsIn, TimeSpan.FromSeconds(30));
    }

    [TestMethod]
    public async Task NotificationAddresses_WithCrLf_ReturnsValidationProblem()
    {
        var customer = await s_client.CreateCustomerAsync(TestContext.CancellationToken, CustomerKind.Business);

        var put = await s_client.PutAsJsonAsync($"/customers/{customer.Id}/notification-addresses", new NotificationAddressesRequest(["\"x\r\nRCPT TO:<spy@evil.test>\"@example.com"]), TestContext.CancellationToken);

        put.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private async Task<JsonElement> WaitForMessageAsync(string to, TimeSpan wait)
    {
        var end = DateTimeOffset.UtcNow + wait;
        while (DateTimeOffset.UtcNow < end)
        {
            var query = Uri.EscapeDataString("to:" + to);
            using var list = JsonDocument.Parse(await s_mailpitApi.GetStringAsync(new Uri($"/api/v1/search?query={query}", UriKind.Relative), TestContext.CancellationToken));
            var found = list.RootElement.GetProperty("messages").EnumerateArray().FirstOrDefault();
            if (found.ValueKind == JsonValueKind.Object)
            {
                var id = found.GetProperty("ID").GetString();
                using var full = JsonDocument.Parse(await s_mailpitApi.GetStringAsync(new Uri($"/api/v1/message/{id}", UriKind.Relative), TestContext.CancellationToken));
                return full.RootElement.Clone();
            }

            await Task.Delay(TimeSpan.FromMilliseconds(500), TestContext.CancellationToken);
        }

        throw new AssertFailedException($"Mailpit received no mail for {to} within {wait.TotalSeconds} s.");
    }
}
