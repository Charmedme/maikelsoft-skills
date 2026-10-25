namespace Orders.Api.Shipping;

public sealed class CarrierAddressRejectedException : Exception
{
    public CarrierAddressRejectedException()
    {
    }

    public CarrierAddressRejectedException(string message)
        : base(message)
    {
    }

    public CarrierAddressRejectedException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
