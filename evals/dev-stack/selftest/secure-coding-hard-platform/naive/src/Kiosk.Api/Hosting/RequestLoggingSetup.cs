using Kiosk.Api.Files;
using Microsoft.AspNetCore.HttpLogging;

namespace Kiosk.Api.Hosting;

public static class RequestLoggingSetup
{
    public static IServiceCollection AddKioskRequestLogging(this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment environment)
    {
        // Request properties, headers and timing only. Bodies hold passwords, tokens and personal data.
        services.AddHttpLogging(options =>
        {
            options.LoggingFields = HttpLoggingFields.RequestPropertiesAndHeaders | HttpLoggingFields.ResponsePropertiesAndHeaders | HttpLoggingFields.Duration;
            options.CombineLogs = true;
        });

        var storage = configuration.GetSection(FileStorageOptions.Section).Get<FileStorageOptions>() ?? new FileStorageOptions();
        services.AddW3CLogging(options =>
        {
            options.LoggingFields = W3CLoggingFields.All;
            options.LogDirectory = storage.Resolve(environment.ContentRootPath, storage.AccessLogRoot);
            options.FileName = "access-";
            options.FlushInterval = TimeSpan.FromSeconds(2);
        });
        return services;
    }

    public static IApplicationBuilder UseKioskRequestLogging(this IApplicationBuilder app)
    {
        app.UseHttpLogging();
        app.UseW3CLogging();
        return app;
    }
}
