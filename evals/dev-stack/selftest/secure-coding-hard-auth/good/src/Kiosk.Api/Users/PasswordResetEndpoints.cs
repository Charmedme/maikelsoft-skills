using System.Security.Cryptography;
using System.Text;
using Kiosk.Api.Data;
using Kiosk.Api.Hosting;
using Kiosk.Api.Mail;
using Microsoft.EntityFrameworkCore;

namespace Kiosk.Api.Users;

public static class PasswordResetEndpoints
{
    public static IEndpointRouteBuilder MapPasswordResetEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/users");
        group.MapGet("/security-question", GetQuestionAsync);
        group.MapPost("/reset-password", ResetWithAnswerAsync).RequireRateLimiting(RateLimitingSetup.PasswordReset);
        group.MapPost("/forgot-password", ForgotPasswordAsync);
        group.MapPost("/reset-password/confirm", ConfirmResetAsync).RequireRateLimiting(RateLimitingSetup.PasswordReset);
        return app;
    }

    private static async Task<IResult> GetQuestionAsync(string email, KioskDbContext db, CancellationToken ct)
    {
        var normalized = email.Trim().ToLowerInvariant();
        var question = await db.Users.AsNoTracking()
            .Where(u => u.Email == normalized)
            .Join(db.SecurityQuestions, u => u.SecurityQuestionId, q => q.Id, (u, q) => new SecurityQuestionResponse(q.Id, q.Text))
            .SingleOrDefaultAsync(ct);
        return question is null ? Results.NotFound() : Results.Ok(question);
    }

    // The mobile app still uses the security question flow.
    private static async Task<IResult> ResetWithAnswerAsync(SecurityAnswerResetRequest request, KioskDbContext db, IPasswordHasher hasher, CancellationToken ct)
    {
        if (PasswordPolicy.Check(request.NewPassword) is { } passwordProblem)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["newPassword"] = [passwordProblem] });
        }

        var email = request.Email.Trim().ToLowerInvariant();
        var user = await db.Users.SingleOrDefaultAsync(u => u.Email == email, ct);
        if (user?.SecurityAnswer is null
            || !string.Equals(user.SecurityAnswer.Trim(), request.Answer?.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            return Results.Problem(title: "Wrong answer to the security question.", statusCode: StatusCodes.Status401Unauthorized);
        }

        user.PasswordHash = hasher.Hash(request.NewPassword);
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }

    private static async Task<IResult> ForgotPasswordAsync(ForgotPasswordRequest request, IConfiguration configuration, KioskDbContext db, IEmailSender mail, TimeProvider time, CancellationToken ct)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await db.Users.AsNoTracking().SingleOrDefaultAsync(u => u.Email == email, ct);
        if (user is null)
        {
            return Results.Accepted();
        }

        var now = time.GetUtcNow();
        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        db.PasswordResetTokens.Add(new PasswordResetToken { UserId = user.Id, Token = token, ExpiresAt = now.AddHours(1) });

        // The link uses the configured public address, never the Host header of the request.
        var baseUrl = configuration["App:PublicBaseUrl"] ?? throw new InvalidOperationException("App:PublicBaseUrl is not set.");
        var link = $"{baseUrl.TrimEnd('/')}/account/reset?email={Uri.EscapeDataString(user.Email)}&token={token}";
        await mail.QueueAsync(user.Email, "Reset your Kiosk password", MailTemplates.PasswordReset(user.DisplayName ?? user.Email, link), ct);
        await db.SaveChangesAsync(ct);
        return Results.Accepted();
    }

    private static async Task<IResult> ConfirmResetAsync(ResetConfirmRequest request, KioskDbContext db, IPasswordHasher hasher, TimeProvider time, CancellationToken ct)
    {
        if (PasswordPolicy.Check(request.NewPassword) is { } passwordProblem)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["newPassword"] = [passwordProblem] });
        }

        var email = request.Email.Trim().ToLowerInvariant();
        var now = time.GetUtcNow();
        var user = await db.Users.SingleOrDefaultAsync(u => u.Email == email, ct);
        if (user is null)
        {
            return Results.BadRequest();
        }

        var tokens = await db.PasswordResetTokens
            .Where(t => t.UserId == user.Id && t.UsedAt == null && t.ExpiresAt > now)
            .ToListAsync(ct);
        var given = Encoding.UTF8.GetBytes(request.Token ?? string.Empty);
        var match = tokens.FirstOrDefault(t => CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(t.Token), given));
        if (match is null)
        {
            return Results.Problem(title: "The reset link is not valid.", statusCode: StatusCodes.Status400BadRequest);
        }

        match.UsedAt = now;
        user.PasswordHash = hasher.Hash(request.NewPassword);
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }
}
