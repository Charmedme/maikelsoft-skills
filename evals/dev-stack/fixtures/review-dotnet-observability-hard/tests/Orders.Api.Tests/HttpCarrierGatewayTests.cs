using System.Net;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Metrics.Testing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Orders.Api.Orders;
using Orders.Api.Shipping.Carriers;

namespace Orders.Api.Tests;

[TestClass]
[DoNotParallelize]
public sealed class HttpCarrierGatewayTests
{
    private static readonly DeliveryAddress s_address = new("Ann Example", "Main Street 1", "1234 AB", "Utrecht", "NL", "+31600000000");

    public TestContext TestContext { get; set; } = null!;

    [TestMethod]
    public async Task Book_Created_ReturnsTrackingNumber()
    {
        using var handler = new StubHandler(HttpStatusCode.Created, """{"trackingNumber":"3SABC123","labelUrl":"https://labels.test/3SABC123.pdf"}""");
        await using var host = TestHost.Create(handler);

        var result = await host.Gateway.BookAsync(new CarrierBookingRequest(Guid.CreateVersion7(), "postnl", s_address, "ann@example.com", null), TestContext.CancellationToken);

        result.Outcome.Should().Be(BookingOutcome.Booked);
        result.Booking!.TrackingNumber.Should().Be("3SABC123");
        handler.LastUri.Should().Be(new Uri("https://gateway.test/v2/bookings"));
        handler.LastBody.Should().Contain("\"carrier\":\"postnl\"").And.Contain("\"postalCode\":\"1234 AB\"");
    }

    [TestMethod]
    public async Task Book_Refused_ReturnsReason_AndLogsNoRecipientData()
    {
        using var handler = new StubHandler(HttpStatusCode.UnprocessableEntity, """{"code":"ADDRESS_UNKNOWN","message":"The address is not known to the carrier."}""");
        await using var host = TestHost.Create(handler);

        var result = await host.Gateway.BookAsync(new CarrierBookingRequest(Guid.CreateVersion7(), "dhl", s_address, "ann@example.com", null), TestContext.CancellationToken);

        result.Outcome.Should().Be(BookingOutcome.Refused);
        result.ReasonCode.Should().Be("ADDRESS_UNKNOWN");
        var record = host.Logs.GetSnapshot().Single(r => r.Id.Id == 2102);
        record.Level.Should().Be(LogLevel.Warning);
        record.GetStructuredStateValue("ReasonCode").Should().Be("ADDRESS_UNKNOWN");
        foreach (var personal in new[] { "Ann Example", "Main Street", "1234 AB", "31600000000", "ann@example.com" })
        {
            host.Logs.GetSnapshot().Should().NotContain(r => r.Message.Contains(personal, StringComparison.Ordinal));
        }
    }

    [TestMethod]
    public async Task Book_GatewayError_ReturnsFailed()
    {
        using var handler = new StubHandler(HttpStatusCode.ServiceUnavailable, "{}");
        await using var host = TestHost.Create(handler);

        var result = await host.Gateway.BookAsync(new CarrierBookingRequest(Guid.CreateVersion7(), "dpd", s_address, null, null), TestContext.CancellationToken);

        result.Outcome.Should().Be(BookingOutcome.Failed);
    }

    [TestMethod]
    public async Task Book_RecordsDurationPerCarrierAndOutcome()
    {
        using var handler = new StubHandler(HttpStatusCode.UnprocessableEntity, """{"code":"PHONE_REQUIRED","message":"The carrier needs a phone number."}""");
        await using var host = TestHost.Create(handler);
        using var collector = new MetricCollector<double>(null, CarrierMetrics.MeterName, CarrierMetrics.BookingDuration);

        await host.Gateway.BookAsync(new CarrierBookingRequest(Guid.CreateVersion7(), "postnl", s_address, null, null), TestContext.CancellationToken);

        var measurement = collector.LastMeasurement!;
        measurement.Tags["orders.carrier"].Should().Be("postnl");
        measurement.Tags["orders.booking.outcome"].Should().Be("refused");
        measurement.Value.Should().BePositive();
    }

    private sealed class TestHost : IAsyncDisposable
    {
        private readonly ServiceProvider _provider;

        private TestHost(ServiceProvider provider, HttpCarrierGateway gateway)
        {
            _provider = provider;
            Gateway = gateway;
        }

        public HttpCarrierGateway Gateway { get; }

        public FakeLogCollector Logs => _provider.GetFakeLogCollector();

        public static TestHost Create(HttpMessageHandler handler)
        {
            var services = new ServiceCollection();
            services.AddLogging(logging => logging.AddFakeLogging());
            var provider = services.BuildServiceProvider();
            var http = new HttpClient(handler, disposeHandler: false) { BaseAddress = new Uri("https://gateway.test/") };
            var gateway = new HttpCarrierGateway(http, TimeProvider.System, provider.GetRequiredService<ILogger<HttpCarrierGateway>>());
            return new TestHost(provider, gateway);
        }

        public ValueTask DisposeAsync() => _provider.DisposeAsync();
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
