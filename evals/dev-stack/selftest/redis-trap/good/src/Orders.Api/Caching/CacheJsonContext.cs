using System.Text.Json;
using System.Text.Json.Serialization;
using Orders.Api.Orders;

namespace Orders.Api.Caching;

[JsonSourceGenerationOptions(JsonSerializerDefaults.Web)]
[JsonSerializable(typeof(OrderResponse))]
internal sealed partial class CacheJsonContext : JsonSerializerContext;
