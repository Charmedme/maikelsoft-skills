# Decisions of dotnet-analyzers

## Contents

- QUAL-04. `.editorconfig` and `.gitattributes` baseline
- QUAL-11. Package version management
- QUAL-01. Analysis level and warnings-as-errors policy
- QUAL-02. Third-party analyzers
- QUAL-03. C# formatter
- QUAL-13. Vulnerability audit
- QUAL-10. Pre-commit hooks
- QUAL-05. Banned APIs
- QUAL-06. XML documentation policy

The decisions are in the order that a new repository meets them. Each existing repository keeps its own setup until the human decides otherwise (stack rule 3).

## QUAL-04. `.editorconfig` and `.gitattributes` baseline

- Question: Do we start from the `assets/.editorconfig` baseline of this skill (`root = true`, severities for the build) and set line endings in `.gitattributes` with `* text=auto eol=lf`?
- Ask when: the first commit in a new repository.
- Options: Baseline of this skill: `dotnet new editorconfig` content plus the tested severities and naming rules. Copy of the dotnet/runtime `.editorconfig`: large, tuned for the runtime repo. No `.editorconfig`: each IDE uses its own defaults, and the build checks no style.
- Recommended: Baseline of this skill. One root file drives the IDE, the build, and `dotnet format`, and IDE rules reach the build only with severity `warning` and `EnforceCodeStyleInBuild`. LF matches Linux CI and containers.
- Choose another option when: Windows-only tools need CRLF (set `eol=crlf` for those file types only).
- Depends on: none.
- Revisit when: a new SDK adds style rules that the team wants.
- Check: tool (IDE rules in `dotnet build`, `dotnet format --verify-no-changes`).
- Detect: ANA-DET-QUAL-04

## QUAL-11. Package version management

- Question: Do we use Central Package Management (`Directory.Packages.props`), with the analyzers as `GlobalPackageReference`, and a committed lock file with `npm ci` for the frontend?
- Ask when: the first package reference.
- Options: CPM plus `package-lock.json` and `npm ci`: one place for versions. Versions in each `.csproj`: versions drift between projects. CPM plus NuGet lock files (`RestorePackagesWithLockFile`): reproducible restore, more churn in pull requests.
- Recommended: CPM plus `GlobalPackageReference` for analyzers, and an npm lock file with `npm ci`. No NuGet lock files at the start. A `GlobalPackageReference` reaches each project and does not flow to the consumers of a library.
- Choose another option when: strict supply-chain rules require NuGet lock files.
- Depends on: none.
- Revisit when: a compliance audit asks for lock files.
- Check: tool (NU1008 when a project still sets a version under CPM).
- Detect: ANA-DET-QUAL-11

## QUAL-01. Analysis level and warnings-as-errors policy

- Question: Do we use `AnalysisLevel` `latest-recommended` (pinned to `10-recommended` in production repos), nullable warnings as errors everywhere, and all warnings as errors in CI only?
- Ask when: the first project in a new repository (`Directory.Build.props`).
- Options: `latest-recommended` or pinned `10-recommended`, `nullable` in `WarningsAsErrors`, `TreatWarningsAsErrors` when `CI=true`: strict gate, fast local loop. `TreatWarningsAsErrors` everywhere: one rule, slower local work. SDK default (`latest`, minimum mode, 92 warnings): warnings never stop the build. `latest-all` (280 rules): much noise.
- Recommended: The first option. Local work stays fast, CI blocks each warning, and nullable bugs never pass. Agents always build with `-warnaserror`, so they see the CI result.
- Choose another option when: a solo developer or a small team wants one rule for all (warnings as errors everywhere).
- Depends on: none.
- Revisit when: a new SDK major is out (for a pinned level), or the warning count in CI logs grows.
- Check: tool (ANA-PROPS-ANALYSISLEVEL, ANA-PROPS-NULLABLE, ANA-PROPS-NULLABLE-ERRORS, ANA-ANALYSIS-ALL).
- Detect: ANA-DET-QUAL-01

## QUAL-02. Third-party analyzers

- Question: On top of the SDK analyzers, do we add Roslynator, Meziantou (duplicates off), and the VS Threading analyzers, and skip StyleCop and Sonar?
- Ask when: the first project in a new repository.
- Options: SDK analyzers only: fewest IDs. SDK plus Roslynator plus Meziantou plus Microsoft.VisualStudio.Threading.Analyzers: more real bugs found. Add StyleCop.Analyzers: strict layout rules, beta releases only for modern C#. Add SonarAnalyzer.CSharp: license forbids AI analysis of its output.
- Recommended: SDK plus Roslynator plus Meziantou plus VS Threading, with the baseline of this skill. Each package finds real bugs (async void, sync over async, culture, empty catch), and the baseline turns off the duplicates.
- Choose another option when: the team wants the fewest IDs to learn (SDK plus Roslynator only).
- Depends on: QUAL-01.
- Revisit when: one rule causes many suppressions.
- Check: tool (ANA-PKG-SONAR, ANA-PKG-SECURITYCODESCAN, ANA-PKG-STYLECOP).
- Detect: ANA-DET-QUAL-02

