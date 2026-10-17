# Analyzer packages

Checked on nuget.org on 2026-10-16. Check the version again before you add a package (stack rules 2 and 5).

## In the baseline

| Package | Version | License | Status |
|---|---|---|---|
| Roslynator.Analyzers | 5.0.1 (2026-10-04) | Apache-2.0 | Active. Since 5.0 the only way to get the analyzers. |
| Meziantou.Analyzer | 3.0.303 (2026-10-13) | MIT | Active (frequent releases). About 239 rules. |
| Microsoft.VisualStudio.Threading.Analyzers | 18.12.1 (2026-10-12) | MIT | Active. |
| Microsoft.CodeAnalysis.BannedApiAnalyzers | 5.6.0 (2026-07-02) | MIT | Active. Source is now in dotnet/roslyn. |
| Microsoft.CodeAnalysis.NetAnalyzers | Ships in the SDK (10.0.402) | MIT | Do not add the package. Set `AnalysisLevel`. |
| MSTest.Analyzers | 4.5.1 (2026-10-07) | MIT | Comes with MSTest. See `dev-stack:dotnet-testing`. |

## Optional

| Package | Version | License | When |
|---|---|---|---|
| Roslynator.Formatting.Analyzers | 5.0.1 | Apache-2.0 | Only when the team wants RCS0xxx layout rules. All are off by default and overlap `dotnet format`. |
| AsyncFixer | 2.1.0 (2025-12-29) | Apache-2.0 | Instead of VS Threading, not next to it. 6 rules, low activity. |
| Roslynator.CodeAnalysis.Analyzers | 5.0.1 | Apache-2.0 | Only for projects that write Roslyn analyzers. |

## Not allowed or not recommended

| Package | License or status | Reason | Rule |
|---|---|---|---|
| SonarAnalyzer.CSharp | SONAR Source-Available License v1.0 (since November 2024) | The license excludes the use of AI technology to ingest, interpret, or analyze the output of the program. An agent that reads Sonar findings is such a use. Ask legal first. | ANA-PKG-SONAR |
| SecurityCodeScan.VS2019 (and the other SecurityCodeScan packages) | LGPL-3.0, last release 2022-09 | Not maintained. Use the CA security rules (CA2100, CA3xxx, CA5xxx) and CodeQL. | ANA-PKG-SECURITYCODESCAN |
| StyleCop.Analyzers | 1.3.82-beta (MIT), last stable 1.1.118 from 2019 (Apache-2.0) | The stable release does not know C# 8 and later. Modern C# needs a beta. Many rules overlap IDE rules. | ANA-PKG-STYLECOP |
| Microsoft.CodeAnalysis.FxCopAnalyzers | Deprecated on nuget.org | Replaced by the SDK analyzers. | ANA-PKG-FXCOP |
| Microsoft.CodeAnalysis.NetAnalyzers | MIT | The SDK ships a newer version. An old package gives a warning that the SDK has newer analyzers. | ANA-PKG-NETANALYZERS |
