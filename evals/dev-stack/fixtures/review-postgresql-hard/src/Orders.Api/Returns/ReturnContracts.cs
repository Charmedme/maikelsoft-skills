using System.ComponentModel.DataAnnotations;

namespace Orders.Api.Returns;

public sealed record CreateReturnRequest(
    [property: Required] Guid? OrderId,
    [property: Required, StringLength(500, MinimumLength = 3)] string Reason);

public sealed record ReturnResponse(Guid Id, Guid OrderId, string Reason, string Status, DateTimeOffset RequestedAt, DateTimeOffset? ReceivedAt);

public static class ReturnMapping
{
    public static ReturnResponse ToResponse(this ReturnRequest request) =>
        new(request.Id, request.OrderId, request.Reason, request.Status.ToString(), request.RequestedAt, request.ReceivedAt);
}
