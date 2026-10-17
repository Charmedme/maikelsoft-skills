# Local security tools

The tools that an agent can run on a developer machine or in a container. The writer runs the fast set (SKILL.md section "Check"). The security reviewer runs all tools that apply. Each finding of a tool is a candidate. Verify it before you report it.

## Contents

- Overview
- .NET security analyzers
- Dependency audit
- Semgrep CE and Opengrep
- CodeQL
- Secrets: gitleaks
- Trivy
- What the tools cannot see

## Overview

| Tool | Finds | License or cost | Speed |
|------|-------|-----------------|-------|
| stackcheck (this skill) | Opt-outs and anti-patterns by regex | Part of the plugin | Seconds |
| .NET analyzers, `AnalysisModeSecurity=All` | CA security rules, EF1002, EF1003 | In the SDK | Build time |
| NuGetAudit, `dotnet package list --vulnerable` | Known vulnerable packages | In the SDK, needs network | Seconds |
| `npm audit` | Known vulnerable npm packages | In npm, needs network | Seconds |
| Semgrep CE or Opengrep | Pattern and taint rules in one file | Engine LGPL-2.1. Semgrep registry rules: Semgrep Rules License v1.0 | Minutes |
| CodeQL | Data flow across files | Free for public repositories. Private: GitHub Code Security license | Minutes |
| gitleaks | Secrets in files and Git history | MIT | Seconds |
| Trivy | Image and file vulnerabilities, misconfiguration, secrets, SBOM | Apache-2.0 | Minutes |

Do not install a tool without the approval of the human (stack rule 5). When a tool is missing, say "not run: <reason>" in the report.

## .NET security analyzers

Most CA security rules are off by default in .NET 10. Turn on the category for the build:

```
dotnet build -p:AnalysisModeSecurity=All -warnaserror:CA2100,CA2300,CA2301,CA2326,CA2327,CA3075,CA5350,CA5351,CA5359,CA5394,EF1002,EF1003
```

Pass: the build succeeds. Tested on 2026-10-16 with SDK 10.0.402. Bad samples gave CA2326, CA2327, CA5351, CA5359, CA5394, CA2300, CA2301, EF1002 and EF1003. The good examples of this skill gave none.

To make it permanent, put this in `Directory.Build.props` (a team decision, see `dev-stack:dotnet-analyzers`):

```xml
<PropertyGroup>
  <AnalysisModeSecurity>All</AnalysisModeSecurity>
</PropertyGroup>
```

EF1002 and EF1003 come from the EF Core 10 package, not from the SDK. They appear only in projects that reference EF Core.

## Dependency audit

```
dotnet restore                                                   # NuGetAudit warnings NU1901 to NU1904
dotnet package list --vulnerable --include-transitive --format json
npm audit --omit=dev --json
npm audit signatures
```

Pass: "has no vulnerable packages" and `found 0 vulnerabilities`. Fix with `dotnet package update --vulnerable` or `npm audit fix` (review the lock file change).

## Semgrep CE and Opengrep

```
semgrep scan --config p/csharp --config p/typescript --sarif --output semgrep.sarif
opengrep scan --config <own rules folder> --sarif --output opengrep.sarif
```

- Semgrep CE does taint analysis inside one function and one file. Many C# framework rules need the paid version. Treat findings as hints.
- The Semgrep registry rules use the Semgrep Rules License v1.0, which is not open source. Run them. Do not copy them into the repository or the plugin.
- Opengrep (fork of Semgrep CE 1.100.0, LGPL-2.1) has no bundled rules and does taint across functions in one file.

## CodeQL

Only for public repositories, or when the organization has GitHub Code Security (SEC-16).

```
codeql database create codeql-db --language=csharp --build-mode=none
codeql database analyze codeql-db codeql/csharp-queries:codeql-suites/csharp-security-extended.qls --format=sarif-latest --output=codeql.sarif
```

`build-mode=none` works for C# without a build. Useful queries: `cs/sql-injection`, `cs/path-injection`, `cs/ssrf`, `cs/log-forging`.

## Secrets: gitleaks

```
gitleaks git --redact                        # the Git history of the current folder
gitleaks git --redact --log-opts="main..HEAD"  # only the commits of the branch
gitleaks dir --redact <folder>               # files without Git history
```

Exit code 0: no leaks. Exit code 1: leaks or an error. `detect` and `protect` are deprecated since 8.19.0. Gitleaks is feature complete and gets security patches only (SEC-17). A found secret is compromised: rotate it, then remove it.

## Trivy

Pin the version. Trivy 0.69.4 was malicious (March 2026). Current: 0.74.0.

```
trivy fs --scanners vuln,secret,misconfig --severity HIGH,CRITICAL --exit-code 1 .
trivy config .
trivy image --severity HIGH,CRITICAL --ignore-unfixed --exit-code 1 <image>
trivy fs --format cyclonedx --output sbom.json .
```

Verify the checksum of the binary, or use the image by digest.

## What the tools cannot see

No tool finds these reliably. They are the work of the reviewer:

- A missing tenant or owner filter (BOLA, IDOR). This is the top risk in each current list.
- A policy that checks the role but not the tenant.
- State that the server must own but takes from the request.
- A business flow that can be automated (API6).
- A fail-open `catch` in an authorization path.
- SSRF through a partner redirect or a stored URL.
- Personal data in a new log line, PDF, e-mail or prompt.
