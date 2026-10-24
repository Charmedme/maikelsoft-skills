using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Orders.Api.Common;
using Orders.Api.Data;
using Orders.Api.Orders;

namespace Orders.Api.Customers;

public static class CustomerEndpoints
{
    public static IEndpointRouteBuilder MapCustomers(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/customers").WithTags("Customers");
        group.MapPost("/", CreateAsync);
        group.MapGet("/{id:guid}", GetAsync);
        group.MapPut("/{id:guid}/name", RenameAsync);
        group.MapGet("/{id:guid}/orders", OrderHistoryAsync);
        return app;
    }

    private static async Task<Results<Created<CustomerResponse>, Conflict<string>>> CreateAsync(
        CreateCustomerRequest request,
        OrdersDbContext db,
        TimeProvider time,
        CancellationToken ct)
    {
        var customer = new Customer(request.Name, request.Email, time.GetUtcNow());
        db.Customers.Add(customer);
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException e) when (e.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            return TypedResults.Conflict("A customer with this email exists.");
        }

        return TypedResults.Created($"/customers/{customer.Id}", customer.ToResponse());
    }

    private static async Task<Results<Ok<CustomerResponse>, NotFound>> GetAsync(Guid id, OrdersDbContext db, CancellationToken ct)
    {
        var customer = await db.Customers.AsNoTracking().SingleOrDefaultAsync(c => c.Id == id, ct);
        return customer is null ? TypedResults.NotFound() : TypedResults.Ok(customer.ToResponse());
    }

    private static async Task<Results<Ok<CustomerResponse>, NotFound>> RenameAsync(
        Guid id,
        RenameCustomerRequest request,
        OrdersDbContext db,
        CancellationToken ct)
    {
        var customer = await db.Customers.SingleOrDefaultAsync(c => c.Id == id, ct);
        if (customer is null)
        {
            return TypedResults.NotFound();
        }

        customer.Rename(request.Name);
        await db.SaveChangesAsync(ct);
        return TypedResults.Ok(customer.ToResponse());
    }

    /// <summary>The order history of a customer for the account page, newest first by default. 'sort' is a column name.</summary>
    private static async Task<Results<Ok<List<OrderSummaryResponse>>, ValidationProblem>> OrderHistoryAsync(
        Guid id,
        string? sort,
        int? page,
        OrdersDbContext db,
        CancellationToken ct)
    {
        const int pageSize = 20;
        if (page is < 1)
        {
            return Problems.Validation(nameof(page), "The first page is 1.");
        }

        var orderBy = string.IsNullOrWhiteSpace(sort) ? "PlacedAt" : sort;
        var offset = ((page ?? 1) - 1) * pageSize;
        var sql = $"""
            SELECT o."Id", o."Status", o."PlacedAt", o."Express",
                   coalesce(sum(l."Quantity" * l."UnitPrice"), 0) AS "Subtotal"
            FROM orders AS o
            LEFT JOIN order_lines AS l ON l."OrderId" = o."Id"
            WHERE o."CustomerId" = @p0
            GROUP BY o."Id"
            ORDER BY "{orderBy}" DESC, o."Id"
            LIMIT {pageSize} OFFSET {offset}
            """;
        var rows = await db.Database.SqlQueryRaw<OrderSummaryResponse>(sql, id).ToListAsync(ct);
        return TypedResults.Ok(rows);
    }
}
