using System.Security.Claims;
using Microsoft.AspNetCore.OutputCaching;

namespace Kiosk.Api.Hosting;

public static class OutputCacheSetup
{
    public const string BasketSnapshot = "basket-snapshot";

    public const string WishlistSnapshot = "wishlist-snapshot";

    public static IServiceCollection AddKioskOutputCache(this IServiceCollection services)
    {
        services.AddOutputCache(options =>
        {
            options.AddPolicy(BasketSnapshot, new BasketSnapshotPolicy());
            options.AddPolicy(WishlistSnapshot, new WishlistSnapshotPolicy());
        });
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
            context.CacheVaryByRules.VaryByValues["user"] = context.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "anonymous";
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

    /// <summary>The wish list heart in the header shows the count of wishes. Same short cache as the basket badge.</summary>
    private sealed class WishlistSnapshotPolicy : IOutputCachePolicy
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
            context.Tags.Add("wishlists");
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
