using System.Text.Json;
using Orders.Api.Data;

namespace Orders.Api.Messaging;

public static class Outbox
{
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    // Adds the event to the outbox. The caller saves it with its business change in one SaveChangesAsync.
    public static void Enqueue<TEvent>(this OrdersDbContext db, TEvent evt, string wireName, string correlationId, DateTimeOffset now) =>
        db.OutboxMessages.Add(new OutboxMessage(wireName, wireName, JsonSerializer.Serialize(evt, Json), correlationId, now));
}
