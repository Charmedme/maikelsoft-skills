using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Orders.Api.Data;
using Orders.Api.Orders;

namespace Orders.Api.Shipping;

public static class ShipmentEndpoints
{
    public static IEndpointRouteBuilder MapShipments(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/shipments").WithTags("Shipping");
        group.MapPost("/", CreateAsync);
        group.MapGet("/{id:guid}", GetAsync);
        group.MapPost("/{id:guid}/delivered", MarkDeliveredAsync);
        return app;
    }

    private static async Task<Results<Created<ShipmentResponse>, NotFound, Conflict<string>>> CreateAsync(
        CreateShipmentRequest request,
        OrdersDbContext db,
        TimeProvider time,
        CancellationToken ct)
    {
        var order = await db.Orders.SingleOrDefaultAsync(o => o.Id == request.OrderId, ct);
        if (order is null)
        {
            return TypedResults.NotFound();
        }

        if (order.Status is not (OrderStatus.Placed or OrderStatus.Picking))
        {
            return TypedResults.Conflict($"An order with status {order.Status} cannot be shipped.");
        }

        var shipment = new Shipment(order.Id, request.Carrier!.Value, request.TrackingNumber, time.GetUtcNow());
        order.MarkShipped();
        db.Shipments.Add(shipment);
        await db.SaveChangesAsync(ct);
        return TypedResults.Created($"/shipments/{shipment.Id}", shipment.ToResponse());
    }

    private static async Task<Results<Ok<ShipmentResponse>, NotFound>> GetAsync(Guid id, OrdersDbContext db, CancellationToken ct)
    {
        var shipment = await db.Shipments.AsNoTracking().SingleOrDefaultAsync(s => s.Id == id, ct);
        return shipment is null ? TypedResults.NotFound() : TypedResults.Ok(shipment.ToResponse());
    }

    private static async Task<Results<Ok<ShipmentResponse>, NotFound>> MarkDeliveredAsync(
        Guid id,
        OrdersDbContext db,
        TimeProvider time,
        CancellationToken ct)
    {
        var shipment = await db.Shipments.SingleOrDefaultAsync(s => s.Id == id, ct);
        if (shipment is null)
        {
            return TypedResults.NotFound();
        }

        if (shipment.MarkDelivered(time.GetUtcNow()))
        {
            await db.SaveChangesAsync(ct);
        }

        return TypedResults.Ok(shipment.ToResponse());
    }
}
