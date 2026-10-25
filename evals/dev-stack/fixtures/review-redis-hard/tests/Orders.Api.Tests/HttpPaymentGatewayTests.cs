using System.Net;
using System.Text;
using FluentAssertions;
using Orders.Api.Payments;

namespace Orders.Api.Tests;

[TestClass]
public sealed class HttpPaymentGatewayTests
{
    public TestContext TestContext { get; set; } = null!;

    [TestMethod]
    public async Task Authorize_Declined_ReturnsReason()
    {
        using var handler = new StubHandler("""{"id":"auth_1","status":"declined","reason":"card_expired"}""");
        using var http = new HttpClient(handler) { BaseAddress = new Uri("https://psp.test/") };
        var gateway = new HttpPaymentGateway(http);

        var result = await gateway.AuthorizeAsync(new AuthorizationRequest(Guid.CreateVersion7(), 12.34m, "EUR"), TestContext.CancellationToken);

        result.Authorized.Should().BeFalse();
        result.DeclineReason.Should().Be("card_expired");
        handler.LastBody.Should().Contain("\"amount_minor\":1234");
        handler.LastUri.Should().Be(new Uri("https://psp.test/v1/authorizations"));
    }

    [TestMethod]
    public async Task Authorize_UnknownStatus_Throws()
    {
        using var handler = new StubHandler("""{"id":"auth_2","status":"pending"}""");
        using var http = new HttpClient(handler) { BaseAddress = new Uri("https://psp.test/") };
        var gateway = new HttpPaymentGateway(http);

        var act = () => gateway.AuthorizeAsync(new AuthorizationRequest(Guid.CreateVersion7(), 1m, "EUR"), TestContext.CancellationToken);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    private sealed class StubHandler(string json) : HttpMessageHandler
    {
        public string LastBody { get; private set; } = string.Empty;

        public Uri? LastUri { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastUri = request.RequestUri;
            LastBody = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
        }
    }
}
