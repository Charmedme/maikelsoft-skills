# Review checklist

Each check is `tool (<rule ID>)` when a tool finds it, or `judge` when a reviewer must decide. Run the tools first (`SKILL.md`, section Check). In a review, cite the rule number of `SKILL.md` or the rule ID.

## Contents

- Packages
- Settings and TLS
- Send path
- Connection and failures
- Message and security
- Tests
- Decisions

## Packages

| # | Check | Type |
|---|-------|------|
| 1 | MailKit is 4.16.0 or later (CVE-2026-41319). MimeKit is 4.15.1 or later (CVE-2026-30227). | tool (MAIL-PKG-MIMEKIT-CVE) |
| 2 | With MimeKit 4.15.1 to 4.18.1, `System.Security.Cryptography.Pkcs` 10.0.13 or later is pinned with `CentralPackageTransitivePinningEnabled`. The build has no NU1902 or NU1903. | tool (`dotnet build -warnaserror`, `dotnet package list --vulnerable --include-transitive`) |
| 3 | No `NoWarn` for NU190x and no `NuGetAudit=false`. | tool (SC-NUGETAUDIT-OFF), judge |
| 4 | No `System.Net.Mail.SmtpClient` in new code. | tool (MAIL-SYSTEM-NET-MAIL, RS0030) |
| 5 | No MailKit 5 shapes (`await using` on the client, `SendResult`). MailKit 5 is not released. | judge (the build fails) |

## Settings and TLS

| # | Check | Type |
|---|-------|------|
| 6 | The TLS mode is `StartTls` (587) or `SslOnConnect` (465). `None` only for a local test relay, and the options validation checks that. | tool (MAIL-TLS-DOWNGRADE), judge |
| 7 | No accept-all `ServerCertificateValidationCallback`, and no suppressed CA5359. The image has `ca-certificates`. | tool (MAIL-CERT-BYPASS, CA5359), judge |
| 8 | One options class, bound with `ValidateOnStart`. | judge |
| 9 | The SMTP password comes from user secrets, the environment, or a vault. Not in `appsettings.json`, not in code. | tool (SC-APPSETTINGS-SECRET), judge |

## Send path

| # | Check | Type |
|---|-------|------|
| 10 | No send inside the HTTP request, and no `Task.Run` or fire-and-forget send. | judge |
| 11 | The outbox row is added before the same `SaveChangesAsync` as the business change. A rolled-back change sends no mail. | judge |
| 12 | The sender is a `BackgroundService` that catches each error in its loop. One exception must not stop the host. | judge |
| 13 | The retry delay has a cap, and old rows are marked failed with an alert. | judge |
| 14 | With more than one replica, the rows are claimed (`SKIP LOCKED`, `READPAST`) or one replica sends. | judge |
| 15 | Sent rows are deleted after a retention period. | judge |

## Connection and failures

| # | Check | Type |
|---|-------|------|
| 16 | No `SmtpClient` in DI. One client for each batch. | tool (MAIL-SHARED-CLIENT) |
| 17 | Each batch ends with `DisconnectAsync(true, ct)`, and the client is disposed. | judge |
| 18 | `client.Timeout` is set. Each SMTP call gets the `CancellationToken`. | judge |
| 19 | A 4xx `SmtpCommandException` retries. A 5xx marks the row failed. Connection errors retry. `AuthenticationException` and `NotSupportedException` log an error. | judge |
| 20 | One message for each recipient when the recipients are not one group (one rejected address stops the whole message). | judge |
| 21 | `MimeMessage` is disposed after the send. | judge |

## Message and security

The source of these checks is `dev-stack:secure-coding` (`references/integrations.md` there).

| # | Check | Type |
|---|-------|------|
| 22 | An address from user input: CR and LF rejected, then `MailboxAddress.TryParse`, one address only. | judge |
| 23 | `From` is an address of your own domain. A user address goes in `ReplyTo`. | judge |
| 24 | Typed setters (`Subject`, `To`, `ReplyTo`). No `Headers.Add` with user input. | judge |
| 25 | Each user value in the HTML body is encoded (Razor component, or `HtmlEncoder`). No `MarkupString` for user data. | judge |
| 26 | A text body and an HTML body. | judge |
| 27 | Attachment names come from the code. Large files are not in the outbox row. | judge |
| 28 | Logs hold the outbox ID and the SMTP status, not bodies or recipient lists at Information level. | judge |
| 29 | Microsoft 365 and Gmail use OAuth2 (`SaslMechanismOAuth2`), not a password. | judge |

## Tests

| # | Check | Type |
|---|-------|------|
| 30 | The message builder has a unit test with a hostile name. | judge |
| 31 | The sender has a test against Mailpit with a pinned tag (`axllent/mailpit:v1.31.6`), and the test reads the captured mail through the API. | tool (DET-MAIL-04), judge |
| 32 | No MailHog, no mocked `SmtpClient`, no mail to a real mailbox. | tool (MAIL-MAILHOG), judge |
| 33 | The TLS refusal and the retry after a relay outage have a test (a fake relay in test code). | judge |

## Decisions

| # | Check | Type |
|---|-------|------|
| 34 | New code follows the accepted MAIL decisions in `docs/dev-team/decisions.md`. An open decision is asked, or marked assumed. | judge |
