using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Time.Testing;
using Orders.Api.Data;
using Orders.Api.Payments;
using Orders.Api.Shipping.Carriers;

namespace Orders.Api.Tests;

public sealed class OrdersApiFactory : WebApplicationFactory<Program>
{
    public FakeTimeProvider Time { get; } = new(new DateTimeOffset(2026, 10, 16, 9, 0, 0, TimeSpan.Zero));

    public FakePaymentGateway Payments { get; } = new();

    public FakeCarrierGateway Carriers { get; } = new();

    public async Task MigrateAsync(CancellationToken ct)
    {
        await using var scope = Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
        await db.Database.MigrateAsync(ct);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:Orders", AssemblyFixture.ConnectionString);
        builder.UseSetting("CarrierGateway:BaseUrl", "http://127.0.0.1:9/");
        builder.UseSetting("CarrierGateway:ApiKey", "test-key");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(Time);
            services.RemoveAll<IPaymentGateway>();
            services.AddSingleton<IPaymentGateway>(Payments);
            services.RemoveAll<ICarrierGateway>();
            services.AddSingleton<ICarrierGateway>(Carriers);
        });
    }
}
