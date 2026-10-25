namespace Orders.Api.Shipping.Carriers;

// The log events of a booking. The dispatch scope adds ShipmentId and OrderId to each event,
// so support finds them by the shipment id.
internal static partial class CarrierLog
{
    [LoggerMessage(EventId = 2101, Level = LogLevel.Information, Message = "Carrier {Carrier} booked the parcel in {ElapsedMs} ms")]
    public static partial void Booked(ILogger logger, string carrier, long elapsedMs);

    [LoggerMessage(EventId = 2102, Level = LogLevel.Warning, Message = "Carrier {Carrier} refused the booking: {ReasonCode} {ReasonMessage}")]
    public static partial void Refused(ILogger logger, string carrier, string reasonCode, string reasonMessage);

    [LoggerMessage(EventId = 2104, Level = LogLevel.Error, Message = "Carrier gateway did not answer for carrier {Carrier}: {Error}")]
    public static partial void GatewayUnreachable(ILogger logger, string carrier, string error);
}
