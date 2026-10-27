# Sources

All sources checked on 2026-10-16. Microsoft Learn pages were read with the Microsoft Learn MCP tools.

## Versions and language

- .NET support policy (.NET 10 LTS until 2028-11-14): https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core
- Release metadata for .NET 10 and 11: https://builds.dotnet.microsoft.com/dotnet/release-metadata/10.0/releases.json, https://builds.dotnet.microsoft.com/dotnet/release-metadata/11.0/releases.json
- New features in C# 14: https://learn.microsoft.com/dotnet/csharp/whats-new/csharp-14
- C# language version history (C# 13 `params` collections): https://learn.microsoft.com/dotnet/csharp/whats-new/csharp-version-history
- C# language versioning (no `LangVersion`): https://learn.microsoft.com/dotnet/csharp/language-reference/configure-language-version
- Roslyn breaking changes in .NET 10: https://learn.microsoft.com/dotnet/csharp/whats-new/breaking-changes/compiler%20breaking%20changes%20-%20dotnet%2010
- C# 14 span overload resolution: https://learn.microsoft.com/dotnet/core/compatibility/core-libraries/10.0/csharp-overload-resolution
- Primary constructor warnings (CS9124): https://learn.microsoft.com/dotnet/csharp/language-reference/compiler-messages/constructor-errors
- Breaking changes in .NET 10: https://learn.microsoft.com/dotnet/core/compatibility/10

## .NET 11 and C# 15

- New features in .NET 11 (RC 3): https://learn.microsoft.com/dotnet/core/whats-new/dotnet-11/overview
- New features in the .NET 11 runtime (runtime async): https://learn.microsoft.com/dotnet/core/whats-new/dotnet-11/runtime
- New features in C# 15: https://learn.microsoft.com/dotnet/csharp/whats-new/csharp-15
- Union types reference: https://learn.microsoft.com/dotnet/csharp/language-reference/builtin-types/union
- Roslyn breaking changes in .NET 11: https://learn.microsoft.com/dotnet/csharp/whats-new/breaking-changes/compiler%20breaking%20changes%20-%20dotnet%2011
- Breaking changes in .NET 11: https://learn.microsoft.com/dotnet/core/compatibility/11
- DateOnly and TimeOnly TryParse change: https://learn.microsoft.com/dotnet/core/compatibility/core-libraries/11/dateonly-timeonly-tryparse-argumentexception

## Async and time

- Async guidance (David Fowler): https://github.com/davidfowl/AspNetCoreDiagnosticScenarios/blob/master/AsyncGuidance.md
- Task-based asynchronous pattern, naming: https://learn.microsoft.com/dotnet/standard/asynchronous-programming-patterns/task-based-asynchronous-pattern-tap
- Async streams and `[EnumeratorCancellation]`: https://learn.microsoft.com/dotnet/csharp/asynchronous-programming/generate-consume-asynchronous-stream
- CA2016 (pass the token): https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca2016
- TimeProvider overview: https://learn.microsoft.com/dotnet/standard/datetime/timeprovider-overview
- Testing with FakeTimeProvider: https://learn.microsoft.com/dotnet/core/extensions/timeprovider-testing
- ISystemClock obsolete: https://learn.microsoft.com/aspnet/core/breaking-changes/8/isystemclock-obsolete
- Microsoft.Extensions.TimeProvider.Testing 10.11.0: https://www.nuget.org/packages/Microsoft.Extensions.TimeProvider.Testing

## Project conventions

- MSBuild properties: https://learn.microsoft.com/dotnet/core/project-sdk/msbuild-props
- Central Package Management: https://learn.microsoft.com/nuget/consume-packages/central-package-management
- global.json: https://learn.microsoft.com/dotnet/core/tools/global-json
- Code analysis overview (EnforceCodeStyleInBuild): https://learn.microsoft.com/dotnet/fundamentals/code-analysis/overview
- IDE0005 needs GenerateDocumentationFile: https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/ide0005
- Identifier naming conventions: https://learn.microsoft.com/dotnet/csharp/fundamentals/coding-style/identifier-names
- Compiler options for warnings (`WarningsAsErrors nullable`): https://learn.microsoft.com/dotnet/csharp/language-reference/compiler-options/errors-warnings
- System.Linq.Async 7.0.1 nuspec (no net10.0 dependency on AsyncEnumerable): https://api.nuget.org/v3-flatcontainer/system.linq.async/7.0.0/system.linq.async.nuspec
- MediatR and AutoMapper license change (2025-07-02): https://www.jimmybogard.com/automapper-and-mediatr-commercial-editions-launch-today/

## Team research (internal)

- `csharp-aspnetcore.md`, `defaults-dotnet-api.md`, `analyzers.md`, `dotnet-testing.md` (dev-team research, 2026-10-16).
- Earlier user skills `csharp` and `dotnet-modern`, merged into this skill. Fixed: `params` collections are C# 13, not C# 14.

## Verification

- All C# examples compile with SDK 10.0.402, `AnalysisLevel latest-recommended`, and `-warnaserror`. The `FakeTimeProvider` tests pass with MSTest 4.5.1.
- The examples of `net11.md` compile with SDK 11.0.100-rc.2.26504.105.
- 2026-10-27: versions checked again against the .NET release metadata (10.0.14, SDK 10.0.403, 11.0.0-rc.3) and the C# 15 page. The examples were not compiled again.

## Credits

- dotnet/skills (MIT), https://github.com/dotnet/skills. I read the C# guidance of the `dotnet` plugin through DeepWiki. Paraphrased rules: await a `ValueTask` one time only, seal leaf classes, use `TryGetValue`. Also: `#nullable disable warnings` keeps the annotations.
- Meziantou.Analyzer `AGENTS.md` (MIT), https://github.com/meziantou/Meziantou.Analyzer: "trust the nullable annotations, add no extra null checks" and "use `is null`", paraphrased.
- AspNetCoreDiagnosticScenarios (MIT), https://github.com/davidfowl/AspNetCoreDiagnosticScenarios: async rules for blocking, `async void`, `TaskCompletionSource`, and `CancellationTokenSource` disposal, paraphrased.
