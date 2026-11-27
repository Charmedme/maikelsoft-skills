using Kiosk.Api.Catalog;

namespace Kiosk.Api.Basket;

public sealed class BasketItem
{
    public int Id { get; set; }

    public int BasketId { get; set; }

    public int ProductId { get; set; }

    public Product? Product { get; set; }

    public int Quantity { get; set; }
}
