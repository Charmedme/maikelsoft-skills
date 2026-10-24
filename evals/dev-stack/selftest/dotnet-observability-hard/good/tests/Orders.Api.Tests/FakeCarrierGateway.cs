using System.Collections.Concurrent;
using Orders.Api.Shipping.Carriers;

namespace Orders.Api.Tests;

// Books every parcel and returns a tracking number that starts with the carrier code.
// A recipient whose postal code starts with "0000" is refused, one with "9999" fails.
public sealed class FakeCarrierGateway : ICarrierGateway
{
    public ConcurrentQueue<CarrierBookingRequest> Requests { get; } = new();

    public Task<CarrierBookingResult> BookAsync(CarrierBookingRequest request, CancellationToken ct)
    {
        Requests.Enqueue(request);
        if (request.Address.PostalCode.StartsWith("0000", StringComparison.Ordinal))
        {
            return Task.FromResult(CarrierBookingResult.Refused("POSTAL_CODE_INVALID", "The postal code is not valid."));
        }

        if (request.Address.PostalCode.StartsWith("9999", StringComparison.Ordinal))
        {
            return Task.FromResult(CarrierBookingResult.Failed());
        }

        var tracking = $"{request.CarrierCode.ToUpperInvariant()}{request.ShipmentId.ToString("N")[..10].ToUpperInvariant()}";
        return Task.FromResult(CarrierBookingResult.Booked(new CarrierBooking(tracking, null)));
    }
}
