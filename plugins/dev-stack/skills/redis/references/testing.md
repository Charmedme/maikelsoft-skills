# Testing cache and Redis code

Test cached code against a real Valkey container. A fake `IDistributedCache` is enough only for a unit test of the cache logic itself. Test framework rules are in `dev-stack:dotnet-testing`.

## Contents

- Container
- Two instances on one cache
- Round trip of a cached DTO
- What a test must prove

## Container

Use `Testcontainers.Redis` with an explicit image. The default image of the module is old.

```csharp
s_valkey = new RedisBuilder("valkey/valkey:9.1").Build();
await s_valkey.StartAsync(context.CancellationToken);
var cacheConnectionString = s_valkey.GetConnectionString();
```

Pass the connection string to the app with `UseSetting("ConnectionStrings:Cache", ...)` in the `WebApplicationFactory`. In .NET 10 the value is visible in `builder.Configuration` before `Build()`, so a `ConnectAsync` in `Program.cs` reads it (checked in the lab).

Start the container one time per test assembly, next to the database container.

## Two instances on one cache

L1 staleness shows only with two app instances. Two `WebApplicationFactory<Program>` instances that share the database and the Valkey container act as two nodes.

```csharp
[TestMethod]
public async Task Cancel_OnNodeA_NodeBReturnsCancelled()
{
    using var clientA = s_nodeA.CreateClient();
    using var clientB = s_nodeB.CreateClient();
    var created = await CreateAsync(clientA);
    (await clientB.GetFromJsonAsync<OrderResponse>($"/orders/{created.Id}", TestContext.CancellationToken))!
        .Status.Should().Be("Placed"); // node B now has the order in its cache

    var cancel = await clientA.PostAsync(new Uri($"/orders/{created.Id}/cancel", UriKind.Relative), null, TestContext.CancellationToken);

    cancel.StatusCode.Should().Be(HttpStatusCode.OK);
    (await clientB.GetFromJsonAsync<OrderResponse>($"/orders/{created.Id}", TestContext.CancellationToken))!
        .Status.Should().Be("Cancelled");
}
```

The first read on node B is the important step. Without it, node B has no L1 copy and the test passes with a stale design.

## Round trip of a cached DTO

The first caller gets the instance that the factory returned, so one read proves nothing about serialization. Read two times on one node and one time on the other node. The second read and the read on the other node come from bytes.

```csharp
var first = await client.GetFromJsonAsync<OrderResponse>($"/orders/{created.Id}", TestContext.CancellationToken);
var second = await client.GetFromJsonAsync<OrderResponse>($"/orders/{created.Id}", TestContext.CancellationToken);

second.Should().BeEquivalentTo(first);
second!.Id.Should().Be(created.Id);
second.Lines.Should().HaveCount(2);
```

## What a test must prove

| Behavior | Test |
|----------|------|
| A change shows on all nodes (CACHE-07) | Two factories, read on B, change on A, read on B |
| The cached value keeps all data (CACHE-06) | Two reads on one node and one read on the other node |
| The write path removes the key | Change, then read: the new value |
| The lock prevents a second run | Two calls at the same time: one runs, one returns |
| The app starts when Valkey is down | Start the factory with a closed port: health check reports the cache, the app starts |
