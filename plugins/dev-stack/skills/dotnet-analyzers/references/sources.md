# Sources

All sources checked on 2026-10-16, versions checked again on 2026-10-27. "Tested" means: run on .NET SDK 10.0.402 on Linux in the scratch project of this skill.

## Primary sources

| Fact | Source |
|---|---|
| `AnalysisLevel`, `AnalysisMode`, `EnforceCodeStyleInBuild`, `GenerateDocumentationFile` | [MSBuild code analysis properties](https://learn.microsoft.com/dotnet/core/project-sdk/msbuild-props#code-analysis-properties) |
| Rule counts per mode (92, 145, 280) | SDK files `sdk/10.0.402/Sdks/Microsoft.NET.Sdk/analyzers/build/config/analysislevel_10_*.globalconfig` (tested). Counted again on 2026-10-27 in `buildTransitive/config/` of the Microsoft.CodeAnalysis.NetAnalyzers 10.0.403 package: same counts. |
| Code style rules on build | [Code analysis overview](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/overview) |
| `ErrorLog`, `WarningsAsErrors` with `nullable`, `TreatWarningsAsErrors` and `-warnaserror` | [Compiler options for errors and warnings](https://learn.microsoft.com/dotnet/csharp/language-reference/compiler-options/errors-warnings) |
| IDE0005 needs the documentation file | [IDE0005](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/ide0005) |
| IDE0079 does not run on build | [IDE0079](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/ide0079) |
| CA2016, CA2254, CA1873, CA1848 | [Quality rules](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/) |
| .NET 11 SDK: CA1873 noise fix, `AnalysisLevel=latest` fix | [What's new in the .NET 11 SDK](https://learn.microsoft.com/dotnet/core/whats-new/dotnet-11/sdk#code-analysis-and-warnings) (fetched again on 2026-10-16) |
| `dotnet format` commands, `--verify-no-changes` | [dotnet format](https://learn.microsoft.com/dotnet/core/tools/dotnet-format) (exit code 2 tested) |
| `GlobalPackageReference` | [Central Package Management](https://learn.microsoft.com/nuget/consume-packages/central-package-management#global-package-references) (fetched again on 2026-10-16) |
| NuGetAudit, NU1901 to NU1904, `NuGetAuditSuppress` | [Auditing packages](https://learn.microsoft.com/nuget/concepts/auditing-packages) (fetched again on 2026-10-16) |
| MSTest analyzers, `MSTestAnalysisMode` | [MSTest analyzers](https://learn.microsoft.com/dotnet/core/testing/mstest-analyzers/overview) |
| Roslynator rules, 5.0 changes | [Roslynator repository and CHANGELOG](https://github.com/dotnet/roslynator) |
| Roslynator CLI | [Roslynator CLI docs](https://josefpihrt.github.io/docs/roslynator/cli/commands/analyze) (commands tested with CLI 1.0.1) |
| Roslynator configuration keys | [Roslynator configuration](https://josefpihrt.github.io/docs/roslynator/configuration) |
| Meziantou rules and duplicates | [Meziantou.Analyzer](https://github.com/meziantou/Meziantou.Analyzer) and its [comparison page](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/comparison-with-other-analyzers.md) |
| VSTHRD rules | [VS Threading analyzers](https://microsoft.github.io/vs-threading/analyzers/index.html) |
| BannedApiAnalyzers format | [BannedApiAnalyzers help](https://github.com/dotnet/roslyn/blob/main/src/RoslynAnalyzers/Microsoft.CodeAnalysis.BannedApiAnalyzers/BannedApiAnalyzers.Help.md) |
| SonarAnalyzer.CSharp license (SSALv1) | License file in the package [SonarAnalyzer.CSharp 10.35.0.4138](https://www.nuget.org/packages/SonarAnalyzer.CSharp/10.35.0.4138) and the [Sonar license announcement](https://community.sonarsource.com/t/a-new-sonar-license-for-sonarqube-analyzers/130731) |
| SecurityCodeScan status | [SecurityCodeScan repository](https://github.com/security-code-scan/security-code-scan) (last commit 2022-11-11) |
| StyleCop.Analyzers status | [StyleCopAnalyzers repository](https://github.com/DotNetAnalyzers/StyleCopAnalyzers) |
| FxCopAnalyzers deprecation | nuget.org registration API for Microsoft.CodeAnalysis.FxCopAnalyzers (checked on 2026-10-16) |
| Package versions | nuget.org flat container API (checked on 2026-10-16) |

## Tested in this skill

- The four files in `assets/`: clean solution 0 warnings, bad file 24 rule IDs, SARIF suppression record.
- `MSTestAnalysisMode` with an `IsTestProject` condition in `Directory.Build.props` stays `Default`. Without the condition it is `Recommended`.
- Fixers per ID (the table in `agent-loop.md`), and that `dotnet format analyzers` needs `--severity info` for those IDs.
- `WarningsAsErrors` and `WarningsNotAsErrors` per ID with `-warnaserror`.
- CA1873 on property access in log arguments on SDK 10.0.402.
- `dotnet format --check` does not exist.

## Credits

- [dotnet/skills](https://github.com/dotnet/skills) (MIT): the idea to layer `Directory.Build.props` and to prefer analyzer code fixes over free text edits. Paraphrased, no text copied.
- [managedcode/dotnet-skills](https://github.com/managedcode/dotnet-skills) (MIT): the idea "one owner per rule, plan the severities before you add a package". Paraphrased. Its `roslynator find-unused` command does not exist. This skill lists it as a wrong command.
- [Meziantou.Analyzer](https://github.com/meziantou/Meziantou.Analyzer) (MIT): the duplicate map, paraphrased in `rule-catalog.md`.
