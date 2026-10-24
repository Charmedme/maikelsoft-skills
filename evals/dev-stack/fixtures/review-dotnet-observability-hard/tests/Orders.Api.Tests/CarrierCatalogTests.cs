using FluentAssertions;
using Orders.Api.Shipping;

namespace Orders.Api.Tests;

[TestClass]
public sealed class CarrierCatalogTests
{
    [TestMethod]
    public void TryGet_IgnoresCase()
    {
        CarrierCatalog.TryGet("PostNL", out var carrier).Should().BeTrue();
        carrier!.DisplayName.Should().Be("PostNL");
    }

    [TestMethod]
    public void TrackingUrl_EscapesTheTrackingNumber()
    {
        CarrierCatalog.TryGet("dhl", out var carrier);

        var url = CarrierCatalog.TrackingUrl(carrier!, "AB 12&x=1");

        url.Query.Should().Be("?tracking-id=AB%2012%26x%3D1");
    }
}
