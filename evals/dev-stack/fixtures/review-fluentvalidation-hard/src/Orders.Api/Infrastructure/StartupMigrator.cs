using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Orders.Api.Data;

namespace Orders.Api.Infrastructure;

// Applies the pending migrations before the service accepts requests.
public sealed partial class StartupMigrator(
    IServiceScopeFactory scopes,
    IOptions<DatabaseOptions> options,
    ILogger<StartupMigrator> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!options.Value.MigrateOnStartup)
        {
            return;
        }

        await using var scope = scopes.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
        await db.Database.MigrateAsync(cancellationToken);
        LogMigrated();
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    [LoggerMessage(EventId = 1001, Level = LogLevel.Information, Message = "The database migrations are applied.")]
    private partial void LogMigrated();
}
