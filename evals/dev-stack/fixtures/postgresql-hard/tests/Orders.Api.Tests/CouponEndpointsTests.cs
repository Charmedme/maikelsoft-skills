using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Orders.Api.Coupons;
using Orders.Api.Orders;

namespace Orders.Api.Tests;

[TestClass]
[DoNotParallelize]
public sealed class CouponEndpointsTests
{
    private static OrdersApiFactory s_factory = null!;
    private static HttpClient s_client = null!;

    public TestContext TestContext { get; set; } = null!;

    private CancellationToken Ct => TestContext.CancellationToken;

    [ClassInitialize]
    public static async Task ClassInitializeAsync(TestContext context)
    {
        s_factory = new OrdersApiFactory();
        await s_factory.MigrateAsync(context.CancellationToken);
        s_client = s_factory.CreateClient();
    }

    [ClassCleanup]
    public static async Task ClassCleanupAsync()
    {
        s_client.Dispose();
        await s_factory.DisposeAsync();
    }

    [TestMethod]
    public async Task PlaceOrder_WithCoupon_AppliesDiscount()
    {
        await CreateCouponAsync("TEN-OFF", 10m, 5);

        var order = await s_client.CreateOrderAsync(Ct, quantity: 4, unitPrice: 5m, couponCode: "ten-off");

        order.Total.Should().Be(18m);
    }

    [TestMethod]
    public async Task PlaceOrder_CouponUsedUp_ReturnsValidationProblem()
    {
        await CreateCouponAsync("ONCE", 50m, 1);
        await s_client.CreateOrderAsync(Ct, couponCode: "ONCE");

        var response = await s_client.PostAsJsonAsync("/orders", new CreateOrderRequest(Guid.CreateVersion7(), [new CreateOrderLineRequest("SKU-1", 1, 1m)], "ONCE"), Ct);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [TestMethod]
    public async Task CreateCoupon_SameCodeTwice_ReturnsConflict()
    {
        await CreateCouponAsync("TWICE", 5m, 1);

        var response = await s_client.PostAsJsonAsync("/coupons", new CreateCouponRequest("twice", 5m, 1, s_factory.Time.GetUtcNow().AddDays(1)), Ct);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [TestMethod]
    public async Task GetCoupon_AfterUse_ShowsUsesLeft()
    {
        await CreateCouponAsync("COUNT", 5m, 3);
        await s_client.CreateOrderAsync(Ct, couponCode: "COUNT");

        var coupon = await s_client.GetFromJsonAsync<CouponResponse>("/coupons/count", Ct);

        coupon!.UsesLeft.Should().Be(2);
    }

    private async Task CreateCouponAsync(string code, decimal percentOff, int maxUses)
    {
        var response = await s_client.PostAsJsonAsync("/coupons", new CreateCouponRequest(code, percentOff, maxUses, s_factory.Time.GetUtcNow().AddDays(30)), Ct);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }
}
