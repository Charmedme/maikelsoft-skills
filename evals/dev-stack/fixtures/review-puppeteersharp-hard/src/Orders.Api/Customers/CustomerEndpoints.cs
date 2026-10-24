using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
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
        group.MapPut("/{id:guid}/branding", SetBrandingAsync);
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

    private static async Task<Results<Ok<BrandingResponse>, NotFound, ValidationProblem>> SetBrandingAsync(
        Guid id,
        BrandingRequest request,
        OrdersDbContext db,
        CustomerLogoFetcher logos,
        CancellationToken ct)
    {
        var customer = await db.Customers.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (customer is null)
        {
            return TypedResults.NotFound();
        }

        if (customer.Kind != CustomerKind.Business)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["Kind"] = ["Only a business customer can have branding."],
            });
        }

        LogoImage? logo = null;
        if (request.LogoUrl is not null)
        {
            logo = await logos.FetchAsync(request.LogoUrl, ct);
            if (logo is null)
            {
                return TypedResults.ValidationProblem(new Dictionary<string, string[]>
                {
                    [nameof(request.LogoUrl)] = ["The logo could not be loaded. Use a PNG or JPEG on a public https address."],
                });
            }
        }

        customer.SetBranding(request.LogoUrl, logo, request.FooterText);
        await db.SaveChangesAsync(ct);
        return TypedResults.Ok(new BrandingResponse(customer.BrandingLogoUrl, customer.BrandingFooter));
    }
}
