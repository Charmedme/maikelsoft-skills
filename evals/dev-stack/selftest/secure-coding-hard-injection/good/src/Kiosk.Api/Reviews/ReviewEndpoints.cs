using Kiosk.Api.Data;
using Kiosk.Api.Hosting;
using Microsoft.EntityFrameworkCore;

namespace Kiosk.Api.Reviews;

public static class ReviewEndpoints
{
    public static IEndpointRouteBuilder MapReviewEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/products/{productId:int}/reviews", ListAsync);
        app.MapPost("/api/products/{productId:int}/reviews", CreateAsync).RequireAuthorization();
        app.MapPatch("/api/reviews", UpdateAsync).RequireAuthorization();
        return app;
    }

    private static async Task<List<ReviewDto>> ListAsync(int productId, KioskDbContext db, CancellationToken ct) =>
        (await db.Reviews.AsNoTracking().Where(r => r.ProductId == productId).OrderByDescending(r => r.CreatedAt).ToListAsync(ct))
            .Select(ReviewDto.From).ToList();

    private static async Task<IResult> CreateAsync(int productId, CreateReviewRequest request, HttpContext http, KioskDbContext db, TimeProvider time, CancellationToken ct)
    {
        if (!await db.Products.AnyAsync(p => p.Id == productId && p.DeletedAt == null, ct))
        {
            return Results.NotFound();
        }

        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["message"] = ["Write a review text."] });
        }

        var review = new Review
        {
            ProductId = productId,
            Author = request.Author ?? http.User.GetEmail(),
            Message = request.Message.Trim(),
            Rating = request.Rating,
            CreatedAt = time.GetUtcNow(),
        };
        db.Reviews.Add(review);
        await db.SaveChangesAsync(ct);
        return Results.Created($"/api/products/{productId}/reviews", ReviewDto.From(review));
    }

    private static async Task<IResult> UpdateAsync(UpdateReviewRequest request, HttpContext http, KioskDbContext db, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["message"] = ["Write a review text."] });
        }

        var author = http.User.GetEmail();
        var message = request.Message.Trim();
        var modified = await db.Reviews.Where(r => r.Id == request.Id && r.Author == author)
            .ExecuteUpdateAsync(s => s.SetProperty(r => r.Message, message), ct);
        return modified == 0 ? Results.NotFound() : Results.Ok(new ReviewUpdateResult(modified));
    }
}
