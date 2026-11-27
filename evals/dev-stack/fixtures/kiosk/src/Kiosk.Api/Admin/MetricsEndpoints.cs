using Kiosk.Api.Hosting;

namespace Kiosk.Api.Admin;

public static class MetricsEndpoints
{
    public static IEndpointRouteBuilder MapMetricsEndpoints(this IEndpointRouteBuilder app)
    {
        // Scraped by the metrics sidecar in the same pod.
        app.MapGet("/metrics", async (ShopStatistics stats, CancellationToken ct) =>
                Results.Text(await stats.PrometheusAsync(ct), "text/plain; version=0.0.4"))
            .AddEndpointFilter<LoopbackOnlyFilter>();

        // Numbers for the dashboard on the office screen.
        app.MapGet("/api/metrics/summary", async (ShopStatistics stats, CancellationToken ct) => await stats.SummaryAsync(ct));
        return app;
    }
}
