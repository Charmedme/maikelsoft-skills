using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Orders.Api.Data;

namespace Orders.Api.Tests.EvalProbes;

// Eval probe for aspnetcore-trap. A fake stock service on a free local port counts the calls of the app.
[TestClass]
[DoNotParallelize]
public sealed class StockClientProbe
{
    public TestContext TestContext { get; set; } = null!;

    [TestMethod]
    public async Task StockDown_AppTriesAtMostFourTimes()
    {
        await using var stock = await FakeStock.StartAsync(FakeStock.Mode.Unavailable);
        await using var factory = Factory(stock.BaseUrl);
        using var client = factory.CreateClient();
        var orderId = await PlaceOrderAsync(factory, client, TestContext.CancellationToken);

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(TestContext.CancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(60));
        await client.PostAsync(new Uri($"/orders/{orderId}/cancel", UriKind.Relative), null, cts.Token);
        await stock.WaitUntilQuietAsync(TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(40));

        Assert.IsTrue(stock.Calls is >= 1 and <= 4, $"The app called the stock service {stock.Calls} times for one cancel. Expected 1 to 4.");
    }

    [TestMethod]
    public async Task StockHangs_OneTryStopsAfterAboutTwoSeconds()
    {
        await using var stock = await FakeStock.StartAsync(FakeStock.Mode.Hang);
        await using var factory = Factory(stock.BaseUrl);
        using var client = factory.CreateClient();
        var orderId = await PlaceOrderAsync(factory, client, TestContext.CancellationToken);

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(TestContext.CancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(8));
        try
        {
            await client.PostAsync(new Uri($"/orders/{orderId}/cancel", UriKind.Relative), null, cts.Token);
        }
        catch (OperationCanceledException)
        {
        }

        var first = await stock.FirstAbortAsync(TimeSpan.FromSeconds(10));
        Assert.IsNotNull(first, "The first call to the stock service was not stopped within 10 seconds.");
        Assert.IsTrue(first.Value.TotalSeconds is >= 1.5 and <= 3.5, $"The first call to the stock service stopped after {first.Value.TotalSeconds:F1} s. Expected about 2 s.");
    }

