using System.Collections.Concurrent;
using System.IO.Compression;
using System.Net;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Orders.Api.Data;

namespace Orders.Api.Tests.EvalProbes;

// Eval probe for elasticsearch-trap. The app talks to a fake Elasticsearch server (FakeSearchServer)
// at ConnectionStrings:Search. The probe uses only HTTP (POST /orders) and watches the write requests
// that reach the fake. A document whose body contains a SKU that starts with "STALE-" gets a 409
// version conflict, as Elasticsearch answers when the index already has a newer external version.
[TestClass]
[DoNotParallelize]
public sealed class EsSyncProbe
{
    private static readonly TimeSpan s_wait = TimeSpan.FromSeconds(20);

    public TestContext TestContext { get; set; } = null!;

    // Trap: without external versions, an older update that comes late overwrites a newer one.
    [TestMethod]
    public async Task Index_UsesExternalVersion()
    {
        var ct = TestContext.CancellationToken;
        using var search = new FakeSearchServer();
        var logs = new ErrorLog();
        await using var node = new Node(search.Url, logs);
        await MigrateAsync(node, ct);
        using var client = node.CreateClient();

        await CreateOrderAsync(client, "VERSION-1", ct);

        var write = await search.WaitForWriteAsync("VERSION-1", s_wait, ct);
        Assert.IsNotNull(write, "No index request for the new order reached Elasticsearch within 20 seconds.");
        Assert.IsTrue(write.UsesExternalVersion, $"The index request has no external version: {write.Summary}");
    }

    // Trap: one stale (409) item must not stop the sync, throw, or block the other documents.
    [TestMethod]
    public async Task StaleItem_IsSkipped_AndTheRestIsIndexed()
    {
        var ct = TestContext.CancellationToken;
        using var search = new FakeSearchServer();
        var logs = new ErrorLog();
        await using var node = new Node(search.Url, logs);
        await MigrateAsync(node, ct);
        using var client = node.CreateClient();

        await CreateOrderAsync(client, "STALE-1", ct);
        await CreateOrderAsync(client, "FRESH-1", ct);

        var fresh = await search.WaitForWriteAsync("FRESH-1", s_wait, ct);
        Assert.IsNotNull(fresh, "The order after the stale one did not reach Elasticsearch within 20 seconds.");
        await Task.Delay(TimeSpan.FromSeconds(6), ct); // time for retries of a failed batch

        Assert.IsTrue(search.WritesOf("STALE-1") <= 2, $"The stale document was sent {search.WritesOf("STALE-1")} times: the sync retries a 409.");
        string[] signs = ["409", "conflict", "bulk"];
        var syncErrors = logs.Entries.Where(e => signs.Any(s => e.Contains(s, StringComparison.OrdinalIgnoreCase))).ToList();
        Assert.IsEmpty(syncErrors, "The sync logged the stale item as an error: " + string.Join(" | ", syncErrors.Select(e => e[..Math.Min(e.Length, 300)])));
    }

    private static async Task MigrateAsync(Node node, CancellationToken ct)
    {
        await using var scope = node.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<OrdersDbContext>().Database.MigrateAsync(ct);
    }

    private static async Task CreateOrderAsync(HttpClient client, string sku, CancellationToken ct)
    {
        var body = new { customerId = Guid.CreateVersion7(), lines = new[] { new { sku, quantity = 1, unitPrice = 2.00m } } };
        using var response = await client.PostAsJsonAsync("/orders", body, ct);
        Assert.IsTrue(response.IsSuccessStatusCode, $"POST /orders with {sku} returned {(int)response.StatusCode}: {await response.Content.ReadAsStringAsync(ct)}");
    }

