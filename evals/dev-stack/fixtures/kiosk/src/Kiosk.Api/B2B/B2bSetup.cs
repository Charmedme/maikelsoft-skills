namespace Kiosk.Api.B2B;

public static class B2bSetup
{
    public static IServiceCollection AddB2b(this IServiceCollection services)
    {
        services.AddSingleton<XmlOrderImporter>();
        services.AddSingleton<JsonOrderImporter>();
        services.AddSingleton<PriceListImporter>();
        return services;
    }
}
