using System.Net;

namespace Kiosk.Worker;

public static class StatusEndpoints
{
    public static IEndpointRouteBuilder MapStatusEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/status", () => Results.Ok(new { status = "ok" }));

        // Job details for the operators. Reachable only from the node itself.
        app.MapGet("/status/jobs", (HttpContext http, OutboxMailer mailer) =>
        {
            var remote = http.Connection.RemoteIpAddress;
            if (remote is null || !IPAddress.IsLoopback(remote))
            {
                return Results.StatusCode(StatusCodes.Status403Forbidden);
            }

            return Results.Ok(new { outboxSent = mailer.Sent, lastRun = mailer.LastRun });
        });
        return app;
    }
}
