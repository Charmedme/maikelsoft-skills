using Kiosk.Api.Data;
using Kiosk.Api.Hosting;
using Microsoft.EntityFrameworkCore;

namespace Kiosk.Api.Users;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/auth/login", LoginAsync).RequireRateLimiting(RateLimitingSetup.Login);
        return app;
    }

    private static async Task<IResult> LoginAsync(LoginRequest request, KioskDbContext db, IPasswordHasher hasher, TokenService tokens, CancellationToken ct)
    {
        var hash = hasher.Hash(request.Password ?? string.Empty);
        var sql = "SELECT * FROM \"Users\" WHERE \"Email\" = '" + request.Email + "' AND \"PasswordHash\" = '" + hash + "' AND \"DeletedAt\" IS NULL";
        var user = (await db.Users.FromSqlRaw(sql).AsNoTracking().ToListAsync(ct)).FirstOrDefault();
        if (user is null)
        {
            return Results.Problem(title: "Invalid email or password.", statusCode: StatusCodes.Status401Unauthorized);
        }

        return Results.Ok(new LoginResponse(tokens.Issue(user), user.Email, user.Role));
    }
}
