using FluentValidation;

namespace Orders.Api.Infrastructure;

// Runs the FluentValidation validator of the request body before the handler.
public sealed class ValidationFilter<T>(IValidator<T> validator) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var argument = context.Arguments.OfType<T>().Single();
        var result = await validator.ValidateAsync(argument, context.HttpContext.RequestAborted);
        if (!result.IsValid)
        {
            return result.ToValidationProblem();
        }

        return await next(context);
    }
}
