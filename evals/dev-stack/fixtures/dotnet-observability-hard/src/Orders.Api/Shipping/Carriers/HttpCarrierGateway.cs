using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace Orders.Api.Shipping.Carriers;

// The carrier gateway API: POST v2/bookings. See docs/carrier-gateway-api.md.
public sealed class HttpCarrierGateway(HttpClient http, ILogger<HttpCarrierGateway> logger) : ICarrierGateway
{
    public async Task<CarrierBooking> BookAsync(CarrierBookingRequest request, CancellationToken ct)
    {
        using var activity = ShippingTelemetry.Source.StartActivity("carrier.book");
        activity?.SetTag("carrier.code", request.CarrierCode);
        activity?.SetTag("shipment.id", request.ShipmentId);

        var body = new GatewayBookingRequest(
            request.CarrierCode,
            request.ShipmentId.ToString("N"),
            new GatewayRecipient(
                request.Address.RecipientName,
                request.Address.Street,
                request.Address.PostalCode,
                request.Address.City,
                request.Address.CountryCode,
                request.Address.Phone,
                request.RecipientEmail),
            request.Instructions);

        using var response = await http.PostAsJsonAsync(new Uri("v2/bookings", UriKind.Relative), body, ct);
        if (!response.IsSuccessStatusCode)
        {
            // Keep the full answer of the gateway: support needs it to see why a booking failed.
            var answer = await response.Content.ReadAsStringAsync(ct);
            logger.LogWarning("Carrier gateway answered {StatusCode} for carrier {Carrier}: {Answer}", (int)response.StatusCode, request.CarrierCode, answer);
            activity?.SetStatus(System.Diagnostics.ActivityStatusCode.Error);
            throw new CarrierBookingException(request.CarrierCode, (int)response.StatusCode);
        }

        var booking = await response.Content.ReadFromJsonAsync<GatewayBookingResponse>(ct)
            ?? throw new CarrierBookingException("The carrier gateway returned an empty body.");
        return new CarrierBooking(booking.TrackingNumber, booking.LabelUrl);
    }

    private sealed record GatewayBookingRequest(
        [property: JsonPropertyName("carrier")] string Carrier,
        [property: JsonPropertyName("reference")] string Reference,
        [property: JsonPropertyName("recipient")] GatewayRecipient Recipient,
        [property: JsonPropertyName("instructions")] string? Instructions);

    private sealed record GatewayRecipient(
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("street")] string Street,
        [property: JsonPropertyName("postalCode")] string PostalCode,
        [property: JsonPropertyName("city")] string City,
        [property: JsonPropertyName("country")] string Country,
        [property: JsonPropertyName("phone")] string? Phone,
        [property: JsonPropertyName("email")] string? Email);

    private sealed record GatewayBookingResponse(
        [property: JsonPropertyName("trackingNumber")] string TrackingNumber,
        [property: JsonPropertyName("labelUrl")] Uri? LabelUrl);
}
