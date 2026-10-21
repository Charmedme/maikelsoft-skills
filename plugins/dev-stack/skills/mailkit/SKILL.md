---
name: mailkit
description: Outgoing mail code with MailKit 4 and MimeKit 4 in .NET. Use when code or a review sends email over SMTP, for example MailKit SmtpClient, MimeMessage, BodyBuilder, SecureSocketOptions, SMTP settings and the SMTP password, a mail outbox or a background mail sender, retries after a relay failure, mail templates, OAuth2 SMTP for Microsoft 365 or Gmail, the Pkcs audit warning after MailKit is added, or mail tests with Mailpit. Not for reading mail (IMAP, POP3), Microsoft Graph sendMail, the Azure Communication Services Email SDK, or messages between services (dev-stack:rabbitmq, dev-stack:wolverine).
metadata:
  verified: "2026-10-19"
  versions: "MailKit 4.18.1, MimeKit 4.18.1, System.Security.Cryptography.Pkcs 10.0.13, Mailpit v1.31.6, Testcontainers.Mailpit 4.16.0, .NET 10"
---

# MailKit

How to send mail from a .NET service with MailKit and MimeKit: settings, TLS, the send path, the message, failures, and tests. The security rules for mail (header injection, sender spoofing, HTML encoding) belong to `dev-stack:secure-coding`. Rule 13 gives the short form. The outbox table uses EF Core (`dev-stack:efcore`).

## First

1. Read `references/stack-rules.md`.
2. Find the version: `MailKit` and `MimeKit` in `Directory.Packages.props` or `*.csproj`.
   - 4.15.1 to 4.x: use this file.
   - Below 4.15.1: MimeKit has CVE-2026-30227 (SMTP command injection). Tell the human, and upgrade when the task touches mail.
   - 5.x: not released on 2026-10-19. Look up the breaking changes before you write code (stack rule 2).
   - `System.Net.Mail.SmtpClient`: existing code keeps it until a task changes the sender. New code uses MailKit (rule 1).
3. Find the send path: an outbox table, Wolverine (`UseWolverine`), a job library, or a send inside the request.

Done when you can name the MailKit version, the TLS mode and port, and the send path.

## Defaults for new code

Ask the human before you apply a default that has no accepted decision (stack rule 4). The full entries are in `references/decisions.md`.

| Area | Default | ID |
|------|---------|----|
| TLS | `StartTls` on port 587, or `SslOnConnect` on port 465. Never `Auto`. | MAIL-01 |
| Send path | An outbox row in the same `SaveChangesAsync` as the business change. A `BackgroundService` sends. The Wolverine durable local queue when the service uses Wolverine. | MAIL-02 |
| HTML templates | Razor components rendered with `HtmlRenderer`. | MAIL-03 |
| Tests | Mailpit through `Testcontainers.Mailpit`, with a pinned image tag. | MAIL-04 |
| Microsoft 365 | OAuth2 SMTP (`SaslMechanismOAuth2`). | MAIL-05 |
| Settings | Typed options with `ValidateOnStart`. The password from user secrets or the environment. | CFG-01, CFG-02 |

Existing code keeps its style (stack rule 3).

## Rules

### Packages

1. Use MailKit. Microsoft does not recommend `System.Net.Mail.SmtpClient` for new code (DE0005): it has no modern authentication and weak TLS control.
2. Use MailKit and MimeKit 4.15.1 or later. MimeKit 4.18.1 pulls `System.Security.Cryptography.Pkcs` 10.0.0 (NU1902, NU1903), so `dotnet build -warnaserror` fails. Add a direct `PackageVersion` for Pkcs 10.0.13 or later with `CentralPackageTransitivePinningEnabled`. This pin has standing approval (stack rule 5). Name it in the report. Do not hide the warnings with `NoWarn` or `NuGetAudit=false`.

```xml
<!-- MimeKit 4.18.1 pulls Pkcs 10.0.0 (GHSA-2fmv-rv2c-59x7, GHSA-846f-f764-76mx). Transitive pinning lifts it. -->
<PackageVersion Include="System.Security.Cryptography.Pkcs" Version="10.0.13" />
```

