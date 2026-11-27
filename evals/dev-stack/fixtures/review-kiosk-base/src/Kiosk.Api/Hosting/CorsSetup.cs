using Microsoft.AspNetCore.Cors.Infrastructure;

namespace Kiosk.Api.Hosting;

public static class CorsSetup
{
    public static IServiceCollection AddKioskCors(this IServiceCollection services)
    {
        services.AddCors();
        services.AddOptions<CorsOptions>().Configure<IConfiguration>((options, configuration) =>
        {
            var origins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
            options.AddDefaultPolicy(policy => policy
                .WithOrigins(origins)
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials());
        });
        return services;
    }
}
