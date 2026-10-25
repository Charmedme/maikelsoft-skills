# Mail decisions

The team decisions that mail code needs. The order is the order in which a new project meets them. The recommendation is the catalog default. Ask the human before you apply it (stack rule 4). Options and secrets (CFG-01, CFG-02) are in `dev-stack:aspnetcore`. The outbox for messages between services (MSG-09) is in `dev-stack:wolverine`.

## Contents

- MAIL-01. SMTP transport security
- MAIL-02. Mail send path
- MAIL-03. Mail templates
- MAIL-04. Mail tests
- MAIL-05. Microsoft 365 mail

## MAIL-01. SMTP transport security

- Question: Which TLS mode and port does the service use to connect to the SMTP relay?
- Ask when: the first code that connects to an SMTP relay.
- Options: `StartTls` on port 587: the connection starts in plain text and must switch to TLS before `AUTH`, else MailKit throws. `SslOnConnect` on port 465: TLS from the first byte (implicit TLS). `Auto` or `StartTlsWhenAvailable`: TLS only when the relay offers STARTTLS, else plain text. `None`: plain text.
- Recommended: `StartTls` on 587, or `SslOnConnect` on 465 when the relay offers that port. Both fail closed. `Auto` sends the password and the mail in plain text when an attacker or a wrong relay removes the STARTTLS offer.
- Choose another option when: a local test relay (Mailpit, a fake server) has no TLS. Then use `None`, only for `localhost`, and check that in the options validation.
- Depends on: none.
- Revisit when: the relay changes, or the provider drops port 587 or 465.
- Check: tool (MAIL-TLS-DOWNGRADE, MAIL-CERT-BYPASS).
- Detect: DET-MAIL-01.

## MAIL-02. Mail send path

- Question: How does a mail leave the service: inside the request, through an own outbox table, through Wolverine, or through a job library?
- Ask when: the first feature that sends a mail after a business change.
- Options: Inside the request: simple. But a slow or down relay fails or slows the request, and a mail can go out for a change that the database rolled back. Own outbox table plus a `BackgroundService`: the row is saved in the same transaction as the business change, a sender retries, no new package. Wolverine durable local queue: the same guarantee from the Wolverine outbox, with retries and a dead-letter queue. Job library (Hangfire): a dashboard and retries. But Hangfire saves the job outside the EF transaction, unless the job storage uses the same database and transaction.
- Recommended: An own small outbox table and a `BackgroundService`. It keeps the order and the mail in step without a new package. When the service already uses Wolverine, use its durable local queue with `IDbContextOutbox<T>` instead (`dev-stack:wolverine`).
- Choose another option when: the service already runs Wolverine or Hangfire for other work. The mail volume needs scheduling, priorities, or a dashboard.
- Depends on: MSG-01 (in process or a broker), DATA-02 (data access library).
- Revisit when: the service gets more than one replica (claim rows, see `outbox-sender.md`), or the mail volume is more than some thousand mails each hour.
- Check: judge.
- Detect: DET-MAIL-02.

## MAIL-03. Mail templates

- Question: Which template engine renders the HTML body of a mail?
- Ask when: the first mail with an HTML body that contains data.
- Options: Razor components with `HtmlRenderer` (.NET 8+): first party, compiled, typed parameters, encodes each value by default. Razor.Templating.Core: Razor views (`.cshtml`) outside MVC, MIT. Scriban: Liquid-like text templates that load at run time, BSD-2-Clause, no HTML encoding by default. Fluid: Liquid templates, MIT, an encoder must be passed. String interpolation: no encoding at all.
- Recommended: Razor components with `HtmlRenderer`. They are part of ASP.NET Core, the compiler checks them, and they encode each value, so a customer name cannot add HTML to the mail.
- Choose another option when: non-developers edit the templates at run time (Scriban or Fluid, with an HTML encoder). The project already has `.cshtml` mail views (Razor.Templating.Core).
- Depends on: none.
- Revisit when: marketing needs editable templates, or the mails need a design system (MJML or a provider template).
- Check: judge.
- Detect: DET-MAIL-03.

## MAIL-04. Mail tests

- Question: Which SMTP server do the integration tests use?
- Ask when: the first test of the mail sender.
- Options: Mailpit with Testcontainers.Mailpit: a real SMTP server with a REST API to read the captured mails, MIT, maintained. smtp4dev: a similar server with a web UI, BSD-3-Clause. A fake SMTP server in the test code: no container, full control over the replies (for TLS and failure tests). MailHog: not maintained. A mocked `SmtpClient`: proves nothing about the SMTP dialog.
- Recommended: Mailpit through `Testcontainers.Mailpit` with a pinned image tag (`axllent/mailpit:v1.32.0`). A small fake server in test code for the cases that Mailpit cannot give (no STARTTLS, a 4xx or 5xx reply).
- Choose another option when: Docker is not available in CI. Then use the fake server only.
- Depends on: TEST-07 (integration test strategy).
- Revisit when: a new Mailpit major version, or the team moves to another test container setup.
- Check: tool (MAIL-MAILHOG), judge.
- Detect: DET-MAIL-04.

## MAIL-05. Microsoft 365 mail

- Question: How does the service send mail through Microsoft 365 (or Gmail)?
- Ask when: the relay is Exchange Online or Gmail.
- Options: OAuth2 SMTP (SMTP AUTH with XOAUTH2, `SaslMechanismOAuth2`) with an app registration and a client credentials token: keeps MailKit and the outbox. Microsoft Graph `sendMail`: HTTP API, `Mail.Send` application permission limited with an application access policy, no SMTP. Azure Communication Services Email: a managed mail service with its own domain setup and SDK. Basic authentication (user name and password): Microsoft is retiring it for SMTP AUTH.
- Recommended: OAuth2 SMTP when the code already uses MailKit. It changes only the authentication step. Name Graph and Azure Communication Services as the alternatives.
- Choose another option when: the tenant turns off SMTP AUTH (Graph `sendMail`). The volume is high or the mail goes to customers from an own domain (Azure Communication Services Email).
- Depends on: MAIL-01, CFG-02.
- Revisit when: Microsoft publishes the final date for the end of basic authentication, or the tenant policy changes.
- Check: judge.
- Detect: DET-MAIL-05.
