# Tests against RabbitMQ

How to test publishers and consumers against a real broker. The general Testcontainers rules (one container per assembly, image pins, Docker in CI) are in `dev-stack:dotnet-testing`. The code compiles with Testcontainers.RabbitMq 4.16.0, RabbitMQ.Client 7.2.2, and MSTest 4.

## Contents

- What to test where
- The broker
- Wait with a signal
- The broker-down path
- Read a queue without a client

## What to test where

| Test | How |
|------|-----|
| Handler logic | A plain unit test of the method that the consumer calls. No broker. |
| Contract | Serialize the record and compare it with a stored JSON sample. No broker. |
| Topology, publish, consume, acks, confirms | A real broker in a container. |
| Broker down, redelivery, duplicates | A real broker: stop it, start it, publish the same message two times. |

Do not mock `IChannel` or `IConnection`. A mock proves nothing about acks, confirms, prefetch, or redelivery.

## The broker

- Pin the production major version: `new RabbitMqBuilder("rabbitmq:4.3-management-alpine")`. The parameterless constructor is obsolete (CS0618) and starts `rabbitmq:3.11`.
- `GetConnectionString()` returns an `amqp://` URI for the mapped port.

## Wait with a signal

```csharp
[TestMethod]
public async Task PublishedEvent_ReachesTheQueue()
{
    var ct = TestContext.CancellationToken;
    await using var broker = new RabbitMqBuilder("rabbitmq:4.3-management-alpine").Build();
    await broker.StartAsync(ct);

    var factory = new ConnectionFactory { Uri = new Uri(broker.GetConnectionString()) };
    await using var connection = await factory.CreateConnectionAsync(ct);
    await using var channel = await connection.CreateChannelAsync(cancellationToken: ct);
    await Topology.DeclareAsync(channel, ct);

    var received = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
    var consumer = new AsyncEventingBasicConsumer(channel);
    consumer.ReceivedAsync += async (_, ea) =>
    {
        received.TrySetResult(ea.BasicProperties.Type);
        await channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
    };
    await channel.BasicConsumeAsync("billing.order-placed", autoAck: false, consumer, ct);

    await using var publish = await PublishExample.CreatePublishChannelAsync(connection, ct);
    await PublishExample.PublishAsync(publish, new OrderPlacedV1(Guid.CreateVersion7(), Guid.CreateVersion7(), 10m, DateTimeOffset.UtcNow), Guid.CreateVersion7(), "test", ct);

    var type = await received.Task.WaitAsync(TimeSpan.FromSeconds(10), ct);
    Assert.AreEqual("order.placed.v1", type);
}
```

`WaitAsync` with a timeout ends the test as soon as the message comes, and fails with a clear timeout when it does not. A fixed `Task.Delay` is slow when it is long and flaky when it is short.

## The broker-down path

To test "no event lost while the broker is down", the broker must come back on the same address:

1. Pick two free host ports and bind them: `new ContainerBuilder("rabbitmq:4.3-management-alpine").WithPortBinding(amqpPort, 5672).WithPortBinding(httpPort, 15672)`. A random mapping changes after a restart.
2. Start the app (for example with `WebApplicationFactory`) with the fixed AMQP URI.
3. `await container.StopAsync(ct)`, call the API, and expect a success.
4. Dispose the app and start a new one. This proves that the event was in the database, not only in memory.
5. `await container.StartAsync(ct)`, wait until the AMQP listener is up, and wait for the event with a timeout.

Wait until the broker is ready with the management API: `GET /api/health/checks/port-listener/5672` returns 200.

## Read a queue without a client

The management API reads messages for an assertion: `POST /api/queues/%2F/{queue}/get` with `{"count":10,"ackmode":"ack_requeue_false","encoding":"auto"}` and basic authentication. Each item has `exchange`, `routing_key`, `properties` (`type`, `message_id`, `delivery_mode`, `content_type`), and `payload`. `GET /api/queues/%2F/{queue}` returns the queue with its `type`.