### Settings and TLS

3. Set the TLS mode explicitly: `StartTls` on 587, `SslOnConnect` on 465, `None` only for a local test relay. `Auto` and `StartTlsWhenAvailable` continue in plain text when the relay does not offer STARTTLS. Then the password and the mail travel unencrypted. With `StartTls`, MailKit throws `NotSupportedException` before `AUTH`.
4. Keep the certificate check on. Do not set an accept-all `ServerCertificateValidationCallback` (CA5359 finds it). Install `ca-certificates` in a Linux image. A wrong port and mode pair is the most common cause of `SslHandshakeException`.
5. Bind one options class with `ValidateOnStart`. Read the password from user secrets or the environment (`Smtp__Password`), never from `appsettings.json`.

```csharp
public sealed class SmtpOptions
{
    public const string SectionName = "Smtp";

    [Required]
    public string Host { get; set; } = string.Empty;

    [Range(1, 65535)]
    public int Port { get; set; } = 587;

    // StartTls on 587, SslOnConnect on 465. None only for a local test relay.
    [AllowedValues(SecureSocketOptions.StartTls, SecureSocketOptions.SslOnConnect, SecureSocketOptions.None)]
    public SecureSocketOptions Security { get; set; } = SecureSocketOptions.StartTls;

    public string? UserName { get; set; }

    public string? Password { get; set; } // user secrets or Smtp__Password

    [Required, EmailAddress]
    public string From { get; set; } = string.Empty;

    [Range(1000, 300000)]
    public int TimeoutMs { get; set; } = 30000;
}
```

```csharp
builder.Services.AddOptions<SmtpOptions>()
    .BindConfiguration(SmtpOptions.SectionName)
    .ValidateDataAnnotations()
    .Validate(
        smtp => smtp.Security != SecureSocketOptions.None || smtp.Host is "localhost" or "127.0.0.1" or "::1",
        "Smtp:Security None is only for a local test relay.")
    .ValidateOnStart();
```

### Send path

6. Do not send mail inside the HTTP request or the business transaction. Add an outbox row in the same `SaveChangesAsync` as the business change, and send from a `BackgroundService` (MAIL-02). Then a slow or down relay does not fail the request. And when the database rolls back the change, no mail goes out. Do not use `Task.Run` or a fire-and-forget call. The code loses the mail when the relay is down or the process stops.
7. Retry with a cap, for example 5 s, 10 s, 20 s, then every 30 s. Give up after one day, with an alert. A long backoff delays all mail for a long time after the relay is back.
8. Expect a mail more than one time. A crash after the send and before the save of `SentAt` sends it again. With more than one replica, claim the rows (`FOR UPDATE SKIP LOCKED` in `dev-stack:postgresql`, `READPAST` in `dev-stack:sqlserver`) or run one sender.

```csharp
db.Orders.Add(order); // the EF provider sets order.Id here
db.OutboxMails.Add(await confirmation.CreateAsync(order, request.CustomerName, request.Email));
await db.SaveChangesAsync(ct); // order and mail in one transaction
```

The full outbox entity and sender are in `references/outbox-sender.md`.

### Connection

9. Create one `SmtpClient` for each batch: connect, authenticate, send the batch, `DisconnectAsync(true, ct)`, dispose. The client is not thread-safe, so do not register it in DI. `Dispose` alone does not send `QUIT`.
10. Set `client.Timeout` (the default is 120000 ms for each step) and pass the `CancellationToken` to each call.

```csharp
using var client = new SmtpClient { Timeout = smtp.TimeoutMs };
await client.ConnectAsync(smtp.Host, smtp.Port, smtp.Security, ct);
if (!string.IsNullOrEmpty(smtp.UserName))
{
    await client.AuthenticateAsync(smtp.UserName, smtp.Password ?? string.Empty, ct);
}

foreach (var mail in due)
{
    using var message = ToMessage(mail, smtp); // MimeMessage is IDisposable
    await client.SendAsync(message, ct);       // rule 11: catch SmtpCommandException for each message
    mail.MarkSent(time.GetUtcNow());
}

await client.DisconnectAsync(true, ct);
```

### Failures

