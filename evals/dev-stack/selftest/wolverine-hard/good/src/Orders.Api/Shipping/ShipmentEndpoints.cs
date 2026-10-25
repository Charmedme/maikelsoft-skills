using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Orders.Api.Contracts;
using Orders.Api.Data;
using Orders.Api.Orders;
using Wolverine;
using Wolverine.EntityFrameworkCore;

namespace Orders.Api.Shipping;

public static class ShipmentEndpoints
{
    public static IEndpointRouteBuilder MapShipments(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/shipments").WithTags("Shipping");
        group.MapPost("/", CreateAsync);
        group.MapGet("/{id:guid}", GetAsync);
        group.MapPost("/{id:guid}/book", BookAsync);
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

        if (order.Status is not (OrderStatus.Placed or OrderStatus.Paid))
        {
            return TypedResults.Conflict("Only a placed or paid order can get a shipment.");
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

    // Customer service books a parcel by hand: when the automatic booking did not work, or after an address fix.
    private static async Task<Results<Ok<ShipmentResponse>, NotFound, Conflict<string>, UnprocessableEntity<ProblemDetails>>> BookAsync(
        Guid id,
        BookShipmentRequest request,
        IDbContextOutbox<OrdersDbContext> outbox,
        ICarrierClient carrier,
        TimeProvider time,
        CancellationToken ct)
    {
        var db = outbox.DbContext;
        var shipment = await db.Shipments.FirstOrDefaultAsync(s => s.Id == id, ct);
        if (shipment is null)
        {
            return TypedResults.NotFound();
        }

        if (!shipment.CanBeBooked)
        {
            return TypedResults.Conflict("Only a pending shipment or a shipment with an address problem can be booked.");
        }

        var order = await db.Orders.AsNoTracking().Include(o => o.Lines).FirstAsync(o => o.Id == shipment.OrderId, ct);
        if (order.DeliveryAddress is null)
        {
            return TypedResults.Conflict("The order has no delivery address.");
        }

        var customer = await db.Customers.AsNoTracking().FirstAsync(c => c.Id == order.CustomerId, ct);
        CarrierBooking booking;
        try
        {
            booking = await carrier.BookAsync(ParcelBooking.Request(shipment, customer, order.DeliveryAddress, request.Speed!.Value), ct);
        }
        catch (CarrierAddressRejectedException ex)
        {
            return TypedResults.UnprocessableEntity(new ProblemDetails
            {
                Title = "The carrier refused the delivery address.",
                Detail = ex.Message,
                Status = StatusCodes.Status422UnprocessableEntity,
            });
        }

        shipment.Book(booking.Carrier, booking.TrackingNumber, time.GetUtcNow());
        var (booked, pick) = ParcelBooking.Messages(shipment, order);
        await outbox.PublishAsync(booked);
        await outbox.SendAsync(pick);
        await outbox.SaveChangesAndFlushMessagesAsync(ct); // the booking and its messages in one transaction

        return TypedResults.Ok(shipment.ToResponse());
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

        if (shipment.Status is not (ShipmentStatus.Pending or ShipmentStatus.Booked))
        {
            return TypedResults.Conflict("The shipment is already dispatched.");
        }

        var order = await db.Orders.FirstAsync(o => o.Id == shipment.OrderId, ct);
        shipment.Dispatch(carrier.Code, request.TrackingNumber, time.GetUtcNow());
        order.MarkShipped();
        await db.SaveChangesAsync(ct);

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
