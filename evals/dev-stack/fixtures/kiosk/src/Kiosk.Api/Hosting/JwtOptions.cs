namespace Kiosk.Api.Hosting;

public sealed class JwtOptions
{
    public const string Section = "Jwt";

    public string Issuer { get; set; } = "kiosk";

    public string Audience { get; set; } = "kiosk-api";

    public string SigningKey { get; set; } = string.Empty;

    public int LifetimeMinutes { get; set; } = 120;
}
