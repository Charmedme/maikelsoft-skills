# Project conventions

The files at the repository root that control the build: `global.json`, `Directory.Build.props`, `Directory.Packages.props`, `.editorconfig`. The decisions are SOL-03 to SOL-08 in `decisions.md`. The analyzer packages, severities, and `BannedSymbols.txt` belong to `dev-stack:dotnet-analyzers`.

## Contents

- global.json
- Directory.Build.props
- Directory.Packages.props
- .editorconfig
- Solution file
- Traps

## global.json

```json
{
  "sdk": {
    "version": "10.0.402",
    "rollForward": "latestFeature",
    "allowPrerelease": false
  },
  "test": {
    "runner": "Microsoft.Testing.Platform"
  }
}
```

- `latestFeature` accepts newer patches and feature bands of the same major version. CI and developers use the same compiler.
- `dotnet test` with MSTest 4 on SDK 10 needs the `test` section. Without it, `dotnet test` fails with "Testing with VSTest target is no longer supported". With it, use `dotnet test --project <path>`.

## Directory.Build.props

For apps. A library that targets more than one framework does not set `TargetFramework` here: each project sets `TargetFrameworks`.

```xml
<Project>
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <AnalysisLevel>latest-recommended</AnalysisLevel>
    <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
    <NoWarn>$(NoWarn);CS1591</NoWarn>
    <WarningsAsErrors>$(WarningsAsErrors);nullable</WarningsAsErrors>
    <TreatWarningsAsErrors Condition="'$(CI)' == 'true'">true</TreatWarningsAsErrors>
  </PropertyGroup>
</Project>
```

| Property | Reason |
|----------|--------|
| `TargetFramework net10.0` | LTS, support until 2028-11-14 |
| `Nullable enable` | Null bugs become compiler warnings |
| `ImplicitUsings enable` | Fewer `using` lines |
| `AnalysisLevel latest-recommended` | About 145 CA rules as warnings |
| `EnforceCodeStyleInBuild` | IDE rules reach the build log |
| `GenerateDocumentationFile` and `NoWarn CS1591` | Needed for IDE0005 in the build. No XML doc needed on each public member. |
| `WarningsAsErrors nullable` | Null warnings stop each build (SOL-06) |
| `TreatWarningsAsErrors` in CI | Strict merge gate, fast local loop (SOL-06) |

- Do not set `LangVersion`. The target framework selects the C# version: 14 for `net10.0`, 15 for `net11.0`. `latest` or `preview` turns on preview features without a decision.
- A test project uses `MSTest.Sdk` and gets its own settings from `dev-stack:dotnet-testing`.
- A nested `Directory.Build.props` (for example in `tests/`) replaces the root file. Import the root file in it: `<Import Project="$([MSBuild]::GetPathOfFileAbove('Directory.Build.props', '$(MSBuildThisFileDirectory)../'))" />`.

## Directory.Packages.props

```xml
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
    <CentralPackageTransitivePinningEnabled>true</CentralPackageTransitivePinningEnabled>
  </PropertyGroup>
  <ItemGroup>
    <PackageVersion Include="Microsoft.Extensions.TimeProvider.Testing" Version="10.11.0" />
  </ItemGroup>
</Project>
```

- A project references a package without a version: `<PackageReference Include="Microsoft.Extensions.TimeProvider.Testing" />`. A `Version` there gives NU1008.
- Use exact versions. A floating version (`8.*`) gives NU1011 under CPM and a different package on each restore.
- `VersionOverride` on a `PackageReference` is for a real exception only. Write the reason in a comment.
- Transitive pinning: on in apps. In a library, it adds the pinned packages to the nuspec. Turn it off there.
- Analyzer packages for all projects go in `GlobalPackageReference` (see `dev-stack:dotnet-analyzers`).
- Before you add a `PackageVersion`, check the package on nuget.org and get the approval of the human (stack rule 5).

Packages that the team does not allow, or that need a decision:

| Package | Status | Use instead |
|---------|--------|-------------|
| MediatR (all versions) | Not allowed | Direct handler classes. With approval: Mediator (martinothamar, MIT) or Wolverine |
| AutoMapper (all versions) | Not allowed | Manual mapping, or Mapperly (Apache-2.0) for large mappings |
| FluentAssertions 8 or later | Paid for commercial use | FluentAssertions 7.2.2 or AwesomeAssertions (see `dev-stack:dotnet-testing`) |
| System.Linq.Async on `net10.0` | Not needed | Built-in `System.Linq.AsyncEnumerable` |

## .editorconfig

Start with `dotnet new editorconfig`. Then set the rules that the build must enforce to `warning`:

```ini
root = true

[*.cs]
csharp_style_namespace_declarations = file_scoped:warning
dotnet_diagnostic.IDE0161.severity = warning
dotnet_diagnostic.IDE0005.severity = warning
```

- Rules at `suggestion` or `silent` do not show in `dotnet build`. An agent that reads the build log does not see them.
- Put comments on their own line, not after a value.
- The full baseline (naming rule for `_camelCase`, Roslynator and Meziantou severities) is in `dev-stack:dotnet-analyzers`.

## Solution file

- New solutions use `.slnx`. `dotnet new sln` creates it on SDK 10.
- Use `dotnet sln migrate` to convert an `.sln` only when the task asks for it.

## Traps

| Trap | Effect | Fix |
|------|--------|-----|
| `LangVersion latest` with `net10.0` | With a newer SDK, `latest` selects a C# version that `net10.0` does not support | Remove it |
| `LangVersion preview` | Preview features compile without a decision | Remove it |
| `TreatWarningsAsErrors` on an old repository | Hundreds of errors | Report first, then make it stricter in small steps (SOL-06) |
| NU1901 to NU1904 in `NoWarn` | A known vulnerable package builds without notice | Keep them as errors. Accept one package with a reason. |
| `dotnet restore` audit on SDK 10 | Transitive packages are audited, a new CVE can stop the build | Update or pin the package in `Directory.Packages.props` |
