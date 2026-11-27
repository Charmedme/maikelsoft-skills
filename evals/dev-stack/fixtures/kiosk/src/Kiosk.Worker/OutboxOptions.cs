namespace Kiosk.Worker;

public sealed class OutboxOptions
{
    public const string Section = "Outbox";

    public bool Enabled { get; set; } = true;

    public int PollSeconds { get; set; } = 10;

    public int BatchSize { get; set; } = 20;
}
