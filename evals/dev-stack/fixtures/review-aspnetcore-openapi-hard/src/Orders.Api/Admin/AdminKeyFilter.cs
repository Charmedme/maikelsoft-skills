using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;

namespace Orders.Api.Admin;

// The admin endpoints are for the back-office tool of our staff. It sends the key in the X-Admin-Key header.
public sealed class AdminKeyFilter(IOptions<AdminOptions> options) : IEndpointFilter
{
    public const string HeaderName = "X-Admin-Key";

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var expected = options.Value.ApiKey;
        var actual = context.HttpContext.Request.Headers[HeaderName].ToString();
        if (string.IsNullOrEmpty(expected)
            || !CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(expected), Encoding.UTF8.GetBytes(actual)))
        {
            return TypedResults.Unauthorized();
        }

        return await next(context);
    }
}
