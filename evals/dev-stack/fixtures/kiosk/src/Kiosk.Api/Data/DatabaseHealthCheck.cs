using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Kiosk.Api.Data;

public sealed class DatabaseHealthCheck(KioskDbContext db) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default) =>
        await db.Database.CanConnectAsync(cancellationToken)
            ? HealthCheckResult.Healthy()
            : HealthCheckResult.Unhealthy("The database does not answer.");
}
