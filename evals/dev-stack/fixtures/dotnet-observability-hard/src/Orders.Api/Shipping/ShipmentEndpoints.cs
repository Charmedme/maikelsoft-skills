using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Orders.Api.Data;
using Orders.Api.Orders;
using Orders.Api.Shipping.Carriers;

namespace Orders.Api.Shipping;

public static class ShipmentEndpoints
{
    private const string LogCategory = "Orders.Api.Shipping";

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
        ICarrierGateway carriers,
        ShippingMetrics metrics,
        ILoggerFactory loggerFactory,
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

        // Every log line of a shipment step carries the shipment and the order, so that support can
        // filter on ShipmentId in Application Insights.
        var logger = loggerFactory.CreateLogger(LogCategory);
        using var scope = logger.BeginScope(new Dictionary<string, object?>
        {
            ["ShipmentId"] = shipment.Id,
            ["OrderId"] = shipment.OrderId,
        });
        using var activity = ShippingTelemetry.Source.StartActivity("shipment.dispatch");
        activity?.SetTag("shipment.id", shipment.Id);
        activity?.SetTag("carrier.code", carrier.Code);

        if (shipment.Status != ShipmentStatus.Pending)
        {
            return TypedResults.Conflict("The shipment is already dispatched.");
        }

        var order = await db.Orders.FirstAsync(o => o.Id == shipment.OrderId, ct);
        if (order.DeliveryAddress is null)
        {
            logger.LogWarning("Order {OrderId} has no delivery address", order.Id);
            return TypedResults.Conflict("The order has no delivery address.");
        }

        var customer = await db.Customers.AsNoTracking().FirstAsync(c => c.Id == order.CustomerId, ct);
        CarrierBooking booking;
        try
        {
            booking = await carriers.BookAsync(
                new CarrierBookingRequest(shipment.Id, carrier.Code, order.DeliveryAddress, customer.Email, order.DeliveryInstructions),
                ct);
        }
        catch (CarrierBookingException ex)
        {
            logger.LogError("Booking with carrier {Carrier} failed: {Reason}", carrier.Code, ex.Message);
            throw;
        }

        shipment.Dispatch(carrier.Code, booking.TrackingNumber, time.GetUtcNow());
        order.MarkShipped();
        await db.SaveChangesAsync(ct);
        metrics.Dispatched(carrier.Code, order.CustomerId);

        return TypedResults.Ok(shipment.ToResponse());
    }

    private static async Task<Results<Ok<ShipmentResponse>, NotFound, Conflict<string>>> MarkDeliveredAsync(
        Guid id,
        OrdersDbContext db,
        ShippingMetrics metrics,
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

        using var activity = ShippingTelemetry.Source.StartActivity("shipment.delivered");
        activity?.SetTag("shipment.id", shipment.Id);
        shipment.MarkDelivered(time.GetUtcNow());
        await db.SaveChangesAsync(ct);
        metrics.Delivered(shipment.Carrier!);
        return TypedResults.Ok(shipment.ToResponse());
    }
}
