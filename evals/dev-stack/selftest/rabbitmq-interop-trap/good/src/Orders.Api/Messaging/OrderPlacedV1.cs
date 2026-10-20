using System.Text.Json;

namespace Orders.Api.Messaging;

/// <summary>Wire contract for the Go warehouse service. Change it only by adding fields.</summary>
public sealed record OrderPlacedV1(Guid OrderId, Guid CustomerId, decimal Total, DateTimeOffset PlacedAt)
{
    public const string WireName = "order.placed.v1";

    public const string RoutingKey = "order.placed.v1";

    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
}
