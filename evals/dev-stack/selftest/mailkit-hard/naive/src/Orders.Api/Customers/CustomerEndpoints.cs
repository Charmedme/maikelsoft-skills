using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using MimeKit;
using Orders.Api.Data;

namespace Orders.Api.Customers;

public static class CustomerEndpoints
{
    public static IEndpointRouteBuilder MapCustomers(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/customers").WithTags("Customers");
        group.MapPost("/", CreateAsync);
        group.MapGet("/{id:guid}", GetAsync);
        group.MapPut("/{id:guid}/email", ChangeEmailAsync);
        group.MapPut("/{id:guid}/notification-addresses", ReplaceNotificationAddressesAsync);
        return app;
    }

    private static async Task<Results<Created<CustomerResponse>, ValidationProblem>> CreateAsync(
        CreateCustomerRequest request,
        OrdersDbContext db,
        TimeProvider time,
        CancellationToken ct)
    {
        if (request.Kind == CustomerKind.Business && string.IsNullOrWhiteSpace(request.CompanyName))
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                [nameof(request.CompanyName)] = ["A business customer needs a company name."],
            });
        }

        var customer = new Customer(request.Name, request.Email, request.Kind!.Value, request.CompanyName, time.GetUtcNow());
        db.Customers.Add(customer);
        await db.SaveChangesAsync(ct);
        return TypedResults.Created($"/customers/{customer.Id}", customer.ToResponse());
    }

    private static async Task<Results<Ok<CustomerResponse>, NotFound>> GetAsync(Guid id, OrdersDbContext db, CancellationToken ct)
    {
        var customer = await db.Customers.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id, ct);
        return customer is null ? TypedResults.NotFound() : TypedResults.Ok(customer.ToResponse());
    }

    private static async Task<Results<Ok<CustomerResponse>, NotFound>> ChangeEmailAsync(
        Guid id,
        ChangeEmailRequest request,
        OrdersDbContext db,
        CancellationToken ct)
    {
        var customer = await db.Customers.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (customer is null)
        {
            return TypedResults.NotFound();
        }

        customer.ChangeEmail(request.Email);
        await db.SaveChangesAsync(ct);
        return TypedResults.Ok(customer.ToResponse());
    }

    private static async Task<Results<Ok<CustomerResponse>, NotFound, ValidationProblem>> ReplaceNotificationAddressesAsync(
        Guid id,
        NotificationAddressesRequest request,
        OrdersDbContext db,
        CancellationToken ct)
    {
        var invalid = request.Addresses.Where(a => !MailboxAddress.TryParse(a, out _)).ToList();
        if (invalid.Count > 0)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                [nameof(request.Addresses)] = ["Each notification address must be a valid email address."],
            });
        }

        var customer = await db.Customers.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (customer is null)
        {
            return TypedResults.NotFound();
        }

        customer.ReplaceNotificationAddresses(request.Addresses.Select(a => a.Trim()).Distinct(StringComparer.OrdinalIgnoreCase));
        await db.SaveChangesAsync(ct);
        return TypedResults.Ok(customer.ToResponse());
    }
}
