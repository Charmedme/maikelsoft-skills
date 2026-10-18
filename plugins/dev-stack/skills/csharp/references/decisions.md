# Decisions

The team decisions of the `csharp` skill, in the order that a new project meets them. Each block is one catalog entry. The recommendation is not a decision: ask the human, and log the answer (stack rule 4). Existing code keeps its own style.

## Contents

- SOL-05. SDK pin (global.json)
- SOL-01. Solution shape
- SOL-03. Shared build policy (Directory.Build.props)
- SOL-06. Warnings policy
- SOL-07. Code style enforcement (.editorconfig)
- SOL-08. Naming conventions
- SOL-04. Central Package Management
- SOL-02. Folder organization
- SOL-09. Interfaces for services
- CFG-04. Time source

Ask SOL-05, SOL-01, SOL-03, SOL-06, SOL-07, SOL-08, and SOL-04 together as one "repository setup" question, with all recommendations filled in. Ask the others one by one when the work needs them.

## SOL-05. SDK pin (global.json)

- Question: Do we pin the .NET SDK in `global.json`, and with which roll-forward rule?
- Ask when: the first commit of a repository.
- Options: Pin the feature band: `rollForward: latestFeature`, `allowPrerelease: false`. Pin the exact patch: `rollForward: disable`. No `global.json`: each machine uses its newest SDK.
- Recommended: `10.0.402` with `latestFeature` and `allowPrerelease: false`. CI and each developer then use the same compiler, and patches and newer feature bands still roll forward.
- Choose another option when: an audit needs builds that are the same byte for byte (pin the exact patch).
- Depends on: none.
- Revisit when: a new SDK feature band ships, or the team moves to .NET 11.
- Check: judge.
- Detect: CS-DET-SOL-05.

## SOL-01. Solution shape

- Question: Do we start with one project with feature folders, or with a layered solution (Clean Architecture: Domain, Application, Infrastructure, Api)?
- Ask when: the first `dotnet new` for a new service.
- Options: Single project, vertical slices: each feature folder holds the endpoint, request, response, handler, and validator. Clean Architecture: four projects, dependencies point inward. N-tier: Web, Business, Data. Modular monolith: one host, one project per business module, modules talk only through public contracts. Hybrid: Clean Architecture projects with feature folders inside Application.
- Recommended: Single project with feature folders. It is the shape of the `dotnet new` template and has the smallest cost per change. A folder can move to its own project later without a rewrite.
- Choose another option when: many use cases share many domain rules (Clean Architecture or hybrid). More than one team works in the same deployable unit (modular monolith).
- Depends on: none.
- Revisit when: a second team joins, the project has about 150 to 200 files, or domain rules repeat across slices.
- Check: judge.
- Detect: CS-DET-SOL-01.

## SOL-03. Shared build policy (Directory.Build.props)

- Question: Do we put the shared MSBuild properties in one `Directory.Build.props` at the repository root?
- Ask when: the second project in the repository (a test project counts).
- Options: `Directory.Build.props` with the shared properties, project files keep only what is different. The same properties in each `.csproj`.
- Recommended: `Directory.Build.props` with `TargetFramework net10.0`, `Nullable enable`, `ImplicitUsings enable`, `AnalysisLevel latest-recommended`, `EnforceCodeStyleInBuild true`. No `LangVersion`. One policy file prevents drift between projects.
- Choose another option when: the repository has one project only.
- Depends on: none.
- Revisit when: .NET 11 is released. Decide if `TargetFramework` moves. .NET 11 is STS.
- Check: tool (CS-PROPS-NULLABLE, CS-LANGVERSION).
- Detect: CS-DET-SOL-03.

## SOL-06. Warnings policy

- Question: Do warnings stop the build: all warnings, only nullable warnings, or only in CI?
- Ask when: the first CI pipeline, or together with SOL-03.
- Options: `TreatWarningsAsErrors` everywhere. Nullable warnings as errors always, all warnings as errors only in CI. Warnings stay warnings.
- Recommended: Nullable warnings as errors always (`WarningsAsErrors` with `nullable`). All warnings as errors in CI. NuGet audit warnings (NU1901 to NU1904) stay errors. Null bugs then stop each build, and the local loop stays fast.
- Choose another option when: an existing repository has many warnings (report first, then make the rules stricter in small steps).
- Depends on: SOL-03.
- Revisit when: the local warning count is zero (then turn on errors everywhere).
- Check: tool (`dotnet build -warnaserror`).
- Detect: CS-DET-SOL-06.

## SOL-07. Code style enforcement (.editorconfig)

