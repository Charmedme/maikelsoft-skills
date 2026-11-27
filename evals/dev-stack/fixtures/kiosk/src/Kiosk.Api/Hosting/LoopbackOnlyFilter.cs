using System.Net;

namespace Kiosk.Api.Hosting;

/// <summary>Allows the call only from the same host, for example the metrics scraper sidecar.</summary>
public sealed class LoopbackOnlyFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var address = context.HttpContext.Connection.RemoteIpAddress;
        if (address is null || !IPAddress.IsLoopback(address))
        {
            return Results.StatusCode(StatusCodes.Status403Forbidden);
        }

        return await next(context);
    }
}
