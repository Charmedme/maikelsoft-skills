namespace Kiosk.Api.Users;

public static class PasswordPolicy
{
    public const int MinimumLength = 12;

    private static readonly HashSet<string> s_common = new(StringComparer.OrdinalIgnoreCase)
    {
        "password", "password123", "123456789012", "qwertyuiopas", "admin123", "letmein", "welcome123", "iloveyou",
    };

    /// <summary>Returns the reason why the password is not accepted, or null when it is accepted.</summary>
    public static string? Check(string? password)
    {
        if (string.IsNullOrEmpty(password) || password.Length < MinimumLength)
        {
            return $"Use a password of {MinimumLength} characters or more.";
        }

        return s_common.Contains(password) ? "This password is too common." : null;
    }
}
