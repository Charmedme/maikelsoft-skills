using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Orders.Api.Catalog;
using Orders.Api.Customers;
using Orders.Api.Orders;
using Orders.Api.Shipping;

namespace Orders.Api.Tests;

// Small helpers that create test data through the public API.
public static class ApiClient
{
    public static async Task<CustomerResponse> CreateCustomerAsync(this HttpClient client, CancellationToken ct, CustomerKind kind = CustomerKind.Consumer, string? email = null)
    {
        var request = new CreateCustomerRequest(
            "Ann Example",
            email ?? $"ann-{Guid.CreateVersion7():N}@example.com",
            kind,
            kind == CustomerKind.Business ? "Example Trading B.V." : null);
        var response = await client.PostAsJsonAsync("/customers", request, ct);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<CustomerResponse>(ct))!;
    }

    public static async Task<ProductResponse> CreateProductAsync(this HttpClient client, CancellationToken ct, decimal price = 2.50m, VatRate vatRate = VatRate.Standard)
    {
        var request = new CreateProductRequest("T-" + Guid.CreateVersion7().ToString("N")[12..], "Test product", price, vatRate);
        var response = await client.PostAsJsonAsync("/products", request, ct);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<ProductResponse>(ct))!;
    }

    // Creates a product with the unit price, and an order of two of it.
    public static async Task<OrderResponse> CreateOrderAsync(this HttpClient client, Guid customerId, CancellationToken ct, decimal unitPrice = 2.50m, string? deliveryInstructions = null)
    {
        var product = await client.CreateProductAsync(ct, unitPrice);
        var request = new CreateOrderRequest(customerId, [new CreateOrderLineRequest(product.Sku, 2)], "PO-1001", deliveryInstructions);
        var response = await client.PostAsJsonAsync("/orders", request, ct);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<OrderResponse>(ct))!;
    }

    public static async Task<ShipmentResponse> CreateShipmentAsync(this HttpClient client, Guid orderId, CancellationToken ct)
    {
        var response = await client.PostAsJsonAsync("/shipments", new CreateShipmentRequest(orderId), ct);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<ShipmentResponse>(ct))!;
    }
}
