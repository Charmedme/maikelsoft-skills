# Integrations: Valkey and Redis, RabbitMQ, Elasticsearch, MailKit, PuppeteerSharp

The security part of each integration. Client use is in the stack skills (`dev-stack:redis`, `dev-stack:rabbitmq`, `dev-stack:elasticsearch`, `dev-stack:mailkit`, `dev-stack:puppeteersharp`). The C# code was compiled with MailKit 4.18.1, PuppeteerSharp 25.14.0 and StackExchange.Redis 3.4.5.

## Contents

- Valkey and Redis
- RabbitMQ
- Elasticsearch
- MailKit
- PuppeteerSharp with untrusted HTML

## Valkey and Redis

- A fresh Valkey has `user default on nopass ~* &* +@all`: no password and all rights. Create ACL users with a password and the least rights. Turn off `default` or give it a password. Turn on TLS. Keep the port inside the cluster.
- Put the tenant in each cache key. A key without the tenant leaks data between tenants on a cache hit.

  ```csharp
  public sealed class TenantCache(IConnectionMultiplexer redis, ITenantContext tenant)
  {
      // The tenant is part of each key, so one tenant cannot read the entry of another.
      private string Key(string name) => $"t:{tenant.TenantId:N}:{name}";

      public async Task<ShipmentResponse?> GetAsync(Guid shipmentId)
      {
          var value = await redis.GetDatabase().StringGetAsync(Key($"shipment:{shipmentId:N}"));
          return value.IsNullOrEmpty ? null : System.Text.Json.JsonSerializer.Deserialize<ShipmentResponse>((string)value!);
      }
  }
  ```

- Serialize cached values with System.Text.Json and a fixed type. A value in the cache is input: an attacker with cache access must not get code execution through a type name.
- Keep the connection string with the password in the secret store.
- Data Protection keys in Valkey (SEC-20 option C) need persistence and backups.

## RabbitMQ

- The `guest` user works only from localhost by default. Do not use it in production, and do not set `loopback_users` to none.
- One user and one vhost per service, with permissions on only its own exchanges and queues (SEC-22).
- TLS between the services and the broker. Credentials from the secret store.
- Each message is untrusted input:
  - Validate the schema like a request body.
  - Check the tenant in the message again. Do not trust it because it came from inside.
  - Deserialize with System.Text.Json and `[JsonDerivedType]`, never with `TypeNameHandling`.
- Do not put secrets or full personal records in messages that many consumers read.

## Elasticsearch

- Security is on by default since Elasticsearch 8.0. Keep it on.
- Use an API key with rights on only the indexes of the service.
- Add the tenant filter to each query, vector search too. Put it in a `filter` clause that the caller cannot change.
- Do not pass raw user input to `query_string` or `simple_query_string`. They have their own query language, with field access and costly wildcards. Use `match`, `term` or `multi_match` with fixed fields.
- Cap `size` and `from`. Deep paging is a denial of service.

## MailKit

```csharp
public sealed class SmtpOptions
{
    public required string Host { get; init; }
    public int Port { get; init; } = 587;
    public SecureSocketOptions Security { get; init; } = SecureSocketOptions.StartTls; // never Auto in production
    public required string UserName { get; init; }
    public required string Password { get; init; } // from the secret store, not from appsettings.json
}

public async Task SendStatusMailAsync(string recipient, string customerName, string reference, CancellationToken ct)
{
    // Reject CR and LF before parsing an address from user data (header injection).
    if (recipient.AsSpan().IndexOfAny('\r', '\n') >= 0 || !MailboxAddress.TryParse(recipient, out var to))
    {
        throw new ArgumentException("The recipient address is not valid.", nameof(recipient));
    }

    var message = new MimeMessage();
    message.From.Add(new MailboxAddress("Shipment service", "no-reply@example.com")); // a domain we control
    message.To.Add(to);
    message.Subject = $"Shipment {reference}"; // typed setter, not a raw header

    var html = HtmlEncoder.Default;
    var body = new BodyBuilder
    {
        TextBody = $"Dear {customerName}, shipment {reference} has a new status.",
        HtmlBody = $"<p>Dear {html.Encode(customerName)}, shipment {html.Encode(reference)} has a new status.</p>",
    };
    message.Body = body.ToMessageBody();

    using var client = new SmtpClient(); // default certificate validation stays on
    await client.ConnectAsync(options.Host, options.Port, options.Security, ct);
    await client.AuthenticateAsync(options.UserName, options.Password, ct);
    await client.SendAsync(message, ct);
    await client.DisconnectAsync(true, ct);
}
```

