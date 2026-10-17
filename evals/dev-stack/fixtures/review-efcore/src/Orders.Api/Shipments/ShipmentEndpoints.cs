using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Orders.Api.Data;
using Orders.Api.Orders;

namespace Orders.Api.Shipments;

public static class ShipmentEndpoints
{
    public static IEndpointRouteBuilder MapShipments(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/shipments").WithTags("Shipments");
        group.MapPost("/", CreateAsync);
        group.MapGet("/search", SearchAsync);
        group.MapGet("/by-customer/{customerId:guid}", ByCustomerAsync);
        return app;
    }

    private static async Task<Created<Shipment>> CreateAsync(
        Shipment shipment,
        IRepository<Shipment> shipments,
        IRepository<Order> orders,
        IUnitOfWork unitOfWork,
        CancellationToken ct)
    {
        var order = await orders.GetByIdAsync(shipment.OrderId, ct);
        order!.MarkShipped();
        shipment.ShippedAt = DateTime.Now;
        shipments.Add(shipment);
        await unitOfWork.CommitAsync(ct);
        return TypedResults.Created($"/shipments/{shipment.Id}", shipment);
    }

    private static async Task<Ok<List<Shipment>>> SearchAsync(string carrier, OrdersDbContext db, CancellationToken ct)
    {
        var result = await db.Set<Shipment>()
            .FromSqlRaw($"SELECT * FROM shipments WHERE \"Carrier\" = '{carrier}'")
            .ToListAsync(ct);
        return TypedResults.Ok(result);
    }

    private static async Task<Ok<List<ShipmentSummary>>> ByCustomerAsync(Guid customerId, string? city, OrdersDbContext db, CancellationToken ct)
    {
        var orderIds = await db.Orders
            .Where(order => order.CustomerId == customerId)
            .Select(order => order.Id)
            .ToListAsync(ct);

        var summaries = new List<ShipmentSummary>();
        foreach (var orderId in orderIds)
        {
            var shipment = await db.Set<Shipment>()
                .Where(s => s.OrderId == orderId && (city == null || s.Destination.City.ToLower() == city.ToLower()))
                .Select(s => new ShipmentSummary(s.Id, s.Carrier, s.TrackingCode))
                .FirstOrDefaultAsync(ct);
            if (shipment is not null)
            {
                summaries.Add(shipment);
            }
        }

        return TypedResults.Ok(summaries);
    }
}

public sealed record ShipmentSummary(Guid Id, string Carrier, string TrackingCode);
