using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;

namespace Orders.Api.Tests;

[TestClass]
[DoNotParallelize]
public sealed class OpenApiDocumentTests
{
    public TestContext TestContext { get; set; } = null!;

    [TestMethod]
    public async Task OpenApi_EveryOperation_HasOperationId()
    {
        using var document = await ReadDocumentAsync();

        var missing = Operations(document.RootElement)
            .Where(op => !op.Operation.TryGetProperty("operationId", out _))
            .Select(op => $"{op.Method} {op.Path}")
            .ToList();

        missing.Should().BeEmpty();
    }

    [TestMethod]
    public async Task OpenApi_AdminEndpoints_AreNotInDocument()
    {
        using var document = await ReadDocumentAsync();

        Operations(document.RootElement).Select(op => op.Path).Should().NotContain(path => path.StartsWith("/admin", StringComparison.Ordinal));
    }

    [TestMethod]
    public async Task OpenApi_CancelOrder_Documents409AsProblemJson()
    {
        using var document = await ReadDocumentAsync();

        var conflict = document.RootElement.GetProperty("paths").GetProperty("/orders/{id}/cancel")
            .GetProperty("post").GetProperty("responses").GetProperty("409");

        conflict.GetProperty("content").TryGetProperty("application/problem+json", out _).Should().BeTrue();
    }

    [TestMethod]
    public async Task OpenApi_InProduction_IsNotServed()
    {
        await using var factory = new OrdersApiFactory().WithWebHostBuilder(builder => builder.UseEnvironment("Production"));
        using var client = factory.CreateClient();

        using var document = await client.GetAsync(new Uri("/openapi/v1.json", UriKind.Relative), TestContext.CancellationToken);
        using var ui = await client.GetAsync(new Uri("/scalar", UriKind.Relative), TestContext.CancellationToken);

        document.StatusCode.Should().Be(HttpStatusCode.NotFound);
        ui.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private async Task<JsonDocument> ReadDocumentAsync()
    {
        await using var factory = new OrdersApiFactory().WithWebHostBuilder(builder => builder.UseEnvironment("Development"));
        using var client = factory.CreateClient();
        var json = await client.GetStringAsync(new Uri("/openapi/v1.json", UriKind.Relative), TestContext.CancellationToken);
        return JsonDocument.Parse(json);
    }

    private static IEnumerable<(string Path, string Method, JsonElement Operation)> Operations(JsonElement document) =>
        from path in document.GetProperty("paths").EnumerateObject()
        from operation in path.Value.EnumerateObject()
        where operation.Value.ValueKind == JsonValueKind.Object && operation.Value.TryGetProperty("responses", out _)
        select (path.Name, operation.Name.ToUpperInvariant(), operation.Value);
}
