using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.QueryDsl;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Orders.Api.Search;

public static class ProductSearchEndpoints
{
    public const int PageSize = 20;

    // index.max_result_window: from + size above it fails. Deeper pages are not offered.
    public const int MaxResults = 10_000;

    public static IEndpointRouteBuilder MapProductSearch(this IEndpointRouteBuilder app)
    {
        app.MapGet("/products/search", SearchAsync).WithTags("Catalog");
        return app;
    }

    private static async Task<Results<Ok<ProductSearchResponse>, ValidationProblem>> SearchAsync(
        ElasticsearchClient client,
        CancellationToken ct,
        string? q = null,
        string? brand = null,
        string? category = null,
        string lang = "nl",
        int page = 1)
    {
        if (page < 1 || page * PageSize > MaxResults)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                [nameof(page)] = [$"The page must be between 1 and {MaxResults / PageSize}."],
            });
        }

        if (lang is not ("nl" or "de"))
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                [nameof(lang)] = ["The language must be nl or de."],
            });
        }

        var response = await client.SearchAsync<ProductDocument>(s => s
            .Indices(ProductIndex.Name)
            .From((page - 1) * PageSize)
            .Size(PageSize)
            .Query(BuildQuery(q, brand, category, lang))
            .Sort(sort => sort.Score(sc => sc.Order(SortOrder.Desc)).Field(d => d.Sku, f => f.Order(SortOrder.Asc))),
            ct);

        var items = response.Documents
            .Select(d => new ProductSearchHit(d.Sku, d.Name, d.Brand, d.Category, d.Price))
            .ToList();
        return TypedResults.Ok(new ProductSearchResponse(items, response.Total, page, PageSize));
    }

    private static Query BuildQuery(string? text, string? brand, string? category, string lang)
    {
        // Clauses that do not score are filters: no score, and Elasticsearch caches them.
        var filters = new List<Query>
        {
            new TermQuery { Field = Infer.Field<ProductDocument>(d => d.Active), Value = true },
        };
        if (!string.IsNullOrWhiteSpace(brand))
        {
            filters.Add(new TermQuery { Field = Infer.Field<ProductDocument>(d => d.Brand), Value = brand.Trim() });
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            filters.Add(new TermQuery { Field = Infer.Field<ProductDocument>(d => d.Category), Value = category.Trim().ToLowerInvariant() });
        }

        var query = new BoolQuery
        {
            Filter = filters,
            Must = [new TermQuery { Field = Infer.Field<ProductDocument>(d => d.InStock), Value = true }],
        };
        if (!string.IsNullOrWhiteSpace(text))
        {
            var description = lang == "de"
                ? Infer.Field<ProductDocument>(d => d.DescriptionDe!)
                : Infer.Field<ProductDocument>(d => d.DescriptionNl!);
            query.Must.Add(
                new MultiMatchQuery
                {
                    Query = text,
                    Fields = new[] { Infer.Field<ProductDocument>(d => d.Name), description },
                    Fuzziness = new Fuzziness("AUTO"),
                });
        }

        return query;
    }
}
