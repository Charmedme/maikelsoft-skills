using System.ComponentModel.DataAnnotations;

namespace Orders.Api.Picking;

public sealed record CreatePickTaskRequest([property: Required] Guid? OrderId);

public sealed record TakeNextPickRequest(
    [property: Required, StringLength(32, MinimumLength = 1)] string PickerId,
    [property: Required, StringLength(8, MinimumLength = 1)] string Zone);

public sealed record PutPickZoneRequest([property: Required, StringLength(8, MinimumLength = 1)] string Zone);

public sealed record PickTaskResponse(
    Guid Id,
    Guid OrderId,
    string Zone,
    string Status,
    string? PickerId,
    DateTimeOffset CreatedAt,
    DateTimeOffset? TakenAt,
    DateTimeOffset? PickedAt);

public static class PickTaskMapping
{
    public static PickTaskResponse ToResponse(this PickTask task) =>
        new(task.Id, task.OrderId, task.Zone, task.Status.ToString(), task.PickerId, task.CreatedAt, task.TakenAt, task.PickedAt);
}