    private sealed class Node(string searchUrl, ErrorLog logs) : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseSetting("ConnectionStrings:Orders", AssemblyFixture.ConnectionString);
            builder.UseSetting("ConnectionStrings:Search", searchUrl);
            builder.UseSetting("Search:ApiKey", "probe-key");
            builder.ConfigureServices(services => services.AddSingleton<ILoggerProvider>(logs));
        }
    }

    internal sealed record Write(string Summary, bool UsesExternalVersion);

    // Records each log entry at level Error or higher, with its exception.
    private sealed class ErrorLog : ILoggerProvider, ILogger
    {
        public ConcurrentQueue<string> Entries { get; } = new();

        public ILogger CreateLogger(string categoryName) => this;

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Error;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (IsEnabled(logLevel))
            {
                Entries.Enqueue(formatter(state, exception) + " " + exception);
            }
        }

        public void Dispose()
        {
        }
    }

    // A small HTTP server that answers like Elasticsearch 9 for the calls that an indexer makes.
    private sealed class FakeSearchServer : IDisposable
    {
        private readonly HttpListener _listener = new();
        private readonly ConcurrentQueue<(string Body, Write Write)> _writes = new();
        private readonly CancellationTokenSource _stop = new();

        public FakeSearchServer()
        {
            var port = FreePort();
            Url = $"http://127.0.0.1:{port}";
            _listener.Prefixes.Add(Url + "/");
            _listener.Start();
            _ = Task.Run(LoopAsync);
        }

        public string Url { get; }

        public int WritesOf(string sku) => _writes.Count(w => w.Body.Contains(sku, StringComparison.Ordinal));

        public async Task<Write?> WaitForWriteAsync(string sku, TimeSpan timeout, CancellationToken ct)
        {
            var end = DateTime.UtcNow + timeout;
            while (DateTime.UtcNow < end)
            {
                var hit = _writes.FirstOrDefault(w => w.Body.Contains(sku, StringComparison.Ordinal));
                if (hit.Write is not null)
                {
                    return hit.Write;
                }

                await Task.Delay(100, ct);
            }

            return null;
        }

        public void Dispose()
        {
            _stop.Cancel();
            _listener.Close();
            _stop.Dispose();
        }

        private static int FreePort()
        {
            using var socket = new TcpListener(IPAddress.Loopback, 0);
            socket.Start();
            return ((IPEndPoint)socket.LocalEndpoint).Port;
        }

        private async Task LoopAsync()
        {
            while (!_stop.IsCancellationRequested)
            {
                HttpListenerContext context;
                try
                {
                    context = await _listener.GetContextAsync();
                }
                catch (Exception) when (_stop.IsCancellationRequested)
                {
                    return;
                }
                catch (HttpListenerException)
                {
                    return;
                }

                try
                {
                    await HandleAsync(context);
                }
                catch (Exception)
                {
                    // A failed answer must not stop the server.
                }
            }
        }

        private async Task HandleAsync(HttpListenerContext context)
        {
            var request = context.Request;
            var path = request.Url!.AbsolutePath;
            var query = request.Url.Query;
            string body;
            var input = request.Headers["Content-Encoding"] is { } encoding && encoding.Contains("gzip", StringComparison.OrdinalIgnoreCase)
                ? new GZipStream(request.InputStream, CompressionMode.Decompress) // client 9 compresses request bodies
                : request.InputStream;
            using (var reader = new StreamReader(input, Encoding.UTF8))
            {
                body = await reader.ReadToEndAsync();
            }

            var (status, json) = Answer(request.HttpMethod, path, query, body);
            var response = context.Response;
            response.StatusCode = status;
            response.Headers["X-Elastic-Product"] = "Elasticsearch";
            response.ContentType = "application/json";
            if (request.HttpMethod != "HEAD" && json is not null)
            {
                var bytes = Encoding.UTF8.GetBytes(json);
                response.ContentLength64 = bytes.Length;
                await response.OutputStream.WriteAsync(bytes);
            }

            response.Close();
        }

        private (int Status, string? Json) Answer(string method, string path, string query, string body)
        {
            var segments = path.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
            var last = segments.Length > 0 ? segments[^1] : string.Empty;

            if (last == "_bulk")
            {
                return (200, Bulk(segments.Length > 1 ? segments[0] : "orders", body));
            }

            if (segments.Length >= 2 && segments[^2] is "_doc" or "_create" or "_update" && method is "PUT" or "POST")
            {
                var external = query.Contains("version_type=external", StringComparison.OrdinalIgnoreCase);
                _writes.Enqueue((body, new Write($"{method} {path}{query}", external)));
                return body.Contains("STALE-", StringComparison.Ordinal)
                    ? (409, Conflict(last))
                    : (201, $$"""{"_index":"orders_v1","_id":"{{last}}","_version":1,"result":"created","_shards":{"total":1,"successful":1,"failed":0},"_seq_no":0,"_primary_term":1}""");
            }

            if (last == "_search")
            {
                return (200, """{"took":1,"timed_out":false,"_shards":{"total":1,"successful":1,"skipped":0,"failed":0},"hits":{"total":{"value":0,"relation":"eq"},"max_score":null,"hits":[]}}""");
            }

            if (last == "_pit")
            {
                return method == "DELETE"
                    ? (200, """{"succeeded":true,"num_freed":1}""")
                    : (200, """{"id":"probe-pit"}""");
            }

            if (last == "_count")
            {
                return (200, """{"count":0,"_shards":{"total":1,"successful":1,"skipped":0,"failed":0}}""");
            }

            if (method == "HEAD")
            {
                return (404, null); // an index or alias does not exist yet
            }

            if (method == "GET" && segments.Length == 0)
            {
                return (200, """{"name":"probe","cluster_name":"probe","version":{"number":"9.5.5","build_flavor":"default"},"tagline":"You Know, for Search"}""");
            }

            if (method == "GET" && last.StartsWith("_alias", StringComparison.Ordinal))
            {
                return (404, """{"error":"alias [orders] missing","status":404}""");
            }

            if (method == "PUT" && segments.Length == 1)
            {
                return (200, $$"""{"acknowledged":true,"shards_acknowledged":true,"index":"{{segments[0]}}"}""");
            }

            return (200, """{"acknowledged":true}""");
        }

        private string Bulk(string defaultIndex, string body)
        {
            var lines = body.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            var items = new List<string>();
            var errors = false;
            for (var i = 0; i < lines.Length; i++)
            {
                using var action = JsonDocument.Parse(lines[i]);
                var op = action.RootElement.EnumerateObject().First();
                var meta = op.Value;
                var id = meta.TryGetProperty("_id", out var idValue) ? idValue.ToString() : Guid.NewGuid().ToString("N");
                var index = meta.TryGetProperty("_index", out var indexValue) ? indexValue.GetString() ?? defaultIndex : defaultIndex;
                var external = meta.TryGetProperty("version_type", out var versionType)
                    && string.Equals(versionType.GetString(), "external", StringComparison.OrdinalIgnoreCase);
                var source = op.Name == "delete" || i + 1 >= lines.Length ? string.Empty : lines[++i];
                if (op.Name != "delete")
                {
                    _writes.Enqueue((source, new Write($"_bulk {lines[i - 1]}", external)));
                }

                if (source.Contains("STALE-", StringComparison.Ordinal))
                {
                    errors = true;
                    items.Add("{\"" + op.Name + "\":{\"_index\":\"" + index + "\",\"_id\":\"" + id + "\",\"status\":409,\"error\":{\"type\":\"version_conflict_engine_exception\",\"reason\":\"[" + id + "]: version conflict, current version [99] is higher or equal to the one provided\",\"index\":\"" + index + "\"}}}");
                }
                else
                {
                    items.Add("{\"" + op.Name + "\":{\"_index\":\"" + index + "\",\"_id\":\"" + id + "\",\"_version\":1,\"result\":\"created\",\"_shards\":{\"total\":1,\"successful\":1,\"failed\":0},\"_seq_no\":0,\"_primary_term\":1,\"status\":201}}");
                }
            }

            return $$"""{"took":3,"errors":{{(errors ? "true" : "false")}},"items":[{{string.Join(',', items)}}]}""";
        }

        private static string Conflict(string id) =>
            $$"""{"error":{"root_cause":[{"type":"version_conflict_engine_exception","reason":"[{{id}}]: version conflict, current version [99] is higher or equal to the one provided","index":"orders_v1"}],"type":"version_conflict_engine_exception","reason":"[{{id}}]: version conflict, current version [99] is higher or equal to the one provided","index":"orders_v1"},"status":409}""";
    }
}
