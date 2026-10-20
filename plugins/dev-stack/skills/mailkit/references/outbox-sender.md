# Outbox and background sender

The send path of decision MAIL-02: an outbox row in the same transaction as the business change, and a `BackgroundService` that sends the due rows. The code was compiled and tested with MailKit 4.18.1, EF Core 10.0.13 and Npgsql 10.0.3, against a fake relay and Mailpit v1.31.6.

## Contents

- Why an outbox
- The outbox row
- Write the row
- The sender
- Retry plan
- More than one replica
- Operations
- Wolverine instead of an own outbox

## Why an outbox

A send inside the request has three failure modes:

- The relay is down: the request fails, or the code catches the error and the mail is lost.
- The relay is slow: the request waits up to `client.Timeout` for each SMTP step (120 s by default).
- The transaction rolls back after the send: the customer gets a mail for an order that does not exist.

An outbox row is saved in the same `SaveChangesAsync` as the order. Either both are saved, or neither. A background sender reads the due rows and retries until the relay accepts the mail.

## The outbox row

Keep the whole mail in the row: recipient, subject, text body, HTML body. Then the sender does not need the business data, and a later change of a template does not change a mail that is waiting. Keep a reference (a blob name) for a large attachment, not the bytes.

```csharp
public sealed class OutboxMail
{
    private OutboxMail()
    {
        ToAddress = string.Empty;
        ToName = string.Empty;
        Subject = string.Empty;
        TextBody = string.Empty;
        HtmlBody = string.Empty;
    }

    public OutboxMail(string toAddress, string toName, string subject, string textBody, string htmlBody, DateTimeOffset now)
    {
        ToAddress = toAddress;
        ToName = toName;
        Subject = subject;
        TextBody = textBody;
        HtmlBody = htmlBody;
        CreatedAt = now;
        NextAttemptAt = now;
    }

    public Guid Id { get; private set; }
    public string ToAddress { get; private set; }
    public string ToName { get; private set; }
    public string Subject { get; private set; }
    public string TextBody { get; private set; }
    public string HtmlBody { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public int Attempts { get; private set; }
    public DateTimeOffset NextAttemptAt { get; private set; }
    public DateTimeOffset? SentAt { get; private set; }
    public DateTimeOffset? FailedAt { get; private set; }
    public string? LastError { get; private set; }

    public void MarkSent(DateTimeOffset now) => SentAt = now;

    public void MarkFailed(DateTimeOffset now, string error)
    {
        FailedAt = now;
        LastError = error;
    }

    public void Retry(DateTimeOffset now, string error)
    {
        Attempts++;
        LastError = error;
        // 5 s, 10 s, 20 s, then every 30 s.
        var delay = TimeSpan.FromSeconds(Math.Min(30, 5 * Math.Pow(2, Math.Min(Attempts - 1, 4))));
        NextAttemptAt = now + delay;
    }
}
```

Map it with an `IEntityTypeConfiguration<OutboxMail>`: a table name, a maximum length for each text column, and an index on `NextAttemptAt`. Add a migration (`dev-stack:efcore`). `LastError` holds the SMTP status and the exception type, never the message body.

## Write the row

Build the mail and add the row before the one `SaveChangesAsync` of the request. The EF provider sets a `Guid` key on `Add`, so the order ID is known before the save.

```csharp
db.Orders.Add(order); // the EF provider sets order.Id here
db.OutboxMails.Add(await confirmation.CreateAsync(order, request.CustomerName, request.Email));
await db.SaveChangesAsync(ct);
```

Validate the address before this point: reject CR and LF, then `MailboxAddress.TryParse` (rule 13 of `SKILL.md`). A bad address gives a 400 answer, not a failed outbox row.

## The sender

One `BackgroundService`. It creates a scope for each batch (the DbContext is scoped), and one `SmtpClient` for each batch.

