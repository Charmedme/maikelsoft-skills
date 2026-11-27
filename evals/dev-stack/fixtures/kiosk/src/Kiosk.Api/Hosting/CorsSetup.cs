namespace Kiosk.Api.Hosting;

public static class CorsSetup
{
    public static IServiceCollection AddKioskCors(this IServiceCollection services)
    {
        // Storefront, partner portals and preview deployments all run on their own subdomains.
        services.AddCors(options => options.AddDefaultPolicy(policy => policy
            .SetIsOriginAllowed(_ => true)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()));
        return services;
    }
}
