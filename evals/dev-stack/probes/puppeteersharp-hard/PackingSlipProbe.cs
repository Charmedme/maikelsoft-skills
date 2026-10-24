using System.Globalization;
using System.IO.Compression;
using System.Net;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Orders.Api.Customers;
using Orders.Api.Data;
using Orders.Api.Orders;
using Orders.Api.Shipping;

namespace Orders.Api.Tests.EvalProbes;

// Eval probe for puppeteersharp-hard. It renders packing slips through the public API with the real
// chrome-headless-shell (PUPPETEER_EXECUTABLE_PATH) and reads the PDF bytes. A local TCP listener counts
// each connection that the service (Chrome or an HttpClient) opens to it.
[TestClass]
[DoNotParallelize]
public sealed partial class PackingSlipProbe
{
    // DOC-01: A4 portrait is 595.92 x 841.92 points in a Chrome PDF. Letter is 612 x 792.
    private const double A4Width = 595.92;
    private const double A4Height = 841.92;

    // The fill of the even table rows (#f2f5fb in DocumentStyles), as Chrome writes it into the page content.
    private const string ZebraFill = ".949 .9608 .9843 rg";

    public TestContext TestContext { get; set; } = null!;

    [ClassInitialize]
    public static async Task MigrateAsync(TestContext context)
    {
        var options = new DbContextOptionsBuilder<OrdersDbContext>().UseNpgsql(AssemblyFixture.ConnectionString).Options;
        await using var db = new OrdersDbContext(options);
        await db.Database.MigrateAsync(context.CancellationToken);
    }

    [TestMethod]
    public async Task PackingSlip_BusinessBranding_ReturnsPdf()
    {
        await using var factory = new ProbeFactory();
        using var client = Client(factory);
        var customer = await CreateCustomerAsync(client, CustomerKind.Business);

        var put = await client.PutAsJsonAsync($"/customers/{customer.Id}/branding", new { logoUrl = (string?)null, footerText = "Goods-in: dock 4, 07:00-15:00" }, TestContext.CancellationToken);
        Assert.IsTrue(put.IsSuccessStatusCode, $"PUT /customers/{{id}}/branding with a footer only gave {(int)put.StatusCode}: {await put.Content.ReadAsStringAsync(TestContext.CancellationToken)}");

        var pdf = await PackingSlipAsync(client, customer.Id, "PO-77812");
        Assert.AreEqual("%PDF-", Encoding.ASCII.GetString(pdf, 0, 5), "The packing slip is not a PDF.");
    }

    [TestMethod]
    public async Task PackingSlip_IsA4()
    {
        await using var factory = new ProbeFactory();
        using var client = Client(factory);
        var customer = await CreateCustomerAsync(client, CustomerKind.Consumer);

        var pdf = await PackingSlipAsync(client, customer.Id, null);

        var boxes = MediaBoxes(pdf);
        Assert.IsTrue(boxes.Count > 0, "The PDF has no /MediaBox.");
        foreach (var (width, height) in boxes)
        {
            Assert.IsTrue(Math.Abs(width - A4Width) < 2 && Math.Abs(height - A4Height) < 2, $"Page size {width} x {height} points is not A4 portrait (595.92 x 841.92). 612 x 792 is Letter, the default of PdfOptions.");
        }
    }

    [TestMethod]
    public async Task PackingSlip_PrintsBackgrounds()
    {
        await using var factory = new ProbeFactory();
        using var client = Client(factory);
        var customer = await CreateCustomerAsync(client, CustomerKind.Consumer);

        var pdf = await PackingSlipAsync(client, customer.Id, null);

        var content = PageContent(pdf);
        Assert.Contains(ZebraFill, content, "The PDF has no background fill (the header bar and the table rows of DocumentStyles). PdfOptions.PrintBackground is false by default.");
    }

