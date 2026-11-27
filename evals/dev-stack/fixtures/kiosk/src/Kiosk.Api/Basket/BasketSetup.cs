namespace Kiosk.Api.Basket;

public static class BasketSetup
{
    public static IServiceCollection AddBasket(this IServiceCollection services)
    {
        services.AddScoped<BasketService>();
        return services;
    }
}
