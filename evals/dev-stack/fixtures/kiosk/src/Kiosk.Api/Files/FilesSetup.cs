namespace Kiosk.Api.Files;

public static class FilesSetup
{
    public static IServiceCollection AddFiles(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<FileStorageOptions>(configuration.GetSection(FileStorageOptions.Section));
        return services;
    }
}
