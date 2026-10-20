# Mail tests

How to test mail code at three levels (decision MAIL-04). The test code was compiled and run with MSTest 4.5.1, Testcontainers.Mailpit 4.16.0, Mailpit v1.31.6 and MailKit 4.18.1. Test setup in general is in `dev-stack:dotnet-testing`.

## Contents

- Three levels
- The message builder as a unit
- Business tests with a fake sender
- The sender against Mailpit
- A fake relay for TLS and failure tests
- What not to do

## Three levels

| Level | What it proves | Tool |
|-------|----------------|------|
| Message builder | Subject, recipient, text and HTML content, encoding | Plain unit test, no SMTP |
| Business code | An order writes one outbox row, a failed save writes none | The real database (Testcontainers), no SMTP |
| Sender | The SMTP dialog, TLS mode, retries, the captured mail | Mailpit, or a fake relay in test code |

## The message builder as a unit

Make the builder a class with no SMTP dependency. Then a test calls it and reads the result. Test a hostile name:

```csharp
await using var services = new ServiceCollection().BuildServiceProvider();
var builder = new OrderConfirmationMail(services, NullLoggerFactory.Instance, new FakeTimeProvider());
var order = new Order(Guid.CreateVersion7(), DateTimeOffset.UnixEpoch);
order.AddLine("SKU-7", 2, 4.25m);

var mail = await builder.CreateAsync(order, "<b>Eve</b> & Co", "eve@example.com");

mail.HtmlBody.Should().Contain("&lt;b&gt;Eve&lt;/b&gt; &amp; Co").And.NotContain("<b>Eve</b>");
mail.TextBody.Should().Contain("SKU-7");
```

## Business tests with a fake sender

When the business code calls a sender port (for example `IEmailSender`), give the test a hand-written fake that records the calls. With an outbox, the business test reads the outbox table instead. Both tests run without an SMTP server.

## The sender against Mailpit

Mailpit is a real SMTP server with a REST API. Pin the image tag. The parameterless `MailpitBuilder()` constructor is obsolete (CS0618).

```csharp
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

    [TestMethod]
    public async Task CreateOrder_SendsConfirmationWithEncodedName()
    {
        var email = $"buyer-{Guid.CreateVersion7():N}@example.com";
        var request = new CreateOrderRequest(Guid.CreateVersion7(), [new CreateOrderLineRequest("SKU-7", 2, 4.25m)], "<b>Ann</b>", email);

        var response = await s_client.PostAsJsonAsync("/orders", request, TestContext.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var message = await WaitForMessageAsync(email, TimeSpan.FromSeconds(30));
        message.GetProperty("Subject").GetString().Should().Be("Order received");
        message.GetProperty("HTML").GetString().Should().Contain("&lt;b&gt;Ann&lt;/b&gt;");
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
```

Dispose the factory, the HTTP clients, and the container in `[ClassCleanup]`. The test migrates the database before it starts the app host, so that the background sender does not read a missing table.

Notes:

- `SmtpPort` is the mapped host port. `GetWebAddress()` is the base URL of the web UI and the API.
- Mailpit offers `AUTH` only with `WithSmtpAuthCredentials`. Without it, a sender that calls `AuthenticateAsync` gets `NotSupportedException`. Either set credentials, or let the sender skip `AUTH` when `UserName` is empty.
- Use a unique recipient for each test, and search for it (`/api/v1/search?query=to:...`). Then tests do not read the mails of other tests.
- `/api/v1/message/{ID}` returns `Text`, `HTML`, `Subject`, `From`, `To` and the attachments.
- A test with TLS uses Mailpit with its own certificate. Trust only that certificate, in test code. Do not add an accept-all callback to the production sender.

## A fake relay for TLS and failure tests

Mailpit always answers like a good server. For these cases, use a small `TcpListener` relay in the test project that writes the SMTP replies of the test:

| Case | Fake relay behavior | Expected result |
|------|---------------------|-----------------|
| No STARTTLS | EHLO offers `AUTH PLAIN LOGIN` but not `STARTTLS` | The app sends no `AUTH` and no `MAIL FROM` |
| Relay down, then up | Start the listener a few seconds after the order | The order gets 201. The mail arrives after the relay starts |
| Recipient rejected | `550 5.1.1` on `RCPT TO` | The row is marked failed, the next row is sent |
| Greylisting | `451 4.7.1` on `RCPT TO` | The row is retried later |

The relay reads lines with a `StreamReader`, answers `220`, `250`, `354` and `221`, records each command, and stores the data between `DATA` and `.`. Load the stored data with `MimeMessage.LoadAsync` to check the content.

## What not to do

- Do not mock `SmtpClient` or `ISmtpClient`. A mock proves nothing about TLS, `AUTH`, or the replies.
- Do not use MailHog: it is not maintained.
- Do not send to a real mailbox from a test.
- Do not wait a fixed time for a mail. Poll with a time limit.
