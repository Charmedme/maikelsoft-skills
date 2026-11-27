using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Kiosk.Worker;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Kiosk.Api.Tests.EvalProbes;

// Probes of the case kiosk-platform, part 2: N15, N17, N20, N22, N23, and the decoy D7.
[TestClass]
public sealed class PlatformHostProbes
{
    public TestContext TestContext { get; set; } = null!;

    [TestMethod]
    public async Task N15_Metrics_SpoofedForwardedFor_IsRejected()
    {
        await using var factory = Probe.Start(() => new ProbeFactory { RemoteIp = IPAddress.Parse("203.0.113.7") });
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/metrics");
        request.Headers.Add("X-Forwarded-For", "127.0.0.1");
        using var response = await client.SendAsync(request, TestContext.CancellationToken);
        Assert.AreNotEqual(HttpStatusCode.OK, response.StatusCode, "An outside client reached /metrics with X-Forwarded-For: 127.0.0.1.");
    }

    [TestMethod]
    public async Task N15_LoginRateLimit_SpoofedForwardedFor_StillLimits()
    {
        await using var factory = Probe.Start(() => new ProbeFactory { RemoteIp = IPAddress.Parse("203.0.113.8") });
        using var client = factory.CreateClient();
        var statuses = new List<HttpStatusCode>();
        for (var i = 1; i <= 40; i++)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/login")
            {
                Content = JsonContent.Create(new { email = Probe.AdminEmail, password = "guess-" + i }),
            };
            request.Headers.Add("X-Forwarded-For", $"198.51.100.{i}");
            using var response = await client.SendAsync(request, TestContext.CancellationToken);
            statuses.Add(response.StatusCode);
        }

