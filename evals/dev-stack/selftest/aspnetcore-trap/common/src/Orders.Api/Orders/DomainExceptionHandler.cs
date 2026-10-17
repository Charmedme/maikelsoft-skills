using Microsoft.AspNetCore.Diagnostics;

namespace Orders.Api.Orders;

public sealed class DomainExceptionHandler(IProblemDetailsService problems) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var known = exception is InvalidOperationException;
        httpContext.Response.StatusCode = known ? StatusCodes.Status409Conflict : StatusCodes.Status500InternalServerError;
        return await problems.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = { Title = known ? "The order rules do not allow this." : "An unexpected error occurred." },
        });
    }
}
