namespace Kiosk.Api.Orders;

public static class OrdersSetup
{
    public static IServiceCollection AddOrders(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<PaymentOptions>(configuration.GetSection(PaymentOptions.Section));
        services.AddScoped<IPaymentGateway, PaymentGateway>();
        services.AddScoped<OrderService>();
        return services;
    }
}
