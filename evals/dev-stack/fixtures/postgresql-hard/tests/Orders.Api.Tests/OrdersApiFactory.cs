using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Time.Testing;
using Npgsql;
using Orders.Api.Data;

namespace Orders.Api.Tests;

/// <summary>The API on its own database in the shared container, with a fake clock and without the background job.</summary>
public sealed class OrdersApiFactory : WebApplicationFactory<Program>
{
    public OrdersApiFactory()
    {
        ConnectionString = new NpgsqlConnectionStringBuilder(AssemblyFixture.ConnectionString)
        {
            Database = "test_" + Guid.NewGuid().ToString("N")[..12],
        }.ConnectionString;
    }

    public FakeTimeProvider Time { get; } = new(new DateTimeOffset(2026, 10, 16, 9, 0, 0, TimeSpan.Zero));

    public string ConnectionString { get; }

    public async Task MigrateAsync(CancellationToken ct)
    {
        await using var scope = Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
        await db.Database.MigrateAsync(ct);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:Orders", ConnectionString);
        builder.UseSetting("Returns:ExpiryJobEnabled", "false");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(Time);
        });
    }
}