    [TestMethod]
    public async Task BrandingLogo_InternalAddress_NeverRequested()
    {
        await using var listener = CountingListener.Start();
        await using var factory = new ProbeFactory();
        using var client = Client(factory);
        var customer = await CreateCustomerAsync(client, CustomerKind.Business);
        string[] urls =
        [
            $"https://127.0.0.1:{listener.Port}/logo.png",
            $"https://localhost:{listener.Port}/logo.png",
            $"http://127.0.0.1:{listener.Port}/logo.png",
        ];

        foreach (var url in urls)
        {
            // The service may refuse the URL (4xx) or accept it. Either way it must never connect to it.
            using var put = await client.PutAsJsonAsync($"/customers/{customer.Id}/branding", new { logoUrl = url, footerText = (string?)null }, TestContext.CancellationToken);
            _ = await PackingSlipAsync(client, customer.Id, null, requireOk: false);
        }

        await Task.Delay(TimeSpan.FromSeconds(1), TestContext.CancellationToken);
        Assert.AreEqual(0, listener.Connections, $"The service opened {listener.Connections} connection(s) to an internal address from a customer logo URL (SSRF).");
    }

    [TestMethod]
    public async Task OwnLogo_NotFetchedForEachDocument()
    {
        await using var listener = CountingListener.Start();
        await using var factory = new ProbeFactory(("Documents:LogoUrl", $"http://127.0.0.1:{listener.Port}/logo.png"));
        using var client = Client(factory);
        var customer = await CreateCustomerAsync(client, CustomerKind.Consumer);

        for (var i = 0; i < 3; i++)
        {
            _ = await PackingSlipAsync(client, customer.Id, null);
        }

        await Task.Delay(TimeSpan.FromSeconds(1), TestContext.CancellationToken);
        Assert.IsTrue(listener.Connections <= 1, $"Three packing slips opened {listener.Connections} connections to the logo host: each document loads remote assets at render time.");
    }

    private static HttpClient Client(ProbeFactory factory)
    {
        var client = factory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(150);
        return client;
    }

    private async Task<CustomerResponse> CreateCustomerAsync(HttpClient client, CustomerKind kind)
    {
        var request = new CreateCustomerRequest(
            "Probe Buyer",
            $"probe-{Guid.CreateVersion7():N}@example.com",
            kind,
            kind == CustomerKind.Business ? "Probe Trading B.V." : null);
        using var response = await client.PostAsJsonAsync("/customers", request, TestContext.CancellationToken);
        Assert.AreEqual(HttpStatusCode.Created, response.StatusCode, "POST /customers failed.");
        return (await response.Content.ReadFromJsonAsync<CustomerResponse>(TestContext.CancellationToken))!;
    }

    private async Task<byte[]> PackingSlipAsync(HttpClient client, Guid customerId, string? reference, bool requireOk = true)
    {
        var order = new CreateOrderRequest(customerId, [new CreateOrderLineRequest("SKU-PROBE-1", 2, 4.50m), new CreateOrderLineRequest("SKU-PROBE-2", 1, 9.99m)], reference, "Leave at the back door.");
        using var created = await client.PostAsJsonAsync("/orders", order, TestContext.CancellationToken);
        Assert.AreEqual(HttpStatusCode.Created, created.StatusCode, "POST /orders failed.");
        var orderId = (await created.Content.ReadFromJsonAsync<OrderResponse>(TestContext.CancellationToken))!.Id;
        using var shipped = await client.PostAsJsonAsync("/shipments", new CreateShipmentRequest(orderId), TestContext.CancellationToken);
        Assert.AreEqual(HttpStatusCode.Created, shipped.StatusCode, "POST /shipments failed.");
        var shipmentId = (await shipped.Content.ReadFromJsonAsync<ShipmentResponse>(TestContext.CancellationToken))!.Id;

        using var response = await client.GetAsync(new Uri($"/shipments/{shipmentId}/packing-slip.pdf", UriKind.Relative), TestContext.CancellationToken);
        var bytes = await response.Content.ReadAsByteArrayAsync(TestContext.CancellationToken);
        if (requireOk)
        {
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, $"GET packing-slip.pdf failed: {Encoding.UTF8.GetString(bytes, 0, Math.Min(bytes.Length, 400))}");
        }

