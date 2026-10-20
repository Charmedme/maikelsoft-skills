using System.Globalization;
using System.Text;
using Elastic.Clients.Elasticsearch;

namespace Orders.Api.Search;

public sealed class OrderExport(OrderSearch search)
{
    private const int PageSize = 1000;

    public async Task WriteCsvAsync(Stream output, CancellationToken ct)
    {
        await using var writer = new StreamWriter(output, new UTF8Encoding(false));
        await writer.WriteLineAsync("id,customerId,status,placedAt,total");

        for (var page = 0; ; page++)
        {
            var response = await search.Client.SearchAsync<OrderDocument>(s => s
                .Indices(OrderIndex.Name)
                .From(page * PageSize)
                .Size(PageSize)
                .Sort(so => so.Field(d => d.PlacedAt, f => f.Order(SortOrder.Desc))),
                ct);

            if (!response.IsValidResponse)
            {
                throw new InvalidOperationException(response.DebugInformation);
            }

            if (response.Documents.Count == 0)
            {
                break;
            }

            foreach (var d in response.Documents)
            {
                await writer.WriteLineAsync(string.Create(CultureInfo.InvariantCulture, $"{d.Id},{d.CustomerId},{d.Status},{d.PlacedAt:O},{d.Total}"));
            }

            await writer.FlushAsync(ct);
        }
    }
}
