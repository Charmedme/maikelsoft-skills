namespace Orders.Api.Infrastructure;

public sealed class DatabaseOptions
{
    public const string SectionName = "Database";

    // Apply the EF Core migrations when the service starts. The deploy pipeline has no migration step.
    public bool MigrateOnStartup { get; set; } = true;
}
