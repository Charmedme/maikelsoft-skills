using Orders.Api.Telemetry;

namespace Orders.Api.Orders;

internal static partial class OrderLog
{
    [LoggerMessage(EventId = 1001, Level = LogLevel.Information, Message = "Order {OrderId} created for customer {CustomerId} ({CustomerEmail})")]
    public static partial void OrderCreated(ILogger logger, Guid orderId, Guid customerId, [PersonalData] string? customerEmail);

    [LoggerMessage(EventId = 1002, Level = LogLevel.Error, Message = "Creating an order for customer {CustomerId} failed")]
    public static partial void CreateFailed(ILogger logger, Exception exception, Guid customerId);
}
