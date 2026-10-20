using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Orders.Api.Data;

namespace Orders.Api.Invoices;

public static class InvoiceEndpoints
{
    public static IEndpointRouteBuilder MapInvoices(this IEndpointRouteBuilder app)
    {
        app.MapGet("/orders/{id:guid}/invoice.pdf", GetInvoiceAsync).WithTags("Orders");
        return app;
    }

    private static async Task<Results<FileContentHttpResult, NotFound>> GetInvoiceAsync(
        Guid id,
        OrdersDbContext db,
        InvoiceRenderer renderer,
        CancellationToken ct)
    {
        var order = await db.Orders
            .AsNoTracking()
            .Include(o => o.Lines)
            .SingleOrDefaultAsync(o => o.Id == id, ct);
        if (order is null)
        {
            return TypedResults.NotFound();
        }

        var pdf = await renderer.RenderAsync(InvoiceTemplate.Render(order), ct);
        return TypedResults.File(pdf, "application/pdf", $"invoice-{order.Id}.pdf");
    }
}
