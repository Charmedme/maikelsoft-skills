using System.Net;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Orders.Api.Orders;
using Orders.Api.Shipping.Carriers;

namespace Orders.Api.Tests;

[TestClass]
public sealed class HttpCarrierGatewayTests
{
    private static readonly DeliveryAddress s_address = new("Ann Example", "Main Street 1", "1234 AB", "Utrecht", "NL", "+31600000000");

    public TestContext TestContext { get; set; } = null!;

    [TestMethod]
    public async Task Book_Created_ReturnsTrackingNumber()
    {
        using var handler = new StubHandler(HttpStatusCode.Created, """{"trackingNumber":"3SABC123","labelUrl":"https://labels.test/3SABC123.pdf"}""");
        using var http = new HttpClient(handler) { BaseAddress = new Uri("https://gateway.test/") };
        var gateway = new HttpCarrierGateway(http, NullLogger<HttpCarrierGateway>.Instance);

        var booking = await gateway.BookAsync(new CarrierBookingRequest(Guid.CreateVersion7(), "postnl", s_address, "ann@example.com", null), TestContext.CancellationToken);

        booking.TrackingNumber.Should().Be("3SABC123");
        handler.LastUri.Should().Be(new Uri("https://gateway.test/v2/bookings"));
        handler.LastBody.Should().Contain("\"carrier\":\"postnl\"").And.Contain("\"postalCode\":\"1234 AB\"");
    }

    [TestMethod]
    public async Task Book_Refused_Throws()
    {
        using var handler = new StubHandler(HttpStatusCode.UnprocessableEntity, """{"code":"ADDRESS_UNKNOWN","message":"The address is not known to the carrier."}""");
        using var http = new HttpClient(handler) { BaseAddress = new Uri("https://gateway.test/") };
        var gateway = new HttpCarrierGateway(http, NullLogger<HttpCarrierGateway>.Instance);

        var act = () => gateway.BookAsync(new CarrierBookingRequest(Guid.CreateVersion7(), "dhl", s_address, null, null), TestContext.CancellationToken);

        (await act.Should().ThrowAsync<CarrierBookingException>()).Which.StatusCode.Should().Be(422);
    }

    private sealed class StubHandler(HttpStatusCode status, string json) : HttpMessageHandler
    {
        public string LastBody { get; private set; } = string.Empty;

        public Uri? LastUri { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastUri = request.RequestUri;
            LastBody = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
        }
    }
}