- Question: Do we use the `dotnet new editorconfig` defaults (file-scoped namespaces, explicit types) and enforce the style in the build?
- Ask when: the first commit.
- Options: Template defaults, with the key rules at `warning` and `EnforceCodeStyleInBuild true`. A team-specific file (for example `var` everywhere). No `.editorconfig`.
- Recommended: The template `.editorconfig`. Set IDE0161 (file-scoped namespace), IDE1006 (naming), and IDE0005 (unused usings, needs `GenerateDocumentationFile`) to `warning`. Keep `var` at `silent`. Only rules at `warning` or `error` reach the build log.
- Choose another option when: the organization already has a shared `.editorconfig`.
- Depends on: SOL-03, SOL-08.
- Revisit when: never.
- Check: tool (`dotnet format --verify-no-changes`). The severities belong to `dev-stack:dotnet-analyzers`.
- Detect: CS-DET-SOL-07.

## SOL-08. Naming conventions

- Question: Do we use the Microsoft conventions: `_camelCase` private fields, `s_camelCase` private static fields, `Async` suffix on awaitable methods?
- Ask when: the first class with a private field or an async method.
- Options: Microsoft style: `_camelCase`, `s_camelCase`, `Async` suffix. `_camelCase` for all private fields, `Async` suffix. No prefix (`this.field`), no `Async` suffix in app code.
- Recommended: Microsoft style, with PascalCase constants and the `Async` suffix on all awaitable methods, also endpoint handlers. One rule for all methods is easier to check. Primary constructor parameters stay camelCase.
- Choose another option when: the codebase or the organization already uses another style.
- Depends on: none.
- Revisit when: never.
- Check: tool (IDE1006 with the naming rule).
- Detect: CS-DET-SOL-08.

## SOL-04. Central Package Management

- Question: Do we manage all NuGet versions in one `Directory.Packages.props` (Central Package Management)?
- Ask when: the first package added to a second project.
- Options: CPM with `ManagePackageVersionsCentrally`, optional transitive pinning. Versions in each `.csproj`.
- Recommended: CPM on, `CentralPackageTransitivePinningEnabled` true in apps. One version per package in the repository, and a vulnerable transitive package can be pinned in one place.
- Choose another option when: a library repository where transitive pinning adds dependencies to the nuspec (keep CPM, turn pinning off).
- Depends on: SOL-03.
- Revisit when: never.
- Check: tool (NU1008, NU1011, CS-FLOATING-VERSION).
- Detect: CS-DET-SOL-04.

## SOL-02. Folder organization

- Question: Do we group files by feature (`Features/Orders/CreateOrder.cs`) or by type (`Controllers/`, `Services/`, `Models/`)?
- Ask when: the second feature in a project.
- Options: By feature: one folder per business capability. By type: one folder per technical kind. By feature, then by use case: `Features/Orders/Create/`.
- Recommended: By feature. A change to one use case stays in one folder. Type folders spread one change over many folders.
- Choose another option when: the project is very small (fewer than about 10 files), or the team works in an existing codebase with type folders.
- Depends on: SOL-01.
- Revisit when: never for new code. It follows SOL-01.
- Check: judge.
- Detect: CS-DET-SOL-02-FEATURE, CS-DET-SOL-02-TYPE.

## SOL-09. Interfaces for services

- Question: Do we add an interface for each service, or only where a second implementation or a test seam needs one?
- Ask when: the first service class registered in DI.
- Options: An interface for each service (`IOrderService` and `OrderService`). An interface only at a seam: external systems (HTTP, mail, file storage) or more than one implementation. No interfaces: test through the HTTP boundary.
- Recommended: An interface only at a seam. `TimeProvider` and `HttpMessageHandler` already give test seams. Each extra interface is one more file to change, with no gain.
- Choose another option when: SOL-01 is Clean Architecture (Application defines the interfaces that Infrastructure implements).
- Depends on: SOL-01.
- Revisit when: unit tests need many hand-written fakes for one class.
- Check: judge.
- Detect: CS-DET-SOL-09.

## CFG-04. Time source

- Question: Do we inject `TimeProvider` for each read of the current time, each delay, and each timer?
- Ask when: the first use of the current time or of a delay.
- Options: `TimeProvider` (register `TimeProvider.System`, use `FakeTimeProvider` in tests). `DateTime.UtcNow` or `DateTimeOffset.UtcNow` directly. An own `IClock` interface.
- Recommended: `TimeProvider`, with instants as UTC `DateTimeOffset`. It is in the BCL since .NET 8, `Task.Delay`, `PeriodicTimer`, and `CancellationTokenSource` accept it, and tests control it with `FakeTimeProvider`.
- Choose another option when: none for new code.
- Depends on: none.
- Revisit when: never.
- Check: tool (CS-CLOCK, CS-THREAD-SLEEP). For a build error, `dev-stack:dotnet-analyzers` adds RS0030 with `BannedSymbols.txt`.
- Detect: CS-DET-CFG-04.
