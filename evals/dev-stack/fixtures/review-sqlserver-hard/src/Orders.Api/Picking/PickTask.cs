namespace Orders.Api.Picking;

// One pick task for each order: a picker takes it, walks the zone and marks it picked.
public sealed class PickTask
{
    private PickTask()
    {
        Zone = string.Empty;
    }

    public PickTask(Guid orderId, string zone, DateTimeOffset createdAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(zone);

        // UUIDv7 is time-ordered, so new tasks go to the end of the clustered index (DATA-07).
        Id = Guid.CreateVersion7();
        OrderId = orderId;
        Zone = zone;
        CreatedAt = createdAt;
        Status = PickStatus.Open;
    }

    public Guid Id { get; private set; }

    public Guid OrderId { get; private set; }

    public string Zone { get; private set; }

    public PickStatus Status { get; private set; }

    public string? PickerId { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? TakenAt { get; private set; }

    public DateTimeOffset? PickedAt { get; private set; }

    public void MarkPicked(DateTimeOffset now)
    {
        if (Status != PickStatus.Picking)
        {
            throw new InvalidOperationException("Only a task that a picker took can be marked picked.");
        }

        Status = PickStatus.Picked;
        PickedAt = now;
    }
}
