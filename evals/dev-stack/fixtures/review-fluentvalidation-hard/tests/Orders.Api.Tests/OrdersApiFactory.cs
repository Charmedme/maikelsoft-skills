using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Time.Testing;
using Orders.Api.Data;

namespace Orders.Api.Tests;

public sealed class OrdersApiFactory : WebApplicationFactory<Program>
{
    public const string AdminKey = "test-admin-key";

    public FakeTimeProvider Time { get; } = new(new DateTimeOffset(2026, 10, 16, 9, 0, 0, TimeSpan.Zero));

    public async Task MigrateAsync(CancellationToken ct)
    {
        await using var scope = Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
        await db.Database.MigrateAsync(ct);
    }

    public async Task<T> WithDbAsync<T>(Func<OrdersDbContext, Task<T>> work)
    {
        await using var scope = Services.CreateAsyncScope();
        return await work(scope.ServiceProvider.GetRequiredService<OrdersDbContext>());
    }

    public HttpClient CreateAdminClient()
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add("X-Admin-Key", AdminKey);
        return client;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:Orders", AssemblyFixture.ConnectionString);
        // The assembly fixture migrates the shared database once. Parallel test classes must not migrate it again.
        builder.UseSetting("Database:MigrateOnStartup", "false");
        builder.UseSetting("Admin:ApiKey", AdminKey);
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(Time);
        });
    }
}
