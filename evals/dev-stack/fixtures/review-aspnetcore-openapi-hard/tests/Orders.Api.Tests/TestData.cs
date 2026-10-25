using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using Orders.Api.Orders;

namespace Orders.Api.Tests;

internal static class TestData
{
    // The API writes enum values as names.
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };

    public static CreateOrderRequest Order(Guid? customerId = null, params (string Sku, int Quantity)[] lines) =>
        new(
            customerId ?? Guid.CreateVersion7(),
            lines.Length == 0
                ? [new CreateOrderLineRequest("SKU-1", 1, 18.50m)]
                : [.. lines.Select(line => new CreateOrderLineRequest(line.Sku, line.Quantity, 10m))]);

    public static async Task<OrderResponse> PlaceOrderAsync(HttpClient client, CreateOrderRequest request, CancellationToken ct)
    {
        using var message = new HttpRequestMessage(HttpMethod.Post, "/orders") { Content = JsonContent.Create(request) };
        message.Headers.Add("Idempotency-Key", Guid.CreateVersion7().ToString("N"));
        using var response = await client.SendAsync(message, ct);
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Created, await response.Content.ReadAsStringAsync(ct));
        return (await response.Content.ReadFromJsonAsync<OrderResponse>(ct))!;
    }
}
