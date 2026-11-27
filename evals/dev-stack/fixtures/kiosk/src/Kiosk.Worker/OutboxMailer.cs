using Microsoft.Extensions.Options;
using Npgsql;

namespace Kiosk.Worker;

public sealed class OutboxMailer(NpgsqlDataSource dataSource, IMailTransport transport, IOptions<OutboxOptions> options, TimeProvider time, ILogger<OutboxMailer> logger) : BackgroundService
{
    private long _sent;

    public long Sent => Interlocked.Read(ref _sent);

    public DateTimeOffset? LastRun { get; private set; }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var settings = options.Value;
        if (!settings.Enabled)
        {
            return;
        }

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(settings.PollSeconds), time);
        do
        {
            try
            {
                await SendBatchAsync(settings.BatchSize, stoppingToken);
            }
            catch (Exception ex) when (ex is NpgsqlException or InvalidOperationException)
            {
                logger.LogWarning(ex, "The outbox run failed. The next run tries again.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task SendBatchAsync(int batchSize, CancellationToken ct)
    {
        LastRun = time.GetUtcNow();
        await using var connection = await dataSource.OpenConnectionAsync(ct);
        var batch = new List<OutgoingMail>();
        await using (var select = new NpgsqlCommand(
            "SELECT \"Id\", \"To\", \"Subject\", \"Body\" FROM \"OutboxEmails\" WHERE \"SentAt\" IS NULL ORDER BY \"Id\" LIMIT @batch", connection))
        {
            select.Parameters.AddWithValue("batch", batchSize);
            await using var reader = await select.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                batch.Add(new OutgoingMail(reader.GetInt64(0), reader.GetString(1), reader.GetString(2), reader.GetString(3)));
            }
        }

        foreach (var mail in batch)
        {
            await transport.SendAsync(mail, ct);
            await using var update = new NpgsqlCommand(
                "UPDATE \"OutboxEmails\" SET \"SentAt\" = now(), \"Attempts\" = \"Attempts\" + 1 WHERE \"Id\" = @id", connection);
            update.Parameters.AddWithValue("id", mail.Id);
            await update.ExecuteNonQueryAsync(ct);
            Interlocked.Increment(ref _sent);
        }
    }
}
