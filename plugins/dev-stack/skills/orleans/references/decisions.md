# Orleans decisions

The team decisions that Orleans code needs. The order is the order in which a new service meets them. The recommendation is the catalog default. Ask the human before you apply it (stack rule 4).

Batch the questions: ask ORL-01, ORL-02, ORL-03, and ORL-10 together as one "first silo" question. Ask the others one by one when the work needs them.

## Contents

- ORL-01. Hosting shape
- ORL-02. Clustering provider
- ORL-03. Grain storage provider and serializer
- ORL-10. Target framework of the silo
- ORL-04. State write policy
- ORL-05. Scheduled work
- ORL-06. Reentrancy
- ORL-07. Streams
- ORL-08. Dashboard
- ORL-09. Test cluster scope

## ORL-01. Hosting shape

- Question: Does the silo run in the same process as the API, or does the API use a separate Orleans client?
- Ask when: the first silo.
- Options: Co-hosted: `UseOrleans` in the web app, the API injects `IGrainFactory`. Separate: silo hosts with `UseOrleans`, the API hosts with `UseOrleansClient`.
- Recommended: Co-hosted. One process has one deploy, no gateway hop, and no client connection to manage.
- Choose another option when: the front end must scale apart from the silos, or the silos must not take public traffic.
- Depends on: none.
- Revisit when: the API and the grains need a different number of instances.
- Check: judge.
- Detect: none.

## ORL-02. Clustering provider

- Question: Which membership table does the cluster use?
- Ask when: the first silo that runs outside a developer machine.
- Options: Redis (`Microsoft.Orleans.Clustering.Redis`, GA since 10.0). ADO.NET on PostgreSQL or SQL Server (`Microsoft.Orleans.Clustering.AdoNet`, with the SQL scripts of the package). Azure Table (`Microsoft.Orleans.Clustering.AzureStorage`). Localhost clustering: one silo, development and tests only.
- Recommended: The store that the service already runs. Redis when the service has Redis, else ADO.NET on PostgreSQL. A second store only for the membership table is one more system to operate.
- Choose another option when: the platform team gives a managed store, or the cluster runs on Azure with Azure Storage.
- Depends on: ORL-01.
- Revisit when: the store has an outage that stops the cluster.
- Check: judge.
- Detect: DET-ORL-02.

## ORL-03. Grain storage provider and serializer

- Question: Where does grain state go, and with which storage serializer?
- Ask when: the first `[PersistentState]`.
- Options: Redis (`Microsoft.Orleans.Persistence.Redis`). ADO.NET (`Microsoft.Orleans.Persistence.AdoNet`). Azure Blob or Table (`Microsoft.Orleans.Persistence.AzureStorage`). Memory storage: tests only. Serializer: the default of the provider (JSON with Newtonsoft.Json for most providers). A System.Text.Json grain storage serializer exists since 10.3.
- Recommended: The same store as ORL-02, with the default serializer. Name the provider `"store"`. A change of serializer in a live service makes the stored state unreadable unless the new serializer reads the old format.
- Choose another option when: the state is large and changes often (a measurement shows the cost). The team wants System.Text.Json for a new service with no stored state.
- Depends on: ORL-02.
- Revisit when: a new state type needs a type that the serializer does not handle.
- Check: judge.
- Detect: DET-ORL-03.

## ORL-10. Target framework of the silo

- Question: Does the silo target `net10.0` or `net11.0`?
- Ask when: the first silo, or a move of the solution to .NET 11.
- Options: `net10.0`: LTS, the asset that Orleans 10.4.2 ships. `net11.0`: STS, uses the `net10.0` assets.
- Recommended: `net10.0`. Use `net11.0` when the solution already targets it. A build and an `InProcessTestCluster` test passed on `net11.0` with Orleans 10.4.2 (SDK 11.0.100).
- Choose another option when: the other projects of the solution target `net11.0`. Do not use `net8.0` or `net9.0`: both are out of support since 2026-11-10.
- Depends on: none.
- Revisit when: Orleans ships a `net11.0` asset.
- Check: tool (`dotnet build`, `dotnet test`).
- Detect: none.

## ORL-04. State write policy

