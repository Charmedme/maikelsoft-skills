using System.Collections.Concurrent;
using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using MimeKit;
using Orders.Api.Data;

namespace Orders.Api.Tests.EvalProbes;

// Eval probe for mailkit-hard. It dispatches shipments through the public API and reads what reaches
// a fake SMTP relay on a local port. The relay offers AUTH but not STARTTLS, and it rejects each
// recipient that starts with "reject-" with 550 5.1.1. The tests that need a delivered mail set
// Smtp:Security to None on 127.0.0.1 (the README allows that for a local test relay).
[TestClass]
[DoNotParallelize]
public sealed class ShippingNoticeProbe
{
    private const string Password = "eval-smtp-password-5d02";
    private static readonly TimeSpan s_deliveryWait = TimeSpan.FromSeconds(40);

    public TestContext TestContext { get; set; } = null!;

    [ClassInitialize]
    public static async Task MigrateAsync(TestContext context)
    {
        // Migrate before an app host starts, so that a background sender never sees a missing table.
        var options = new DbContextOptionsBuilder<OrdersDbContext>().UseNpgsql(AssemblyFixture.ConnectionString).Options;
        await using var db = new OrdersDbContext(options);
        await db.Database.MigrateAsync(context.CancellationToken);
    }

    [TestMethod]
    public async Task Dispatch_SendsShippingNotice()
    {
        await using var smtp = FakeSmtp.Start();
        await using var factory = Factory(smtp.Port, "None");
        using var client = factory.CreateClient();
        var email = NewEmail("buyer");
        var tracking = NewTracking();

        await DispatchAsync(client, email, "Please ring twice.", tracking, notificationAddresses: null);

        using var message = await smtp.WaitForMessageAsync(email, s_deliveryWait);
        Assert.IsNotNull(message, $"No mail for {email} reached the relay within {s_deliveryWait.TotalSeconds} s. Relay log: {smtp.Summary}");
        Assert.Contains("on its way", message.Subject ?? string.Empty, StringComparison.OrdinalIgnoreCase, "The subject is not the shipping notice subject.");
        Assert.IsFalse(message.From.Mailboxes.Any(m => m.Address == email), "The From address is the customer address.");
        Assert.IsFalse(string.IsNullOrWhiteSpace(message.TextBody), "The mail has no text body.");
        var html = message.HtmlBody ?? string.Empty;
        Assert.Contains(tracking, html, "The HTML body does not contain the tracking number or link.");
        Assert.Contains("SKU-PROBE-1", html, "The HTML body does not list the order line.");
        Assert.Contains("Please ring twice.", html, "The HTML body does not contain the delivery instructions.");
    }

    [TestMethod]
    public async Task Dispatch_RelayDown_ShipmentDispatched_MailSentWhenRelayUp()
    {
        var port = FreePort();
        await using var factory = Factory(port, "None");
        using var client = factory.CreateClient();
        var email = NewEmail("patient");

        await DispatchAsync(client, email, null, NewTracking(), notificationAddresses: null);

        await Task.Delay(TimeSpan.FromSeconds(4), TestContext.CancellationToken);
        await using var smtp = FakeSmtp.Start(port);
        var wait = TimeSpan.FromSeconds(75);
        using var message = await smtp.WaitForMessageAsync(email, wait);
        Assert.IsNotNull(message, $"The notice for {email} did not reach the relay within {wait.TotalSeconds} s after the relay came back. Relay log: {smtp.Summary}");
    }

    [TestMethod]
    public async Task Dispatch_OneAddressRejected_OthersGetNotice()
    {
        await using var smtp = FakeSmtp.Start();
        await using var factory = Factory(smtp.Port, "None");
        using var client = factory.CreateClient();
        var email = NewEmail("owner");
        var goodsIn = NewEmail("goods-in");
        var rejected = NewEmail("reject-old-team");

        await DispatchAsync(client, email, null, NewTracking(), notificationAddresses: [rejected, goodsIn]);

        using var toCustomer = await smtp.WaitForMessageAsync(email, s_deliveryWait);
        using var toGoodsIn = await smtp.WaitForMessageAsync(goodsIn, TimeSpan.FromSeconds(5));
        Assert.IsNotNull(toCustomer, $"The customer got no notice because one notification address was rejected. Relay log: {smtp.Summary}");
        Assert.IsNotNull(toGoodsIn, $"The goods-in address got no notice because another notification address was rejected. Relay log: {smtp.Summary}");
    }

    [TestMethod]
    public async Task Instructions_AreHtmlEncoded()
    {
        await using var smtp = FakeSmtp.Start();
        await using var factory = Factory(smtp.Port, "None");
        using var client = factory.CreateClient();
        var email = NewEmail("encoded");

        await DispatchAsync(client, email, "<b>Back door</b> & gate", NewTracking(), notificationAddresses: null);

        using var message = await smtp.WaitForMessageAsync(email, s_deliveryWait);
        Assert.IsNotNull(message, $"No mail for {email} reached the relay within {s_deliveryWait.TotalSeconds} s. Relay log: {smtp.Summary}");
        var html = message.HtmlBody ?? string.Empty;
        Assert.DoesNotContain("<b>Back door</b>", html, "The delivery instructions are in the HTML body without encoding.");
        Assert.Contains("&lt;b&gt;Back door&lt;/b&gt;", html, "The HTML body does not contain the encoded delivery instructions.");
    }

