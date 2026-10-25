using System.ComponentModel.DataAnnotations;

namespace Orders.Api.Returns;

public sealed record CreateReturnRequest(
    [property: Required] ReturnReason? Reason,
    [property: StringLength(1000)] string? Comment);

public sealed record ReturnResponse(Guid Id, Guid OrderId, string Reason, string? Comment, string Status, DateTimeOffset RequestedAt);

public static class ReturnMapping
{
    public static ReturnResponse ToResponse(this ReturnRequest request) =>
        new(request.Id, request.OrderId, request.Reason.ToString(), request.Comment, request.Status.ToString(), request.RequestedAt);
}