        return bytes;
    }

    private static List<(double Width, double Height)> MediaBoxes(byte[] pdf)
    {
        var text = Encoding.Latin1.GetString(pdf);
        return [.. MediaBoxPattern().Matches(text).Select(m => (
            double.Parse(m.Groups[3].Value, CultureInfo.InvariantCulture) - double.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture),
            double.Parse(m.Groups[4].Value, CultureInfo.InvariantCulture) - double.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture)))];
    }

    // The page content streams of a Chrome PDF are FlateDecode (zlib). Decompress each stream that inflates.
    private static string PageContent(byte[] pdf)
    {
        var text = Encoding.Latin1.GetString(pdf);
        var content = new StringBuilder();
        foreach (Match m in StreamPattern().Matches(text))
        {
            var start = m.Index + m.Length;
            var end = text.IndexOf("endstream", start, StringComparison.Ordinal);
            if (end < 0)
            {
                continue;
            }

            try
            {
                using var zlib = new ZLibStream(new MemoryStream(pdf, start, end - start), CompressionMode.Decompress);
                using var reader = new StreamReader(zlib, Encoding.Latin1);
                content.Append(reader.ReadToEnd());
            }
            catch (InvalidDataException)
            {
                // Not a zlib stream (an image or a font): skip it.
            }
        }

        return content.ToString();
    }

    [GeneratedRegex(@"/MediaBox\s*\[\s*([-0-9.]+)\s+([-0-9.]+)\s+([-0-9.]+)\s+([-0-9.]+)\s*\]")]
    private static partial Regex MediaBoxPattern();

    [GeneratedRegex(@"stream\r?\n")]
    private static partial Regex StreamPattern();

    private sealed class ProbeFactory(params (string Key, string Value)[] settings) : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseSetting("ConnectionStrings:Orders", AssemblyFixture.ConnectionString);
            foreach (var (key, value) in settings)
            {
                builder.UseSetting(key, value);
            }
        }
    }

    // Accepts TCP connections on a free loopback port and counts them. For a plain HTTP request it answers
    // with a 1x1 PNG; a TLS hello gets no answer and the socket closes after 2 seconds.
    private sealed class CountingListener : IAsyncDisposable
    {
        private static readonly byte[] s_png = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==");
        private readonly TcpListener _listener;
        private readonly CancellationTokenSource _stop = new();
        private readonly Task _loop;
        private int _connections;

        private CountingListener()
        {
            _listener = new TcpListener(IPAddress.Loopback, 0);
            _listener.Start();
            _loop = Task.Run(AcceptAsync);
        }

        public int Port => ((IPEndPoint)_listener.LocalEndpoint).Port;

        public int Connections => Volatile.Read(ref _connections);

        public static CountingListener Start() => new();

        public async ValueTask DisposeAsync()
        {
            await _stop.CancelAsync();
            _listener.Stop();
            try
            {
                await _loop;
            }
            catch (OperationCanceledException)
            {
            }

            _stop.Dispose();
        }

        private async Task AcceptAsync()
        {
            while (!_stop.IsCancellationRequested)
            {
                TcpClient socket;
                try
                {
                    socket = await _listener.AcceptTcpClientAsync(_stop.Token);
                }
                catch (Exception e) when (e is OperationCanceledException or SocketException or ObjectDisposedException)
                {
                    return;
                }

                Interlocked.Increment(ref _connections);
                _ = Task.Run(() => AnswerAsync(socket));
            }
        }

        private static async Task AnswerAsync(TcpClient socket)
        {
            using (socket)
            using (var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(2)))
            {
                try
                {
                    var stream = socket.GetStream();
                    var buffer = new byte[4096];
                    var read = await stream.ReadAsync(buffer, timeout.Token);
                    if (read > 3 && buffer[0] == (byte)'G' && buffer[1] == (byte)'E' && buffer[2] == (byte)'T')
                    {
                        var head = Encoding.ASCII.GetBytes($"HTTP/1.1 200 OK\r\nContent-Type: image/png\r\nContent-Length: {s_png.Length}\r\nConnection: close\r\n\r\n");
                        await stream.WriteAsync(head, timeout.Token);
                        await stream.WriteAsync(s_png, timeout.Token);
                    }
                }
                catch (Exception e) when (e is OperationCanceledException or IOException or SocketException)
                {
                }
            }
        }
    }
}
