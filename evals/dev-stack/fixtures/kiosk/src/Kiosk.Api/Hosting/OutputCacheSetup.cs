using Microsoft.AspNetCore.OutputCaching;

namespace Kiosk.Api.Hosting;

public static class OutputCacheSetup
{
    public const string BasketSnapshot = "basket-snapshot";

    public static IServiceCollection AddKioskOutputCache(this IServiceCollection services)
    {
        services.AddOutputCache(options => options.AddPolicy(BasketSnapshot, new BasketSnapshotPolicy()));
        return services;
    }

    /// <summary>
    /// The basket badge in the header polls the basket every few seconds. A short cache keeps that load off the database.
    /// </summary>
    private sealed class BasketSnapshotPolicy : IOutputCachePolicy
    {
        public ValueTask CacheRequestAsync(OutputCacheContext context, CancellationToken cancellation)
        {
            var isGet = HttpMethods.IsGet(context.HttpContext.Request.Method);
            context.EnableOutputCaching = true;
            context.AllowCacheLookup = isGet;
            context.AllowCacheStorage = isGet;
            context.AllowLocking = true;
            context.ResponseExpirationTimeSpan = TimeSpan.FromSeconds(20);
            context.CacheVaryByRules.QueryKeys = "*";
            context.Tags.Add("baskets");
            return ValueTask.CompletedTask;
        }

        public ValueTask ServeFromCacheAsync(OutputCacheContext context, CancellationToken cancellation) => ValueTask.CompletedTask;

        public ValueTask ServeResponseAsync(OutputCacheContext context, CancellationToken cancellation)
        {
            if (context.HttpContext.Response.StatusCode != StatusCodes.Status200OK)
            {
                context.AllowCacheStorage = false;
            }

            return ValueTask.CompletedTask;
        }
    }
}
