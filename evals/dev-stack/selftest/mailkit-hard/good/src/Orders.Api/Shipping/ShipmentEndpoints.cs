using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Orders.Api.Data;
using Orders.Api.Mail;
using Orders.Api.Orders;

namespace Orders.Api.Shipping;

public static class ShipmentEndpoints
{
    public static IEndpointRouteBuilder MapShipments(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/shipments").WithTags("Shipping");
        group.MapPost("/", CreateAsync);
        group.MapGet("/{id:guid}", GetAsync);
        group.MapPost("/{id:guid}/dispatch", DispatchAsync);
        group.MapPost("/{id:guid}/delivered", MarkDeliveredAsync);
        return app;
    }

    private static async Task<Results<Created<ShipmentResponse>, NotFound, Conflict<string>>> CreateAsync(
        CreateShipmentRequest request,
        OrdersDbContext db,
        TimeProvider time,
        CancellationToken ct)
    {
        var order = await db.Orders.FirstOrDefaultAsync(o => o.Id == request.OrderId, ct);
        if (order is null)
        {
            return TypedResults.NotFound();
        }

        if (order.Status != OrderStatus.Placed)
        {
            return TypedResults.Conflict("Only a placed order can get a shipment.");
        }

        if (await db.Shipments.AnyAsync(s => s.OrderId == order.Id, ct))
        {
            return TypedResults.Conflict("The order already has a shipment.");
        }

        var shipment = new Shipment(order.Id, time.GetUtcNow());
        db.Shipments.Add(shipment);
        await db.SaveChangesAsync(ct);
        return TypedResults.Created($"/shipments/{shipment.Id}", shipment.ToResponse());
    }

    private static async Task<Results<Ok<ShipmentResponse>, NotFound>> GetAsync(Guid id, OrdersDbContext db, CancellationToken ct)
    {
        var shipment = await db.Shipments.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id, ct);
        return shipment is null ? TypedResults.NotFound() : TypedResults.Ok(shipment.ToResponse());
    }

    private static async Task<Results<Ok<ShipmentResponse>, NotFound, ValidationProblem, Conflict<string>>> DispatchAsync(
        Guid id,
        DispatchShipmentRequest request,
        OrdersDbContext db,
        TimeProvider time,
        CancellationToken ct)
    {
        if (!CarrierCatalog.TryGet(request.Carrier, out var carrier))
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                [nameof(request.Carrier)] = [$"Unknown carrier. Known carriers: {string.Join(", ", CarrierCatalog.All.Select(c => c.Code))}."],
            });
        }

        var shipment = await db.Shipments.FirstOrDefaultAsync(s => s.Id == id, ct);
        if (shipment is null)
        {
            return TypedResults.NotFound();
        }

        if (shipment.Status != ShipmentStatus.Pending)
        {
            return TypedResults.Conflict("The shipment is already dispatched.");
        }

        var order = await db.Orders.Include(o => o.Lines).FirstAsync(o => o.Id == shipment.OrderId, ct);
        var customer = await db.Customers.AsNoTracking().FirstAsync(c => c.Id == order.CustomerId, ct);
        var now = time.GetUtcNow();
        shipment.Dispatch(carrier.Code, request.TrackingNumber, now);
        order.MarkShipped();
        db.OutboxMails.AddRange(ShippingNoticeMail.Create(customer, order, shipment, carrier, now));
        await db.SaveChangesAsync(ct); // shipment, order and notices in one transaction

        return TypedResults.Ok(shipment.ToResponse());
    }

    private static async Task<Results<Ok<ShipmentResponse>, NotFound, Conflict<string>>> MarkDeliveredAsync(
        Guid id,
        OrdersDbContext db,
        TimeProvider time,
        CancellationToken ct)
    {
        var shipment = await db.Shipments.FirstOrDefaultAsync(s => s.Id == id, ct);
        if (shipment is null)
        {
            return TypedResults.NotFound();
        }

        if (shipment.Status != ShipmentStatus.Dispatched)
        {
            return TypedResults.Conflict("Only a dispatched shipment can be delivered.");
        }

        shipment.MarkDelivered(time.GetUtcNow());
        await db.SaveChangesAsync(ct);
        return TypedResults.Ok(shipment.ToResponse());
    }
}
