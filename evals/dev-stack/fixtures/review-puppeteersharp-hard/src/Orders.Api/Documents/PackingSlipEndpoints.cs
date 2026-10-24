using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Orders.Api.Data;

namespace Orders.Api.Documents;

public static class PackingSlipEndpoints
{
    public static IEndpointRouteBuilder MapPackingSlips(this IEndpointRouteBuilder app)
    {
        app.MapGet("/shipments/{id:guid}/packing-slip.pdf", GetAsync).WithTags("Documents");
        return app;
    }

    private static async Task<Results<FileContentHttpResult, NotFound, ProblemHttpResult>> GetAsync(
        Guid id,
        OrdersDbContext db,
        PdfRenderer renderer,
        IOptions<DocumentOptions> options,
        CancellationToken ct)
    {
        var shipment = await db.Shipments.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id, ct);
        if (shipment is null)
        {
            return TypedResults.NotFound();
        }

        var order = await db.Orders.AsNoTracking().Include(o => o.Lines).FirstAsync(o => o.Id == shipment.OrderId, ct);
        var customer = await db.Customers.AsNoTracking().FirstAsync(c => c.Id == order.CustomerId, ct);
        try
        {
            var pdf = await renderer.RenderAsync(PackingSlipTemplate.Render(shipment, order, customer, options.Value), ct);
            return TypedResults.File(pdf, "application/pdf", $"packing-slip-{shipment.Id:N}.pdf");
        }
        catch (TimeoutException)
        {
            // SetContentAsync and PdfDataAsync throw TimeoutException when Documents:RenderTimeoutMs runs out.
            return TypedResults.Problem(title: "The packing slip could not be rendered in time.", statusCode: StatusCodes.Status503ServiceUnavailable);
        }
    }
}
