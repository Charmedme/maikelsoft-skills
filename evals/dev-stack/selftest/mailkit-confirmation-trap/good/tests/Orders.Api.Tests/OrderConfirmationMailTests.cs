using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Orders.Api.Orders;
using Testcontainers.Mailpit;

namespace Orders.Api.Tests;

// Sends through a real SMTP server (Mailpit) and reads the captured mail from its REST API.
[TestClass]
[DoNotParallelize]
public sealed class OrderConfirmationMailTests
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

        var orders = new OrdersApiFactory();
        await orders.MigrateAsync(context.CancellationToken);
        await orders.DisposeAsync();

        s_factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ConnectionStrings:Orders", AssemblyFixture.ConnectionString);
            builder.UseSetting("Smtp:Host", s_mailpit.Hostname);
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
    public async Task CreateOrder_SendsConfirmationWithEncodedName()
    {
        var email = $"buyer-{Guid.CreateVersion7():N}@example.com";
        var request = new CreateOrderRequest(Guid.CreateVersion7(), [new CreateOrderLineRequest("SKU-7", 2, 4.25m)], "<b>Ann</b>", email);

        var response = await s_client.PostAsJsonAsync("/orders", request, TestContext.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var message = await WaitForMessageAsync(email, TimeSpan.FromSeconds(30));
        message.GetProperty("Subject").GetString().Should().Be("Order received");
        message.GetProperty("From").GetProperty("Address").GetString().Should().Be("orders@example.com");
        message.GetProperty("Text").GetString().Should().Contain("SKU-7");
        var html = message.GetProperty("HTML").GetString();
        html.Should().Contain("&lt;b&gt;Ann&lt;/b&gt;").And.NotContain("<b>Ann</b>");
    }

    [TestMethod]
    public async Task CreateOrder_EmailWithLineBreak_ReturnsValidationProblem()
    {
        var request = new CreateOrderRequest(Guid.CreateVersion7(), [new CreateOrderLineRequest("SKU-1", 1, 1m)], "Ann", "ann@example.com\r\nBcc: other@example.com");

        var response = await s_client.PostAsJsonAsync("/orders", request, TestContext.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private async Task<JsonElement> WaitForMessageAsync(string to, TimeSpan wait)
    {
        var end = DateTimeOffset.UtcNow + wait;
        while (DateTimeOffset.UtcNow < end)
        {
            using var list = JsonDocument.Parse(await s_mailpitApi.GetStringAsync(new Uri($"/api/v1/search?query={Uri.EscapeDataString("to:" + to)}", UriKind.Relative), TestContext.CancellationToken));
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