    [TestMethod]
    public async Task RelayWithoutStartTls_GetsNoPassword()
    {
        // No Smtp:Security override: the production setting of the app applies.
        await using var smtp = FakeSmtp.Start();
        await using var factory = Factory(smtp.Port, security: null);
        using var client = factory.CreateClient();

        await DispatchAsync(client, NewEmail("tls"), null, NewTracking(), notificationAddresses: null);
        await smtp.WaitForGreetingAsync(TimeSpan.FromSeconds(40));
        await Task.Delay(TimeSpan.FromSeconds(2), TestContext.CancellationToken);

        var lines = smtp.Lines;
        Assert.IsTrue(lines.Any(line => line.StartsWith("EHLO", StringComparison.OrdinalIgnoreCase)), "The app did not connect to the relay within 40 seconds.");
        Assert.IsFalse(lines.Any(line => line.StartsWith("AUTH", StringComparison.OrdinalIgnoreCase)), "The app sent AUTH over a connection without TLS.");
        Assert.IsFalse(lines.Any(line => line.StartsWith("MAIL FROM", StringComparison.OrdinalIgnoreCase)), "The app sent the mail over a connection without TLS.");
    }

    [TestMethod]
    public async Task NotificationAddress_WithCrLf_NeverReachesRelay()
    {
        await using var smtp = FakeSmtp.Start();
        await using var factory = Factory(smtp.Port, "None");
        using var client = factory.CreateClient();
        var marker = Guid.CreateVersion7().ToString("N");
        var hostile = $"\"x\r\nRCPT TO:<spy-{marker}@evil.test>\"@example.com";

        await DispatchAsync(client, NewEmail("crlf"), null, NewTracking(), notificationAddresses: [hostile], requireAddressesAccepted: false);
        await Task.Delay(TimeSpan.FromSeconds(12), TestContext.CancellationToken);

        Assert.IsFalse(smtp.Lines.Any(line => line.Contains(marker, StringComparison.Ordinal)), "An address with CR LF injected an SMTP command: " + smtp.Summary);
    }

    private static string NewEmail(string prefix) => $"{prefix}-{Guid.CreateVersion7():N}@example.com";

    private static string NewTracking() => "3SPROBE" + Guid.CreateVersion7().ToString("N")[^10..].ToUpperInvariant();

