using Orders.Api.Orders;

namespace Orders.Api.Search;

public sealed class OrderIndex(OrderSearch search)
{
    public const string Name = "orders";

    public async Task EnsureCreatedAsync(CancellationToken ct)
    {
        var exists = await search.Client.Indices.ExistsAsync(Name, ct);
        if (!exists.Exists)
        {
            await search.Client.Indices.CreateAsync(Name, ct);
        }
    }

    public async Task IndexAsync(Order order, CancellationToken ct)
    {
        await search.Client.IndexAsync(OrderDocument.From(order), i => i.Index(Name).Id(order.Id.ToString()), ct);
    }
}
