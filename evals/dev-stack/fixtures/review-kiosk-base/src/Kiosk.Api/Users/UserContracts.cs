namespace Kiosk.Api.Users;

public sealed record LoginRequest(string? Email, string? Password);

public sealed record LoginResponse(string Token, string Email, string Role);

public sealed record RegisterRequest(
    string Email,
    string Password,
    string? DisplayName,
    int? SecurityQuestionId,
    string? SecurityAnswer);

public sealed record UserSummary(int Id, string Email, string Role, string? DisplayName);

public sealed record UserProfile(int Id, string Email, string Role, string? DisplayName, string? ProfileImage);

public sealed record ProfileRequest(string? DisplayName);

/// <summary>The fields of the own profile that a JSON Patch may change.</summary>
public sealed class ProfilePatch
{
    public string? DisplayName { get; set; }
}

public sealed record ProfileImageRequest(string Url);

public sealed record SecurityAnswerResetRequest(string Email, string Answer, string NewPassword);

public sealed record ForgotPasswordRequest(string Email);

public sealed record ResetConfirmRequest(string Email, string Token, string NewPassword);

public sealed record SecurityQuestionResponse(int Id, string Text);
