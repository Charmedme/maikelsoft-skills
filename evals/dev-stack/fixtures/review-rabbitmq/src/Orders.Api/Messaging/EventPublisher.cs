using System.Text.Json;
using RabbitMQ.Client;

namespace Orders.Api.Messaging;

public sealed class EventPublisher(IChannel channel)
{
    private static readonly JsonSerializerOptions s_json = new(JsonSerializerDefaults.Web);

    public void Publish(OrderPlaced evt)
    {
        var body = JsonSerializer.SerializeToUtf8Bytes(evt, s_json);
        var properties = new BasicProperties { ContentType = "application/json", Type = "order.placed.v1" };

        // Fire and forget, so that the request does not wait for RabbitMQ.
        _ = channel.BasicPublishAsync("orders.events", "order.placed.v1", mandatory: false, properties, body);
    }
}
