using System.Net;
using System.Text;
using FluentAssertions;
using Orders.Api.Contracts;
using Orders.Api.Orders;
using Orders.Api.Shipping;

namespace Orders.Api.Tests;

[TestClass]
public sealed class HttpCarrierClientTests
{
    private static readonly DeliveryAddress s_address = new("Hofstraat 1", "7271 AB", "Borculo", "NL");

    public TestContext TestContext { get; set; } = null!;

    [TestMethod]
    public async Task Book_Created_ReturnsTrackingNumberAndSendsIdempotencyKey()
    {
        using var handler = new StubHandler(HttpStatusCode.Created, """{"bookingId":"bk_1","carrier":"dhl","trackingNumber":"JVGL0001"}""");
        using var http = new HttpClient(handler) { BaseAddress = new Uri("https://parcels.test/") };
        var client = new HttpCarrierClient(http);

        var booking = await client.BookAsync(new CarrierBookingRequest("ref-1", "Ann Example", s_address, ShippingSpeed.Express), TestContext.CancellationToken);

        booking.TrackingNumber.Should().Be("JVGL0001");
        booking.Carrier.Should().Be("dhl");
        handler.LastUri.Should().Be(new Uri("https://parcels.test/v2/bookings"));
        handler.LastIdempotencyKey.Should().Be("ref-1");
        handler.LastBody.Should().Contain("\"service\":\"express\"");
    }

    [TestMethod]
    public async Task Book_Unprocessable_ThrowsAddressRejected()
    {
        using var handler = new StubHandler(HttpStatusCode.UnprocessableEntity, """{"code":"address_rejected","detail":"Unknown street."}""");
        using var http = new HttpClient(handler) { BaseAddress = new Uri("https://parcels.test/") };
        var client = new HttpCarrierClient(http);

        var act = () => client.BookAsync(new CarrierBookingRequest("ref-2", "Ann Example", s_address, ShippingSpeed.Standard), TestContext.CancellationToken);

        (await act.Should().ThrowAsync<CarrierAddressRejectedException>()).WithMessage("Unknown street.");
    }

    [TestMethod]
    public async Task Book_ServiceUnavailable_ThrowsHttpRequestException()
    {
        using var handler = new StubHandler(HttpStatusCode.ServiceUnavailable, "{}");
        using var http = new HttpClient(handler) { BaseAddress = new Uri("https://parcels.test/") };
        var client = new HttpCarrierClient(http);

        var act = () => client.BookAsync(new CarrierBookingRequest("ref-3", "Ann Example", s_address, ShippingSpeed.Standard), TestContext.CancellationToken);

        await act.Should().ThrowAsync<HttpRequestException>();
    }

    private sealed class StubHandler(HttpStatusCode status, string json) : HttpMessageHandler
    {
        public string LastBody { get; private set; } = string.Empty;

        public Uri? LastUri { get; private set; }

        public string? LastIdempotencyKey { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastUri = request.RequestUri;
            LastIdempotencyKey = request.Headers.TryGetValues("Idempotency-Key", out var values) ? values.Single() : null;
            LastBody = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
        }
    }
}