- Question: When does a grain write its state?
- Ask when: the first grain with state that changes often (a counter, a rate).
- Options: Write in each call that changes state: no loss, one write for each call. Flush timer: fewer writes, a silo crash loses the changes since the last write. Write on deactivation only: does not run after a crash, so the loss has no limit.
- Recommended: Write in each call that changes state. Use a flush timer only when the product owner accepts the loss window. Write it in the code and in the report.
- Choose another option when: the write rate overloads the store (a measurement), and the team accepts a loss of the data (view counts, statistics).
- Depends on: ORL-03.
- Revisit when: the store is slow, or a user reports lost data.
- Check: judge.
- Detect: none.

## ORL-05. Scheduled work

- Question: Which mechanism runs work on a schedule?
- Ask when: the first periodic or delayed work in a grain.
- Options: Grain timer (`RegisterGrainTimer`): only while the grain is active, not stored. Reminder (`RegisterOrUpdateReminder`): stored, activates the grain, period of 1 minute or more. Durable Jobs (`Microsoft.Orleans.DurableJobs`): `10.4.2-alpha.1`, preview. A `BackgroundService` outside the grains (`dev-stack:aspnetcore`).
- Recommended: A grain timer for work while the grain is active. A reminder when the work must run after deactivation or a silo restart. No Durable Jobs until a stable release: an alpha API can change in each release.
- Choose another option when: the work does not belong to one grain. Then a `BackgroundService` that calls the grains.
- Depends on: ORL-02 (the reminder table uses a provider too).
- Revisit when: Durable Jobs ships a stable version.
- Check: tool (ORL-TIMER-OLD), judge.
- Detect: none.

## ORL-06. Reentrancy

- Question: Can a grain take a new call while it awaits?
- Ask when: a call chain deadlocks or times out (A calls B, B calls A), or a read method is slow under load.
- Options: Not reentrant (default). `[ReadOnly]` on read methods. `[AlwaysInterleave]` on one interface method. `[Reentrant]` on the grain class.
- Recommended: Not reentrant, with `[ReadOnly]` on methods that only read. Fix a call cycle in the design first. `[Reentrant]` only with a comment that says why the state stays correct at each `await`.
- Choose another option when: a measurement shows that one slow call blocks many others and the methods do not share state.
- Depends on: none.
- Revisit when: a state bug appears after an `await`.
- Check: judge.
- Detect: none.

## ORL-07. Streams

- Question: Does the service use Orleans streams, and on which provider?
- Ask when: the first event from one grain to many grains.
- Options: No streams: direct grain calls or observers. Memory streams: lost at a silo crash, tests only. A persistent provider (Azure Queue, Event Hubs). NATS streams: `10.4.2-alpha.1`, preview. A broker for messages to other services (`dev-stack:rabbitmq`, `dev-stack:wolverine`).
- Recommended: No streams until a need. Streams only for events inside the cluster, on a persistent provider. Messages to other services go through the broker of the team.
- Choose another option when: the team already runs Event Hubs or Azure Queues.
- Depends on: ORL-02.
- Revisit when: events get lost or arrive out of order.
- Check: judge.
- Detect: none.

## ORL-08. Dashboard

- Question: Does the cluster show the Orleans Dashboard, and to whom?
- Ask when: the first production cluster, or an existing `OrleansDashboard` package on an upgrade to 10.
- Options: No dashboard: OpenTelemetry metrics only (`dev-stack:dotnet-observability`). `Microsoft.Orleans.Dashboard` with `MapOrleansDashboard().RequireAuthorization()`. The old `OrleansDashboard` package (OrleansContrib): stops at 8.2.0.
- Recommended: `Microsoft.Orleans.Dashboard` on an internal route with an authorization policy for operators. The docs mark it preview, and NuGet 10.4.2 has no preview tag. Say so in the decision.
- Choose another option when: the team watches the cluster in Grafana or Azure Monitor already.
- Depends on: ORL-01.
- Revisit when: the docs drop the preview label.
- Check: judge.
- Detect: DET-ORL-08.

## ORL-09. Test cluster scope

- Question: How many test clusters does a test project start?
- Ask when: the first grain test.
- Options: One `InProcessTestCluster` for each test assembly (`[AssemblyInitialize]`), a new grain key for each test. One cluster for each test class (`[ClassInitialize]`). One cluster for each test: slow, about 3 seconds each.
- Recommended: One cluster for each assembly, a new grain key (`Guid.NewGuid()`) for each test. Tests can then run in parallel without shared state.
- Choose another option when: a test class needs other silo options (another provider, a fake service). Then one cluster for that class.
- Depends on: none.
- Revisit when: the test run gets slow, or tests fail only in parallel.
- Check: tool (ORL-TEST-OLD), judge.
- Detect: none.
