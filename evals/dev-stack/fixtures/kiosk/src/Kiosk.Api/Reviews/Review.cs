namespace Kiosk.Api.Reviews;

public sealed class Review
{
    public int Id { get; set; }

    public int ProductId { get; set; }

    public required string Author { get; set; }

    public required string Message { get; set; }

    public int Rating { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
