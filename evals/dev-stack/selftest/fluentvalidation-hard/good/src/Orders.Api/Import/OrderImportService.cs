using FluentValidation;
using Orders.Api.Data;
using Orders.Api.Orders;

namespace Orders.Api.Import;

// Imports the orders of the sales team. A rejected order does not stop the import; the report lists its codes.
public sealed class OrderImportService(IValidator<CreateOrderRequest> validator, OrdersDbContext db, TimeProvider time)
{
    public async Task<ImportReport> ImportAsync(IReadOnlyList<ImportedOrder> orders, CancellationToken ct)
    {
        var rejected = new List<ImportRejection>();
        var imported = 0;
        foreach (var item in orders)
        {
            var result = await validator.ValidateAsync(item.Request, ct);
            if (!result.IsValid)
            {
                rejected.Add(new ImportRejection(item.ExternalRef, [.. result.Errors.Select(error => error.ErrorCode).Distinct()]));
                continue;
            }

            var order = new Order(item.Request.CustomerId!.Value, time.GetUtcNow());
            foreach (var line in item.Request.Lines)
            {
                order.AddLine(line.Sku, line.Quantity, line.UnitPrice);
            }

            db.Orders.Add(order);
            imported++;
        }

        await db.SaveChangesAsync(ct);
        return new ImportReport(imported, rejected);
    }
}
