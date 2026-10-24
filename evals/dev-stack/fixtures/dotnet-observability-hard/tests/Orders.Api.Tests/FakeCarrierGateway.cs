using System.Collections.Concurrent;
using Orders.Api.Shipping.Carriers;

namespace Orders.Api.Tests;

// Books every parcel and returns a tracking number that starts with the carrier code.
public sealed class FakeCarrierGateway : ICarrierGateway
{
    public ConcurrentQueue<CarrierBookingRequest> Requests { get; } = new();

    public Task<CarrierBooking> BookAsync(CarrierBookingRequest request, CancellationToken ct)
    {
        Requests.Enqueue(request);
        var tracking = $"{request.CarrierCode.ToUpperInvariant()}{request.ShipmentId.ToString("N")[..10].ToUpperInvariant()}";
        return Task.FromResult(new CarrierBooking(tracking, null));
    }
}
