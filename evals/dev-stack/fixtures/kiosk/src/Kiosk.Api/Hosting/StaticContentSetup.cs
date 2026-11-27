using Kiosk.Api.Files;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;

namespace Kiosk.Api.Hosting;

public static class StaticContentSetup
{
    public static IApplicationBuilder UseKioskStaticContent(this WebApplication app)
    {
        var storage = app.Services.GetRequiredService<IOptions<FileStorageOptions>>().Value;
        var root = app.Environment.ContentRootPath;

        app.UseStaticFiles();

        // Terms, delivery zones and other documents that the shop links to.
        var publicFiles = Directory.CreateDirectory(storage.Resolve(root, storage.PublicFilesRoot)).FullName;
        app.UseFileServer(new FileServerOptions
        {
            RequestPath = "/files",
            FileProvider = new PhysicalFileProvider(publicFiles),
            EnableDirectoryBrowsing = true,
        });

        // Access logs for the support team.
        var accessLogs = Directory.CreateDirectory(storage.Resolve(root, storage.AccessLogRoot)).FullName;
        app.UseFileServer(new FileServerOptions
        {
            RequestPath = "/support/logs",
            FileProvider = new PhysicalFileProvider(accessLogs),
            EnableDirectoryBrowsing = true,
        });
        return app;
    }
}