11. Classify each failure. The outbox row gets the result.

| Exception | Meaning | Action |
|-----------|---------|--------|
| `SmtpCommandException`, `StatusCode` 4xx | Greylisting, mailbox full, rate limit | Retry the message later |
| `SmtpCommandException`, `StatusCode` 5xx | The relay refuses the message or the address | Mark the row failed. No retry |
| `SocketException`, `IOException`, `SmtpProtocolException`, `ServiceNotConnectedException`, `SslHandshakeException` | Relay down or connection broken | Retry the unsent rows later |
| `AuthenticationException`, `NotSupportedException` | Wrong credentials, or no STARTTLS | Retry later and log an error: a person must act |

12. Send one message for each recipient when the recipients are not one group. One rejected recipient throws `SmtpCommandException` (`RecipientNotAccepted`), and MailKit then sends the message to nobody.

### Message

13. Treat user data as untrusted (summary of `dev-stack:secure-coding`, file `references/integrations.md` there):
    - Reject CR and LF in an address, then use `MailboxAddress.TryParse`. It returns `false` for a list of addresses.
    - Use the typed setters (`Subject`, `To`, `ReplyTo`). Do not add raw headers from user input.
    - Send `From` from your own domain. Put the user address in `ReplyTo`.
    - HTML-encode each user value. A Razor component does this (MAIL-03, `references/templates.md`).
    - Do not log bodies or recipient lists at Information level. Log the outbox ID and the SMTP status.
14. Build the body with `BodyBuilder`, with `TextBody` and `HtmlBody`. Give an attachment a file name that you control. Keep a blob reference in the outbox row, not a large attachment.

### Microsoft 365 and Gmail

15. Use OAuth2: an MSAL client credentials token for `https://outlook.office365.com/.default`, then `AuthenticateAsync(new SaslMechanismOAuth2(mailbox, token), ct)`. Microsoft is retiring basic authentication for SMTP AUTH. Read `references/microsoft365.md` first.

### Tests

16. Test the message builder as a plain unit. Use a fake sender port in business tests. Test the sender against Mailpit (MAIL-04). Do not mock `SmtpClient`. Do not use MailHog: it is not maintained.

```csharp
s_mailpit = new MailpitBuilder("axllent/mailpit:v1.31.6")
    .WithSmtpAuthCredentials(new NetworkCredential("orders", "test-password"), allowInsecure: true)
    .Build();
await s_mailpit.StartAsync(context.CancellationToken);
// SMTP: s_mailpit.Hostname and s_mailpit.SmtpPort. API: s_mailpit.GetWebAddress() + "/api/v1/search?query=to:..."
```

The full test and a fake relay for the TLS test are in `references/testing.md`.

## Never write

| Wrong | Correct |
|-------|---------|
| `new System.Net.Mail.SmtpClient(host) { EnableSsl = true }` | `MailKit.Net.Smtp.SmtpClient` with `ConnectAsync(host, 587, SecureSocketOptions.StartTls, ct)` |
| `SecureSocketOptions.Auto` or `StartTlsWhenAvailable` | `StartTls` (587) or `SslOnConnect` (465) |
| `client.ServerCertificateValidationCallback = (s, c, h, e) => true;` | The default check and a CA bundle in the image |
| `services.AddSingleton<SmtpClient>()` | `using var client = new SmtpClient();` for each batch, inside the sender |
| `_ = Task.Run(() => sender.SendAsync(mail));` in an endpoint | An outbox row and a `BackgroundService` |
| `await mail.SendAsync(...)` after `SaveChangesAsync` in the endpoint | The outbox row in the same `SaveChangesAsync` |
| `await using var client = new SmtpClient();`, `SendResult r = await client.SendAsync(...)` | MailKit 4: `using var client`, and `string response = await client.SendAsync(message, ct)` |
| `<NoWarn>NU1903</NoWarn>` after you add MailKit | `<PackageVersion Include="System.Security.Cryptography.Pkcs" Version="10.0.13" />` |
| `HtmlBody = $"<p>Hello {name}</p>"` | A Razor component, or `HtmlEncoder.Default.Encode(name)` |
| `message.From.Add(MailboxAddress.Parse(form.Email))` | `From` from your own domain, `message.ReplyTo.Add(address)` |
| `message.Headers.Add("Subject", input)` | `message.Subject = input;` |
| One message with all customers in `To` | One message for each recipient |
| `image: mailhog/mailhog` | `image: axllent/mailpit:v1.31.6`, or Testcontainers.Mailpit |
| `new MailpitBuilder().Build()` | `new MailpitBuilder("axllent/mailpit:v1.31.6").Build()` (the parameterless constructor is obsolete) |

