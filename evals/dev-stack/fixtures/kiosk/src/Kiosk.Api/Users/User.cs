namespace Kiosk.Api.Users;

public sealed class User
{
    public int Id { get; set; }

    public required string Email { get; set; }

    public required string PasswordHash { get; set; }

    public string Role { get; set; } = Roles.Customer;

    public string? DisplayName { get; set; }

    public string? TotpSecret { get; set; }

    public int? SecurityQuestionId { get; set; }

    public string? SecurityAnswer { get; set; }

    public string? ProfileImage { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }
}
