namespace Orders.Api.Admin;

public sealed class AdminOptions
{
    public const string SectionName = "Admin";

    // The key of the back-office tool. It comes from the secret store in production; it is not in appsettings.json.
    public string? ApiKey { get; set; }
}
