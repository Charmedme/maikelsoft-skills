namespace Kiosk.Api.B2B;

public sealed class PartnerOrderDocument
{
    public string Reference { get; set; } = string.Empty;

    public string DeliveryAddress { get; set; } = string.Empty;

    public string? Note { get; set; }

    public List<PartnerOrderLine> Lines { get; set; } = [];

    /// <summary>Partner-specific data. We keep it as sent, for the partner archive.</summary>
    public object? Extensions { get; set; }
}

public sealed class PartnerOrderLine
{
    public string Sku { get; set; } = string.Empty;

    public int Quantity { get; set; }
}

public sealed record PriceUpdate(string Sku, decimal Amount);

public sealed record PriceListResult(int Updated);
