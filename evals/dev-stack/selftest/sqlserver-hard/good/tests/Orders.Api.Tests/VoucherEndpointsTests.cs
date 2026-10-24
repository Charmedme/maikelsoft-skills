using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Orders.Api.Orders;
using Orders.Api.Reporting;
using Orders.Api.Vouchers;

namespace Orders.Api.Tests;

[TestClass]
[DoNotParallelize]
public sealed class VoucherEndpointsTests
{
    private static OrdersApiFactory s_factory = null!;
    private static HttpClient s_client = null!;

    public TestContext TestContext { get; set; } = null!;

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
    public async Task Put_NewCode_CreatesVoucher()
    {
        var code = NewCode();

        var response = await s_client.PutAsJsonAsync($"/vouchers/{code.ToLowerInvariant()}", new PutVoucherRequest(10, 3), TestContext.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadFromJsonAsync<VoucherResponse>(TestContext.CancellationToken)).Should().Be(new VoucherResponse(code, 10, 3, 3));
    }

    [TestMethod]
    public async Task Put_ParallelSameNewCode_AllSucceed()
    {
        var code = NewCode();

        var responses = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ =>
            s_client.PutAsJsonAsync($"/vouchers/{code}", new PutVoucherRequest(10, 3), TestContext.CancellationToken)));

        responses.Should().OnlyContain(r => r.StatusCode == HttpStatusCode.OK);
    }

    [TestMethod]
    public async Task Order_WithVoucher_GetsDiscountAndUsesOneUse()
    {
        var code = NewCode();
        await s_client.PutAsJsonAsync($"/vouchers/{code}", new PutVoucherRequest(10, 3), TestContext.CancellationToken);
        var customer = await s_client.CreateCustomerAsync(TestContext.CancellationToken);

        var order = await PlaceAsync(customer.Id, code);

        order.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await order.Content.ReadFromJsonAsync<OrderResponse>(TestContext.CancellationToken);
        body!.Discount.Should().Be(0.50m);
        body.VoucherCode.Should().Be(code);
        (await s_client.GetFromJsonAsync<VoucherResponse>($"/vouchers/{code}", TestContext.CancellationToken))!.UsesLeft.Should().Be(2);
    }

    [TestMethod]
    public async Task Order_SameCustomerTwice_ReturnsConflict()
    {
        var code = NewCode();
        await s_client.PutAsJsonAsync($"/vouchers/{code}", new PutVoucherRequest(10, 3), TestContext.CancellationToken);
        var customer = await s_client.CreateCustomerAsync(TestContext.CancellationToken);
        await PlaceAsync(customer.Id, code);

        var second = await PlaceAsync(customer.Id, code);

        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await s_client.GetFromJsonAsync<VoucherResponse>($"/vouchers/{code}", TestContext.CancellationToken))!.UsesLeft.Should().Be(2);
    }

    [TestMethod]
    public async Task Order_ParallelOrders_StopAtMaxUses()
    {
        var code = NewCode();
        await s_client.PutAsJsonAsync($"/vouchers/{code}", new PutVoucherRequest(10, 3), TestContext.CancellationToken);
        var customers = new List<Guid>();
        for (var i = 0; i < 12; i++)
        {
            customers.Add((await s_client.CreateCustomerAsync(TestContext.CancellationToken)).Id);
            await s_client.ReceiveStockAsync("SKU-1", 2, TestContext.CancellationToken);
        }

        var responses = await Task.WhenAll(customers.Select(id => s_client.PostAsJsonAsync(
            "/orders", new CreateOrderRequest(id, [new CreateOrderLineRequest("SKU-1", 2, 2.50m)], VoucherCode: code), TestContext.CancellationToken)));

        responses.Count(r => r.StatusCode == HttpStatusCode.Created).Should().Be(3);
        responses.Count(r => r.StatusCode == HttpStatusCode.Conflict).Should().Be(9);
    }

    [TestMethod]
    public async Task Order_UnknownVoucher_ReturnsValidationProblem()
    {
        var customer = await s_client.CreateCustomerAsync(TestContext.CancellationToken);

        var response = await PlaceAsync(customer.Id, NewCode());

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [TestMethod]
    public async Task Report_OrderJustBeforeMidnightUtc_IsOnNextAmsterdamDay()
    {
        var code = NewCode();
        await s_client.PutAsJsonAsync($"/vouchers/{code}", new PutVoucherRequest(10, 3), TestContext.CancellationToken);
        var customer = await s_client.CreateCustomerAsync(TestContext.CancellationToken);
        // The fake clock cannot go back: the tests that run after this one do not depend on the time.
        s_factory.Time.SetUtcNow(new DateTimeOffset(2026, 11, 30, 23, 15, 0, TimeSpan.Zero));
        await PlaceAsync(customer.Id, code);

        var rows = await s_client.GetFromJsonAsync<List<VoucherDay>>("/reports/vouchers?from=2026-11-30&to=2026-12-01", TestContext.CancellationToken);

        rows.Should().ContainSingle(r => r.Code == code).Which.Should().Be(new VoucherDay(code, new DateOnly(2026, 12, 1), 1, 0.50m));
    }

    private async Task<HttpResponseMessage> PlaceAsync(Guid customerId, string code)
    {
        await s_client.ReceiveStockAsync("SKU-1", 2, TestContext.CancellationToken);
        return await s_client.PostAsJsonAsync(
            "/orders", new CreateOrderRequest(customerId, [new CreateOrderLineRequest("SKU-1", 2, 2.50m)], VoucherCode: code), TestContext.CancellationToken);
    }

    private static string NewCode() => "TST" + Guid.CreateVersion7().ToString("N")[^10..].ToUpperInvariant();
}
