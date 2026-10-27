# Configuration

The full `UseWolverine` setup for a web service with EF Core on PostgreSQL and the RabbitMQ transport, then the parts one by one. The code compiles with WolverineFx 6.51.0 on .NET 10. The line `.DeliveryLimitForDurableQuorumQueues(-1)` needs 6.51.3 or later, and compiled with 6.56.0 on 2026-10-27. Use 6.51.3 or later in a project. The `Program.cs` below passed these tests with a real RabbitMQ 4.3 broker:

- A cancel command.
- A business error that is not retried.
- A quorum queue.
- A message that reached the exchange after the broker was down and the service restarted.

## Contents

- Program.cs
- Message store and transactions
- RabbitMQ transport
- Listener modes
- Durability settings
- Code generation
- Tracing

## Program.cs

```csharp
var builder = WebApplication.CreateBuilder(args);

var ordersDb = builder.Configuration.GetConnectionString("Orders")
    ?? throw new InvalidOperationException("The connection string 'Orders' is missing.");
var rabbitMq = builder.Configuration.GetConnectionString("RabbitMq")
    ?? throw new InvalidOperationException("The connection string 'RabbitMq' is missing.");

builder.UseWolverine(opts =>
{
    opts.ServiceName = "orders-api";
    opts.ApplicationAssembly = typeof(Program).Assembly; // tests start the app from another assembly

    // The code generator must see through the DbContext registration (no service location).
    opts.Services.AddDbContextWithWolverineIntegration<OrdersDbContext>(o => o.UseNpgsql(ordersDb));
    opts.PersistMessagesWithPostgresql(ordersDb, "wolverine");
    opts.UseEntityFrameworkCoreTransactions();
    opts.Policies.AutoApplyTransactions();
    opts.Policies.UseDurableOutboxOnAllSendingEndpoints();
    opts.Policies.UseDurableInboxOnAllListeners();

    opts.UseRabbitMq(new Uri(rabbitMq))
        .UseQuorumQueues()
        // The durable inbox owns the retries. No broker limit on its quorum queues.
        .DeliveryLimitForDurableQuorumQueues(-1)
        .ConfigureChannelCreation(o =>
        {
            o.PublisherConfirmationsEnabled = true;
            o.PublisherConfirmationTrackingEnabled = true;
        })
        .CustomizeDeadLetterQueueing(new DeadLetterQueue("orders-api.dlq"))
        .AutoProvision();

    opts.PublishMessage<OrderPlaced>().ToRabbitExchange("orders.events");
    opts.ListenToRabbitQueue("orders-api.cancel-order");

    // Business errors: no retry.
    opts.Policies.OnException<OrderCancelRejectedException>().MoveToErrorQueue();

    // Transient errors (MSG-08).
    opts.Policies.OnException<NpgsqlException>()
        .RetryWithCooldown(50.Milliseconds(), 250.Milliseconds(), 1.Seconds())
        .Then.ScheduleRetry(10.Seconds(), 1.Minutes()).WithFullJitter()
        .Then.MoveToErrorQueue();
});

builder.Services.CritterStackDefaults(x =>
{
    x.Production.GeneratedCodeMode = TypeLoadMode.Static;
    x.Production.AssertAllPreGeneratedTypesExist = true;
    x.Development.GeneratedCodeMode = TypeLoadMode.Dynamic;
});

var app = builder.Build();
app.MapOrders();
return await app.RunJasperFxCommands(args);
```

Namespaces: `JasperFx`, `JasperFx.CodeGeneration`, `JasperFx.Core`, `Wolverine`, `Wolverine.EntityFrameworkCore`, `Wolverine.ErrorHandling`, `Wolverine.Postgresql`, `Wolverine.RabbitMQ`. Packages: `WolverineFx`, `WolverineFx.EntityFrameworkCore`, `WolverineFx.Postgresql` (or `WolverineFx.SqlServer`), `WolverineFx.RabbitMQ`, `WolverineFx.RuntimeCompilation`.

The configuration values are read when the builder runs. `WebApplicationFactory` settings from `UseSetting` are visible at that time.

## Message store and transactions

- `PersistMessagesWithPostgresql(connectionString, schema)` keeps the outbox, the inbox, and scheduled messages in the service database. Wolverine creates its tables at startup.
- `UseEntityFrameworkCoreTransactions()` and `AutoApplyTransactions()` make each handler that uses a `DbContext` call `SaveChangesAsync` and flush the outgoing messages in one transaction. Do not call `SaveChangesAsync` in such a handler.
- `UseDurableOutboxOnAllSendingEndpoints()`: an outgoing message is a row first. The row goes away after the send. A restart while the broker is down keeps the message.
- `UseDurableInboxOnAllListeners()`: an incoming message is a row before the handler runs. Wolverine acks the broker after the row exists.

## RabbitMQ transport

| Setting | Default | Set |
|---------|---------|-----|
| Queue type | classic | `.UseQuorumQueues()` |
| `x-delivery-limit` on a quorum queue of a durable inbox | none, so the broker default of 20 applies. Wolverine logs a startup warning | `.DeliveryLimitForDurableQuorumQueues(-1)`, or `.DeliveryLimit(-1)` on one listener (6.51.3 or later) |
| Publisher confirms | off | `.ConfigureChannelCreation(o => { o.PublisherConfirmationsEnabled = true; o.PublisherConfirmationTrackingEnabled = true; })` |
| Topology | not declared | `.AutoProvision()`, or declared by operations |
| Dead-letter queue | one shared `wolverine-dead-letter-queue` | `.CustomizeDeadLetterQueueing(new DeadLetterQueue("{service}.dlq"))` |
| Exchange type of `ToRabbitExchange` | fanout | `ToRabbitExchange("orders.events", e => e.ExchangeType = ExchangeType.Topic)` when consumers bind with routing keys |