    [TestMethod]
    public async Task UnexpectedError_IsLoggedAndNotShown()
    {
        var logs = new ConcurrentQueue<(LogLevel Level, Exception? Error)>();
        await using var factory = Factory("http://127.0.0.1:9", services =>
        {
            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(new ThrowingTimeProvider());
        }, logs);
        using var client = factory.CreateClient();
        await MigrateAsync(factory, TestContext.CancellationToken);

        var body = new { customerId = Guid.CreateVersion7(), lines = new[] { new { sku = "SKU-1", quantity = 1, unitPrice = 1m } } };
        var response = await client.PostAsJsonAsync("/orders", body, TestContext.CancellationToken);
        var text = await response.Content.ReadAsStringAsync(TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.DoesNotContain(ProbeException.Secret, text);
        Assert.IsTrue(logs.Any(entry => entry.Level >= LogLevel.Error && entry.Error is ProbeException), "No error log entry has the unexpected exception.");
    }

    private static WebApplicationFactory<Program> Factory(string stockBaseUrl, Action<IServiceCollection>? services = null, ConcurrentQueue<(LogLevel, Exception?)>? logs = null) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ConnectionStrings:Orders", AssemblyFixture.ConnectionString);
            builder.UseSetting("Stock:BaseUrl", stockBaseUrl);
            if (logs is not null)
            {
                builder.ConfigureLogging(logging => logging.AddProvider(new CaptureProvider(logs)));
            }

            if (services is not null)
            {
                builder.ConfigureTestServices(services);
            }
        });

    private static async Task MigrateAsync(WebApplicationFactory<Program> factory, CancellationToken ct)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<OrdersDbContext>().Database.MigrateAsync(ct);
    }

    private static async Task<Guid> PlaceOrderAsync(WebApplicationFactory<Program> factory, HttpClient client, CancellationToken ct)
    {
        await MigrateAsync(factory, ct);
        var body = new { customerId = Guid.CreateVersion7(), lines = new[] { new { sku = "SKU-1", quantity = 1, unitPrice = 1m } } };
        var response = await client.PostAsJsonAsync("/orders", body, ct);
        var text = await response.Content.ReadAsStringAsync(ct);
        Assert.IsTrue(response.IsSuccessStatusCode, $"POST /orders returned {(int)response.StatusCode}: {text}");
        return JsonDocument.Parse(text).RootElement.GetProperty("id").GetGuid();
    }

    private sealed class ProbeException() : Exception(Secret)
    {
        public const string Secret = "eval-probe-secret-7c1f";
    }

    // Throws only when code of the app itself asks for the time (for example POST /orders), not when the framework does.
    private sealed class ThrowingTimeProvider : TimeProvider
    {
        public override DateTimeOffset GetUtcNow()
        {
            var frames = new StackTrace().GetFrames();
            var fromApp = frames.Any(frame => frame.GetMethod()?.DeclaringType?.Assembly == typeof(Program).Assembly);
            var fromFramework = frames.Any(frame => frame.GetMethod()?.DeclaringType?.Namespace?.StartsWith("Polly", StringComparison.Ordinal) == true);
            return fromApp && !fromFramework ? throw new ProbeException() : base.GetUtcNow();
        }
    }

    private sealed class CaptureProvider(ConcurrentQueue<(LogLevel, Exception?)> logs) : ILoggerProvider
    {
        public ILogger CreateLogger(string categoryName) => new Capture(logs);

        public void Dispose()
        {
        }

        private sealed class Capture(ConcurrentQueue<(LogLevel, Exception?)> logs) : ILogger
        {
            public IDisposable? BeginScope<TState>(TState state)
                where TState : notnull => null;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
                logs.Enqueue((logLevel, exception));
        }
    }

    private sealed class FakeStock : IAsyncDisposable
    {
        private readonly WebApplication _app;
        private readonly ConcurrentQueue<TimeSpan> _aborts = new();
        private int _calls;

        private FakeStock(WebApplication app) => _app = app;

        public enum Mode
        {
            Unavailable,
            Hang,
        }

        public int Calls => Volatile.Read(ref _calls);

        public string BaseUrl { get; private set; } = string.Empty;

        public static async Task<FakeStock> StartAsync(Mode mode)
        {
            var builder = WebApplication.CreateSlimBuilder();
            builder.WebHost.UseUrls("http://127.0.0.1:0");
            builder.Logging.ClearProviders();
            var app = builder.Build();
            var stock = new FakeStock(app);
            app.Run(async context =>
            {
                Interlocked.Increment(ref stock._calls);
                if (mode == Mode.Unavailable)
                {
                    context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
                    context.Response.Headers.RetryAfter = "0";
                    return;
                }

                var watch = Stopwatch.StartNew();
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(30), context.RequestAborted);
                }
                catch (OperationCanceledException)
                {
                    stock._aborts.Enqueue(watch.Elapsed);
                }
            });
            await app.StartAsync();
            stock.BaseUrl = app.Urls.First();
            return stock;
        }

        public async Task WaitUntilQuietAsync(TimeSpan quiet, TimeSpan max)
        {
            var end = DateTime.UtcNow + max;
            var last = Calls;
            var since = DateTime.UtcNow;
            while (DateTime.UtcNow < end && DateTime.UtcNow - since < quiet)
            {
                await Task.Delay(100);
                if (Calls != last)
                {
                    last = Calls;
                    since = DateTime.UtcNow;
                }
            }
        }

        public async Task<TimeSpan?> FirstAbortAsync(TimeSpan wait)
        {
            var end = DateTime.UtcNow + wait;
            while (DateTime.UtcNow < end)
            {
                if (_aborts.TryPeek(out var first))
                {
                    return first;
                }

                await Task.Delay(100);
            }

            return null;
        }

        public async ValueTask DisposeAsync()
        {
            await _app.StopAsync();
            await _app.DisposeAsync();
        }
    }
}
