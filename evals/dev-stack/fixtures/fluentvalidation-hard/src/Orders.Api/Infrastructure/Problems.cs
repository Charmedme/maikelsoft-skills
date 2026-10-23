using Microsoft.AspNetCore.Http.HttpResults;

namespace Orders.Api.Infrastructure;

public static class Problems
{
    public static ProblemHttpResult NotFound(string code, string title) => Create(StatusCodes.Status404NotFound, code, title);

    public static ProblemHttpResult Conflict(string code, string title) => Create(StatusCodes.Status409Conflict, code, title);

    public static ProblemHttpResult BadRequest(string code, string title) => Create(StatusCodes.Status400BadRequest, code, title);

    public static ProblemHttpResult UnprocessableEntity(string code, string title, string detail) =>
        Create(StatusCodes.Status422UnprocessableEntity, code, title, detail);

    private static ProblemHttpResult Create(int status, string code, string title, string? detail = null) =>
        TypedResults.Problem(
            statusCode: status,
            title: title,
            detail: detail,
            extensions: new Dictionary<string, object?> { ["code"] = code });
}
