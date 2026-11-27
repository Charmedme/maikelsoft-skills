using Microsoft.AspNetCore.DataProtection;

namespace Kiosk.Api.Hosting;

public static class DataProtectionSetup
{
    public static IServiceCollection AddKioskDataProtection(this IServiceCollection services, IConfiguration configuration)
    {
        // All instances must read the same key ring. In production the ring is on a mounted volume outside the
        // repository (DataProtection:KeyPath). Without that setting, keys live in memory only.
        var builder = services.AddDataProtection().SetApplicationName("kiosk");
        var keyPath = configuration["DataProtection:KeyPath"];
        if (string.IsNullOrWhiteSpace(keyPath))
        {
            builder.UseEphemeralDataProtectionProvider();
        }
        else
        {
            builder.PersistKeysToFileSystem(new DirectoryInfo(keyPath));
        }

        return services;
    }
}
