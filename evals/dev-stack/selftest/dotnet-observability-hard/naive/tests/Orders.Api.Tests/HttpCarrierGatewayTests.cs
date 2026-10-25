using System.Diagnostics.Metrics;
using System.Net;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
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
        await using var services = new ServiceCollection().AddMetrics().BuildServiceProvider();
        var gateway = new HttpCarrierGateway(http, new CarrierTelemetry(services.GetRequiredService<IMeterFactory>()), NullLogger<HttpCarrierGateway>.Instance);

        var result = await gateway.BookAsync(new CarrierBookingRequest(Guid.CreateVersion7(), "postnl", s_address, "ann@example.com", null), TestContext.CancellationToken);

        result.Booking!.TrackingNumber.Should().Be("3SABC123");
        handler.LastUri.Should().Be(new Uri("https://gateway.test/v2/bookings"));
    }

    [TestMethod]
    public async Task Book_Refused_ReturnsReason()
    {
        using var handler = new StubHandler(HttpStatusCode.UnprocessableEntity, """{"code":"ADDRESS_UNKNOWN","message":"The address is not known to the carrier."}""");
        using var http = new HttpClient(handler) { BaseAddress = new Uri("https://gateway.test/") };
        await using var services = new ServiceCollection().AddMetrics().BuildServiceProvider();
        var gateway = new HttpCarrierGateway(http, new CarrierTelemetry(services.GetRequiredService<IMeterFactory>()), NullLogger<HttpCarrierGateway>.Instance);

        var result = await gateway.BookAsync(new CarrierBookingRequest(Guid.CreateVersion7(), "dhl", s_address, null, null), TestContext.CancellationToken);

        result.Outcome.Should().Be(BookingOutcome.Refused);
        result.ReasonCode.Should().Be("ADDRESS_UNKNOWN");
    }

    private sealed class StubHandler(HttpStatusCode status, string json) : HttpMessageHandler
    {
        public Uri? LastUri { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastUri = request.RequestUri;
            return Task.FromResult(new HttpResponseMessage(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") });
        }
    }
}
