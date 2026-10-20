using System.Globalization;
using System.Text;
using Elastic.Clients.Elasticsearch;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Orders.Api.Search;

public static class OrderSearchEndpoints
{
    private const int PageSize = 20;
    private const int ExportPageSize = 1000;

    public static IEndpointRouteBuilder MapOrderSearch(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/orders").WithTags("Order search");
        group.MapGet("/search", SearchAsync);
        group.MapGet("/export", ExportAsync);
        return app;
    }

    private static async Task<Ok<List<OrderDocument>>> SearchAsync(string q, string? status, int? page, ElasticsearchClient client, CancellationToken ct)
    {
        var response = await client.SearchAsync<OrderDocument>(s => s
            .Indices("orders")
            .From((page ?? 0) * PageSize)
            .Size(PageSize)
            .Query(query => query.Bool(b => b.Must(
                m => m.Match(mt => mt.Field(d => d.Skus).Query(q).Fuzziness(new Fuzziness("AUTO"))),
                m => m.Term(t => t.Field(d => d.Status).Value(status ?? "Placed")))))
            .Sort(so => so.Field(d => d.PlacedAt, f => f.Order(SortOrder.Desc))),
            ct);

        return TypedResults.Ok(response.Documents.ToList());
    }

    private static async Task<FileContentHttpResult> ExportAsync(ElasticsearchClient client, CancellationToken ct)
    {
        var csv = new StringBuilder("id,customerId,status,placedAt,total\n");
        for (var page = 0; ; page++)
        {
            var response = await client.SearchAsync<OrderDocument>(s => s
                .Indices("orders")
                .From(page * ExportPageSize)
                .Size(ExportPageSize)
                .Sort(so => so.Field(d => d.PlacedAt, f => f.Order(SortOrder.Desc))),
                ct);
            if (response.Documents.Count == 0)
            {
                break;
            }

            foreach (var d in response.Documents)
            {
                csv.Append(CultureInfo.InvariantCulture, $"{d.Id},{d.CustomerId},{d.Status},{d.PlacedAt:O},{d.Total}\n");
            }
        }

        return TypedResults.File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", "orders.csv");
    }
}