- `UseRabbitMqUsingNamedConnection("rabbitmq")` reads the connection from configuration (Aspire style).
- Wolverine publishes with `mandatory: false`. Without the topology, the broker drops the message and Wolverine sees a success.
- `.ExternallyOwned()` on `ListenToRabbitQueue(...)`, `ToRabbitQueue(...)`, or `ToRabbitExchange(...)`: Wolverine does not declare, purge, or delete it, also with `AutoProvision()`. Use it for a queue that another team declares. Without it, Wolverine declares the queue with its own arguments, for example its `x-dead-letter-exchange`. The broker refuses with `PRECONDITION_FAILED` (or `ACCESS_REFUSED` without configure permission). Wolverine then retries the broker start.
- A queue per listener for dead letters: `ListenToRabbitQueue("x").DeadLetterQueueing(new DeadLetterQueue("x.dlq"))`. Use it only when another team owns the queue (MSG-06).

## Listener modes

```csharp
opts.ListenToRabbitQueue("orders-api.ship-order").UseDurableInbox();                // business messages
opts.ListenToRabbitQueue("orders-api.refresh-cache").ProcessInParallelWithNativeAcks(); // no database
opts.ListenToRabbitQueue("orders-api.audit").ListenerCount(4).PreFetchCount(20);   // parallel inline
```

| Mode | Ack | Crash | Use |
|------|-----|-------|-----|
| `Inline` (RabbitMQ default, prefetch 100) | After the handler | The broker sends the message again | Simple consumers, order with `ListenerCount(1)` |
| Durable inbox | After the inbox row | Wolverine runs the row again | Business messages |
| `BufferedInMemory` | At receipt | Buffered messages are lost | Only for messages that may get lost |
| `ProcessInParallelWithNativeAcks()` | After the handler, parallel | The broker sends the message again | Parallel work without a database |

## Durability settings

```csharp
opts.Durability.KeepAfterMessageHandling = 10.Minutes(); // window for duplicate detection by envelope ID
opts.Durability.InboxStaleTime = 20.Minutes();           // longer than the longest handler with its retries
```

`DeliveryOptions.DeduplicationId` sets a business key for duplicate detection on the receiver:

```csharp
await bus.PublishAsync(evt, new DeliveryOptions { DeduplicationId = $"order-placed:{orderId}" });
```

## Code generation

- Wolverine generates the code that calls each handler. `Dynamic` compiles it at run time with Roslyn and needs `WolverineFx.RuntimeCompilation`. `Static` loads types that `codegen write` wrote before.
- `dotnet run -- codegen write` writes `Internal/Generated/`. Commit the folder. CI runs the command again and fails on a diff.
- `codegen write` starts the host. Keep connections to the database and the broker out of the top of `Program.cs`, before `RunJasperFxCommands(args)`.
- `opts.CodeGeneration.AlwaysUseServiceLocationFor<T>()` allows service location for one type that the generator cannot see through. Add a comment with the reason.

### Registrations that the generator cannot see through

Wolverine 6 uses `ServiceLocationPolicy.NotAllowed` by default. The generated code builds each handler dependency with its constructor. A registration through a factory hides the constructor, so the generator needs service location and throws `InvalidServiceLocationException`. In `Dynamic` mode the host starts, and the error comes on the first message of that handler.

| Registration | Handler can inject it | Fix |
|--------------|-----------------------|-----|
| `AddScoped<IFoo, Foo>()` | Yes | None |
| `AddScoped<IFoo>(sp => new Foo(...))` | No | `AddScoped<IFoo, Foo>()`, or `AlwaysUseServiceLocationFor<IFoo>()` |
| `AddHttpClient<IFoo, Foo>()` or `AddHttpClient<IFoo, Foo>((sp, http) => ...)` (typed client) | No | `AlwaysUseServiceLocationFor<IFoo>()` with a reason |
| `IHttpClientFactory` (singleton from `AddHttpClient`) | Yes | None. Call `factory.CreateClient("name")` in the handler |
| `AddDbContext<T>(...)` | No | `AddDbContextWithWolverineIntegration<T>`, or `optionsLifetime: ServiceLifetime.Singleton` |

```csharp
// Typed HttpClient: AddHttpClient registers ICarrierClient through a factory.
opts.CodeGeneration.AlwaysUseServiceLocationFor<ICarrierClient>();
```

An endpoint that injects the same client works, because ASP.NET Core resolves it from the container. A test factory that replaces the client with `AddSingleton<ICarrierClient>(fake)` also hides the error. Start the host once with the real registration and send one message to each handler.

Do not use `RestoreV5Defaults()` or `ServiceLocationPolicy.AllowedButWarn` as the fix. They turn the check off for each type.

## Tracing

Add `tracing.AddSource("Wolverine")` and `metrics.AddMeter("Wolverine*")` to OpenTelemetry (`dev-stack:dotnet-observability`). Wolverine sets a correlation ID on each envelope and carries the trace context through the transport.
