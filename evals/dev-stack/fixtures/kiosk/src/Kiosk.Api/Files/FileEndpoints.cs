using Kiosk.Api.Data;
using Kiosk.Api.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Kiosk.Api.Files;

public static class FileEndpoints
{
    private const long MaxUploadBytes = 5 * 1024 * 1024;

    private static readonly string[] s_uploadTypes = ["image/png", "image/jpeg", "image/gif", "application/pdf"];

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

        var root = storage.Value.Resolve(environment.ContentRootPath, storage.Value.InvoiceRoot);
        var path = Path.Combine(root, name);
        if (!File.Exists(path))
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

        if (!s_uploadTypes.Contains(file.ContentType, StringComparer.OrdinalIgnoreCase))
        {
            return Results.Problem(title: "Upload an image or a PDF file.", statusCode: StatusCodes.Status415UnsupportedMediaType);
        }

        var folder = Directory.CreateDirectory(storage.Value.Resolve(environment.ContentRootPath, storage.Value.UploadRoot));
        var fileName = Path.GetFileName(file.FileName);
        await using (var target = File.Create(Path.Combine(folder.FullName, fileName)))
        {
            await file.CopyToAsync(target, ct);
        }

        var url = "/uploads/" + Uri.EscapeDataString(fileName);
        return Results.Created(url, new { url });
    }
}
