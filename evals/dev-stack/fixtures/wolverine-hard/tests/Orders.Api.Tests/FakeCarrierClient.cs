using System.Collections.Concurrent;
using Orders.Api.Shipping;

namespace Orders.Api.Tests;

public sealed class FakeCarrierClient : ICarrierClient
{
    private int _next;

    public ConcurrentQueue<CarrierBookingRequest> Requests { get; } = new();

    // Postal codes that the carrier refuses.
    public ConcurrentDictionary<string, string> RejectedPostalCodes { get; } = new();

    public Task<CarrierBooking> BookAsync(CarrierBookingRequest request, CancellationToken ct)
    {
        Requests.Enqueue(request);
        if (RejectedPostalCodes.TryGetValue(request.Address.PostalCode, out var reason))
        {
            throw new CarrierAddressRejectedException(reason);
        }

        var number = Interlocked.Increment(ref _next);
        return Task.FromResult(new CarrierBooking($"bk-{number}", "dhl", $"JVGL{number:D10}"));
    }
}
