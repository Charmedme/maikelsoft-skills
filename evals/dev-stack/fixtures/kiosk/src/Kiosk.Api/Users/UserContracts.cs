namespace Kiosk.Api.Users;

public sealed record LoginRequest(string? Email, string? Password);

public sealed record LoginResponse(string Token, string Email, string Role);

public sealed record RegisterRequest(
    string Email,
    string Password,
    string? DisplayName,
    int? SecurityQuestionId,
    string? SecurityAnswer,
    string? Role);

public sealed record UserSummary(int Id, string Email, string Role, string? DisplayName);

public sealed record ProfileRequest(string? DisplayName);

public sealed record ProfileImageRequest(string Url);

public sealed record SecurityAnswerResetRequest(string Email, string Answer, string NewPassword);

public sealed record ForgotPasswordRequest(string Email);

public sealed record ResetConfirmRequest(string Email, string Token, string NewPassword);

public sealed record SecurityQuestionResponse(int Id, string Text);
