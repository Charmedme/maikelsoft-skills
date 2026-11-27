using System.Xml;
using Kiosk.Api.Data;
using Kiosk.Api.Hosting;
using Microsoft.EntityFrameworkCore;

namespace Kiosk.Api.Wishlists;

public static class WishlistEndpoints
{
    public static IServiceCollection AddWishlists(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<PriceAlertOptions>(configuration.GetSection(PriceAlertOptions.Section));
        services.AddScoped<WishlistQueries>();
        services.AddScoped<WishlistExporter>();
        services.AddSingleton<WishlistImporter>();
        return services;
    }

    public static IEndpointRouteBuilder MapWishlistEndpoints(this IEndpointRouteBuilder app)
    {
        var lists = app.MapGroup("/api/wishlists").RequireAuthorization();
        lists.MapGet("/mine", ListMineAsync).CacheOutput(OutputCacheSetup.WishlistSnapshot);
        lists.MapPost("/", CreateAsync);
        lists.MapGet("/{id:int}", GetAsync);
        lists.MapPost("/{id:int}/items", AddItemAsync);
        lists.MapGet("/search", (string q, WishlistQueries queries, CancellationToken ct) => queries.SearchPublicAsync(q, ct));
        lists.MapGet("/{id:int}/export", ExportAsync);
        lists.MapPost("/{id:int}/import", ImportAsync).RequireAuthorization(Policies.ApiClient).DisableAntiforgery();
        return app;
    }

    private static async Task<List<WishlistDto>> ListMineAsync(HttpContext http, WishlistQueries queries, CancellationToken ct)
    {
        var userId = http.User.GetUserId();
        var lists = await queries.WithItems().AsNoTracking().Where(w => w.OwnerId == userId).OrderBy(w => w.Id).ToListAsync(ct);
        return lists.Select(WishlistDto.From).ToList();
    }

    private static async Task<IResult> CreateAsync(HttpContext http, CreateWishlistRequest request, KioskDbContext db, TimeProvider time, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["name"] = ["Give the list a name."] });
        }

        var list = new Wishlist { OwnerId = http.User.GetUserId(), Name = request.Name.Trim(), IsPublic = request.IsPublic, CreatedAt = time.GetUtcNow() };
        db.Wishlists.Add(list);
        await db.SaveChangesAsync(ct);
        return Results.Created($"/api/wishlists/{list.Id}", WishlistDto.From(list));
    }

    private static async Task<IResult> GetAsync(int id, WishlistQueries queries, CancellationToken ct) =>
        await queries.WithItems().AsNoTracking().SingleOrDefaultAsync(w => w.Id == id, ct) is { } list
            ? Results.Ok(WishlistDto.From(list))
            : Results.NotFound();

    private static async Task<IResult> AddItemAsync(int id, AddWishlistItemRequest request, HttpContext http, KioskDbContext db, CancellationToken ct)
    {
        var userId = http.User.GetUserId();
        var list = await db.Wishlists.SingleOrDefaultAsync(w => w.Id == id && w.OwnerId == userId, ct);
        if (list is null || !await db.Products.AnyAsync(p => p.Id == request.ProductId && p.DeletedAt == null, ct))
        {
            return Results.NotFound();
        }

        var item = new WishlistItem { WishlistId = list.Id, ProductId = request.ProductId, Quantity = request.Quantity, Note = request.Note?.Trim(), TargetPrice = request.TargetPrice };
        db.Set<WishlistItem>().Add(item);
        list.PriceAlerts |= request.TargetPrice is not null;
        await db.SaveChangesAsync(ct);
        return Results.Created($"/api/wishlists/{list.Id}", new { item.Id });
    }

    private static async Task<IResult> ExportAsync(int id, string? template, HttpContext http, WishlistQueries queries, WishlistExporter exporter, CancellationToken ct)
    {
        var userId = http.User.GetUserId();
        var list = await queries.WithItems().AsNoTracking().SingleOrDefaultAsync(w => w.Id == id && w.OwnerId == userId, ct);
        if (list is null)
        {
            return Results.NotFound();
        }

        var text = await exporter.ExportAsync(WishlistDto.From(list), template ?? "default", ct);
        return text is null ? Results.NotFound() : Results.Text(text, "text/plain");
    }

    // The mobile app sends the registry file with its bearer token; no browser form posts here.
    private static async Task<IResult> ImportAsync(int id, IFormFile file, HttpContext http, KioskDbContext db, WishlistImporter importer, CancellationToken ct)
    {
        var userId = http.User.GetUserId();
        var list = await db.Wishlists.SingleOrDefaultAsync(w => w.Id == id && w.OwnerId == userId, ct);
        if (list is null)
        {
            return Results.NotFound();
        }

        IReadOnlyList<ImportedItem> imported;
        try
        {
            await using var stream = new MemoryStream();
            await file.CopyToAsync(stream, ct);
            stream.Position = 0;
            imported = importer.Parse(stream);
        }
        catch (Exception ex) when (ex is XmlException or FormatException)
        {
            return Results.Problem(title: "The registry file is not valid.", statusCode: StatusCodes.Status400BadRequest);
        }

        var skus = imported.Select(i => i.Sku).ToList();
        var products = await db.Products.Where(p => skus.Contains(p.Sku) && p.DeletedAt == null).ToDictionaryAsync(p => p.Sku, p => p.Id, ct);
        foreach (var item in imported.Where(i => products.ContainsKey(i.Sku) && i.Quantity is > 0 and <= 100))
        {
            db.Set<WishlistItem>().Add(new WishlistItem { WishlistId = list.Id, ProductId = products[item.Sku], Quantity = item.Quantity, Note = item.Note });
        }

        await db.SaveChangesAsync(ct);
        return Results.Ok(new { imported = products.Count });
    }
}
