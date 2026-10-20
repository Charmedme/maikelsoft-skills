using FluentValidation;
using FluentValidation.Results;

namespace Orders.Api.Orders;

// Runs the FluentValidation validator of T before the handler.
public sealed class ValidationFilter<T>(IValidator<T> validator) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        if (context.Arguments.OfType<T>().FirstOrDefault() is not { } argument)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "The request body is missing.",
                extensions: new Dictionary<string, object?> { ["code"] = "request.body_missing" });
        }

        var result = await validator.ValidateAsync(argument, context.HttpContext.RequestAborted);
        return result.IsValid ? await next(context) : result.ToValidationProblem();
    }
}

public static class ValidationResultExtensions
{
    // Same shape as decision API-20: messages in "errors", stable codes in "errorCodes".
    public static IResult ToValidationProblem(this ValidationResult result) =>
        TypedResults.ValidationProblem(
            result.ToDictionary(),
            extensions: new Dictionary<string, object?>
            {
                ["code"] = "validation.failed",
                ["errorCodes"] = result.Errors
                    .GroupBy(error => error.PropertyName)
                    .ToDictionary(group => group.Key, group => group.Select(error => error.ErrorCode).Distinct().ToArray()),
            });
}
