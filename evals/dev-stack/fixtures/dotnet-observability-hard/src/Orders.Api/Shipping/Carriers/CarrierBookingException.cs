namespace Orders.Api.Shipping.Carriers;

public sealed class CarrierBookingException : Exception
{
    public CarrierBookingException()
    {
    }

    public CarrierBookingException(string message)
        : base(message)
    {
    }

    public CarrierBookingException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public CarrierBookingException(string carrierCode, int statusCode)
        : base($"The carrier gateway did not book the parcel for carrier '{carrierCode}' (HTTP {statusCode}).")
    {
        CarrierCode = carrierCode;
        StatusCode = statusCode;
    }

    public string? CarrierCode { get; }

    public int? StatusCode { get; }
}
