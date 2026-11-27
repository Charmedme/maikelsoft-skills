using Microsoft.AspNetCore.HttpOverrides;

namespace Kiosk.Api.Hosting;

public static class ForwardedHeadersSetup
{
    public static IServiceCollection AddKioskForwardedHeaders(this IServiceCollection services)
    {
        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            // The ingress pods get a new address on each deployment, so we cannot list them.
            options.KnownIPNetworks.Clear();
            options.KnownProxies.Clear();
        });
        return services;
    }
}
