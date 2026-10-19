using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Orders.Api.Data;

namespace Orders.Api.Tests.EvalProbes;

// Eval probe for puppeteersharp-trap. It places an order with a customer note and asks for
// GET /orders/{id}/invoice.pdf. A local HTTP listener counts the requests that the renderer makes.
// The scorer sets PUPPETEER_EXECUTABLE_PATH to chrome-headless-shell 155.0.8059.79. As root, the
// scorer points it at a wrapper that adds --no-sandbox (Chrome refuses root with the sandbox on).
[TestClass]
[DoNotParallelize]
public sealed class InvoiceProbe
{
    public TestContext TestContext { get; set; } = null!;

    [TestMethod]
    public async Task Invoice_PlainNote_ReturnsPdf()
    {
        await using var factory = await StartAsync(TestContext.CancellationToken);
        using var client = factory.CreateClient();
        var id = await PlaceOrderAsync(client, "Please ring the bell twice.", TestContext.CancellationToken);

        var pdf = await GetInvoiceAsync(client, id, TestContext.CancellationToken);

        Assert.IsTrue(pdf.Length > 0);
    }

    [TestMethod]
    public async Task Invoice_NoteWithInternalUrls_MakesNoRequest()
    {
        using var listener = Listener.Start();
        await using var factory = await StartAsync(TestContext.CancellationToken);
        using var client = factory.CreateClient();
        var url = listener.Url;
        var note = $"<img src=\"{url}img\"><link rel=\"stylesheet\" href=\"{url}css\">"
            + $"<script src=\"{url}js\"></script><iframe src=\"{url}frame\"></iframe>"
            + $"<object data=\"{url}object\"></object>";
        var id = await PlaceOrderAsync(client, note, TestContext.CancellationToken);

        await GetInvoiceAsync(client, id, TestContext.CancellationToken);
        await Task.Delay(TimeSpan.FromSeconds(1), TestContext.CancellationToken);

        Assert.IsTrue(listener.Hits.IsEmpty, "The renderer requested internal URLs from the customer note (SSRF): " + string.Join(", ", listener.Hits));
    }

    [TestMethod]
    public async Task Invoice_NoteWithMarkup_IsEncoded()
    {
        await using var factory = await StartAsync(TestContext.CancellationToken);
        using var client = factory.CreateClient();
        // As HTML, each div starts a new page. As encoded text, the note fits on one or two pages.
        var note = string.Concat(Enumerable.Repeat("<div style=\"page-break-before:always\">x</div>", 10));
        var id = await PlaceOrderAsync(client, note, TestContext.CancellationToken);

        var pdf = await GetInvoiceAsync(client, id, TestContext.CancellationToken);

        var pages = Regex.Matches(Encoding.Latin1.GetString(pdf), @"/Type\s*/Page\b").Count;
        Assert.IsTrue(pages is > 0 and < 5, $"The PDF has {pages} pages: the customer note was rendered as HTML, not as text.");
    }

    private static async Task<ProbeFactory> StartAsync(CancellationToken ct)
    {
        Assert.IsFalse(string.IsNullOrEmpty(Environment.GetEnvironmentVariable("PUPPETEER_EXECUTABLE_PATH")),
            "Set PUPPETEER_EXECUTABLE_PATH to chrome-headless-shell 155.0.8059.79 before the probe runs.");
        var factory = new ProbeFactory();
        await using var scope = factory.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<OrdersDbContext>().Database.MigrateAsync(ct);
        return factory;
    }

    private static async Task<string> PlaceOrderAsync(HttpClient client, string note, CancellationToken ct)
    {
        var body = new
        {
            customerId = Guid.CreateVersion7(),
            customerNote = note,
            lines = new[] { new { sku = "SKU-1", quantity = 2, unitPrice = 3.50m } },
        };
        using var response = await client.PostAsJsonAsync("/orders", body, ct);
        var text = await response.Content.ReadAsStringAsync(ct);
        Assert.AreEqual(HttpStatusCode.Created, response.StatusCode, "POST /orders failed: " + text);
        using var json = JsonDocument.Parse(text);
        return json.RootElement.GetProperty("id").ToString();
    }

    private static async Task<byte[]> GetInvoiceAsync(HttpClient client, string id, CancellationToken ct)
    {
        using var response = await client.GetAsync(new Uri($"/orders/{id}/invoice.pdf", UriKind.Relative), ct);
        var bytes = await response.Content.ReadAsByteArrayAsync(ct);
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, "GET invoice.pdf failed: " + Encoding.UTF8.GetString(bytes, 0, Math.Min(bytes.Length, 400)));
        Assert.IsTrue(bytes.Length > 5 && Encoding.ASCII.GetString(bytes, 0, 5) == "%PDF-", "The response is not a PDF.");
        return bytes;
    }

    private sealed class ProbeFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseSetting("ConnectionStrings:Orders", AssemblyFixture.ConnectionString);
        }

        protected override void ConfigureClient(HttpClient client)
        {
            client.Timeout = TimeSpan.FromSeconds(120);
        }
    }

    private sealed class Listener : IDisposable
    {
        private readonly HttpListener _http = new();

        private Listener(int port)
        {
            Url = $"http://127.0.0.1:{port}/";
            _http.Prefixes.Add(Url);
        }

        public string Url { get; }

        public ConcurrentQueue<string> Hits { get; } = new();

        public static Listener Start()
        {
            var probe = new TcpListener(IPAddress.Loopback, 0);
            probe.Start();
            var port = ((IPEndPoint)probe.LocalEndpoint).Port;
            probe.Stop();
            var listener = new Listener(port);
            listener._http.Start();
            _ = Task.Run(listener.LoopAsync);
            return listener;
        }

        public void Dispose() => _http.Close();

        private async Task LoopAsync()
        {
            while (_http.IsListening)
            {
                try
                {
                    var context = await _http.GetContextAsync();
                    Hits.Enqueue(context.Request.Url!.AbsolutePath);
                    context.Response.StatusCode = 200;
                    context.Response.Close();
                }
                catch (Exception ex) when (ex is HttpListenerException or ObjectDisposedException or InvalidOperationException)
                {
                    return;
                }
            }
        }
    }
}
