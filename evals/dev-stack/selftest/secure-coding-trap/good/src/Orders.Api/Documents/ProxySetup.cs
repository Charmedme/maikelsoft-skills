using Microsoft.AspNetCore.HttpOverrides;

namespace Orders.Api.Documents;

public static class ProxySetup
{
    public static IServiceCollection AddProxySupport(this IServiceCollection services) =>
        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.KnownIPNetworks.Add(System.Net.IPNetwork.Parse("10.0.0.0/8"));
        });
}
