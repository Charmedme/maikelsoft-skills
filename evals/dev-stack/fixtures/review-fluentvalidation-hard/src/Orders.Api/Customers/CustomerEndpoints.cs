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
        group.MapPost("/", CreateAsync)
            .AddEndpointFilter<ValidationFilter<CreateCustomerRequest>>()
            .ProducesValidationProblem();
        group.MapGet("/{id:guid}", GetAsync);
        group.MapPut("/{id:guid}", UpdateAsync)
            .AddEndpointFilter<ValidationFilter<UpdateCustomerRequest>>()
            .ProducesValidationProblem();
        return app;
    }

    /// <summary>Registers a customer with a delivery address. The email address must be unique.</summary>
    private static async Task<Created<CustomerResponse>> CreateAsync(
        CreateCustomerRequest request,
        OrdersDbContext db,
        TimeProvider time,
        CancellationToken ct)
    {
        // CreateCustomerRequestValidator checks that the email address is not taken.
        var customer = new Customer(request.Email, request.Name, DeliveryAddress.From(request.Address), time.GetUtcNow());
        db.Customers.Add(customer);
        await db.SaveChangesAsync(ct);

        return TypedResults.Created($"/customers/{customer.Id}", customer.ToResponse());
    }

    /// <summary>Gets one customer.</summary>
    private static async Task<Results<Ok<CustomerResponse>, NotFound>> GetAsync(Guid id, OrdersDbContext db, CancellationToken ct)
    {
        var customer = await db.Customers.AsNoTracking().SingleOrDefaultAsync(c => c.Id == id, ct);
        return customer is null ? TypedResults.NotFound() : TypedResults.Ok(customer.ToResponse());
    }

    /// <summary>Changes the email address, the name, and optionally the delivery address of a customer.</summary>
    private static async Task<Results<Ok<CustomerResponse>, ProblemHttpResult>> UpdateAsync(
        Guid id,
        UpdateCustomerRequest request,
        OrdersDbContext db,
        CancellationToken ct)
    {
        var customer = await db.Customers.SingleOrDefaultAsync(c => c.Id == id, ct);
        if (customer is null)
        {
            return Problems.NotFound(ProblemCodes.CustomerNotFound, "The customer does not exist.");
        }

        customer.Update(request.Email, request.Name, request.Address is null ? null : DeliveryAddress.From(request.Address));
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.IsUniqueViolation())
        {
            return Problems.Conflict(ProblemCodes.CustomerEmailTaken, "A customer with this email address exists.");
        }

        return TypedResults.Ok(customer.ToResponse());
    }
}
