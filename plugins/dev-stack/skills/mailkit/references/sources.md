# Sources

Checked on 2026-10-19. The facts of this skill come from these primary sources. Look them up again when `metadata.verified` in `SKILL.md` is older than 6 months.

## Contents

- Versions
- MailKit and MimeKit
- Microsoft
- Mailpit
- Checked by compile and run
- Credits

## Versions

| Item | Version on 2026-10-19 | Source |
|------|-----------------------|--------|
| MailKit | 4.18.1 (2026-09-27), MIT | [nuget.org MailKit](https://www.nuget.org/packages/MailKit) |
| MimeKit | 4.18.1 (2026-09-19), MIT. Depends on System.Security.Cryptography.Pkcs 10.0.0 | [nuget.org MimeKit](https://www.nuget.org/packages/MimeKit) |
| MailKit 5.0 | Not released, also not as a prerelease | [nuget.org MailKit versions](https://www.nuget.org/packages/MailKit#versions-body-tab) |
| System.Security.Cryptography.Pkcs | 10.0.13 (the pin) | [nuget.org](https://www.nuget.org/packages/System.Security.Cryptography.Pkcs) |
| Pkcs 10.0.0 advisories | GHSA-2fmv-rv2c-59x7 (moderate), GHSA-846f-f764-76mx (high) | [GHSA-2fmv-rv2c-59x7](https://github.com/advisories/GHSA-2fmv-rv2c-59x7), [GHSA-846f-f764-76mx](https://github.com/advisories/GHSA-846f-f764-76mx) |
| CVE-2026-30227 | MimeKit 4.15.0 and older. Fixed in 4.15.1 | [GHSA-g7hc-96xr-gvvx](https://github.com/advisories/GHSA-g7hc-96xr-gvvx) |
| Mailpit | v1.31.6 (2026-10-15), MIT | [Docker Hub axllent/mailpit](https://hub.docker.com/r/axllent/mailpit) |
| Testcontainers.Mailpit | 4.16.0 (module since 4.14.0) | [nuget.org](https://www.nuget.org/packages/Testcontainers.Mailpit) |
| Microsoft.Identity.Client | 4.90.0 (compiled against) | [nuget.org](https://www.nuget.org/packages/Microsoft.Identity.Client) |
| Template engines | Scriban 7.5.1 (BSD-2-Clause), Fluid.Core (MIT), Razor.Templating.Core 3.1.0 (MIT), smtp4dev (BSD-3-Clause) | nuget.org package metadata |

## MailKit and MimeKit

- [MailKit FAQ](https://github.com/jstedfast/MailKit/blob/master/FAQ.md): ports and `SecureSocketOptions`, the certificate warning, `SslHandshakeException`, connection reuse.
- [MailKit documentation: SmtpClient](https://mimekit.net/docs/html/T_MailKit_Net_Smtp_SmtpClient.htm): `Timeout`, `ConnectAsync`, `DisconnectAsync`, thread safety.
- [MailKit SecureSocketOptions](https://mimekit.net/docs/html/T_MailKit_Security_SecureSocketOptions.htm): `Auto` and `StartTlsWhenAvailable` fall back to plain text.
- [ExchangeOAuth2.md](https://github.com/jstedfast/MailKit/blob/master/ExchangeOAuth2.md) and [GMailOAuth2.md](https://github.com/jstedfast/MailKit/blob/master/GMailOAuth2.md): the OAuth2 flows with `SaslMechanismOAuth2`.

## Microsoft

- [SmtpClient (System.Net.Mail)](https://learn.microsoft.com/dotnet/api/system.net.mail.smtpclient) and [DE0005](https://github.com/dotnet/platform-compat/blob/master/docs/DE0005.md): not recommended for new code.
- [Authenticate an IMAP, POP or SMTP connection using OAuth](https://learn.microsoft.com/exchange/client-developer/legacy-protocols/how-to-authenticate-an-imap-pop-smtp-application-by-using-oauth): `SMTP.SendAsApp`, `New-ServicePrincipal`, `Add-MailboxPermission`, the scope `https://outlook.office365.com/.default`.
- [SMTP onboarding to App RBAC](https://learn.microsoft.com/exchange/client-developer/legacy-protocols/smtp-app-rbac-onboarding): the management role instead of mailbox permissions.
- [Enable or disable SMTP AUTH in Exchange Online](https://learn.microsoft.com/exchange/clients-and-mobile-in-exchange-online/authenticated-client-smtp-submission)
- [Deprecation of basic authentication in Exchange Online](https://learn.microsoft.com/exchange/clients-and-mobile-in-exchange-online/deprecation-of-basic-authentication-exchange-online): SMTP AUTH basic authentication is being retired. The final date was not firm on 2026-10-19.
- [Render Razor components outside of ASP.NET Core](https://learn.microsoft.com/aspnet/core/blazor/components/render-components-outside-of-aspnetcore): `HtmlRenderer`.
- [NuGet audit](https://learn.microsoft.com/nuget/concepts/auditing-packages) and [Central package management, transitive pinning](https://learn.microsoft.com/nuget/consume-packages/central-package-management#transitive-pinning).

## Mailpit

- [Mailpit API v1](https://mailpit.axllent.org/docs/api-v1/): `/api/v1/search`, `/api/v1/message/{ID}`.
- [Testcontainers for .NET, Mailpit module](https://dotnet.testcontainers.org/modules/mailpit/): `MailpitBuilder(string image)`, `SmtpPort`, `GetWebAddress()`, `WithSmtpAuthCredentials`.

## Checked by compile and run

These facts were compiled or run with .NET SDK 10.0.402 on 2026-10-19:

- MailKit 4.18.1 without a Pkcs pin: `dotnet build -warnaserror` fails with NU1902 and NU1903. With Pkcs 10.0.13 in `Directory.Packages.props` and transitive pinning: 0 warnings.
- `SecureSocketOptions.Auto` against a relay without STARTTLS: `AUTH` and `MAIL FROM` in plain text. `StartTls`: no `AUTH`, the connect throws.
- One rejected recipient (`550`): `SmtpCommandException` with `RecipientNotAccepted`, and no `DATA`.
- `SmtpClient.Timeout` default 120000 ms. `CheckCertificateRevocation` default `true`.
- `MimeMessage.Subject` removes CR and LF. `MailboxAddress.TryParse` returns `false` for an address list and for an address with CR LF.
- `EmailAddressAttribute` on .NET 10 returns `false` for an address with CR or LF.
- The outbox sender, the Razor template, and the Mailpit test of `outbox-sender.md`, `templates.md` and `testing.md`: build with `-warnaserror` and tests green against Mailpit v1.31.6. The relay outage, the TLS refusal, the HTML encoding and the CR LF refusal pass the eval probe `mailkit-outbox`.
- The OAuth2 code in `microsoft365.md`: compiled, not run against a tenant.

## Credits

- MailKit `FAQ.md`, `ExchangeOAuth2.md`, `GMailOAuth2.md` (jstedfast/MailKit, MIT): the port and mode table, the certificate warning, and the OAuth2 flow, paraphrased.
- Aaronontheweb/dotnet-skills `aspire-mailpit-integration` (MIT): the idea of a Mailpit API client in tests, paraphrased.
- dotnet/platform-compat DE0005 (MIT): the reason for rule 1.
