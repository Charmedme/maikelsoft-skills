using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Orders.Api.Shipping.Carriers;

// The carrier gateway API: POST v2/bookings. See docs/carrier-gateway-api.md.
public sealed class HttpCarrierGateway(HttpClient http, TimeProvider time, ILogger<HttpCarrierGateway> logger) : ICarrierGateway
{
    public async Task<CarrierBookingResult> BookAsync(CarrierBookingRequest request, CancellationToken ct)
    {
        using var activity = ShippingTelemetry.Source.StartActivity("carrier.book");
        activity?.SetTag("carrier.code", request.CarrierCode);
        activity?.SetTag("shipment.id", request.ShipmentId);
        var started = time.GetTimestamp();

        var result = await SendAsync(request, ct);

        var elapsed = time.GetElapsedTime(started);
        CarrierMetrics.BookingCompleted(request.CarrierCode, result.Outcome, request.Address.PostalCode, elapsed.TotalMilliseconds);
        activity?.SetTag("booking.outcome", result.Outcome.ToString().ToLowerInvariant());
        switch (result.Outcome)
        {
            case BookingOutcome.Booked:
                CarrierLog.Booked(logger, request.CarrierCode, (long)elapsed.TotalMilliseconds);
                break;
            case BookingOutcome.Refused:
                activity?.SetStatus(ActivityStatusCode.Error, result.ReasonCode);
                CarrierLog.Refused(logger, request.CarrierCode, result.ReasonCode!, result.ReasonMessage!);
                break;
            default:
                activity?.SetStatus(ActivityStatusCode.Error);
                break;
        }

        return result;
    }

    private async Task<CarrierBookingResult> SendAsync(CarrierBookingRequest request, CancellationToken ct)
    {
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

        try
        {
            using var response = await http.PostAsJsonAsync(new Uri("v2/bookings", UriKind.Relative), body, ct);
            if (!response.IsSuccessStatusCode)
            {
                // Keep the full answer of the gateway: support needs it to see why a booking failed.
                var answer = await response.Content.ReadAsStringAsync(ct);
                logger.LogWarning("Carrier gateway answered {StatusCode} for carrier {Carrier}: {Answer}", (int)response.StatusCode, request.CarrierCode, answer);
                if (response.StatusCode == HttpStatusCode.UnprocessableEntity)
                {
                    var refusal = ParseRefusal(answer);
                    return CarrierBookingResult.Refused(refusal?.Code ?? "UNKNOWN", refusal?.Message ?? "The carrier refused the booking.");
                }

                return CarrierBookingResult.Failed();
            }

            var booking = await response.Content.ReadFromJsonAsync<GatewayBookingResponse>(ct);
            return booking is null || string.IsNullOrWhiteSpace(booking.TrackingNumber)
                ? CarrierBookingResult.Failed()
                : CarrierBookingResult.Booked(new CarrierBooking(booking.TrackingNumber, booking.LabelUrl));
        }
        catch (Exception ex) when (ex is HttpRequestException || (ex is TaskCanceledException && !ct.IsCancellationRequested))
        {
            CarrierLog.GatewayUnreachable(logger, request.CarrierCode, ex.Message);
            return CarrierBookingResult.Failed();
        }
    }

    private static GatewayRefusal? ParseRefusal(string answer)
    {
        try
        {
            return JsonSerializer.Deserialize<GatewayRefusal>(answer);
        }
        catch (JsonException)
        {
            return null;
        }
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

    private sealed record GatewayRefusal(
        [property: JsonPropertyName("code")] string? Code,
        [property: JsonPropertyName("message")] string? Message);
}
