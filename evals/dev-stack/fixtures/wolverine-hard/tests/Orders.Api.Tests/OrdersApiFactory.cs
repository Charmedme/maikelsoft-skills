using JasperFx.CommandLine;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Time.Testing;
using Orders.Api.Data;
using Orders.Api.Payments;
using Orders.Api.Shipping;
using Wolverine;

namespace Orders.Api.Tests;

public sealed class OrdersApiFactory : WebApplicationFactory<Program>
{
    public OrdersApiFactory()
    {
        // Program.cs ends with RunJasperFxCommands(args). Without this flag the host does not start in a test.
        JasperFxEnvironment.AutoStartHost = true;
    }

    public FakeTimeProvider Time { get; } = new(new DateTimeOffset(2026, 10, 16, 9, 0, 0, TimeSpan.Zero));

    public FakePaymentGateway Payments { get; } = new();

    public FakeCarrierClient Carrier { get; } = new();

    public IHost WolverineHost => Services.GetRequiredService<IHost>();

    public async Task MigrateAsync(CancellationToken ct)
    {
        await using var scope = Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
        await db.Database.MigrateAsync(ct);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:Orders", AssemblyFixture.ConnectionString);
        builder.UseSetting("ConnectionStrings:RabbitMq", "amqp://guest:guest@127.0.0.1:5672/");
        builder.UseSetting("Carrier:ApiKey", "test-key");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(Time);
            services.RemoveAll<IPaymentGateway>();
            services.AddSingleton<IPaymentGateway>(Payments);
            services.RemoveAll<ICarrierClient>();
            services.AddSingleton<ICarrierClient>(Carrier);
            services.DisableAllExternalWolverineTransports();
            services.RunWolverineInSoloMode();
        });
    }
}
