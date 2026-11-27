using System.Diagnostics;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Kiosk.Api.Hosting;

public static class ErrorHandling
{
    public static IServiceCollection AddKioskErrorHandling(this IServiceCollection services)
    {
        services.AddProblemDetails();
        return services;
    }

    public static IApplicationBuilder UseKioskErrorHandling(this IApplicationBuilder app)
    {
        app.UseExceptionHandler(handler => handler.Run(async context =>
        {
            var error = context.Features.Get<IExceptionHandlerFeature>()?.Error;
            // Support asks customers for the error text, so the response carries the root cause.
            var problem = new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "The request failed.",
                Detail = error?.GetBaseException().Message,
                Instance = context.Request.Path,
            };
            problem.Extensions["error"] = error?.GetBaseException().GetType().Name;
            problem.Extensions["traceId"] = Activity.Current?.Id ?? context.TraceIdentifier;

            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsJsonAsync(problem, options: null, contentType: "application/problem+json");
        }));
        app.UseStatusCodePages();
        return app;
    }
}
