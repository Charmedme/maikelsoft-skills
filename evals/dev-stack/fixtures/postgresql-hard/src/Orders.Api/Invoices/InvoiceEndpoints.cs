using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Orders.Api.Data;
using Orders.Api.Orders;

namespace Orders.Api.Invoices;

public static class InvoiceEndpoints
{
    private static readonly TimeZoneInfo s_dutchTime = TimeZoneInfo.FindSystemTimeZoneById("Europe/Amsterdam");

    public static IEndpointRouteBuilder MapInvoices(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/invoices").WithTags("Invoices");
        group.MapPost("/", CreateAsync);
        group.MapGet("/{number}", GetAsync);
        return app;
    }

    private static async Task<Results<Created<InvoiceResponse>, NotFound, Conflict<string>>> CreateAsync(
        CreateInvoiceRequest request,
        OrdersDbContext db,
        TimeProvider time,
        CancellationToken ct)
    {
        var order = await db.Orders.AsNoTracking().Include(o => o.Lines).SingleOrDefaultAsync(o => o.Id == request.OrderId, ct);
        if (order is null)
        {
            return TypedResults.NotFound();
        }

        if (order.Status != OrderStatus.Shipped)
        {
            return TypedResults.Conflict("Only a shipped order gets an invoice.");
        }

        var now = time.GetUtcNow();
        var year = TimeZoneInfo.ConvertTime(now, s_dutchTime).Year;
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var invoice = new Invoice(order.Id, await InvoiceNumbers.NextAsync(db, year, ct), order.Total, now);
        db.Invoices.Add(invoice);
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException e) when (e.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            return TypedResults.Conflict("The order already has an invoice.");
        }

        await transaction.CommitAsync(ct);
        return TypedResults.Created($"/invoices/{invoice.Number}", invoice.ToResponse());
    }

    private static async Task<Results<Ok<InvoiceResponse>, NotFound>> GetAsync(string number, OrdersDbContext db, CancellationToken ct)
    {
        var invoice = await db.Invoices.AsNoTracking().SingleOrDefaultAsync(i => i.Number == number, ct);
        return invoice is null ? TypedResults.NotFound() : TypedResults.Ok(invoice.ToResponse());
    }
}
