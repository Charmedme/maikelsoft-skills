using FluentValidation.Results;

namespace Orders.Api.Infrastructure;

public static class ValidationResultExtensions
{
    // Messages in "errors" (the standard shape), stable codes per field in "errorCodes" (decision API-20, assumed).
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
