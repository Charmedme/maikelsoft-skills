namespace Orders.Api.Catalog;

public static class Promotions
{
    public static decimal Discounted(decimal price, int percentOff) =>
        Math.Round(price * (100 - percentOff) / 100m, 2, MidpointRounding.AwayFromZero);
}
