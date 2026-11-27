using Microsoft.AspNetCore.DataProtection;

namespace Kiosk.Api.Hosting;

public static class DataProtectionSetup
{
    public static IServiceCollection AddKioskDataProtection(this IServiceCollection services, IWebHostEnvironment environment)
    {
        // All instances must read the same key ring, or a session cookie from one pod fails on the next.
        services.AddDataProtection()
            .SetApplicationName("kiosk")
            .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(environment.ContentRootPath, "keys")));
        return services;
    }
}