- TLS mode: `SslOnConnect` on port 465, `StartTls` on port 587. `Auto` maps port 587 to `StartTlsWhenAvailable`. That mode continues in plain text when the server does not offer STARTTLS. `None` is for a local test server only.
- Do not set `ServerCertificateValidationCallback` to accept everything. Install `ca-certificates` in the container.
- MimeKit 4.15.0 and older: CR and LF in a quoted local part allowed SMTP command injection (CVE-2026-30227, GHSA-g7hc-96xr-gvvx). Use 4.15.1 or later, and reject CR and LF in addresses from users.
- Use typed setters (`Subject`, `From`, `To`, `ReplyTo`), not `Headers.Add` with raw user input.
- Do not let users choose `From`. Put the user address in `ReplyTo`. Else the app becomes a spoofing relay, and SPF, DKIM and DMARC fail.
- HTML-encode each user value in an HTML body. Use a template engine that encodes by default. `HtmlToHtml.FilterHtml` is not an XSS filter (obsolete since MimeKit 4.17.0).
- Take recipients from trusted data (the account), not from a free field, unless the feature is "send to this address" with a rate limit.
- Do not log message bodies or recipients at Information level. Log a message id and the SMTP status.
- Microsoft 365 no longer accepts basic authentication for SMTP AUTH. Use OAuth2 (`SaslMechanismOAuth2`).

## PuppeteerSharp with untrusted HTML

HTML that contains user data is untrusted. A browser that renders it can fetch internal URLs (cloud metadata, internal services), read `file://` paths in the container, and run scripts.

```csharp
public static async Task<byte[]> RenderAsync(IBrowser browser, string htmlWithEncodedValues, CancellationToken ct)
{
    await using var context = await browser.CreateBrowserContextAsync();
    await using var page = await context.NewPageAsync();

    await page.SetJavaScriptEnabledAsync(false);
    await page.SetRequestInterceptionAsync(true);
    page.Request += async (_, e) =>
    {
        var url = e.Request.Url;
        if (url.StartsWith("data:", StringComparison.Ordinal) || url == "about:blank")
        {
            await e.Request.ContinueAsync();
        }
        else
        {
            await e.Request.AbortAsync();
        }
    };

    await page.SetContentAsync(htmlWithEncodedValues, new SetContentOptions { Timeout = 15_000 });
    return await page.PdfDataAsync(new PdfOptions { Format = PuppeteerSharp.Media.PaperFormat.A4 });
}
```

- Render your own template with encoded values. Inline the assets (CSS, fonts, images as `data:` URIs). Then block all network.
- Chrome 149 and later: `LaunchOptions.Allowlist` (URL patterns) makes Chrome itself fail other requests. It works with CDP only.
- When the page must load remote assets, allow only your asset host, and block private ranges with a container egress policy too. A URL string check alone fails against DNS rebinding and redirects.
- Disable JavaScript when the document does not need it.
- Never pass a user URL to `GoToAsync` without an allowlist. Never allow `file://` or `chrome://`.
- PuppeteerSharp does not add `--no-sandbox` by default. Keep it that way: run Chromium as a non-root user with the sandbox on. Accept `--no-sandbox` only for fully trusted templates, with an ADR, a non-root user, a read-only file system, no mounted secrets and restricted egress.
- At scale, render in a separate worker container with minimal rights, not in the web app process.
- `IPage.SetContentAsync(string, NavigationOptions)` is obsolete in 25.14. Use `SetContentOptions`.
