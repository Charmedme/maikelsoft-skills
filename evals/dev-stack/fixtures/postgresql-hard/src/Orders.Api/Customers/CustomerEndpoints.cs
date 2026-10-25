using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Orders.Api.Data;

namespace Orders.Api.Customers;

public static class CustomerEndpoints
{
    public static IEndpointRouteBuilder MapCustomers(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/customers").WithTags("Customers");
        group.MapPost("/", CreateAsync);
        group.MapGet("/{id:guid}", GetAsync);
        group.MapPut("/{id:guid}/name", RenameAsync);
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
}
