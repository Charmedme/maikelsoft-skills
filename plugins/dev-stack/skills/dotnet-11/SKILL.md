---
name: dotnet-11
description: Move of a .NET 10 project or solution to .NET 11 (net11.0, C# 15, ASP.NET Core 11, EF Core 11). Covers the version gate, global.json, the target framework and the Microsoft packages in one change, the CPU baseline, the breaking changes for app code, runtime async, and the upgrade checklist with its build and test commands. Use when the user wants to upgrade, migrate, or move a project to .NET 11 or net11.0, asks if the team should move to .NET 11, changes global.json to an 11.0 SDK, or gets a build error after the move (UseConcurrencyLimiter, NU1510, EFOptimizeContext, no EF Core Design package). Not for C# 15 syntax such as union or closed types (dev-stack:csharp), EF Core queries or mapping (dev-stack:efcore), a new project on .NET 10, or a move from .NET Framework.
metadata:
  verified: "2026-11-19"
  versions: ".NET 11.0.0 (SDK 11.0.100, C# 15, STS), ASP.NET Core 11.0.0, EF Core 11.0.0, .NET 10.0.15 (SDK 10.0.404, LTS)"
---

# Move to .NET 11

This skill owns the move of a project from `net10.0` to `net11.0`. The C# 15 language rules are in `dev-stack:csharp` (`references/net11.md`), and this skill does not copy them. The framework rules on .NET 11 are in `dev-stack:aspnetcore`, `dev-stack:aspnetcore-openapi`, and `dev-stack:efcore`. This skill lists only what changes at the move.

## First

1. Read `references/stack-rules.md`.
2. Read the versions in `global.json`, `*.csproj`, `Directory.Build.props`, and `Directory.Packages.props`. Also read the base images in each `Dockerfile` and the SDK in the CI files.
   - `net10.0`: use this file.
   - `net8.0` or `net9.0`: both are out of support since 2026-11-10. Tell the human. Recommend `net10.0` (LTS) first. A move from 8 or 9 to 11 also gets the .NET 10 breaking changes (`dev-stack:csharp`, `dev-stack:aspnetcore`).
   - `net11.0` already: use the sections Check and Review checklist.
3. Find the answer to the version gate (NET-01). A request of the human to move to .NET 11 is the answer. Do the move, and write the reason in the report. Ask NET-01 only when the move is your own idea, for example to use a C# 15 feature. The support of .NET 10 (LTS) and .NET 11 (STS) ends on the same day, 2028-11-14. So a move gives features, not a longer support time. When you ask and the task names no .NET 11 feature, recommend to stay on .NET 10.
4. Compare the packages of the project with the blockers in the next table.

Done when you can name the current target, the SDK, each package with a blocker, and the answer of the human to NET-01.

| Package or item | State on 2026-11-19 | Effect on the move |
|-----------------|---------------------|--------------------|
| `Npgsql.EntityFrameworkCore.PostgreSQL` | 11.0.0-rc.3 is the latest. No GA. | EF Core 11 needs a provider of the same major. Keep EF Core 10 and EFCore.PG 10, or wait. An RC needs a decision of the human. |
| `Asp.Versioning.OpenApi` 10.2.3 | Allows `Microsoft.OpenApi` below 3.0 only | `Microsoft.AspNetCore.OpenApi` 11 needs `Microsoft.OpenApi` 3.11 or later. A versioned API with an OpenAPI document stays on .NET 10. |
| Orleans 10.4.2 | No `net11.0` asset. A silo on `net11.0` builds, runs, and passes a test cluster test. | No blocker. |
| Aspire 17 | Minimum .NET 10. `Aspire.Hosting.AppHost` has `net11.0` assets. | No blocker. |
| `ModelContextProtocol` 2.4.0 | Builds and runs on `net11.0` | No blocker. |
| x86 and x64 hosts | The runtime needs an `x86-64-v2` CPU | The app does not start on an older CPU (rule 6). |

## Defaults for new code

Ask the human before you apply a default that has no accepted decision (stack rule 4). The full entries are in `references/decisions.md`.

| Area | Default | ID |
|------|---------|----|
| Version gate | Stay on .NET 10 (LTS). Move to .NET 11 only for a named feature. | NET-01 |
| Runtime async in own assemblies | Off. The .NET libraries and ASP.NET Core already use it. | NET-02 |
| Library packages | Multi-target `net10.0;net11.0` for a package that other apps use | NET-03 |

## Existing code

At the move, the defects of stack rule 3 include these. Each one builds on one machine, or hides until production.

| Defect | Rule |
|--------|------|
| `global.json` with a preview or RC SDK of .NET 11 | 2 |
| `<EnablePreviewFeatures>` or `<LangVersion>preview` from a test with a preview SDK | 5, 7 |
| `app.UseConcurrencyLimiter()` | 9 |
| An empty string in `appsettings.Production.json` or in an environment variable that means "not set" | 8 |
| A `switch` with a `_ =>` arm over a closed set of cases | 12 |

## Rules

### Versions in one change

1. Change all versions in one commit:
   - `global.json` and the target framework.
   - Each `Microsoft.AspNetCore.*`, `Microsoft.Extensions.*`, and `Microsoft.EntityFrameworkCore*` package (11.0.0).
   - The `dotnet-ef` tool in `.config/dotnet-tools.json`.
   - The base images (`mcr.microsoft.com/dotnet/aspnet:11.0`, `sdk:11.0`) and the SDK in CI (`11.0.x`).

   A target framework change alone keeps EF Core 10 and the old SDK pin. The build then passes on one machine and fails in CI.

   ```json
   {
     "sdk": {
       "version": "11.0.100",
       "rollForward": "latestFeature",
       "allowPrerelease": false
     }
   }
   ```

   ```xml
   <!-- Directory.Build.props -->
   <TargetFramework>net11.0</TargetFramework>
   <WarningsAsErrors>$(WarningsAsErrors);CS8509</WarningsAsErrors>
   ```

2. Use the GA SDK 11.0.100 or later. Do not trust a source that says that .NET 11 is in preview. The Microsoft agent skill `migrate-dotnet10-to-dotnet11` (github.com/dotnet/skills) still says "preview" and pins `11.0.100-preview.3`. Use its list of areas only, not its versions. Do not install .NET Upgrade Assistant, because Microsoft deprecated it. Edit the files yourself.
3. Some `Microsoft.Extensions.*` libraries are now in the shared framework. When the project targets only `net11.0`, remove their `PackageReference` (NU1510). Keep it in a project with `net10.0;net11.0`. The list of the 9 libraries is in `references/breaking-changes.md`.
4. EF Core 11: add `Microsoft.EntityFrameworkCore.Design` directly, with `PrivateAssets="all"`. The Tools package no longer brings it, and `dotnet ef` fails without it. Replace `<EFOptimizeContext>` with `<EFScaffoldModelStage>` and `<EFPrecompileQueriesStage>`, because the build ignores the old property. The other EF Core 11 changes are in `dev-stack:efcore` (`references/ef11.md`).
5. Do not set `LangVersion`. The target framework selects C# 15.

### Runtime and host

6. Check the CPU of each host, VM, and build agent. The x86 and x64 runtime of .NET 11 needs `x86-64-v2` (SSE4.2 and POPCNT). On an older CPU the runtime does not start. On Linux, `/lib64/ld-linux-x86-64.so.2 --help | grep x86-64-v2` shows `supported`. Put the result of each host in the report.
7. Runtime async in your own assemblies needs decision NET-02. To turn it on, set `<Features>runtime-async=on</Features>` without `EnablePreviewFeatures`. A build on SDK 11.0.100 proved that the preview flag is not necessary. .NET 11 removes the variable `DOTNET_RuntimeAsync`. To turn it off, set `<UseRuntimeAsync>false</UseRuntimeAsync>`.
8. Check the 3 host changes. Each one changes the behavior without a build error:
   - `IHost.RunAsync` and `StopAsync` throw when a `BackgroundService` fails. The process exits with a code that is not 0. Keep the catch per iteration (`dev-stack:aspnetcore`, rule 35). Change a test that expects a normal return.
   - A DI scope collects the exceptions of each service that fails in its disposal. Change code that catches only one exception type from a scope disposal.
   - An empty string in a later configuration provider now replaces the value of an earlier provider. Remove each empty entry that means "not set". Validate the options with `ValidateOnStart`.

### ASP.NET Core 11

9. Replace `app.UseConcurrencyLimiter()` and `services.AddConcurrencyLimiter(...)` with the rate limiter. ASP.NET Core 11 removed the middleware, so the build gives CS1061. It is not only "deprecated". This form compiled on SDK 11.0.100:

   ```csharp
   builder.Services.AddRateLimiter(options =>
   {
       options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
       options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(_ =>
           RateLimitPartition.GetConcurrencyLimiter("all", _ => new ConcurrencyLimiterOptions
           {
               PermitLimit = 100,
               QueueLimit = 50,
           }));
   });

   app.UseRateLimiter();
   ```

10. OpenAPI: `Microsoft.AspNetCore.OpenApi` 11 uses `Microsoft.OpenApi` 3.x, and the default document version is OpenAPI 3.2. Transformers and snapshot tests change. The rules are in `dev-stack:aspnetcore-openapi`.
11. Read `references/breaking-changes.md` for the other changes. Examples are the removed MVC compatibility options, the stricter Kestrel, and the OpenTelemetry tags. Others are the SqlClient authentication package and the Blazor template without `UseAntiforgery()`.

### C# 15 after the move

12. For a closed set of results, new code uses a `union`. Its `switch` names each case and has no `_ =>` arm. A missing case is warning CS8509, not an error, and a `_ =>` arm hides it. So rule 1 makes CS8509 an error. The syntax is in `dev-stack:csharp` (`references/net11.md`). Do not rewrite a working hierarchy in the upgrade commit.
13. Rename an identifier named `union` or `closed` to `@union` or `@closed`. Write `@with(...)` for a call to a method named `with` in a collection expression. The build shows each case.

## Upgrade steps

Do these steps only when NET-01 has the answer "move": the request of the human, or the reply to your question.

1. To check the SDK, run `dotnet --list-sdks`. Done when the list has 11.0.100 or later.
2. Change the files of rule 1. Apply rules 3 to 5. Done when each `net10.0` that stays is on purpose. This command lists them: `grep -rn "net10.0" --include=*.csproj --include=*.props .`
3. To restore the packages, run `dotnet restore`. Done when the output has no NU1510, NU1605, NU1608, or NU1107.
4. To find the traps, run `python3 <this skill folder>/scripts/stackcheck.py --root <repo>`. Done when the summary shows 0 errors.
5. To build, run `dotnet build -warnaserror --no-incremental`. Done when the build shows 0 warnings and 0 errors.
6. Optional: for each project with EF Core migrations, run `dotnet ef migrations has-pending-model-changes`. Done when it reports no changes, or when you added and read a migration for the snapshot change.
7. To run the tests, run `dotnet test`. Done when the summary shows 0 failed.
8. Start the host one time with the production configuration. Check the CPU of each host (rule 6). Done when the host starts and each host supports `x86-64-v2`.

## Never write

| Wrong | Correct |
|-------|---------|
| `"version": "11.0.100-preview.3"` in `global.json` | `"version": "11.0.100"` with `"rollForward": "latestFeature"` |
| `dotnet tool install -g upgrade-assistant` | Edit the project files and `global.json` yourself (rule 1) |
| `<TargetFramework>net11.0</TargetFramework>` with EF Core 10.0.x and SDK 10.0.404 in `global.json` | All versions of rule 1 in one change |
| `<EnablePreviewFeatures>true</EnablePreviewFeatures>` or `DOTNET_RuntimeAsync=1` for runtime async | `<Features>runtime-async=on</Features>` after decision NET-02 |
| `app.UseConcurrencyLimiter();` "if it still compiles" | `AddRateLimiter` with `GetConcurrencyLimiter`, and `UseRateLimiter()` |
| `Microsoft.EntityFrameworkCore.Tools` as the only EF tool package | Also `Microsoft.EntityFrameworkCore.Design` with `PrivateAssets="all"` |
| `<EFOptimizeContext>true</EFOptimizeContext>` | `<EFScaffoldModelStage>` and `<EFPrecompileQueriesStage>` |
| `net8.0` or `net9.0` for a new project | `net10.0`, or `net11.0` after NET-01 |
| An abstract record with `_ => throw new UnreachableException()` in new `net11.0` code | A `union` and a `switch` with each case, CS8509 as an error |
| "Move to .NET 11 for a longer support time" | .NET 10 and .NET 11 end support on the same day, 2028-11-14 |
| `Npgsql.EntityFrameworkCore.PostgreSQL` 11.0.0-rc.3 next to EF Core 11.0.0 without a decision | EF Core 10 and EFCore.PG 10 until EFCore.PG 11 is GA |
| `<LangVersion>15</LangVersion>` or `<LangVersion>preview</LangVersion>` | No `LangVersion` |

## Check

Run the tools after the last change. Fix each error. For each warning, fix it or give the reason.

1. To run the rules of this skill, run `python3 <this skill folder>/scripts/stackcheck.py --root <repo>`. Done when the summary shows 0 errors.
2. To build with the analyzers, run `dotnet build -warnaserror --no-incremental`. Done when the build shows 0 warnings and 0 errors. CS8509, CS1061 (removed APIs), and CS0618 show here.
3. To find vulnerable packages, run `dotnet package list --vulnerable --include-transitive`. Done when the list has no rows.
4. To run the tests, run `dotnet test`. Done when the summary shows 0 failed.

## Review checklist

The full list is in `references/review-checklist.md`.

1. The human answered NET-01, and the reason is a feature, not the support time. judge
2. Each version of rule 1 names .NET 11. tool (N11-PREVIEW-SDK, N11-OLD-TFM), judge
3. No preview flags and no `LangVersion`. tool (N11-PREVIEW-FLAGS)
4. No `UseConcurrencyLimiter`. tool (N11-CONCURRENCY-LIMITER, CS1061)
5. EF Core 11 has the Design package and no `EFOptimizeContext`. tool (N11-EF-OPTIMIZE), judge
6. Each blocker of the table in "First" has an answer. judge
7. CS8509 is an error, and no `switch` over a union or a closed type has a `_ =>` arm. tool (build), judge
8. The report names the CPU check of each host. judge

## Report

End with the report of stack rule 8 (`references/stack-rules.md`): the same headings in the same order on each run. Also give:

- Under "Decisions": NET-01 with the feature that is the reason, and NET-02 and NET-03 when the change uses them.
- Under "Packages": each package that moved to 11.0.0, each removed NU1510 reference, and each blocker with its answer.
- Under "Open points": each host whose CPU you could not check.

## References

- `references/breaking-changes.md`: read before step 2 of the upgrade, and when the build or a test fails after the move.
- `references/decisions.md`: read before you ask NET-01, NET-02, or NET-03.
- `references/review-checklist.md`: read when you review a move to .NET 11.
- `references/sources.md`: the primary sources, and the checks by compile on SDK 11.0.100.
- `references/stack-rules.md` and `references/language.md`: the common rules and the output language.
- `scripts/stackcheck.py`: the rule check of this skill.
