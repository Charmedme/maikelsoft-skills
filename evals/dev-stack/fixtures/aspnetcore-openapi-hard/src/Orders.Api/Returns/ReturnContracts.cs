using System.ComponentModel.DataAnnotations;

namespace Orders.Api.Returns;

public sealed record CreateReturnRequest(
    [property: Required] Guid? OrderId,
    [property: Required, StringLength(32, MinimumLength = 1)] string Sku,
    [property: Range(1, 1000)] int Quantity,
    [property: Required] ReturnReason? Reason,
    [property: StringLength(500)] string? Comment);

public sealed record ReturnResponse(
    Guid Id,
    Guid OrderId,
    string Sku,
    int Quantity,
    ReturnReason Reason,
    string? Comment,
    ReturnStatus Status,
    DateTimeOffset RequestedAt);

public static class ReturnMapping
{
    public static ReturnResponse ToResponse(this OrderReturn orderReturn) =>
        new(
            orderReturn.Id,
            orderReturn.OrderId,
            orderReturn.Sku,
            orderReturn.Quantity,
            orderReturn.Reason,
            orderReturn.Comment,
            orderReturn.Status,
            orderReturn.RequestedAt);
}
