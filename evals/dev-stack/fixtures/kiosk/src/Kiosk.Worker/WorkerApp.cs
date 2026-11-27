using System.Net;
using Microsoft.AspNetCore.HttpOverrides;
using Npgsql;

namespace Kiosk.Worker;

/// <summary>Background jobs of the shop: the mail outbox. It also answers the status checks of the ingress.</summary>
public sealed class WorkerApp
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.Configure<OutboxOptions>(builder.Configuration.GetSection(OutboxOptions.Section));
        builder.Services.AddSingleton(sp =>
            NpgsqlDataSource.Create(sp.GetRequiredService<IConfiguration>().GetConnectionString("Kiosk")
                ?? throw new InvalidOperationException("ConnectionStrings:Kiosk is missing.")));
        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddSingleton<IMailTransport, LoggingMailTransport>();
        builder.Services.AddSingleton<OutboxMailer>();
        builder.Services.AddHostedService(sp => sp.GetRequiredService<OutboxMailer>());

        var proxy = IPAddress.Parse(builder.Configuration["Ingress:ProxyAddress"] ?? "10.20.0.5");
        builder.Services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.KnownIPNetworks.Clear();
            options.KnownProxies.Clear();
            options.KnownProxies.Add(proxy);
        });

        var app = builder.Build();
        app.UseForwardedHeaders();
        app.MapStatusEndpoints();
        await app.RunAsync();
    }
}
