namespace Kiosk.Api.Users;

public sealed class PasswordResetToken
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public required string Token { get; set; }

    public DateTimeOffset ExpiresAt { get; set; }

    public DateTimeOffset? UsedAt { get; set; }
}
