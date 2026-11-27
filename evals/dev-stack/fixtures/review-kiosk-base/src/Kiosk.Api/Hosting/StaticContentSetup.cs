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

        // Terms, delivery zones and other documents that the shop links to. Only direct links, no listing.
        var publicFiles = Directory.CreateDirectory(storage.Resolve(root, storage.PublicFilesRoot)).FullName;
        app.UseStaticFiles(new StaticFileOptions
        {
            RequestPath = "/files",
            FileProvider = new PhysicalFileProvider(publicFiles),
        });

        // Access logs stay on the server. Support reads them through the log platform.
        Directory.CreateDirectory(storage.Resolve(root, storage.AccessLogRoot));
        return app;
    }
}
