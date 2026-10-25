using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Orders.Api.Data;
using Orders.Api.Infrastructure;

namespace Orders.Api.Customers;

public static class CustomerEndpoints
{
    public static IEndpointRouteBuilder MapCustomers(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/customers").WithTags("Customers");
        group.MapPost("/", CreateAsync);
        group.MapGet("/{id:guid}", GetAsync);
        return app;
    }

    /// <summary>Registers a customer. The email address must be unique.</summary>
    private static async Task<Results<Created<CustomerResponse>, ProblemHttpResult>> CreateAsync(
        CreateCustomerRequest request,
        OrdersDbContext db,
        TimeProvider time,
        CancellationToken ct)
    {
        var customer = new Customer(request.Email, request.Name, time.GetUtcNow());
        db.Customers.Add(customer);
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.IsUniqueViolation())
        {
            return Problems.Conflict(ProblemCodes.CustomerEmailTaken, "A customer with this email address exists.");
        }

        return TypedResults.Created($"/customers/{customer.Id}", customer.ToResponse());
    }

    /// <summary>Gets one customer.</summary>
    private static async Task<Results<Ok<CustomerResponse>, NotFound>> GetAsync(Guid id, OrdersDbContext db, CancellationToken ct)
    {
        var customer = await db.Customers.AsNoTracking().SingleOrDefaultAsync(c => c.Id == id, ct);
        return customer is null ? TypedResults.NotFound() : TypedResults.Ok(customer.ToResponse());
    }
}
