---
name: dotnet-analyzers
description: Code quality tools for .NET and the agent loop that uses them. Covers SDK analyzers (AnalysisLevel), EnforceCodeStyleInBuild, warnings as errors, Roslynator, Meziantou, VS Threading analyzers, BannedApiAnalyzers, .editorconfig, Directory.Build.props, dotnet format, and SARIF. Use when C# code must be checked or cleaned before a commit, a build shows CA, IDE, RCS, MA, VSTHRD, RS, or MSTEST warnings, a task touches .editorconfig, Directory.Build.props, NoWarn, pragma warning, analyzer packages, or warnings as errors, or a repo needs an analyzer baseline. Not for the C# language rules (dev-stack:csharp), test code (dev-stack:dotnet-testing), writing a new Roslyn analyzer, or linters for other languages.
metadata:
  verified: "2026-10-16"
  versions: ".NET SDK 10.0.402 (Roslyn 5.9), Roslynator.Analyzers 5.0.1, Roslynator CLI 1.0.1, Meziantou.Analyzer 3.0.303, Microsoft.VisualStudio.Threading.Analyzers 18.12.1, Microsoft.CodeAnalysis.BannedApiAnalyzers 5.6.0, MSTest 4"
---

# .NET analyzers

This skill covers the tools that keep .NET code clean and the same in each run. These are the analyzer setup, the severities, `dotnet format`, SARIF, and the build-fix loop. It owns the tool commands and the rule IDs. The reason for each code rule is in `dev-stack:csharp`. MSTest rules are in `dev-stack:dotnet-testing`.

## First

1. Read `references/stack-rules.md`.
2. Read the SDK version in `global.json` (else `dotnet --version`) and the analyzer packages in `Directory.Packages.props` or `Directory.Build.props`. Done when you can name the SDK major and each analyzer version.
3. Version gate:
   - SDK 10: use this skill as written.
   - SDK 11: `AnalysisLevel=latest` uses the .NET 11 rules (a .NET 10 SDK bug used .NET 9 rules). CA1873 no longer flags property access: remove the CA1873 line from `.editorconfig`.
   - Roslynator CLI 1.0.1 runs on Roslyn 5.0. It cannot load an analyzer built for a newer Roslyn, and it skips that analyzer without an error. Use the build as the gate, never the CLI.
4. In an existing repo, read `references/adoption.md` before you change a severity or a build property.

## Defaults for new code

| Area | Default | Decision |
|---|---|---|
| `.editorconfig`, line endings | `assets/.editorconfig`, `root = true`, `* text=auto eol=lf` | QUAL-04 |
| Package versions | Central Package Management, analyzers as `GlobalPackageReference` | QUAL-11 |
| Analysis level | `latest-recommended` (pin `10-recommended` in production repos), nullable as errors, all warnings as errors in CI | QUAL-01 |
| Third-party analyzers | Roslynator, Meziantou, VS Threading, duplicates off | QUAL-02 |
| C# formatter | `dotnet format` with `.editorconfig`, `--verify-no-changes` in CI | QUAL-03 |
| Vulnerability audit | NuGetAudit NU1901 to NU1904 as errors in CI | QUAL-13 |
| Git hooks | None, CI is the gate | QUAL-10 |
| Banned APIs | BannedApiAnalyzers with `assets/BannedSymbols.txt` | QUAL-05 |
| XML docs | Doc file in each project, CS1591 off in apps, on in packages | QUAL-06 |

The baseline files are in `assets/`: `Directory.Build.props`, `Directory.Packages.props`, `.editorconfig`, `BannedSymbols.txt`. The test on 2026-10-16 gave 0 warnings on a clean solution under `-warnaserror`, and 24 rule IDs on a bad file. Read `references/baseline.md` before you copy or merge them.

## Rules

### Setup

1. Put the analyzer properties in one root `Directory.Build.props` and all severities in one root `.editorconfig`. One source stops drift between projects.
2. Use `AnalysisLevel` `latest-recommended` (145 CA warnings). Do not use `latest-all` as the default. Its 280 rules (CA1062, CA2007, CA1515, CA2000, CA1031) give noise. Noise teaches people and agents to suppress.
3. Set `EnforceCodeStyleInBuild=true` and `GenerateDocumentationFile=true` with `NoWarn` CS1591. Without them, IDE rules and IDE0005 never reach the build log.
4. Add `nullable` to `WarningsAsErrors` in each project. Set `TreatWarningsAsErrors` only when `CI=true`. Local work stays fast and the merge gate stays strict.
5. Give each rule that the agent must obey the severity `warning` or `error`. The build log does not show `suggestion` (Info) or `silent` (Hidden) findings.
6. Raise Roslynator rules one ID at a time. `dotnet_analyzer_diagnostic.category-roslynator.severity` also raises Hidden rules (RCS1141 and others) and breaks a clean build.
7. Keep one finding per problem. When two packages report the same problem, keep the CA or IDE rule. Turn the other rule off (see the duplicate map in `references/rule-catalog.md`).
8. Write the SARIF file per project into `obj/` with `ErrorLog` in `Directory.Build.props`. A path on the command line does not expand `$(MSBuildProjectName)`, and a missing folder fails the build with CS0016.
9. Write each `.editorconfig` comment on its own line.

