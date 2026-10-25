namespace Orders.Api.Returns;

// Validated by CreateReturnRequestValidator.
public sealed record CreateReturnRequest(Guid? OrderId, string Sku, int Quantity, ReturnReason? Reason, string? Comment);

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
