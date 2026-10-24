using System.Diagnostics;

namespace Orders.Api.Infrastructure;

// Copies X-Correlation-Id from the request to the response, or uses the trace id when the header is missing.
public sealed class CorrelationIdMiddleware(RequestDelegate next)
{
    public const string HeaderName = "X-Correlation-Id";

    public Task InvokeAsync(HttpContext context)
    {
        var id = context.Request.Headers[HeaderName].ToString();
        if (id.Length is 0 or > 64)
        {
            id = Activity.Current?.TraceId.ToString() ?? context.TraceIdentifier;
        }

        context.Response.Headers[HeaderName] = id;
        return next(context);
    }
}