### Packages

10. Reference analyzers as `GlobalPackageReference` (CPM) or with `PrivateAssets="all"`, so that they do not flow to the consumers of a library.
11. Do not add `Microsoft.CodeAnalysis.NetAnalyzers` to a .NET 5+ project. The SDK ships it. Set `AnalysisLevel` instead.
12. Do not add SonarAnalyzer.CSharp for agent use. Its license (SSALv1) excludes the use of AI to analyze its output. Ask the human and legal when the company already pays for SonarQube.
13. Do not add SecurityCodeScan. It is not maintained since 2022. Use the CA security rules and CodeQL.
14. Add StyleCop.Analyzers only when the team asks for it. All releases for modern C# are beta, and many rules overlap IDE rules and `dotnet format`.
15. Ask the human before you add any analyzer package (stack rule 5).

### Suppressions

16. Fix the code. Suppress only a false positive, and only on the smallest scope: `#pragma warning disable <ID> // <reason>` and `#pragma warning restore <ID>` around the lines, or `[SuppressMessage(..., Justification = "<reason>")]`.
17. Do not add a rule ID to `NoWarn` to make a build pass, and do not set `RunAnalyzers` to false. A `NoWarn` entry hides the rule in the whole project and leaves no trace in SARIF. A `#pragma` stays visible in SARIF as a suppressed result.
18. Do not lower a severity in `.editorconfig` to make a build pass. Change a severity only as a team decision, with the reason in a comment line.
18a. Explain in the report each rule that you raise, lower, or turn off. Write what the rule prevents, and why the change fits this project. Read the documentation page of the rule, not only its title. The short rule message often does not show what the rule protects.

### Agent loop

19. Follow the loop in the "Check" section for each code change. Stop after 3 rounds and report the open findings. More rounds make the agent guess.
20. Use an automatic fixer only for a named list of rule IDs, then build and test again. A bulk fix can change behavior: RCS1154 sorts enum members. It can also change a public signature: the VSTHRD100 fix changes `void` to `Task`.
21. When you think that a finding is a false positive, stop and ask. Do not suppress it without a reason that the human can check.

## Never write

| Wrong | Correct |
|---|---|
| `roslynator find-unused <sln>` | `roslynator find-symbol <proj> --unused --visibility private --symbol-kind method field` |
| Install the Roslynator VSIX to get analyzers | Add the `Roslynator.Analyzers` package (the VSIX has no analyzers since 5.0) |
| `<CodeAnalysisRuleSet>x.ruleset</CodeAnalysisRuleSet>` | `dotnet_diagnostic.<ID>.severity` in `.editorconfig` |
| `<NoWarn>$(NoWarn);CA2016</NoWarn>` | Fix the code, or a local `#pragma` with a reason |
| `<RunAnalyzersDuringBuild>false</RunAnalyzersDuringBuild>` | Fix the code |
| `#pragma warning disable CA1822` (no reason, no restore) | `#pragma warning disable CA1822 // <reason>` ... `#pragma warning restore CA1822` |
| `dotnet_analyzer_diagnostic.category-roslynator.severity = warning` | `dotnet_diagnostic.RCS1075.severity = warning`, one ID per line |
| `roslynator analyze` as the CI gate | `dotnet build -warnaserror --no-incremental` |
| `dotnet build -p:ErrorLog=out.sarif` for a solution | `ErrorLog` with `$(MSBuildProjectName)` in `Directory.Build.props` |
| `dotnet format --check` | `dotnet format --verify-no-changes` |
| `<PackageReference Include="Microsoft.CodeAnalysis.NetAnalyzers" ... />` | `<AnalysisLevel>latest-recommended</AnalysisLevel>` |
| `latest-all` and `TreatWarningsAsErrors` on an old repo | Report first, then raise rules one by one (`references/adoption.md`) |
| A second build that shows 0 warnings means "clean" | Build with `--no-incremental`, or read the SARIF file |

## Check

Each build here uses the baseline properties. Run the steps for the projects that the change touches.

1. To find the changed projects, run this script. It prints the nearest `.csproj` of each changed file:

   ```bash
   git diff --name-only main...HEAD -- '*.cs' '*.csproj' | while read -r f; do d=$(dirname "$f"); while [ "$d" != "." ] && [ -z "$(ls "$d"/*.csproj 2>/dev/null)" ]; do d=$(dirname "$d"); done; ls "$d"/*.csproj 2>/dev/null; done | sort -u
   ```

