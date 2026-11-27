using Microsoft.EntityFrameworkCore;

namespace Kiosk.Api.Data;

public static class DataSetup
{
    public static IServiceCollection AddKioskData(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<KioskDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("Kiosk")));
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<DatabaseInitializer>();
        return services;
    }

    public static async Task InitializeDatabaseAsync(this WebApplication app)
    {
        if (!app.Configuration.GetValue<bool>("Database:Initialize"))
        {
            return;
        }

        await using var scope = app.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<DatabaseInitializer>().InitializeAsync(CancellationToken.None);
    }
}
