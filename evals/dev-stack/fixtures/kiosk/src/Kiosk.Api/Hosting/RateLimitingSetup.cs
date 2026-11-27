using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace Kiosk.Api.Hosting;

public static class RateLimitingSetup
{
    public const string Login = "login";

    public static IServiceCollection AddKioskRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection("RateLimiting:Login");
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy(Login, context =>
            {
                var permitLimit = section.GetValue("PermitLimit", 10);
                var window = TimeSpan.FromSeconds(section.GetValue("WindowSeconds", 60));
                var client = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                return RateLimitPartition.GetFixedWindowLimiter(client, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = permitLimit,
                    Window = window,
                    QueueLimit = 0,
                });
            });
        });
        return services;
    }
}
