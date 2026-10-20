# Tests

How to test Wolverine handlers and flows. The general test rules (MSTest, Testcontainers, one database container per assembly) are in `dev-stack:dotnet-testing`. The code compiles with WolverineFx 6.51.0, `Microsoft.AspNetCore.Mvc.Testing` 10, and MSTest 4. The tracked session tests below ran against the app of `configuration.md`.

## Contents

- What to test where
- The host in a test
- Tracked sessions
- Error policies
- Tests with the broker

## What to test where

| Test | How |
|------|-----|
| Handler logic | Call the static `Handle` method. Assert on the returned messages. No host. |
| Wiring: registrations, transaction, cascading | A tracked session on the real host, external transports off. |
| Error policies | A tracked session with `SendMessageAndWaitAsync`, then `session.MovedToErrorQueue` or the count of `ExecutionStarted` records. |
| Outbox, broker down, quorum queues | A real broker in a container (`dev-stack:rabbitmq`, `references/testing.md` of that skill). |

## The host in a test

```csharp
JasperFxEnvironment.AutoStartHost = true; // Program.cs ends with RunJasperFxCommands
await using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
    builder.ConfigureServices(services =>
    {
        services.DisableAllExternalWolverineTransports();
        services.RunWolverineInSoloMode();
    }));
var host = factory.Services.GetRequiredService<IHost>();
```

- `JasperFxEnvironment.AutoStartHost` (namespace `JasperFx.CommandLine`): without it, an app that ends with `return await app.RunJasperFxCommands(args);` does not start under `WebApplicationFactory`, and `CreateClient()` throws "The server has not been started or no web application was configured". An app that ends with `app.RunAsync()` does not need it.
- `DisableAllExternalWolverineTransports()`: messages to RabbitMQ go to a stub. The test needs no broker.
- `RunWolverineInSoloMode()`: the node takes the durability work at once, without a leader election.
- `opts.ApplicationAssembly = typeof(Program).Assembly` in `Program.cs`: Wolverine finds the handlers in the app, not in the test assembly.

## Tracked sessions

A tracked session runs the message and waits until all cascading work ends, or the timeout.

```csharp
var session = await host.TrackActivity()
    .Timeout(30.Seconds())
    .InvokeMessageAndWaitAsync(new ShipOrder(orderId));

Assert.IsNotNull(session.Sent.SingleMessage<OrderShipped>());
```

- `InvokeMessageAndWaitAsync`: runs the handler inline. An exception goes to the test.
- `SendMessageAndWaitAsync`: sends through the routed endpoint, so the error policies apply.
- `ExecuteAndWaitAsync(ctx => ...)`: runs your own code, for example an HTTP call, and waits for the messages that it starts.
- The session throws on a handler exception. Add `.DoNotAssertOnExceptionsDetected()` when the test expects one.
- Do not wait with `Task.Delay`. The session ends when the work ends.

## Error policies

```csharp
var session = await host.TrackActivity()
    .DoNotAssertOnExceptionsDetected()
    .Timeout(60.Seconds())
    .SendMessageAndWaitAsync(new CancelOrder(shippedOrderId, "test"));

var starts = session.AllRecordsInOrder(MessageEventType.ExecutionStarted)
    .Count(record => record.Envelope?.Message is CancelOrder);
Assert.AreEqual(1, starts); // a business error is not retried
Assert.IsNotNull(session.MovedToErrorQueue.SingleEnvelope<CancelOrder>());
```

## Tests with the broker

For the broker-down path, start RabbitMQ on fixed host ports, start the app without `DisableAllExternalWolverineTransports()`, stop the broker, call the API, dispose the app, start the broker, start a new app, and wait for the message on a queue that the test binds to the exchange. The durable outbox sends it from the database. Read the queue through the management API (`dev-stack:rabbitmq`).