    private static int FreePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    private static WebApplicationFactory<Program> Factory(int smtpPort, string? security) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Production");
            builder.UseSetting("ConnectionStrings:Orders", AssemblyFixture.ConnectionString);
            builder.UseSetting("Smtp:Host", "127.0.0.1");
            builder.UseSetting("Smtp:Port", smtpPort.ToString(CultureInfo.InvariantCulture));
            builder.UseSetting("Smtp:UserName", "orders");
            builder.UseSetting("Smtp:Password", Password);
            if (security is not null)
            {
                builder.UseSetting("Smtp:Security", security);
            }
        });

    private async Task DispatchAsync(HttpClient client, string email, string? instructions, string tracking, string[]? notificationAddresses, bool requireAddressesAccepted = true)
    {
        var ct = TestContext.CancellationToken;
        var business = notificationAddresses is not null;
        var customerId = await PostForIdAsync(client, "/customers", new
        {
            name = "Ann Example",
            email,
            kind = business ? 1 : 0,
            companyName = business ? "Example Trading B.V." : null,
        }, ct);

        if (notificationAddresses is not null)
        {
            using var put = await client.PutAsJsonAsync($"/customers/{customerId}/notification-addresses", new { addresses = notificationAddresses }, ct);
            if (requireAddressesAccepted)
            {
                Assert.IsTrue(put.IsSuccessStatusCode, $"PUT notification-addresses failed: {(int)put.StatusCode} {await put.Content.ReadAsStringAsync(ct)}");
            }
        }

        var orderId = await PostForIdAsync(client, "/orders", new
        {
            customerId,
            customerReference = "PO-PROBE",
            deliveryInstructions = instructions,
            lines = new[] { new { sku = "SKU-PROBE-1", quantity = 2, unitPrice = 3.50m } },
        }, ct);
        var shipmentId = await PostForIdAsync(client, "/shipments", new { orderId }, ct);

        using var dispatch = await client.PostAsJsonAsync($"/shipments/{shipmentId}/dispatch", new { carrier = "postnl", trackingNumber = tracking }, ct);
        Assert.AreEqual(HttpStatusCode.OK, dispatch.StatusCode, "The dispatch must succeed: " + await dispatch.Content.ReadAsStringAsync(ct));
    }

    private static async Task<string> PostForIdAsync(HttpClient client, string path, object body, CancellationToken ct)
    {
        using var response = await client.PostAsJsonAsync(path, body, ct);
        var text = await response.Content.ReadAsStringAsync(ct);
        Assert.AreEqual(HttpStatusCode.Created, response.StatusCode, $"POST {path} failed: {text}");
        using var json = JsonDocument.Parse(text);
        return json.RootElement.GetProperty("id").ToString();
    }

    private sealed class FakeSmtp : IAsyncDisposable
    {
        private readonly TcpListener _listener;
        private readonly ConcurrentQueue<string> _lines = new();
        private readonly ConcurrentQueue<(string[] Recipients, string Data)> _messages = new();
        private readonly CancellationTokenSource _stop = new();
        private readonly TaskCompletionSource _greeted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private Task _loop = Task.CompletedTask;

        private FakeSmtp(int port) => _listener = new TcpListener(IPAddress.Loopback, port);

        public int Port => ((IPEndPoint)_listener.LocalEndpoint).Port;

        public IReadOnlyList<string> Lines => [.. _lines];

        public string Summary => string.Join(" | ", _lines.Where(l => !l.StartsWith("AUTH", StringComparison.OrdinalIgnoreCase)).Take(16));

        public static FakeSmtp Start(int port = 0)
        {
            var smtp = new FakeSmtp(port);
            smtp._listener.Start();
            smtp._loop = smtp.AcceptAsync();
            return smtp;
        }

        public async Task WaitForGreetingAsync(TimeSpan wait) => await Task.WhenAny(_greeted.Task, Task.Delay(wait));

        public async Task<MimeMessage?> WaitForMessageAsync(string recipient, TimeSpan wait)
        {
            var end = DateTime.UtcNow + wait;
            while (DateTime.UtcNow < end)
            {
                foreach (var (recipients, data) in _messages)
                {
                    if (recipients.Any(r => r.Contains(recipient, StringComparison.OrdinalIgnoreCase)))
                    {
                        return await MimeMessage.LoadAsync(new MemoryStream(Encoding.UTF8.GetBytes(data)));
                    }
                }

                await Task.Delay(250);
            }

            return null;
        }

        private async Task AcceptAsync()
        {
            try
            {
                while (!_stop.IsCancellationRequested)
                {
                    var socket = await _listener.AcceptTcpClientAsync(_stop.Token);
                    _ = Task.Run(() => ServeAsync(socket));
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (SocketException)
            {
            }
        }

        private async Task ServeAsync(TcpClient socket)
        {
            using var _ = socket;
            var stream = socket.GetStream();
            using var reader = new StreamReader(stream, Encoding.UTF8);
            await using var writer = new StreamWriter(stream, Encoding.ASCII) { NewLine = "\r\n", AutoFlush = true };
            var recipients = new List<string>();
            var data = new StringBuilder();
            var inData = false;
            try
            {
                await writer.WriteLineAsync("220 fake relay ESMTP");
                while (await reader.ReadLineAsync(_stop.Token) is { } line)
                {
                    if (inData)
                    {
                        if (line == ".")
                        {
                            inData = false;
                            _messages.Enqueue(([.. recipients], data.ToString()));
                            recipients.Clear();
                            data.Clear();
                            await writer.WriteLineAsync("250 queued");
                        }
                        else
                        {
                            data.Append(line.StartsWith("..", StringComparison.Ordinal) ? line[1..] : line).Append("\r\n");
                        }

                        continue;
                    }

                    _lines.Enqueue(line);
                    var upper = line.ToUpperInvariant();
                    if (upper.StartsWith("EHLO", StringComparison.Ordinal) || upper.StartsWith("HELO", StringComparison.Ordinal))
                    {
                        _greeted.TrySetResult();
                        await writer.WriteLineAsync("250-fake relay");
                        await writer.WriteLineAsync("250-8BITMIME");
                        await writer.WriteLineAsync("250 AUTH PLAIN LOGIN");
                    }
                    else if (upper.StartsWith("AUTH", StringComparison.Ordinal))
                    {
                        await writer.WriteLineAsync("235 accepted");
                    }
                    else if (upper.StartsWith("RCPT TO", StringComparison.Ordinal))
                    {
                        if (upper.Contains("<REJECT-", StringComparison.Ordinal))
                        {
                            await writer.WriteLineAsync("550 5.1.1 mailbox unavailable");
                        }
                        else
                        {
                            recipients.Add(line);
                            await writer.WriteLineAsync("250 ok");
                        }
                    }
                    else if (upper == "DATA")
                    {
                        inData = true;
                        await writer.WriteLineAsync("354 go on");
                    }
                    else if (upper == "RSET")
                    {
                        recipients.Clear();
                        await writer.WriteLineAsync("250 ok");
                    }
                    else if (upper == "QUIT")
                    {
                        await writer.WriteLineAsync("221 bye");
                        return;
                    }
                    else
                    {
                        await writer.WriteLineAsync("250 ok");
                    }
                }
            }
            catch (Exception ex) when (ex is IOException or OperationCanceledException or ObjectDisposedException)
            {
            }
        }

        public async ValueTask DisposeAsync()
        {
            await _stop.CancelAsync();
            _listener.Stop();
            await _loop;
            _stop.Dispose();
        }
    }
}