## QUAL-03. C# formatter

- Question: Do we format C# with `dotnet format` driven by `.editorconfig`, and check it in CI with `--verify-no-changes`?
- Ask when: the first commit of C# code.
- Options: `dotnet format` plus `.editorconfig`: in the SDK, no extra tool. CSharpier: opinionated, few options, a separate tool. IDE formatting only: no check.
- Recommended: `dotnet format` plus `.editorconfig`. The same file drives the IDE, the build, and CI, and `--verify-no-changes` exits 2 on drift without a change to the files.
- Choose another option when: the team wants deterministic line wrapping and no style debates. Then use CSharpier, and turn off the overlapping whitespace rules in `.editorconfig`.
- Depends on: QUAL-04.
- Revisit when: formatting comments keep appearing in reviews.
- Check: tool (`dotnet format --verify-no-changes`).
- Detect: ANA-DET-QUAL-03

## QUAL-13. Vulnerability audit

- Question: Do we fail CI on known vulnerable packages: NuGetAudit warnings NU1901 to NU1904 as errors, and `npm audit --omit=dev --audit-level=high`?
- Ask when: the first CI pipeline.
- Options: NuGetAudit as errors in CI plus an `npm audit` gate: built-in tools only. Report only: no gate. External scanner (Snyk, GitHub Advanced Security): extra cost and setup.
- Recommended: NuGetAudit as errors in CI plus the `npm audit` gate. NuGetAudit is on by default, and for `net10.0` it also checks transitive packages. `dotnet build -warnaserror` in CI turns NU1901 to NU1904 into errors. Suppress one advisory with `NuGetAuditSuppress` and a reason.
- Choose another option when: the company already has a scanner with a policy.
- Depends on: QUAL-01, GIT-10.
- Revisit when: false positives block more than one pull request per month.
- Check: tool (restore warnings NU1901 to NU1904).
- Detect: none

## QUAL-10. Pre-commit hooks

- Question: Do we use no committed Git hooks, and let CI be the only gate?
- Ask when: the first CI pipeline.
- Options: No hooks, CI runs format, build, and tests: simplest. Husky plus lint-staged (npm). lefthook: one binary for .NET and npm. Husky.Net (NuGet).
- Recommended: No hooks. A developer can skip a local hook with `--no-verify`, so CI must be the gate anyway. Agents run the build and the format check before each commit.
- Choose another option when: CI often fails on formatting only (then lefthook with `dotnet format` and `prettier` on the staged files).
- Depends on: GIT-10, QUAL-03, QUAL-07.
- Revisit when: more than a few pull requests per month fail CI on format only.
- Check: judge.
- Detect: ANA-DET-QUAL-10

## QUAL-05. Banned APIs

- Question: Do we ban `DateTime.Now` and `UtcNow` (use `TimeProvider`) and a few other APIs with BannedApiAnalyzers?
- Ask when: the first code that reads the time, or the first time a review rejects a specific API.
- Options: BannedApiAnalyzers (RS0030) with a short `BannedSymbols.txt`: a build error with your own message. A written rule in the coding guide only: no check. None.
- Recommended: BannedApiAnalyzers with `assets/BannedSymbols.txt`: `DateTime.Now`, `DateTime.UtcNow`, `DateTimeOffset.Now`, `DateTimeOffset.UtcNow`, `Thread.Sleep`. Add an entry only after a real review finding, so that the list stays short.
- Choose another option when: the codebase is tiny and has one developer (a written rule is enough).
- Depends on: QUAL-01.
- Revisit when: a review rejects the same API two times.
- Check: tool (RS0030).
- Detect: ANA-DET-QUAL-05

## QUAL-06. XML documentation policy

- Question: Do we generate the XML doc file in all projects (for IDE0005)? And do we require doc comments only on the public API of shared libraries and NuGet packages?
- Ask when: the first public type in a library, or the first project file.
- Options: `GenerateDocumentationFile=true` everywhere, `NoWarn` CS1591 in apps, CS1591 on in packages: IDE0005 runs, apps stay free of noise. Doc comments on each public member: much text that rots. No XML docs: IDE0005 does not run on build.
- Recommended: The first option. A doc comment helps where a consumer cannot see the code. For the comment style, use `dev-docs:doc-code-comments` when the environment has it.
- Choose another option when: the project publishes an SDK to external customers (require docs on all public members).
- Depends on: QUAL-01.
- Revisit when: a project becomes a shared package.
- Check: tool (CS1591 in package projects, ANA-PROPS-DOCFILE).
- Detect: none
