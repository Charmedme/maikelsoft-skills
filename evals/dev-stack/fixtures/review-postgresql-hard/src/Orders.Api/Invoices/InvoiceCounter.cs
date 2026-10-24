namespace Orders.Api.Invoices;

public sealed class InvoiceCounter
{
    private InvoiceCounter()
    {
    }

    public int Year { get; private set; }

    public long LastNumber { get; private set; }
}
