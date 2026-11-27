using Kiosk.Api.Hosting;

namespace Kiosk.Api.Users;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/auth/login", LoginAsync).RequireRateLimiting(RateLimitingSetup.Login);
        return app;
    }

    private static async Task<IResult> LoginAsync(LoginRequest request, CredentialValidator credentials, TokenService tokens, CancellationToken ct)
    {
        var user = await credentials.ValidateAsync(request.Email ?? string.Empty, request.Password ?? string.Empty, ct);
        if (user is null)
        {
            return Results.Problem(title: "Invalid email or password.", statusCode: StatusCodes.Status401Unauthorized);
        }

        return Results.Ok(new LoginResponse(tokens.Issue(user), user.Email, user.Role));
    }
}
