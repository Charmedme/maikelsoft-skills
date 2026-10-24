using Microsoft.AspNetCore.Http.HttpResults;

namespace Orders.Api.Common;

public static class Problems
{
    public static ValidationProblem Validation(string field, string message) =>
        TypedResults.ValidationProblem(new Dictionary<string, string[]> { [field] = [message] });
}
