using Microsoft.EntityFrameworkCore;
using Orders.Api.Data;
using Orders.Api.Infrastructure;

namespace Orders.Api.Invoices;

public static class InvoiceEndpoints
{
    public static IEndpointRouteBuilder MapInvoices(this IEndpointRouteBuilder app)
    {
        app.MapGroup("/orders").WithTags("Invoices").MapGet("/{id:guid}/invoice", GetInvoiceAsync)
            .WithName("GetInvoice")
            .Produces(StatusCodes.Status200OK, contentType: "application/pdf")
            .ProducesProblem(StatusCodes.Status404NotFound);
        return app;
    }

    /// <summary>Downloads the invoice of an order as a PDF file.</summary>
    private static async Task<IResult> GetInvoiceAsync(Guid id, OrdersDbContext db, CancellationToken ct)
    {
        var order = await db.Orders.AsNoTracking().Include(o => o.Lines).SingleOrDefaultAsync(o => o.Id == id, ct);
        if (order is null)
        {
            return Problems.NotFound(ProblemCodes.OrderNotFound, "The order does not exist.");
        }

        var number = InvoiceNumber.For(order.Id, order.PlacedAt);
        return TypedResults.File(InvoiceDocument.Render(order), "application/pdf", $"{number}.pdf");
    }
}
