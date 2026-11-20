# Review checklist

Each check is `tool (<rule ID>)` when a tool finds it, or `judge` when a reviewer must decide. Run the tools first (`SKILL.md`, section Check). In a review, cite the rule number of `SKILL.md` or the rule ID.

## Contents

- Version gate
- Versions and packages
- Runtime and host
- ASP.NET Core and EF Core
- C# 15
- Severity
- Do not flag

## Version gate

| # | Check | Type |
|---|-------|------|
| 1 | The human answered NET-01. The reason is a feature, not the support time. | judge |
| 2 | Each blocker of the table in "First" has an answer (EFCore.PG 11, `Asp.Versioning.OpenApi`). | judge |
| 3 | No project stays on `net8.0` or `net9.0` without a note to the human. | tool (N11-OLD-TFM) |

## Versions and packages

| # | Check | Type |
|---|-------|------|
| 4 | `global.json` names a GA SDK 11.0.100 or later, with `rollForward` `latestFeature`. | tool (N11-PREVIEW-SDK), judge |
| 5 | The target framework, the Microsoft packages, the `dotnet-ef` tool, the base images, and the CI SDK all moved in the same change. | judge |
| 6 | No `EnablePreviewFeatures`, no `LangVersion`. | tool (N11-PREVIEW-FLAGS) |
| 7 | No reference to the 9 shared framework libraries in a project with only `net11.0`. | tool (NU1510) |
| 8 | No new package without the approval of the human (stack rule 5). | judge |

## Runtime and host

| # | Check | Type |
|---|-------|------|
| 9 | The report names the CPU check (`x86-64-v2`) of each host and build agent. | judge |
| 10 | Runtime async is on only with decision NET-02, and without a preview flag or `DOTNET_RuntimeAsync`. | tool (N11-PREVIEW-FLAGS), judge |
| 11 | No test expects `IHost.RunAsync` to return after a service failure, or `TryParse` to return `false` for a bad style. | judge |
| 12 | No empty configuration value means "not set". | judge |

## ASP.NET Core and EF Core

| # | Check | Type |
|---|-------|------|
| 13 | No `UseConcurrencyLimiter` or `services.AddConcurrencyLimiter`. | tool (N11-CONCURRENCY-LIMITER, CS1061) |
| 14 | OpenAPI transformers use `Microsoft.OpenApi` 3.x, and the document version is a decision. | judge (`dev-stack:aspnetcore-openapi`) |
| 15 | EF Core 11 projects reference `Microsoft.EntityFrameworkCore.Design` with `PrivateAssets="all"`. | judge |
| 16 | No `EFOptimizeContext`. | tool (N11-EF-OPTIMIZE) |
| 17 | SQL Server projects set `UseCompatibilityLevel` when the server is older than 2022. | judge |
| 18 | `dotnet ef migrations has-pending-model-changes` reports no changes. | tool |

## C# 15

| # | Check | Type |
|---|-------|------|
| 19 | CS8509 is an error in `Directory.Build.props`. | judge, tool (build) |
| 20 | No `switch` over a union or a closed type has a `_ =>` arm. | judge |
| 21 | The upgrade commit does not rewrite working code to C# 15 features. | judge |

## Severity

- Blocking: a preview SDK in `global.json`, a removed API that breaks the build, or a host CPU without `x86-64-v2`. Also a move without the answer to NET-01, or an RC provider package without a decision.
- Major: a partial move (rule 1), no Design package, a `_ =>` arm over a union, no CPU check in the report.
- Minor: a NU1510 reference that stays, a style point in the project files.

## Do not flag

- A project that stays on `net10.0` on purpose. .NET 10 is LTS.
- An abstract record hierarchy in existing code. The upgrade commit does not rewrite it.
- `<TargetFrameworks>net10.0;net11.0</TargetFrameworks>` in a library package (NET-03).
