# Handlers and messages

How Wolverine finds a handler, what a handler can take and return, and how messages get their names. The code compiles with WolverineFx 6.51.0.

## Contents

- Discovery
- Parameters
- Return values and cascading
- Invoke, send, publish
- Scheduled messages
- Contracts and forwarders

## Discovery

Wolverine finds a handler by convention in `opts.ApplicationAssembly`:

- A public class whose name ends in `Handler` or `Consumer`.
- A public method named `Handle`, `HandleAsync`, `Consume`, or `ConsumeAsync`. The first parameter is the message.
- Static classes and static methods work and are preferred: no object for each message.
- A method that returns a primitive (`int`, `bool`, `string`) is excluded ("Returns a primitive type"). Return a record instead.

`dotnet run -- describe` and `dotnet run -- codegen preview` show the handlers that Wolverine found.

## Parameters

After the message, a handler can take:

- Services from the container: a `DbContext` (registered as in rule 4 of `SKILL.md`), `TimeProvider`, `IOptions<T>`, own services registered with `AddScoped<IFoo, Foo>()`.
- `CancellationToken`, `ILogger<T>`, `Envelope` (the metadata), `IMessageContext`.

A lambda registration (`AddScoped<IFoo>(sp => new Foo(...))`) forces service location. Wolverine 6 refuses it on the first message.

## Return values and cascading

The return value of a handler is the list of messages to send next. Wolverine sends them after the transaction commits, through the outbox.

```csharp
public static class ShipOrderHandler
{
    public static async Task<OrderShipped> Handle(ShipOrder command, OrdersDbContext db, CancellationToken ct)
    {
        var order = await db.Orders.SingleAsync(o => o.Id == command.OrderId, ct);
        if (order.Cancelled)
        {
            throw new OrderRuleException($"Order {order.Id} is cancelled.");
        }

        order.Shipped = true;
        return new OrderShipped(order.Id);
    }
}

public static class CancelOrderCascade
{
    public static OutgoingMessages Handle(CancelOrder command)
    {
        var messages = new OutgoingMessages();
        messages.Add(new OrderShipped(command.OrderId));
        messages.Delay(new ShipOrder(command.OrderId), TimeSpan.FromMinutes(5));
        return messages;
    }
}
```

- One message: return the record. More than one: a tuple `(OrderShipped, InvoiceRequested)` or `OutgoingMessages`.
- `IMessageBus` in a handler works, but cascading keeps the handler a pure function that a unit test can check without mocks.
- Do not inject `IMessageBus` into domain services. The service then sends outside the handler's return value, and a reader cannot see the flow.

## Invoke, send, publish

| Call | Route | No handler or subscriber | Use |
|------|-------|--------------------------|-----|
| `InvokeAsync(message)` | Runs the local handler now, in the caller's flow | Throws | A command in the same process, from an endpoint |
| `InvokeAsync<T>(message)` | The same, and returns the response `T` (a record) | Throws | A query in the same process |
| `SendAsync(message)` | To the routed endpoint (local queue or transport) | Throws `IndeterminateRoutesException` | A command that must be handled |
| `PublishAsync(message)` | To each subscriber | Does nothing | An event |

```csharp
public static async Task<StockResult> CheckAsync(IMessageBus bus, CancellationToken ct) =>
    await bus.InvokeAsync<StockResult>(new CheckStock("SKU-1", 2), ct);
```

`InvokeAsync` runs the handler inline: when it fails, the exception goes to the caller. A move to the error queue and a scheduled retry apply only to messages that come from a queue or a transport.

## Scheduled messages

- `messages.Delay(message, delay)` in `OutgoingMessages`, or `bus.ScheduleAsync(message, delay)`.
- With the durable message store, scheduled messages survive a restart.
- Use Wolverine scheduling when the service already uses Wolverine (CFG-07). Do not add a second job library for it.

## Contracts and forwarders

Pin the wire name of each contract. The default name is the full CLR type name, so a rename or a namespace move breaks the consumers.

```csharp
[MessageIdentity("orders.order-placed", Version = 1)]
public sealed record OrderPlacedV1(Guid OrderId) : IForwardsTo<OrderPlacedV2>
{
    public OrderPlacedV2 Transform() => new(OrderId, Guid.Empty);
}

[MessageIdentity("orders.order-placed", Version = 2)]
public sealed record OrderPlacedV2(Guid OrderId, Guid CustomerId);

// In UseWolverine: Wolverine 6 does not scan for forwarders.
opts.RegisterMessageForwarder<OrderPlacedV1, OrderPlacedV2>();
```

- A handler for `OrderPlacedV2` then also receives `OrderPlacedV1` messages from old publishers.
- `opts.UseAutomaticForwarderDiscovery()` brings back the scan of version 5. It is obsolete and goes away in version 7.
