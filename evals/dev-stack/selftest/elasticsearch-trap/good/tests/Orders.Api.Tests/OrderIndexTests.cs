using System.Text;
using Elastic.Clients.Elasticsearch;
using Elastic.Transport;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Orders.Api.Search;

namespace Orders.Api.Tests;

[TestClass]
public sealed class OrderIndexTests
{
    private static readonly OrderDocument s_order = new(
        "0199f6a8-0000-7000-8000-000000000001",
        Guid.CreateVersion7(),
        "Placed",
        new DateTimeOffset(2026, 10, 19, 9, 0, 0, TimeSpan.Zero),
        10m,
        ["SKU-1"],
        7);

    public TestContext TestContext { get; set; } = null!;

    [TestMethod]
    public async Task IndexAsync_OneStaleItem_SkipsItAndDoesNotThrow()
    {
        var (index, _) = Create(200, BulkBody((409, "version_conflict_engine_exception"), (201, null)));

        var act = () => index.IndexAsync([s_order, s_order with { Id = "2" }], TestContext.CancellationToken);

        await act.Should().NotThrowAsync();
    }

    [TestMethod]
    public async Task IndexAsync_ItemFailsWithServerError_Throws()
    {
        var (index, _) = Create(200, BulkBody((503, "unavailable_shards_exception")));

        var act = () => index.IndexAsync([s_order], TestContext.CancellationToken);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [TestMethod]
    public async Task IndexAsync_SendsTheOrderVersionAsExternalVersion()
    {
        var (index, requests) = Create(200, BulkBody((201, null)));

        await index.IndexAsync([s_order], TestContext.CancellationToken);

        requests.Should().ContainSingle().Which.Should().Contain("\"version_type\":\"external\"").And.Contain("\"version\":7");
    }

    private static (OrderIndex Index, List<string> Requests) Create(int status, string body)
    {
        var requests = new List<string>();
        var settings = new ElasticsearchClientSettings(
                new SingleNodePool(new Uri("http://localhost:9200")),
                new InMemoryRequestInvoker(Encoding.UTF8.GetBytes(body), status, null, "application/json", new() { ["x-elastic-product"] = ["Elasticsearch"] }))
            .DisableDirectStreaming()
            .OnRequestCompleted(call => requests.Add(Encoding.UTF8.GetString(call.RequestBodyInBytes ?? [])));
        return (new OrderIndex(new ElasticsearchClient(settings), NullLogger<OrderIndex>.Instance), requests);
    }

    private static string BulkBody(params (int Status, string? Error)[] items)
    {
        var parts = items.Select((item, i) =>
            "{\"index\":{\"_index\":\"orders_v1\",\"_id\":\"" + i + "\",\"status\":" + item.Status
            + (item.Error is null ? ",\"_version\":1,\"result\":\"created\"" : ",\"error\":{\"type\":\"" + item.Error + "\",\"reason\":\"test\"}")
            + "}}");
        var errors = items.Any(item => item.Error is not null) ? "true" : "false";
        return "{\"took\":1,\"errors\":" + errors + ",\"items\":[" + string.Join(',', parts) + "]}";
    }
}
