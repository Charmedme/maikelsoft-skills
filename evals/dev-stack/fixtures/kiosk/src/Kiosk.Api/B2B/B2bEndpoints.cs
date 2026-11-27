using System.Xml;
using Kiosk.Api.Data;
using Kiosk.Api.Hosting;
using Kiosk.Api.Orders;
using Microsoft.EntityFrameworkCore;

namespace Kiosk.Api.B2B;

public static class B2bEndpoints
{
    public static IEndpointRouteBuilder MapB2bEndpoints(this IEndpointRouteBuilder app)
    {
        var b2b = app.MapGroup("/api/b2b").RequireAuthorization(Policies.Partner);
        b2b.MapPost("/orders", ImportXmlAsync).Accepts<string>("application/xml");
        b2b.MapPost("/orders/json", ImportJsonAsync).Accepts<PartnerOrderDocument>("application/json");
        b2b.MapPost("/price-list", ImportPriceListAsync).Accepts<string>("application/xml");
        return app;
    }

    private static async Task<IResult> ImportXmlAsync(HttpRequest request, XmlOrderImporter importer, OrderService orders, KioskDbContext db, CancellationToken ct)
    {
        PartnerOrderDocument document;
        try
        {
            document = importer.Parse(await BufferAsync(request, ct));
        }
        catch (Exception ex) when (ex is XmlException or FormatException)
        {
            return Results.Problem(title: "The order document is not valid.", detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }

        return await PlaceAsync(request.HttpContext, document, orders, db, ct);
    }

    private static async Task<IResult> ImportJsonAsync(HttpRequest request, JsonOrderImporter importer, OrderService orders, KioskDbContext db, CancellationToken ct)
    {
        using var reader = new StreamReader(request.Body);
        PartnerOrderDocument document;
        try
        {
            document = importer.Parse(await reader.ReadToEndAsync(ct));
        }
        catch (Exception ex) when (ex is Newtonsoft.Json.JsonException or FormatException)
        {
            return Results.Problem(title: "The order document is not valid.", detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }

        return await PlaceAsync(request.HttpContext, document, orders, db, ct);
    }

    private static async Task<IResult> ImportPriceListAsync(HttpRequest request, PriceListImporter importer, KioskDbContext db, CancellationToken ct)
    {
        IReadOnlyList<PriceUpdate> prices;
        try
        {
            prices = importer.Parse(await BufferAsync(request, ct));
        }
        catch (Exception ex) when (ex is XmlException or FormatException)
        {
            return Results.Problem(title: "The price list is not valid.", statusCode: StatusCodes.Status400BadRequest);
        }

        var supplier = request.HttpContext.User.GetEmail();
        var skus = prices.Select(p => p.Sku).ToList();
        var products = await db.Products.Where(p => skus.Contains(p.Sku) && p.SupplierEmail == supplier).ToListAsync(ct);
        foreach (var product in products)
        {
            product.Price = prices.Last(p => p.Sku == product.Sku).Amount;
        }

        await db.SaveChangesAsync(ct);
        return Results.Ok(new PriceListResult(products.Count));
    }

    private static async Task<IResult> PlaceAsync(HttpContext http, PartnerOrderDocument document, OrderService orders, KioskDbContext db, CancellationToken ct)
    {
        var skus = document.Lines.Select(l => l.Sku).ToList();
        var ids = await db.Products.Where(p => skus.Contains(p.Sku)).ToDictionaryAsync(p => p.Sku, p => p.Id, ct);
        if (document.Lines.Any(l => !ids.ContainsKey(l.Sku)))
        {
            return Results.Problem(title: "The order names an unknown SKU.", statusCode: StatusCodes.Status400BadRequest);
        }

        var lines = document.Lines.Select(l => new OrderLineRequest(ids[l.Sku], l.Quantity)).ToList();
        var result = await orders.PlaceAsync(
            new PlaceOrder(http.User.GetUserId(), null, lines, document.DeliveryAddress, null, document.Reference, document.Note), ct);
        return result.Order is null
            ? Results.Problem(title: result.Error, statusCode: StatusCodes.Status400BadRequest)
            : Results.Created($"/api/orders/{result.Order.Id}", OrderDto.From(result.Order));
    }

    private static async Task<MemoryStream> BufferAsync(HttpRequest request, CancellationToken ct)
    {
        var buffer = new MemoryStream();
        await request.Body.CopyToAsync(buffer, ct);
        buffer.Position = 0;
        return buffer;
    }
}