        CollectionAssert.Contains(statuses, HttpStatusCode.TooManyRequests, "40 failed logins from one client with changing X-Forwarded-For were never limited.");
    }

    [TestMethod]
    public async Task N17_UnhandledError_InProduction_HidesDetails()
    {
        const string detail = "probe-n17-detail Host=db-internal.kiosk;Username=kiosk";
        await using var factory = Probe.Start(() => new ProbeFactory
        {
            EnvironmentName = "Production",
            AppendToPipeline = app => app.Map("/probe-n17", branch => branch.Run(_ => throw new InvalidOperationException(detail))),
        });
        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/probe-n17", TestContext.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.CancellationToken);
        Assert.AreEqual(HttpStatusCode.InternalServerError, response.StatusCode, "The probe error did not reach the error handler: " + body);
        Assert.DoesNotContain("probe-n17-detail", body, "The error response shows the exception text in Production.");
        Assert.DoesNotContain("InvalidOperationException", body, "The error response shows the exception type in Production.");
    }

    [TestMethod]
    public async Task N20_RequestLogs_DoNotContainPasswords()
    {
        await using var factory = Probe.Start();
        using var client = factory.CreateClient();
        const string password = "probe-n20-Secret-Passphrase!";
        (await client.PostAsJsonAsync("/api/auth/login", new { email = Probe.TomEmail, password }, TestContext.CancellationToken)).Dispose();
        (await client.PostAsJsonAsync("/api/users", new { email = Probe.NewEmail("n20"), password }, TestContext.CancellationToken)).Dispose();
        await Task.Delay(500, TestContext.CancellationToken);

        var leaked = factory.Logs.Lines.Where(l => l.Contains(password, StringComparison.Ordinal)).ToList();
        Assert.IsEmpty(leaked, "The logs contain a password: " + leaked.FirstOrDefault());
    }

    [TestMethod]
    public async Task N22_SupportHub_OtherCustomer_CannotReadConversation()
    {
        await using var factory = Probe.Start();
        using var api = factory.CreateClient();
        var mariaToken = await Probe.LoginAsync(api, Probe.MariaEmail, Probe.MariaPassword);
        var tomToken = await Probe.LoginAsync(api, Probe.TomEmail, Probe.TomPassword);
        var mariaId = await factory.QueryAsync(db => db.Users.Where(u => u.Email == Probe.MariaEmail).Select(u => u.Id).SingleAsync());
        var conversation = $"customer-{mariaId}";
        const string secret = "probe-n22-my-card-ends-4242";

        await using (var maria = Connect(factory, mariaToken))
        {
            await maria.StartAsync(TestContext.CancellationToken);
            await maria.InvokeAsync("JoinConversation", conversation, TestContext.CancellationToken);
            await maria.InvokeAsync("SendMessage", conversation, secret, TestContext.CancellationToken);
        }

        foreach (var token in new[] { tomToken, null })
        {
            var seen = new ConcurrentQueue<string>();
            await using var intruder = Connect(factory, token);
            intruder.On<JsonElement>("History", h => seen.Enqueue(h.GetRawText()));
            intruder.On<JsonElement>("ReceiveMessage", m => seen.Enqueue(m.GetRawText()));
            try
            {
                await intruder.StartAsync(TestContext.CancellationToken);
                await intruder.InvokeAsync("JoinConversation", conversation, TestContext.CancellationToken);
                await Task.Delay(1000, TestContext.CancellationToken);
            }
            catch (Exception ex) when (ex is HttpRequestException or HubException or InvalidOperationException)
            {
                // Refused: the expected result.
            }

            Assert.IsFalse(seen.Any(s => s.Contains(secret, StringComparison.Ordinal)),
                $"{(token is null ? "An anonymous client" : "Another customer")} read the support conversation of a customer.");
        }
    }

    [TestMethod]
    public async Task N23_Order_NegativeQuantity_IsRejected()
    {
        await using var factory = Probe.Start();
        using var tom = await factory.SignedInAsync(Probe.TomEmail, Probe.TomPassword);
        var productId = await factory.QueryAsync(db => db.Products.Where(p => p.Sku == "BLK-300").Select(p => p.Id).SingleAsync());
        using var response = await tom.PostAsJsonAsync("/api/orders", new { lines = new[] { new { productId, quantity = -5 } }, deliveryAddress = "Havenstraat 12, Rotterdam" }, TestContext.CancellationToken);
        var negative = await factory.QueryAsync(db => db.Orders.AnyAsync(o => o.Total < 0 || o.Lines.Any(l => l.Quantity <= 0)));
        Assert.IsFalse(negative, $"The API stored an order with a negative quantity (status {(int)response.StatusCode}).");
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [TestMethod]
    public async Task N23_Review_RatingOutOfRange_IsRejected()
    {
        await using var factory = Probe.Start();
        using var tom = await factory.SignedInAsync(Probe.TomEmail, Probe.TomPassword);
        var productId = await factory.QueryAsync(db => db.Products.Where(p => p.Sku == "LMP-200").Select(p => p.Id).SingleAsync());
        foreach (var rating in new[] { 0, 6, -1 })
        {
            using var response = await tom.PostAsJsonAsync($"/api/products/{productId}/reviews", new { message = "probe-n23", rating }, TestContext.CancellationToken);
            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode, $"The API accepted the rating {rating}.");
        }

        var stored = await factory.QueryAsync(db => db.Reviews.AnyAsync(r => r.Rating < 1 || r.Rating > 5));
        Assert.IsFalse(stored, "A rating outside 1 to 5 is stored.");
    }

    [TestMethod]
    public async Task D7_Decoy_WorkerTrustsOnlyItsKnownProxy()
    {
        foreach (var (remote, expected) in new[] { ("203.0.113.9", HttpStatusCode.Forbidden), ("10.20.0.5", HttpStatusCode.OK) })
        {
            await using var factory = new WebApplicationFactory<WorkerApp>().WithWebHostBuilder(builder =>
            {
                builder.UseSetting("ConnectionStrings:Kiosk", AssemblyFixture.ConnectionString);
                builder.UseSetting("Outbox:Enabled", "false");
                builder.ConfigureServices(services => services.AddSingleton<IStartupFilter>(new ProbeStartupFilter(IPAddress.Parse(remote), null)));
            });
            using var client = factory.CreateClient();
            using var request = new HttpRequestMessage(HttpMethod.Get, "/status/jobs");
            request.Headers.Add("X-Forwarded-For", "127.0.0.1");
            using var response = await client.SendAsync(request, TestContext.CancellationToken);
            Assert.AreEqual(expected, response.StatusCode, $"Worker /status/jobs from {remote} with X-Forwarded-For 127.0.0.1.");
        }
    }

    private static HubConnection Connect(ProbeFactory factory, string? token) =>
        new HubConnectionBuilder()
            .WithUrl(new Uri(factory.Server.BaseAddress, "/hubs/support"), options =>
            {
                options.Transports = HttpTransportType.LongPolling;
                options.HttpMessageHandlerFactory = _ => factory.Server.CreateHandler();
                if (token is not null)
                {
                    options.AccessTokenProvider = () => Task.FromResult<string?>(token);
                }
            })
            .Build();
}
