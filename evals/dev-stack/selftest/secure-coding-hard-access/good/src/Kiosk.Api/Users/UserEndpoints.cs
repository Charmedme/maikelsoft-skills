using Kiosk.Api.Basket;
using Kiosk.Api.Data;
using Kiosk.Api.Hosting;
using Microsoft.AspNetCore.JsonPatch.SystemTextJson;
using Microsoft.AspNetCore.JsonPatch.SystemTextJson.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Kiosk.Api.Users;

public static class UserEndpoints
{
    public static IEndpointRouteBuilder MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        var users = app.MapGroup("/api/users");
        users.MapPost("/", RegisterAsync);
        users.MapGet("/security-questions", async (KioskDbContext db, CancellationToken ct) =>
            await db.SecurityQuestions.AsNoTracking().OrderBy(q => q.Id).Select(q => new SecurityQuestionResponse(q.Id, q.Text)).ToListAsync(ct));

        var me = users.MapGroup("/me").RequireAuthorization();
        me.MapGet("/", GetMeAsync);
        me.MapPatch("/", PatchMeAsync);
        me.MapPut("/profile", UpdateProfileAsync);
        me.MapPut("/profile-image", SetProfileImageAsync);
        return app;
    }

    private static async Task<IResult> RegisterAsync(RegisterRequest request, KioskDbContext db, IPasswordHasher hasher, TimeProvider time, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || !request.Email.Contains('@', StringComparison.Ordinal) || string.IsNullOrEmpty(request.Password))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["email"] = ["Enter an email address and a password."] });
        }

        var email = request.Email.Trim().ToLowerInvariant();
        if (await db.Users.AnyAsync(u => u.Email == email, ct))
        {
            return Results.Conflict();
        }

        var user = new User
        {
            Email = email,
            PasswordHash = hasher.Hash(request.Password),
            Role = Roles.Customer,
            DisplayName = request.DisplayName,
            SecurityQuestionId = request.SecurityQuestionId,
            SecurityAnswer = request.SecurityAnswer,
            CreatedAt = time.GetUtcNow(),
        };
        db.Users.Add(user);
        await db.SaveChangesAsync(ct);
        db.Baskets.Add(new ShoppingBasket { UserId = user.Id });
        await db.SaveChangesAsync(ct);
        return Results.Created($"/api/users/{user.Id}", new UserSummary(user.Id, user.Email, user.Role, user.DisplayName));
    }

    private static async Task<IResult> GetMeAsync(HttpContext http, KioskDbContext db, CancellationToken ct)
    {
        var user = await db.Users.AsNoTracking().SingleOrDefaultAsync(u => u.Id == http.User.GetUserId(), ct);
        return user is null ? Results.NotFound() : Results.Ok(new UserProfile(user.Id, user.Email, user.Role, user.DisplayName, user.ProfileImage));
    }

    private static async Task<IResult> PatchMeAsync(HttpContext http, JsonPatchDocument<ProfilePatch> patch, KioskDbContext db, CancellationToken ct)
    {
        var user = await db.Users.SingleOrDefaultAsync(u => u.Id == http.User.GetUserId(), ct);
        if (user is null)
        {
            return Results.NotFound();
        }

        var profile = new ProfilePatch { DisplayName = user.DisplayName };
        try
        {
            patch.ApplyTo(profile);
            user.DisplayName = profile.DisplayName?.Trim();
        }
        catch (JsonPatchException ex)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["patch"] = [ex.Message] });
        }

        await db.SaveChangesAsync(ct);
        return Results.Ok(new UserSummary(user.Id, user.Email, user.Role, user.DisplayName));
    }

    private static async Task<IResult> UpdateProfileAsync(HttpContext http, ProfileRequest request, KioskDbContext db, CancellationToken ct)
    {
        var user = await db.Users.SingleOrDefaultAsync(u => u.Id == http.User.GetUserId(), ct);
        if (user is null)
        {
            return Results.NotFound();
        }

        user.DisplayName = request.DisplayName?.Trim();
        await db.SaveChangesAsync(ct);
        return Results.Ok(new UserSummary(user.Id, user.Email, user.Role, user.DisplayName));
    }

    private static async Task<IResult> SetProfileImageAsync(HttpContext http, ProfileImageRequest request, KioskDbContext db, ProfileImageFetcher fetcher, CancellationToken ct)
    {
        if (!Uri.TryCreate(request.Url, UriKind.Absolute, out var url) || (url.Scheme != Uri.UriSchemeHttp && url.Scheme != Uri.UriSchemeHttps))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["url"] = ["Enter an http or https address."] });
        }

        var user = await db.Users.SingleAsync(u => u.Id == http.User.GetUserId(), ct);
        try
        {
            user.ProfileImage = await fetcher.DownloadAsync(user.Id, url, ct);
        }
        catch (HttpRequestException)
        {
            return Results.Problem(title: "The image could not be loaded.", statusCode: StatusCodes.Status400BadRequest);
        }

        await db.SaveChangesAsync(ct);
        return Results.Ok(new { profileImage = user.ProfileImage });
    }
}
