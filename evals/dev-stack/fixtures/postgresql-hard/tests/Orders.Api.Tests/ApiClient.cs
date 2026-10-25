using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Orders.Api.Orders;
using Orders.Api.Shipping;

namespace Orders.Api.Tests;

// Small helpers that create test data through the public API.
public static class ApiClient
{
    public static async Task<OrderResponse> CreateOrderAsync(
        this HttpClient client,
        CancellationToken ct,
        Guid? customerId = null,
        string sku = "SKU-1",
        int quantity = 2,
        decimal unitPrice = 2.50m,
        string? couponCode = null)
    {
        var request = new CreateOrderRequest(customerId ?? Guid.CreateVersion7(), [new CreateOrderLineRequest(sku, quantity, unitPrice)], couponCode);
        var response = await client.PostAsJsonAsync("/orders", request, ct);
        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync(ct));
        return (await response.Content.ReadFromJsonAsync<OrderResponse>(ct))!;
    }

    public static async Task<ShipmentResponse> ShipAsync(this HttpClient client, Guid orderId, CancellationToken ct)
    {
        var response = await client.PostAsJsonAsync("/shipments", new CreateShipmentRequest(orderId, Carrier.PostNL, "3SABCD1234567"), ct);
        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync(ct));
        return (await response.Content.ReadFromJsonAsync<ShipmentResponse>(ct))!;
    }

    public static async Task<HttpResponseMessage> NextPickAsync(this HttpClient client, string workerId, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/picking/next");
        request.Headers.Add("X-Worker-Id", workerId);
        return await client.SendAsync(request, ct);
    }
}
