# Baseline files

## Contents

- Files and where they go
- Directory.Build.props
- Directory.Packages.props
- .editorconfig
- BannedSymbols.txt
- .gitattributes
- Test result
- How to test the baseline again

## Files and where they go

| File in `assets/` | Put it in | Merge rule |
|---|---|---|
| `Directory.Build.props` | Repository root | Keep the existing properties. Add the missing ones. Do not remove a stricter value. |
| `Directory.Packages.props` | Repository root | Copy the `GlobalPackageReference` items. Keep the existing `PackageVersion` items. |
| `.editorconfig` | Repository root | In an existing repo, keep its style rules. Add the severities one by one (`adoption.md`). |
| `BannedSymbols.txt` | Repository root, next to `Directory.Build.props` | Keep the existing entries. |

`TargetFramework` is not in the baseline. Each project file keeps its own target.

## Directory.Build.props

| Setting | Value | Reason |
|---|---|---|
| `Nullable` | `enable` | Null bugs become compiler warnings (CS86xx). |
| `AnalysisLevel` | `latest-recommended` | 145 CA rules as warnings. The SDK default (`latest`, minimum mode) has 92. `latest-all` has 280 and gives noise. Pin `10-recommended` when an SDK update must not add warnings. |
| `EnforceCodeStyleInBuild` | `true` | IDE rules with severity `warning` run in `dotnet build`. Without it, only the IDE shows them. |
| `GenerateDocumentationFile` | `true` | IDE0005 (unused using) runs on build only with a documentation file. |
| `NoWarn` | `$(NoWarn);CS1591` | Apps do not need XML docs on each public member (QUAL-06). Remove CS1591 from `NoWarn` in package projects. |
| `WarningsAsErrors` | `$(WarningsAsErrors);nullable` | The keyword `nullable` covers all nullable warnings. They stop each build, also on the local machine. |
| `TreatWarningsAsErrors` | `true` when `CI=true` | All warnings stop the CI build. GitHub Actions and Azure Pipelines set `CI=true`. |
| `MSTestAnalysisMode` | `Recommended` | MSTest Info rules become warnings, MSTEST0003 becomes an error. No condition: `IsTestProject` is not set yet when `Directory.Build.props` loads (tested). Projects without MSTest ignore the property. |
| `ErrorLog` | `$(MSBuildProjectDirectory)/obj/$(MSBuildProjectName).sarif,version=2.1` | One SARIF 2.1 file per project. The `obj/` folder exists when the compiler runs. |
| `AdditionalFiles` | `BannedSymbols.txt` | BannedApiAnalyzers reads the list from this item. |

`TreatWarningsAsErrors` affects the compiler only. `-warnaserror` on the command line affects all MSBuild tasks, also NuGet restore (NU1901 to NU1904). The CI command therefore uses `-warnaserror` too (`ci.md`).

## Directory.Packages.props

| Package | Version (2026-10-16) | License | Reason |
|---|---|---|---|
| Roslynator.Analyzers | 5.0.1 | Apache-2.0 | Readability and correctness rules that the SDK does not have (RCS1075, RCS1155, RCS1210, RCS1229). |
| Meziantou.Analyzer | 3.0.303 | MIT | Async, culture, and design rules (MA0016, MA0040, MA0042, MA0048). |
| Microsoft.VisualStudio.Threading.Analyzers | 18.12.1 | MIT | Async bugs: VSTHRD002 (sync over async), VSTHRD100 (async void), VSTHRD103 (blocking call in async code), VSTHRD110 (unobserved task). |
| Microsoft.CodeAnalysis.BannedApiAnalyzers | 5.6.0 | MIT | Project API rules as build errors (RS0030). |

The four packages set `developmentDependency`, and a `GlobalPackageReference` is private by default, so the analyzers do not flow to the consumers of a library.

A repo without CPM uses `PackageReference` items with `PrivateAssets="all"` in `Directory.Build.props`:

```xml
<ItemGroup>
  <PackageReference Include="Roslynator.Analyzers" Version="5.0.1" PrivateAssets="all" />
</ItemGroup>
```

## .editorconfig

