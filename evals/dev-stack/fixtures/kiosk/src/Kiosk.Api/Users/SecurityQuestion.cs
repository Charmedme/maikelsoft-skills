namespace Kiosk.Api.Users;

public sealed class SecurityQuestion
{
    public int Id { get; set; }

    public required string Text { get; set; }
}
