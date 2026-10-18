# Review checklist

Mark each check `tool` (a command or a rule ID gives the answer) or `judge` (a person or the agent decides).

## Build and format

| # | Check | How |
|---|---|---|
| 1 | The changed projects build with 0 warnings under `-warnaserror --no-incremental` | tool (build) |
| 2 | No nullable warning (CS86xx), and nullable is not turned off in a project, a file, or `.editorconfig` | tool (CS8602, ANA-NULLABLE-OFF, ANA-EDITORCONFIG-NULLABLE-OFF) |
| 3 | `dotnet format --verify-no-changes` exits 0 | tool (dotnet format) |
| 4 | The tests pass after each automatic fix | tool (`dotnet test`) |
| 5 | An automatic fix did not change a public signature without need (VSTHRD100 fix) | judge |

## Suppressions and configuration

| # | Check | How |
|---|---|---|
| 6 | No new `NoWarn` entry for a CA, IDE, RCS, MA, VSTHRD, or RS rule | tool (ANA-NOWARN) |
| 7 | Each new `#pragma warning disable` has a reason on the same line | tool (ANA-PRAGMA-REASON) |
| 8 | Each `#pragma warning disable` has a `restore` and covers the fewest lines | judge (`sarif.py --suppressed`) |
| 9 | Each new `SuppressMessage` has a `Justification` | tool (ANA-SUPPRESS-JUSTIFICATION) |
| 10 | The reason of each suppression is true and the finding is a real false positive | judge |
| 11 | No severity lowered in `.editorconfig` without a team decision | tool (`git diff -- .editorconfig`), judge |
| 12 | No analyzer package removed, `RunAnalyzers` not set to false | tool (ANA-RUNANALYZERS-OFF, `git diff -- '*.props' '*.csproj'`) |
| 13 | No `category-roslynator` bulk severity | tool (ANA-ROSLYNATOR-CATEGORY) |
| 14 | No `latest-all` or `All` analysis mode as the default | tool (ANA-ANALYSIS-ALL) |
| 15 | No rule set file | tool (ANA-RULESET) |
| 16 | `ErrorLog` is not passed with `$(MSBuildProjectName)` on the command line | judge |

## Packages

| # | Check | How |
|---|---|---|
| 17 | No SonarAnalyzer.CSharp | tool (ANA-PKG-SONAR) |
| 18 | No SecurityCodeScan package | tool (ANA-PKG-SECURITYCODESCAN) |
| 19 | No StyleCop.Analyzers without a team decision | tool (ANA-PKG-STYLECOP) |
| 20 | No NetAnalyzers or FxCopAnalyzers package | tool (ANA-PKG-NETANALYZERS, ANA-PKG-FXCOP) |
| 21 | A new analyzer package has the approval of the human | judge |

## Code rules that the build checks

| # | Check | How |
|---|---|---|
| 22 | No banned API (`DateTime.Now`, `UtcNow`, `Thread.Sleep`, others in `BannedSymbols.txt`) | tool (RS0030) |
| 23 | No `async void`, no unobserved task | tool (VSTHRD100, VSTHRD110) |
| 24 | No sync over async, no blocking call in async code | tool (VSTHRD002, VSTHRD103) |
| 25 | Each call gets the `CancellationToken` | tool (CA2016) |
| 26 | Log calls use a constant template | tool (CA2254) |
| 27 | Culture and string comparison are explicit | tool (CA1304, CA1305, CA1311, CA1862, RCS1155) |
| 28 | No empty `catch (Exception)`, no `throw ex;` | tool (RCS1075, CA2200) |
| 29 | Argument checks use throw helpers | tool (CA1510 to CA1513) |
| 30 | No unused usings, members, parameters, or assignments | tool (IDE0005, IDE0051, IDE0052, IDE0059, IDE0060) |
| 31 | Names follow the naming rules | tool (IDE1006) |
| 32 | No unused private code left | tool (`roslynator find-symbol --unused --visibility private`) |
| 33 | Test methods are valid and not skipped without notice | tool (MSTEST0003, MSTEST0030) |
| 34 | A rule that fired is real, not a false positive that needs a tuned config | judge |