2. Build each changed project. Done when the exit code is 0 and the log shows `0 Warning(s)` and `0 Error(s)`:

   ```bash
   dotnet build <project.csproj> -warnaserror --no-incremental -tl:off -v q
   ```

   Each finding is one line (the quiet log prints each line two times):

   ```text
   /repo/src/Shop/Bad.cs(15,34): error RS0030: The symbol 'DateTime.UtcNow' is banned in this project: Use TimeProvider.GetUtcNow(), so that tests can control the time. (https://github.com/dotnet/roslyn/...) [/repo/src/Shop/Shop.csproj]
   ```

3. When the build fails, read the SARIF files. This script prints `path:line:column RULE-ID level message` and exits 1 when a warning or error is open:

   ```bash
   python3 <skill folder>/scripts/sarif.py <repo>
   ```

   Add `--suppressed` to list each `#pragma` and `SuppressMessage`. Add `--all-levels` to see the Info findings that the build log hides. Read `references/agent-loop.md` for the SARIF fields.

4. Fix each finding. For an ID in the fixer table of `references/agent-loop.md`, run the fixer with the exact IDs:

   ```bash
   dotnet format style <project.csproj> --diagnostics IDE0005 IDE0011 IDE0161
   dotnet format analyzers <project.csproj> --diagnostics CA1510 CA2016 RCS1155 --severity info
   roslynator fix <project.csproj> --supported-diagnostics CA1510 CA1829 CA2016
   ```

   Fix the other IDs by hand. Then go back to step 2 (max 3 rounds).

5. Check the format. Done when the exit code is 0. Exit code 2 means that a file needs a change:

   ```bash
   dotnet format <solution-or-project> --verify-no-changes
   ```

6. Run the tests: `dotnet test`. Done when all tests pass.

7. Run the rule check of this skill. Done when it shows 0 errors:

   ```bash
   python3 <skill folder>/scripts/stackcheck.py --root <repo> --changed main
   ```

## Review checklist

| # | Check | How |
|---|---|---|
| 1 | The changed projects build with 0 warnings under `-warnaserror --no-incremental` | tool (build) |
| 2 | No nullable warning (CS86xx), and nullable is not turned off | tool (CS8602, ANA-NULLABLE-OFF, ANA-EDITORCONFIG-NULLABLE-OFF) |
| 3 | `dotnet format --verify-no-changes` exits 0 | tool (dotnet format) |
| 4 | No new `NoWarn` entry for an analyzer rule | tool (ANA-NOWARN) |
| 5 | Each new `#pragma warning disable` has a reason on the same line and a `restore` | tool (ANA-PRAGMA-REASON), judge for the reason |
| 6 | Each new `SuppressMessage` has a `Justification` | tool (ANA-SUPPRESS-JUSTIFICATION), judge for the reason |
| 7 | No severity lowered in `.editorconfig`, no analyzer package removed | tool (`git diff -- .editorconfig '*.props'`), judge |
| 8 | No banned API (`DateTime.UtcNow` and the other entries) | tool (RS0030) |
| 9 | No `async void`, no unobserved task, no sync over async | tool (VSTHRD100, VSTHRD110, VSTHRD002, VSTHRD103) |
| 10 | Each call gets the `CancellationToken` | tool (CA2016) |
| 11 | Log calls use a constant template | tool (CA2254) |
| 12 | Culture and string comparison are explicit | tool (CA1304, CA1305, CA1311, CA1862, RCS1155) |
| 13 | No unused usings, members, or parameters | tool (IDE0005, IDE0051, IDE0052, IDE0060) |
| 14 | No license trap or dead analyzer package | tool (ANA-PKG-SONAR, ANA-PKG-SECURITYCODESCAN) |
| 15 | An automatic fix kept the behavior and the public API | tool (`dotnet test`), judge |

The full list is in `references/review-checklist.md`.

## Report

End with the report of stack rule 8 (`references/stack-rules.md`): the same headings in the same order on each run.

## References

- `references/decisions.md`: read when new code needs a QUAL decision. The question, options, and recommendation for each ID.
- `references/baseline.md`: read before you copy or merge the files in `assets/`. The reason for each setting and the test result.
- `references/agent-loop.md`: read when a build fails. SARIF fields, the fixer table, and the round limit.
- `references/rule-catalog.md`: read when a rule ID is new to you or noisy. Fix patterns, duplicates, and noisy rules.
- `references/roslynator-cli.md`: read before you run `roslynator`. Commands, exit codes, and the Roslyn limit.
- `references/adoption.md`: read before you add the baseline to an existing repo.
- `references/ci.md`: read when you write the CI steps for build, format, and audit.
- `references/packages.md`: read before you add, update, or remove an analyzer package. Versions and licenses.
- `references/review-checklist.md`: read for a full review.
- `references/sources.md`: the primary sources and the check dates.
- `references/stack-rules.md`: the common rules of all dev-stack skills.
- `references/language.md`: the output language rules.
- `scripts/sarif.py`: prints the findings of the SARIF files.
- `scripts/stackcheck.py`: runs `rules.json` on a repo.
