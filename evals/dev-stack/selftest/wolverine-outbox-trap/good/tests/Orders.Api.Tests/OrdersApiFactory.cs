using JasperFx.CommandLine;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Time.Testing;
using Orders.Api.Data;
using Wolverine;

namespace Orders.Api.Tests;

public sealed class OrdersApiFactory : WebApplicationFactory<Program>
{
    public OrdersApiFactory()
    {
        // Program.cs ends with RunJasperFxCommands(args). The host starts under the factory only with this flag.
        JasperFxEnvironment.AutoStartHost = true;
    }

    public FakeTimeProvider Time { get; } = new(new DateTimeOffset(2026, 10, 16, 9, 0, 0, TimeSpan.Zero));

    public async Task MigrateAsync(CancellationToken ct)
    {
        await using var scope = Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
        await db.Database.MigrateAsync(ct);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:Orders", AssemblyFixture.ConnectionString);
        builder.UseSetting("ConnectionStrings:RabbitMq", "amqp://guest:guest@localhost:5672/");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(Time);
            services.DisableAllExternalWolverineTransports();
            services.RunWolverineInSoloMode();
        });
    }
}
