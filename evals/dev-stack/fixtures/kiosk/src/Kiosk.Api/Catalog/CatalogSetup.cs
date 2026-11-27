namespace Kiosk.Api.Catalog;

public static class CatalogSetup
{
    public static IServiceCollection AddCatalog(this IServiceCollection services)
    {
        services.AddScoped<ProductQueries>();
        services.AddScoped<ProductSearch>();
        return services;
    }
}
