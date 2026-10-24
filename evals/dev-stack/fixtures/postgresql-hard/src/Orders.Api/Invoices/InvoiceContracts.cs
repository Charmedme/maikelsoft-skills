using System.ComponentModel.DataAnnotations;

namespace Orders.Api.Invoices;

public sealed record CreateInvoiceRequest([property: Required] Guid? OrderId);

public sealed record InvoiceResponse(Guid Id, Guid OrderId, string Number, decimal Amount, DateTimeOffset IssuedAt);

public static class InvoiceMapping
{
    public static InvoiceResponse ToResponse(this Invoice invoice) =>
        new(invoice.Id, invoice.OrderId, invoice.Number, invoice.Amount, invoice.IssuedAt);
}
