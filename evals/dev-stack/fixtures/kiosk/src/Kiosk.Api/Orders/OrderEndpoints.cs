using System.Globalization;
using System.Security.Claims;
using Kiosk.Api.Data;
using Kiosk.Api.Hosting;
using Kiosk.Api.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Kiosk.Api.Orders;

public static class OrderEndpoints
{
    public static IEndpointRouteBuilder MapOrderEndpoints(this IEndpointRouteBuilder app)
    {
        var orders = app.MapGroup("/api/orders");
        orders.MapGet("/", ListMineAsync).RequireAuthorization();
        orders.MapGet("/{id:int}", GetAsync).RequireAuthorization();
        orders.MapPost("/", CreateAsync).RequireAuthorization();
        orders.MapPost("/guest", CreateGuestAsync);
        orders.MapPost("/{id:int}/report-damage", ReportDamageAsync).RequireAuthorization();
        return app;
    }

    private static async Task<List<OrderDto>> ListMineAsync(HttpContext http, KioskDbContext db, CancellationToken ct)
    {
        var userId = http.User.GetUserId();
        var orders = await db.Orders.AsNoTracking().Include(o => o.Lines)
            .Where(o => o.CustomerId == userId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync(ct);
        return orders.Select(OrderDto.From).ToList();
    }

    private static async Task<IResult> GetAsync(int id, ClaimsPrincipal user, KioskDbContext db, CancellationToken ct)
    {
        var order = await db.Orders.AsNoTracking().Include(o => o.Lines).SingleOrDefaultAsync(o => o.Id == id, ct);
        if (order is null)
        {
            return Results.NotFound();
        }

        int? callerId = int.TryParse(user.FindFirstValue(JwtRegisteredClaimNames.Sub), NumberStyles.Integer, CultureInfo.InvariantCulture, out var sub) ? sub : null;
        if (order.CustomerId != callerId && !user.IsInRole(Roles.Admin))
        {
            return Results.NotFound();
        }

        return Results.Ok(OrderDto.From(order));
    }

    private static async Task<IResult> CreateAsync(HttpContext http, CreateOrderRequest request, OrderService orders, CancellationToken ct)
    {
        var result = await orders.PlaceAsync(new PlaceOrder(http.User.GetUserId(), null, request.Lines, request.DeliveryAddress, request.CouponCode), ct);
        return result.Order is null
            ? Results.ValidationProblem(new Dictionary<string, string[]> { ["order"] = [result.Error!] })
            : Results.Created($"/api/orders/{result.Order.Id}", OrderDto.From(result.Order));
    }

    private static async Task<IResult> CreateGuestAsync(GuestOrderRequest request, OrderService orders, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || !request.Email.Contains('@', StringComparison.Ordinal))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["email"] = ["Enter an email address."] });
        }

        var result = await orders.PlaceAsync(new PlaceOrder(null, request.Email.Trim().ToLowerInvariant(), request.Lines, request.DeliveryAddress, null), ct);
        return result.Order is null
            ? Results.ValidationProblem(new Dictionary<string, string[]> { ["order"] = [result.Error!] })
            : Results.Created($"/api/orders/{result.Order.Id}", OrderDto.From(result.Order));
    }

    private static async Task<IResult> ReportDamageAsync(int id, HttpContext http, KioskDbContext db, CancellationToken ct)
    {
        var userId = http.User.GetUserId();
        var order = await db.Orders.SingleOrDefaultAsync(o => o.Id == id && o.CustomerId == userId, ct);
        if (order is null)
        {
            return Results.NotFound();
        }

        if (order.Status is not (OrderStatus.Delivered or OrderStatus.DamageReported))
        {
            return Results.Conflict();
        }

        order.Status = OrderStatus.DamageReported;
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }
}
