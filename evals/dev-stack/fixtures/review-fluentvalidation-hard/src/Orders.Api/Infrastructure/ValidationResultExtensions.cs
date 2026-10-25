using FluentValidation.Results;

namespace Orders.Api.Infrastructure;

public static class ValidationResultExtensions
{
    // The messages stay in "errors". The client acts on the stable codes in "errorCodes".
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
