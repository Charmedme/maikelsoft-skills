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
            var traceId = Activity.Current?.Id ?? context.TraceIdentifier;
            context.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("Kiosk.Errors")
                .LogError(error, "Unhandled error. Trace id {TraceId}.", traceId);
            // The client gets the trace id only. Support finds the details in the logs.
            var problem = new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "The request failed.",
                Instance = context.Request.Path,
            };
            problem.Extensions["traceId"] = traceId;

            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsJsonAsync(problem, options: null, contentType: "application/problem+json");
        }));
        app.UseStatusCodePages();
        return app;
    }
}
