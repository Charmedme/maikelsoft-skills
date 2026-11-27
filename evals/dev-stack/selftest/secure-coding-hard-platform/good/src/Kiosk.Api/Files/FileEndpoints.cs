using Kiosk.Api.Data;
using Kiosk.Api.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Kiosk.Api.Files;

public static class FileEndpoints
{
    private const long MaxUploadBytes = 5 * 1024 * 1024;

    // Content type, file signature and the extension that we store. The client file name is never used.
    private static readonly (string Type, byte[] Signature, string Extension)[] s_uploadTypes =
    [
        ("image/png", [0x89, 0x50, 0x4E, 0x47], ".png"),
        ("image/jpeg", [0xFF, 0xD8, 0xFF], ".jpg"),
        ("image/gif", [0x47, 0x49, 0x46, 0x38], ".gif"),
        ("application/pdf", [0x25, 0x50, 0x44, 0x46], ".pdf"),
    ];

    public static IEndpointRouteBuilder MapFileEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/invoices/download", DownloadInvoice).RequireAuthorization();
        app.MapGet("/api/orders/{id:int}/invoice", OrderInvoiceAsync).RequireAuthorization();
        app.MapPost("/api/uploads", UploadAsync).RequireAuthorization(Policies.ApiClient).DisableAntiforgery();
        return app;
    }

    private static IResult DownloadInvoice(string name, IOptions<FileStorageOptions> storage, IWebHostEnvironment environment)
    {
        if (!name.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase) && !name.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
        {
            return Results.Problem(title: "Only .pdf and .md files can be downloaded.", statusCode: StatusCodes.Status400BadRequest);
        }

        var root = Path.GetFullPath(storage.Value.Resolve(environment.ContentRootPath, storage.Value.InvoiceRoot));
        var path = Path.GetFullPath(Path.Combine(root, Path.GetFileName(name)));
        if (!path.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.Ordinal) || !File.Exists(path))
        {
            return Results.NotFound();
        }

        var contentType = name.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase) ? "application/pdf" : "text/markdown";
        return Results.File(Path.GetFullPath(path), contentType, Path.GetFileName(path));
    }

    private static async Task<IResult> OrderInvoiceAsync(int id, HttpContext http, KioskDbContext db, IOptions<FileStorageOptions> storage, IWebHostEnvironment environment, CancellationToken ct)
    {
        var userId = http.User.GetUserId();
        var number = await db.Orders.Where(o => o.Id == id && o.CustomerId == userId).Select(o => o.OrderNumber).SingleOrDefaultAsync(ct);
        if (number is null)
        {
            return Results.NotFound();
        }

        var path = Path.Combine(storage.Value.Resolve(environment.ContentRootPath, storage.Value.InvoiceRoot), number + ".pdf");
        return File.Exists(path) ? Results.File(Path.GetFullPath(path), "application/pdf", number + ".pdf") : Results.NotFound();
    }

    private static async Task<IResult> UploadAsync(IFormFile file, IOptions<FileStorageOptions> storage, IWebHostEnvironment environment, CancellationToken ct)
    {
        if (file.Length == 0 || file.Length > MaxUploadBytes)
        {
            return Results.Problem(title: "The file must be between 1 byte and 5 MB.", statusCode: StatusCodes.Status400BadRequest);
        }

        var header = new byte[8];
        await using (var source = file.OpenReadStream())
        {
            _ = await source.ReadAtLeastAsync(header, header.Length, throwOnEndOfStream: false, ct);
        }

        var kind = s_uploadTypes.FirstOrDefault(t =>
            string.Equals(t.Type, file.ContentType, StringComparison.OrdinalIgnoreCase) && header.AsSpan().StartsWith(t.Signature));
        if (kind.Type is null)
        {
            return Results.Problem(title: "Upload an image or a PDF file.", statusCode: StatusCodes.Status415UnsupportedMediaType);
        }

        var folder = Directory.CreateDirectory(storage.Value.Resolve(environment.ContentRootPath, storage.Value.UploadRoot));
        var fileName = Guid.NewGuid().ToString("N") + kind.Extension;
        await using (var target = File.Create(Path.Combine(folder.FullName, fileName)))
        {
            await file.CopyToAsync(target, ct);
        }

        var url = "/uploads/" + Uri.EscapeDataString(fileName);
        return Results.Created(url, new { url });
    }
}
