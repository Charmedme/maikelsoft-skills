using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Orders.Api.Data;
using Orders.Api.Infrastructure;
using Orders.Api.Orders;

namespace Orders.Api.Shipping;

public static class ShippingEndpoints
{
    // The customer reads the shipment of an order. The warehouse ships an order through the admin group.
    public static IEndpointRouteBuilder MapShipping(this IEndpointRouteBuilder app)
    {
        app.MapGroup("/orders").WithTags("Shipping").MapGet("/{id:guid}/shipment", GetAsync).WithName("GetShipment");
        return app;
    }

    public static RouteGroupBuilder MapShippingAdmin(this RouteGroupBuilder admin)
    {
        admin.MapPost("/orders/{id:guid}/ship", ShipAsync).WithName("ShipOrder");
        return admin;
    }

    /// <summary>Gets the shipment and the tracking link of an order.</summary>
    private static async Task<Results<Ok<ShipmentResponse>, NotFound>> GetAsync(Guid id, OrdersDbContext db, CancellationToken ct)
    {
        var shipment = await db.Shipments.AsNoTracking().SingleOrDefaultAsync(s => s.OrderId == id, ct);
        return shipment is null ? TypedResults.NotFound() : TypedResults.Ok(shipment.ToResponse());
    }

    /// <summary>Marks an order as shipped and stores the tracking code.</summary>
    private static async Task<Results<Ok<ShipmentResponse>, ProblemHttpResult>> ShipAsync(
        Guid id,
        ShipOrderRequest request,
        OrdersDbContext db,
        TimeProvider time,
        CancellationToken ct)
    {
        var order = await db.Orders.SingleOrDefaultAsync(o => o.Id == id, ct);
        if (order is null)
        {
            return Problems.NotFound(ProblemCodes.OrderNotFound, "The order does not exist.");
        }

        if (order.Status != OrderStatus.Placed)
        {
            return Problems.Conflict(ProblemCodes.OrderAlreadyShipped, "Only a placed order can be shipped.");
        }

        var now = time.GetUtcNow();
        order.MarkShipped(now);
        var shipment = new Shipment(order.Id, request.Carrier, request.TrackingCode, now);
        db.Shipments.Add(shipment);
        await db.SaveChangesAsync(ct);

        return TypedResults.Ok(shipment.ToResponse());
    }
}