## Check

1. Build with the NuGet audit on:

   ```
   dotnet build -warnaserror
   ```

   Pass: `Build succeeded` with 0 warnings. NU1902 and NU1903 count.

2. List vulnerable packages, transitive ones too:

   ```
   dotnet package list --vulnerable --include-transitive
   ```

   Pass: no vulnerable package.

3. Run the rules of this skill:

   ```
   python3 <skill folder>/scripts/stackcheck.py --root <repo>
   ```

   Pass: 0 errors. For each warning, fix it or add `// stackcheck: ignore <RULE-ID> <reason>`.

4. With `Microsoft.CodeAnalysis.BannedApiAnalyzers` (`dev-stack:dotnet-analyzers`): `T:System.Net.Mail.SmtpClient` in `BannedSymbols.txt` gives RS0030.
5. Run the tests. Pass: all tests green. Also, one test or more sends a mail through Mailpit (`Testcontainers.Mailpit`, pinned tag) and checks the captured mail. When no such test exists, write it before the report.

## Review checklist

The full list is in `references/review-checklist.md`.

1. MailKit and MimeKit 4.15.1 or later, Pkcs pinned, no audit warning hidden. tool (MAIL-PKG-MIMEKIT-CVE, NU1902, NU1903, SC-NUGETAUDIT-OFF)
2. No `System.Net.Mail.SmtpClient` in new code. tool (MAIL-SYSTEM-NET-MAIL, RS0030)
3. TLS mode `StartTls` or `SslOnConnect`, never `Auto` or `StartTlsWhenAvailable`. tool (MAIL-TLS-DOWNGRADE)
4. No accept-all certificate callback, and no suppressed CA5359. tool (MAIL-CERT-BYPASS, CA5359)
5. No `SmtpClient` in DI. One client for each batch, with `DisconnectAsync(true, ct)`. tool (MAIL-SHARED-CLIENT), judge
6. The SMTP password comes from secrets, not from `appsettings.json`. tool (SC-APPSETTINGS-SECRET), judge
7. No send inside the request. The outbox row is in the same `SaveChangesAsync` as the business change. judge
8. A timeout and the `CancellationToken` on each SMTP call. judge
9. 4xx retries, 5xx fails the row, connection failures retry, authentication failures alert. judge
10. One message for each recipient when the recipients are not one group. judge
11. CR and LF rejected, `MailboxAddress.TryParse`, `From` from your own domain, user value in `ReplyTo`. judge
12. The HTML body encodes each user value. judge
13. No bodies or recipient lists in logs at Information level. judge
14. The sender test runs against Mailpit with a pinned tag. No MailHog, no mocked `SmtpClient`. tool (MAIL-MAILHOG), judge

## Report

End with the report of stack rule 8 (`references/stack-rules.md`): the same headings in the same order on each run.

## References

- `references/decisions.md`: read before new code needs a MAIL decision. It has the question, the options, and the recommendation.
- `references/outbox-sender.md`: read when you write the outbox table, the background sender, the retry plan, or the claim of rows with more than one replica.
- `references/templates.md`: read when you write the text or HTML body, a Razor template, or an attachment.
- `references/testing.md`: read before you write a mail test (builder unit test, Mailpit, fake relay).
- `references/microsoft365.md`: read when the relay is Microsoft 365 or Gmail, or when the team asks about Graph or Azure Communication Services.
- `references/review-checklist.md`: read for a full review.
- `references/sources.md`: the primary sources and the credits.
- `references/stack-rules.md` and `references/language.md`: the common rules and the output language.
- `scripts/stackcheck.py`: the rule check of this skill.
