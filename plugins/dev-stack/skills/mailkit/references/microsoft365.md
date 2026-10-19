# Microsoft 365 and Gmail

How to send through Exchange Online or Gmail (decision MAIL-05). The MailKit and MSAL code was compiled with MailKit 4.18.1 and Microsoft.Identity.Client 4.90.0. It did not run against a real tenant.

## Contents

- Why not basic authentication
- OAuth2 SMTP for Microsoft 365
- Gmail
- The alternatives

## Why not basic authentication

Microsoft is retiring basic authentication (user name and password) for SMTP AUTH in Exchange Online. Microsoft moved the dates more than one time. Treat basic authentication as gone for new code. Many tenants also turn off SMTP AUTH for each mailbox. Check that the sending mailbox has SMTP AUTH on, or use Graph.

## OAuth2 SMTP for Microsoft 365

Steps for the tenant admin (once):

1. Register an application in Microsoft Entra ID.
2. Give it the application permission `SMTP.SendAsApp` (Office 365 Exchange Online), and grant admin consent.
3. Register the service principal in Exchange Online (`New-ServicePrincipal`, with the object ID of the enterprise application). Give it `FullAccess` on the sending mailbox only (`Add-MailboxPermission`). Add `SendAs` (`Add-RecipientPermission`) when `From` is another address. RBAC for Applications (a management role `Application SMTP.SendAsApp` with a scope) is the alternative to mailbox permissions.
4. Store the client secret or, better, a certificate in the secret store of the platform (CFG-02).

Code:

```csharp
public static IConfidentialClientApplication CreateMsal(string tenantId, string clientId, string clientSecret) =>
    ConfidentialClientApplicationBuilder.Create(clientId)
        .WithTenantId(tenantId)
        .WithClientSecret(clientSecret)
        .Build();

public static async Task SendWithMicrosoft365Async(IConfidentialClientApplication msal, string mailbox, MimeMessage message, CancellationToken ct)
{
    var token = await msal.AcquireTokenForClient(["https://outlook.office365.com/.default"]).ExecuteAsync(ct);
    using var client = new SmtpClient { Timeout = 30000 };
    await client.ConnectAsync("smtp.office365.com", 587, SecureSocketOptions.StartTls, ct);
    await client.AuthenticateAsync(new SaslMechanismOAuth2(mailbox, token.AccessToken), ct);
    await client.SendAsync(message, ct);
    await client.DisconnectAsync(true, ct);
}
```

- Create the MSAL application once (a singleton). It caches the token until shortly before it expires.
- The scope is `https://outlook.office365.com/.default`, not a Graph scope.
- `mailbox` is the address of the sending mailbox. `From` must be that mailbox, or one that the principal may send as.
- Use `WithCertificate` instead of `WithClientSecret` when the platform can hold a certificate.
- An `AuthenticationException` means a wrong permission or a mailbox without SMTP AUTH. A retry does not help: log an error.

## Gmail

Use OAuth2 for Gmail too. Google does not accept the account password from less secure apps. For a Google Workspace domain, use a service account with domain-wide delegation and the scope `https://mail.google.com/`. Then call `SaslMechanismOAuth2(mailbox, accessToken)` on `smtp.gmail.com`, port 587 with `StartTls` or 465 with `SslOnConnect`. The token code comes from the Google auth library. It is not in this skill: look it up (stack rule 2).

## The alternatives

| Option | When | Note |
|--------|------|------|
| OAuth2 SMTP | The code uses MailKit and the outbox | Only the authentication step changes |
| Microsoft Graph `sendMail` | The tenant turns off SMTP AUTH | Application permission `Mail.Send`. Limit it to the sending mailbox with an application access policy, else the app can send as each mailbox |
| Azure Communication Services Email | High volume, or customer mail from an own domain | A managed service with its own domain verification and SDK. Not SMTP |

With Graph or Azure Communication Services, keep the outbox (MAIL-02). Only the sender changes. These SDKs are not in this skill: look up the current API (stack rule 2).
