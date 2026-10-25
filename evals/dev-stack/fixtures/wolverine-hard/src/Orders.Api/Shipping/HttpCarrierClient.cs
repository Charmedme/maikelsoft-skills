using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Orders.Api.Contracts;

namespace Orders.Api.Shipping;

// The parcel platform API: POST v2/bookings. See docs/carrier-api.md.
public sealed class HttpCarrierClient(HttpClient http) : ICarrierClient
{
    public async Task<CarrierBooking> BookAsync(CarrierBookingRequest request, CancellationToken ct)
    {
        var body = new BookingBody(
            request.Reference,
            new RecipientBody(request.RecipientName, request.Address.Street, request.Address.PostalCode, request.Address.City, request.Address.CountryCode),
            request.Speed == ShippingSpeed.Express ? "express" : "standard");
        using var message = new HttpRequestMessage(HttpMethod.Post, new Uri("v2/bookings", UriKind.Relative))
        {
            Content = JsonContent.Create(body),
        };
        message.Headers.Add("Idempotency-Key", request.Reference);

        using var response = await http.SendAsync(message, ct);
        if (response.StatusCode == HttpStatusCode.UnprocessableEntity)
        {
            var problem = await response.Content.ReadFromJsonAsync<ProblemBody>(ct);
            throw new CarrierAddressRejectedException(problem?.Detail ?? "The carrier cannot deliver to this address.");
        }

        response.EnsureSuccessStatusCode();
        var booking = await response.Content.ReadFromJsonAsync<BookingResponse>(ct)
            ?? throw new InvalidOperationException("The parcel platform returned an empty body.");
        return new CarrierBooking(booking.BookingId, booking.Carrier, booking.TrackingNumber);
    }

    private sealed record BookingBody(
        [property: JsonPropertyName("reference")] string Reference,
        [property: JsonPropertyName("recipient")] RecipientBody Recipient,
        [property: JsonPropertyName("service")] string Service);

    private sealed record RecipientBody(
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("street")] string Street,
        [property: JsonPropertyName("postalCode")] string PostalCode,
        [property: JsonPropertyName("city")] string City,
        [property: JsonPropertyName("country")] string Country);

    private sealed record BookingResponse(
        [property: JsonPropertyName("bookingId")] string BookingId,
        [property: JsonPropertyName("carrier")] string Carrier,
        [property: JsonPropertyName("trackingNumber")] string TrackingNumber);

    private sealed record ProblemBody([property: JsonPropertyName("detail")] string? Detail);
}
