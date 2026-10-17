using Microsoft.Extensions.Diagnostics.HealthChecks;
using Orders.Api.Data;

namespace Orders.Api.Health;

public sealed class DatabaseHealthCheck(OrdersDbContext db) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default) =>
        await db.Database.CanConnectAsync(cancellationToken)
            ? HealthCheckResult.Healthy()
            : HealthCheckResult.Unhealthy("The database is not reachable.");
}
