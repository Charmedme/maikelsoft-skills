# Sources

Checked on 2026-11-19. The facts of this skill come from these primary sources. Look them up again when `metadata.verified` in `SKILL.md` is older than 6 months.

## Contents

- Versions
- Documentation
- Checked by compile
- Old guidance that agents read
- Credits

## Versions

| Item | Version on 2026-11-19 | Source |
|------|-----------------------|--------|
| .NET 11 runtime and ASP.NET Core 11 | 11.0.0 (2026-11-10), STS, end of support 2028-11-14 | [.NET 11 release metadata](https://dotnetcli.blob.core.windows.net/dotnet/release-metadata/11.0/releases.json) |
| .NET 11 SDK | 11.0.100, C# 15 | Same source |
| .NET 10 | 10.0.15 and SDK 10.0.404 (2026-11-10, security release), LTS, end of support 2028-11-14 | [.NET 10 release metadata](https://dotnetcli.blob.core.windows.net/dotnet/release-metadata/10.0/releases.json) |
| EF Core | 11.0.0 | [nuget.org](https://www.nuget.org/packages/Microsoft.EntityFrameworkCore) |
| Npgsql.EntityFrameworkCore.PostgreSQL | 11.0.0-rc.3 (2026-10-28), no GA | [nuget.org](https://www.nuget.org/packages/Npgsql.EntityFrameworkCore.PostgreSQL) |
| Microsoft.AspNetCore.OpenApi 11.0.0 | Depends on `Microsoft.OpenApi` `[3.11.0, 4.0.0)` | [nuget.org](https://www.nuget.org/packages/Microsoft.AspNetCore.OpenApi) |
| Asp.Versioning.OpenApi | 10.2.3 (2026-08-22), `net10.0` only, `Microsoft.OpenApi` `[2.7.5, 3.0.0)` | [nuget.org](https://www.nuget.org/packages/Asp.Versioning.OpenApi) |
| Microsoft.Orleans.* | 10.4.2, `net8.0` and `net10.0` assets | [Orleans releases](https://github.com/dotnet/orleans/releases) |
| Aspire | 17.0.0, minimum .NET 10 | [What's new in Aspire 17](https://aspire.dev/whats-new/aspire-17/) |

## Documentation

- [What's new in .NET 11](https://learn.microsoft.com/dotnet/core/whats-new/dotnet-11/overview): the GA overview.
- [.NET 11 runtime: runtime async](https://learn.microsoft.com/dotnet/core/whats-new/dotnet-11/runtime#runtime-async): `<Features>runtime-async=on</Features>`, `UseRuntimeAsync`, the removed `DOTNET_RuntimeAsync`.
- [Breaking changes in .NET 11](https://learn.microsoft.com/dotnet/core/compatibility/11): runtime, libraries, host, configuration.
- [Minimum hardware requirements](https://learn.microsoft.com/dotnet/core/compatibility/jit/11/minimum-hardware-requirements): `x86-64-v2`.
- [Breaking changes in ASP.NET Core 11](https://learn.microsoft.com/aspnet/core/breaking-changes/11/overview): the concurrency limiter, OpenAPI, MVC, Kestrel, telemetry.
- [What's new in EF Core 11](https://learn.microsoft.com/ef/core/what-is-new/ef-core-11.0/whatsnew): EF Core 11 needs .NET 11.
- [C# compiler breaking changes since C# 14](https://learn.microsoft.com/dotnet/csharp/whats-new/breaking-changes/compiler%20breaking%20changes%20-%20dotnet%2011): `union`, `closed`, `with`.
- [.NET Upgrade Assistant overview](https://learn.microsoft.com/dotnet/core/porting/upgrade-assistant-overview): the deprecation.

## Checked by compile

On 2026-11-19 with the .NET SDK 11.0.100, `net11.0`, one build at a time:

- `<Features>runtime-async=on</Features>` builds without `EnablePreviewFeatures`, with 0 warnings. Reflection shows `MethodImplAttributes.Async` on `Main`.
- `app.UseConcurrencyLimiter()` gives `error CS1061`. `AddConcurrencyLimiter` on `IServiceCollection` gives CS1061 too.
- `AddRateLimiter` with `RateLimitPartition.GetConcurrencyLimiter` and `UseRateLimiter` builds with 0 warnings.
- A `union` with a missing case in a `switch` gives `warning CS8509`, not an error. A `_` arm hides the missing case with no warning.
- Orleans 10.4.2 builds, runs, and passes an `InProcessTestCluster` test on `net11.0`.
- An Aspire 17.0.0 AppHost on `Microsoft.NET.Sdk` builds on `net11.0`.
- `ModelContextProtocol` 2.4.0 builds and runs on `net11.0`.

Not checked by compile: EF Core 10 packages in a `net11.0` project, and the CPU check command on a host without `x86-64-v2`.

## Old guidance that agents read

- [migrate-dotnet10-to-dotnet11](https://github.com/dotnet/skills/blob/main/plugins/dotnet-upgrade/skills/migrate-dotnet10-to-dotnet11/SKILL.md) (Microsoft agent skill): last change 2026-05-11. It says that .NET 11 is in preview and pins `11.0.100-preview.3`. Its list of areas is useful: the EF Core Design package, `EFOptimizeContext`, `SqlVector<T>`, SYSLIB0063, and the ZIP CRC32 check came from it.
- [dotnet-upgrade.instructions.md](https://github.com/github/awesome-copilot/blob/main/instructions/dotnet-upgrade.instructions.md) (awesome-copilot): it installs the deprecated Upgrade Assistant and targets "the latest LTS".

## Credits

The upgrade checklist follows the area list of the Microsoft skill `migrate-dotnet10-to-dotnet11`, with the versions of the GA release.
