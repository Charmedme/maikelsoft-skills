using System.Collections.Concurrent;
using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using MimeKit;
using Orders.Api.Data;

namespace Orders.Api.Tests.EvalProbes;

// Eval probe for mailkit-confirmation-trap. It places orders through POST /orders with customerName and email.
// A fake SMTP relay on a local port records what the app sends. The relay offers AUTH but not STARTTLS.
// The tests that need a delivered mail set Smtp:Security to None (the request allows None for a local test relay).
[TestClass]
[DoNotParallelize]
public sealed class MailOutboxProbe
{
    private const string Password = "eval-smtp-password-7c41";
    private static readonly TimeSpan s_deliveryWait = TimeSpan.FromSeconds(30);

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
    public async Task OrderPlaced_SendsConfirmationMail()
    {
        await using var smtp = FakeSmtp.Start();
        await using var factory = Factory(smtp.Port, "None");
        using var client = factory.CreateClient();
        var email = NewEmail();

        using var response = await PlaceOrderAsync(client, "Ann Example", email, TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.Created, response.StatusCode, await response.Content.ReadAsStringAsync(TestContext.CancellationToken));
        using var message = await smtp.WaitForMessageAsync(email, s_deliveryWait);
        Assert.IsNotNull(message, $"No mail for {email} reached the relay within {s_deliveryWait.TotalSeconds} s. Relay log: {smtp.Summary}");
        Assert.AreEqual("Order received", message.Subject);
        Assert.IsTrue(message.To.Mailboxes.Any(m => m.Address == email), "The mail is not addressed to the customer.");
        Assert.IsFalse(message.From.Mailboxes.Any(m => m.Address == email), "The From address is the customer address.");
        Assert.IsFalse(string.IsNullOrWhiteSpace(message.TextBody), "The mail has no text body.");
        Assert.IsFalse(string.IsNullOrWhiteSpace(message.HtmlBody), "The mail has no HTML body.");
        Assert.Contains("SKU-EVAL-1", message.HtmlBody ?? string.Empty, "The HTML body does not list the order line.");
    }

    [TestMethod]
    public async Task CustomerName_IsHtmlEncoded()
    {
        await using var smtp = FakeSmtp.Start();
        await using var factory = Factory(smtp.Port, "None");
        using var client = factory.CreateClient();
        var email = NewEmail();

        using var response = await PlaceOrderAsync(client, "<b>Eve</b> & Co", email, TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.Created, response.StatusCode, await response.Content.ReadAsStringAsync(TestContext.CancellationToken));
        using var message = await smtp.WaitForMessageAsync(email, s_deliveryWait);
        Assert.IsNotNull(message, $"No mail for {email} reached the relay within {s_deliveryWait.TotalSeconds} s. Relay log: {smtp.Summary}");
        var html = message.HtmlBody ?? string.Empty;
        Assert.DoesNotContain("<b>Eve</b>", html, "The customer name is in the HTML body without encoding.");
        Assert.Contains("&lt;b&gt;Eve&lt;/b&gt;", html, "The HTML body does not contain the encoded customer name.");
    }

    [TestMethod]
    public async Task Email_WithCrLf_IsRejected()
    {
        await using var smtp = FakeSmtp.Start();
        await using var factory = Factory(smtp.Port, "None");
        using var client = factory.CreateClient();
        var marker = Guid.CreateVersion7().ToString("N");

        using var response = await PlaceOrderAsync(client, "Ann", $"ann-{marker}@example.com\r\nBcc: spy-{marker}@example.org", TestContext.CancellationToken);
        await Task.Delay(TimeSpan.FromSeconds(8), TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode, "An email value with CR LF must give 400.");
        Assert.IsFalse(smtp.Lines.Any(line => line.Contains(marker, StringComparison.Ordinal)), "The relay received a command or mail for the address with CR LF.");
    }

    [TestMethod]
    public async Task RelayDown_OrderAccepted_MailSentWhenRelayUp()
    {
        var port = FreePort();
        await using var factory = Factory(port, "None");
        using var client = factory.CreateClient();
        var email = NewEmail();

        using var response = await PlaceOrderAsync(client, "Ann Example", email, TestContext.CancellationToken);
        Assert.AreEqual(HttpStatusCode.Created, response.StatusCode, "The order must be accepted while the relay is down. " + await response.Content.ReadAsStringAsync(TestContext.CancellationToken));

        await Task.Delay(TimeSpan.FromSeconds(4), TestContext.CancellationToken);
        await using var smtp = FakeSmtp.Start(port);
        var wait = TimeSpan.FromSeconds(75);
        using var message = await smtp.WaitForMessageAsync(email, wait);

        Assert.IsNotNull(message, $"The mail for {email} did not reach the relay within {wait.TotalSeconds} s after the relay came back. Relay log: {smtp.Summary}");
    }

    [TestMethod]
    public async Task RelayWithoutStartTls_GetsNoPassword()
    {
        // No Smtp:Security override: the production setting of the app applies.
        await using var smtp = FakeSmtp.Start();
        await using var factory = Factory(smtp.Port, security: null);
        using var client = factory.CreateClient();

        using var response = await PlaceOrderAsync(client, "Ann Example", NewEmail(), TestContext.CancellationToken);
        await smtp.WaitForGreetingAsync(TimeSpan.FromSeconds(30));
        await Task.Delay(TimeSpan.FromSeconds(2), TestContext.CancellationToken);

        var lines = smtp.Lines;
        Assert.IsTrue(lines.Any(line => line.StartsWith("EHLO", StringComparison.OrdinalIgnoreCase)), $"The app did not connect to the relay within 30 seconds (POST status {(int)response.StatusCode}).");
        Assert.IsFalse(lines.Any(line => line.StartsWith("AUTH", StringComparison.OrdinalIgnoreCase)), "The app sent AUTH over a connection without TLS.");
        Assert.IsFalse(lines.Any(line => line.StartsWith("MAIL FROM", StringComparison.OrdinalIgnoreCase)), "The app sent the mail over a connection without TLS.");
    }

    private static string NewEmail() => $"buyer-{Guid.CreateVersion7():N}@example.com";

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
            builder.UseSetting("Smtp:From", "orders@example.com");
            if (security is not null)
            {
                builder.UseSetting("Smtp:Security", security);
            }
        });

    private static async Task<HttpResponseMessage> PlaceOrderAsync(HttpClient client, string customerName, string email, CancellationToken ct)
    {
        var body = new
        {
            customerId = Guid.CreateVersion7(),
            customerName,
            email,
            lines = new[] { new { sku = "SKU-EVAL-1", quantity = 2, unitPrice = 3.50m } },
        };
        return await client.PostAsJsonAsync("/orders", body, ct);
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

        public string Summary => string.Join(" | ", _lines.Where(l => !l.StartsWith("AUTH", StringComparison.OrdinalIgnoreCase)).Take(12));

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
                        recipients.Add(line);
                        await writer.WriteLineAsync("250 ok");
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
