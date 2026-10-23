namespace Orders.Api.Infrastructure;

// Each POST that creates a resource must carry an Idempotency-Key header (1 to 64 visible ASCII characters).
// A key that was used before gives 409, so a retry of the webshop does not create a second order.
public sealed class IdempotencyFilter(IdempotencyStore store) : IEndpointFilter
{
    public const string HeaderName = "Idempotency-Key";

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var key = context.HttpContext.Request.Headers[HeaderName].ToString();
        if (key.Length is 0 or > 64 || key.Any(c => c is < '!' or > '~'))
        {
            return Problems.BadRequest(ProblemCodes.IdempotencyKeyMissing, $"The {HeaderName} header is missing or not valid.");
        }

        if (!store.TryRegister(key))
        {
            return Problems.Conflict(ProblemCodes.IdempotencyKeyReused, $"The {HeaderName} was used before.");
        }

        return await next(context);
    }
}
