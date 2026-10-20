using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Orders.Api.Data;

namespace Orders.Api.Invoices;

public static class InvoiceEndpoints
{
    public static IEndpointRouteBuilder MapInvoices(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/invoices").WithTags("Invoices");
        group.MapGet("/{orderId:guid}.pdf", GetInvoiceAsync);
        group.MapGet("/render", RenderPageAsync);
        return app;
    }

    private static async Task<Results<FileContentHttpResult, NotFound>> GetInvoiceAsync(
        Guid orderId,
        OrdersDbContext db,
        InvoiceRenderer renderer,
        CancellationToken ct)
    {
        var order = await db.Orders.Include(o => o.Lines).SingleOrDefaultAsync(o => o.Id == orderId, ct);
        if (order is null)
        {
            return TypedResults.NotFound();
        }

        var pdf = await renderer.RenderHtmlAsync(InvoiceTemplate.Render(order), ct);
        return TypedResults.File(pdf, "application/pdf", $"invoice-{order.Id}.pdf");
    }

    // Support uses this to turn any page of the webshop into a PDF for a customer.
    private static async Task<FileContentHttpResult> RenderPageAsync(Uri url, InvoiceRenderer renderer, CancellationToken ct)
    {
        var pdf = await renderer.RenderUrlAsync(url, ct);
        return TypedResults.File(pdf, "application/pdf", "page.pdf");
    }
}