| Setting | Reason |
|---|---|
| `charset`, `end_of_line = lf`, `insert_final_newline`, `trim_trailing_whitespace` | One file shape on Windows and Linux. `dotnet format` fixes a BOM (the `CHARSET` finding). |
| `csharp_style_namespace_declarations = file_scoped:warning`, IDE0161 | One namespace shape, one less indent level. |
| `csharp_prefer_braces = true:warning`, IDE0011 | Braces on each `if`, so that a new line cannot fall outside the block. |
| IDE0005, IDE0051, IDE0052, IDE0059, IDE0060 at `warning` | Unused code is noise for readers and agents. The default is Info or Hidden, so the build does not show it. |
| IDE1006 and the naming rules | The SDK has no default naming rule that the build reports. Interfaces `IName`, types and members `PascalCase`, constants `PascalCase`, private and internal fields `_camelCase`, parameters and locals `camelCase`. |
| CA1848 `none` | It asks for `LoggerMessage` delegates in each log call. CA2254 (constant template) stays on. |
| CA1716 `none` | It flags namespace names such as `Shared` or `Global`. |
| CA1873 `suggestion` | On the .NET 10 SDK it flags property access in log arguments. Remove the line on SDK 11. |
| RCS1059, RCS1075, RCS1155, RCS1169, RCS1202, RCS1210, RCS1215, RCS1229 at `warning` | Correctness rules with little noise. |
| RCS1044 `none` | Duplicate of CA2200 (`throw ex;`). |
| RCS1251 `none` | Conflicts with the common record style. |
| MA0001, MA0004, MA0006, MA0011, MA0027, MA0038, MA0134 `none` | Duplicates of CA1307 to CA1310, CA2007, CA1862, CA1304/CA1305, CA2200, CA1822, VSTHRD110. One problem gives one finding. |
| MA0026 `none` | A TODO belongs in the backlog, not in the build log. |
| VSTHRD200, VSTHRD111 `none` | The Async suffix rule and the `ConfigureAwait` rule are noise in ASP.NET Core apps. |
| CA1707 `none` in `tests/**.cs` | Test names such as `Method_State_Result`. |

The SDK file `dotnet new editorconfig` (389 lines) has all style options at `suggestion`. You can add it to the baseline for the IDE, but the build does not report `suggestion` findings.

## BannedSymbols.txt

Format: one entry per line, `<documentation comment ID>;<message>`. `P:` is a property, `M:` is a method with its parameter types, `T:` is a type. Lines that start with `//` are comments. The message shows in the RS0030 finding.

## .gitattributes

```text
* text=auto eol=lf
*.cmd text eol=crlf
*.bat text eol=crlf
*.png binary
*.jpg binary
```

## Test result

Tested on 2026-10-16 with .NET SDK 10.0.402 on Linux, with the four files of `assets/` and MSTest 4.0.2:

| Test | Command | Result |
|---|---|---|
| Clean solution (class library, MSTest project) | `dotnet build Shop.slnx -warnaserror --no-incremental -tl:off -v q` | Exit 0, `0 Warning(s)`, `0 Error(s)` |
| Clean solution in CI mode | `CI=true dotnet build Shop.slnx --no-incremental -tl:off -v q` | Exit 0, `0 Warning(s)` |
| Format | `dotnet format Shop.slnx --verify-no-changes` | Exit 2 before the first format (BOM in a template file), exit 0 after `dotnet format` |
| Tests | `dotnet test Shop.slnx` | 2 passed |
| Bad file (`Bad.cs`, 57 lines) | `dotnet build src/Shop/Shop.csproj -warnaserror --no-incremental -tl:off -v q` | Exit 1, 31 errors, 24 rule IDs |

The SARIF file `obj/Shop.sarif` of the bad file gave these 24 rule IDs:

CA1304, CA1305, CA1311, CA1510, CA1822, CA1829, CA1862, CA2016, CA2200, CA2254, CS8602, IDE0005, IDE0011, IDE0052, IDE0060, IDE0161, IDE1006, MA0016, MA0048, RCS1075, RCS1155, RS0030, VSTHRD100, VSTHRD110. The `#pragma warning disable CA1822` in the file shows in SARIF as one result with `suppressions[0].kind = "inSource"`.

A second bad file with `.Result`, `.Wait()`, `GetAwaiter().GetResult()`, and `File.ReadAllText` in an async method gave VSTHRD002 (3 times) and VSTHRD103. A bad test class gave MSTEST0037 as an error.

## How to test the baseline again

Do this after you change a severity or a package version.

1. Create the solution:

   ```bash
   dotnet new classlib -n Shop -o src/Shop
   dotnet new mstest -n Shop.Tests -o tests/Shop.Tests
   dotnet new sln -n Shop
   dotnet sln add src/Shop tests/Shop.Tests
   dotnet add tests/Shop.Tests reference src/Shop
   ```

2. Copy the four files of `assets/` to the root. Add `<PackageVersion Include="MSTest" Version="<version>" />` to `Directory.Packages.props` and remove the `Version` attribute from the MSTest reference in the test project (else NU1008).
3. Run `dotnet format Shop.slnx`, then build with `-warnaserror --no-incremental`. Done when the build shows 0 warnings.
4. Add a bad file. Build again and run `python3 scripts/sarif.py . --summary`. Done when each rule ID that you expect is in the list.
