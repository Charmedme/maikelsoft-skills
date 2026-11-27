namespace Kiosk.Api.Reviews;

public sealed record CreateReviewRequest(string Message, int Rating);

public sealed record ReviewDto(int Id, int ProductId, string Author, string Message, int Rating, DateTimeOffset CreatedAt)
{
    public static ReviewDto From(Review r) => new(r.Id, r.ProductId, r.Author, r.Message, r.Rating, r.CreatedAt);
}

public sealed record ReviewUpdateResult(int Modified);
