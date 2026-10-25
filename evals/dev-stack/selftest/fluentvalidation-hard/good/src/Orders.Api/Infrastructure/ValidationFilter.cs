using FluentValidation;

namespace Orders.Api.Infrastructure;

// Runs the FluentValidation validator of the request body before the handler.
public sealed class ValidationFilter<T>(IValidator<T> validator) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        // In Production a JSON null body reaches the filter as null. ValidateAsync(null) throws.
        if (context.Arguments.OfType<T>().FirstOrDefault() is not { } argument)
        {
            return Problems.BadRequest("request.body_missing", "The request body is missing.");
        }

        var result = await validator.ValidateAsync(argument, context.HttpContext.RequestAborted);
        return result.IsValid ? await next(context) : result.ToValidationProblem();
    }
}