```csharp
public sealed class OutboxMailSender(
    IServiceScopeFactory scopes,
    IOptions<SmtpOptions> options,
    TimeProvider time,
    ILogger<OutboxMailSender> logger) : BackgroundService
{
    private const int BatchSize = 20;
    private static readonly TimeSpan s_pollInterval = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan s_giveUpAfter = TimeSpan.FromDays(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var sent = 0;
            try
            {
                sent = await SendDueAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "The mail outbox batch failed.");
            }

            if (sent < BatchSize)
            {
                await Task.Delay(s_pollInterval, time, stoppingToken);
            }
        }
    }

    private async Task<int> SendDueAsync(CancellationToken ct)
    {
        await using var scope = scopes.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
        var now = time.GetUtcNow();
        var due = await db.OutboxMails
            .Where(mail => mail.SentAt == null && mail.FailedAt == null && mail.NextAttemptAt <= now)
            .OrderBy(mail => mail.NextAttemptAt)
            .ThenBy(mail => mail.Id)
            .Take(BatchSize)
            .ToListAsync(ct);
        if (due.Count == 0)
        {
            return 0;
        }

        var smtp = options.Value;
        var sent = 0;
        using var client = new SmtpClient { Timeout = smtp.TimeoutMs };
        try
        {
            await client.ConnectAsync(smtp.Host, smtp.Port, smtp.Security, ct);
            if (!string.IsNullOrEmpty(smtp.UserName))
            {
                await client.AuthenticateAsync(smtp.UserName, smtp.Password ?? string.Empty, ct);
            }

            foreach (var mail in due)
            {
                using var message = ToMessage(mail, smtp);
                try
                {
                    await client.SendAsync(message, ct);
                    mail.MarkSent(time.GetUtcNow());
                    sent++;
                }
                catch (SmtpCommandException ex) when ((int)ex.StatusCode >= 500)
                {
                    mail.MarkFailed(time.GetUtcNow(), $"{(int)ex.StatusCode} {ex.ErrorCode}"); // permanent
                }
                catch (SmtpCommandException ex)
                {
                    Postpone(mail, $"{(int)ex.StatusCode} {ex.ErrorCode}"); // 4xx: try again later
                }
            }

            await client.DisconnectAsync(true, ct);
        }
        catch (Exception ex) when (ex is SocketException or IOException or SmtpProtocolException or ServiceNotConnectedException or SslHandshakeException)
        {
            logger.LogWarning("The SMTP relay is not reachable: {Error}.", ex.GetType().Name);
            PostponeUnsent(due, ex.GetType().Name);
        }
        catch (Exception ex) when (ex is AuthenticationException or NotSupportedException)
        {
            logger.LogError("The SMTP relay refused the connection settings: {Error}.", ex.GetType().Name);
            PostponeUnsent(due, ex.GetType().Name);
        }
        finally
        {
            await db.SaveChangesAsync(CancellationToken.None); // keep the results of the sent mails
        }

        return sent;
    }

    private void PostponeUnsent(List<OutboxMail> due, string error)
    {
        foreach (var mail in due.Where(mail => mail.SentAt == null && mail.FailedAt == null))
        {
            Postpone(mail, error);
        }
    }

    private void Postpone(OutboxMail mail, string error)
    {
        var now = time.GetUtcNow();
        if (now - mail.CreatedAt > s_giveUpAfter)
        {
            mail.MarkFailed(now, error);
            logger.LogError("Mail {MailId} was not sent within {Hours} hours.", mail.Id, s_giveUpAfter.TotalHours);
            return;
        }

        mail.Retry(now, error);
    }

    private static MimeMessage ToMessage(OutboxMail mail, SmtpOptions smtp)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(smtp.FromName, smtp.From));
        message.To.Add(new MailboxAddress(mail.ToName, mail.ToAddress));
        message.Subject = mail.Subject;
        message.MessageId = $"{mail.Id:N}@{smtp.From.Split('@')[1]}"; // stable: the same mail keeps its ID on a retry
        message.Body = new BodyBuilder { TextBody = mail.TextBody, HtmlBody = mail.HtmlBody }.ToMessageBody();
        return message;
    }
}
```

`SmtpOptions` here also has `FromName` (a display name). Register the sender with `builder.Services.AddHostedService<OutboxMailSender>();`.

Points that a review checks:

- The loop catches each error. An exception that leaves `ExecuteAsync` stops the host (`BackgroundServiceExceptionBehavior.StopHost`).
- `Task.Delay(interval, time, ct)` uses `TimeProvider`, so a test with `FakeTimeProvider` controls the poll.
- The `finally` block saves with `CancellationToken.None`. A stop during the batch must not lose the `SentAt` of the mails that the relay already accepted.
- After a 4xx or 5xx `SmtpCommandException`, the client stays connected and the batch continues.

## Retry plan

| Attempt | Delay |
|---------|-------|
| 1 | 5 s |
| 2 | 10 s |
| 3 | 20 s |
| 4 and later | 30 s |
| Older than one day | Failed, with an error log |

The cap keeps the delay after a relay outage short. With a cap of 30 s and a poll of 5 s, a mail goes out within about 35 s after the relay is back. Choose a longer cap only when the relay has a rate limit, and tell the business how long a mail can wait.

## More than one replica

Two replicas read the same due rows and send each mail two times. Choose one:

- Claim the rows in one statement: PostgreSQL `SELECT ... FOR UPDATE SKIP LOCKED` inside a transaction (`dev-stack:postgresql`), SQL Server `WITH (UPDLOCK, READPAST, ROWLOCK)` (`dev-stack:sqlserver`). Set a `LockedUntil` column so that a crashed replica releases its rows.
- Run the sender in one replica only (a separate worker deployment with one instance).

A duplicate is still possible after a crash between the send and the save. The stable `Message-Id` lets support find both copies.

## Operations

- Alert on rows with `FailedAt` set, and on the age of the oldest unsent row.
- Delete sent rows after a retention period (for example 30 days) with `ExecuteDeleteAsync`. They contain personal data.
- Do not log the body or the recipient list at Information level. Log the outbox ID, the attempt, and the SMTP status.

## Wolverine instead of an own outbox

When the service uses Wolverine, publish a `SendOrderConfirmation` message to a durable local queue through `IDbContextOutbox<T>`, in the same transaction as the order. The Wolverine handler builds the mail and calls the MailKit sender. Wolverine then owns the retries and the dead-letter queue (MSG-08). Do not add Wolverine only for mail. See `dev-stack:wolverine`.
