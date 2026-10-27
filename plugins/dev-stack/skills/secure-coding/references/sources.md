# Sources

Primary sources for this skill. Checked on 2026-10-16 unless a line says otherwise. The research files of 2026-10-16 (security, FileSignatures, MailKit, PuppeteerSharp, Docker, Kubernetes, defaults for infrastructure and security) are the base. Facts checked again for this skill are marked "checked again".

## Contents

- Reference lists
- Microsoft Learn (.NET 10)
- OWASP cheat sheets
- Frontend
- Integrations
- Containers and Kubernetes
- Supply chain and tools
- Research on AI-generated code
- Checked again for this skill
- Credits

## Reference lists

- OWASP Top 10:2025: https://top10.owasp.org/2025/
- OWASP API Security Top 10:2023: https://api-security.owasp.org/editions/2023/en/0x11-t10/
- OWASP ASVS 5.0.0: https://owasp.org/www-project-ASVS/ and https://github.com/OWASP/ASVS/releases
- CWE Top 25:2025: https://cwe.mitre.org/top25/archive/2025/2025_cwe_top25.html
- OWASP LLM Top 10:2026: https://genai.owasp.org/resource/owasp-genai-llm-top-10-2026/
- OWASP Top 10 for Agentic Applications 2026: https://genai.owasp.org/resource/owasp-top-10-for-agentic-applications-for-2026/
- Threat Modeling Manifesto: https://www.threatmodelingmanifesto.org/

## Microsoft Learn (.NET 10)

- JWT bearer: https://learn.microsoft.com/aspnet/core/security/authentication/configure-jwt-bearer-authentication
- Resource-based authorization: https://learn.microsoft.com/aspnet/core/security/authorization/resource-based
- Policy-based authorization: https://learn.microsoft.com/aspnet/core/security/authorization/policies
- Cookie redirects for API endpoints (.NET 10): https://learn.microsoft.com/aspnet/core/breaking-changes/10/cookie-authentication-api-endpoints
- Antiforgery: https://learn.microsoft.com/aspnet/core/security/anti-request-forgery
- CORS: https://learn.microsoft.com/aspnet/core/security/cors
- Data Protection: https://learn.microsoft.com/aspnet/core/security/data-protection/configuration/overview
- Identity password hasher: https://learn.microsoft.com/aspnet/core/security/authentication/identity-configuration#password-hasher-options
- Rate limiting: https://learn.microsoft.com/aspnet/core/performance/rate-limit
- Kestrel security: https://learn.microsoft.com/aspnet/core/fundamentals/servers/kestrel/security-considerations
- HTTPS and HSTS: https://learn.microsoft.com/aspnet/core/security/enforcing-ssl
- Proxies and load balancers: https://learn.microsoft.com/aspnet/core/host-and-deploy/proxy-load-balancer
- KnownNetworks obsolete: https://learn.microsoft.com/aspnet/core/breaking-changes/10/ipnetwork-knownnetworks-obsolete
- Error handling: https://learn.microsoft.com/aspnet/core/fundamentals/error-handling
- Upload files: https://learn.microsoft.com/aspnet/core/mvc/models/file-uploads
- EF Core SQL queries: https://learn.microsoft.com/ef/core/querying/sql-queries
- EF Core 10 security changes: https://learn.microsoft.com/ef/core/what-is-new/ef-core-10.0/whatsnew#security-related-improvements
- BinaryFormatter migration: https://learn.microsoft.com/dotnet/standard/serialization/binaryformatter-migration-guide/
- Code quality rules: https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/
- Analysis properties: https://learn.microsoft.com/dotnet/core/project-sdk/msbuild-props#code-analysis-properties
- CA2254: https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca2254
- Regex best practices: https://learn.microsoft.com/dotnet/standard/base-types/best-practices-regex
- XmlReader.Create: https://learn.microsoft.com/dotnet/api/system.xml.xmlreader.create
- Data redaction: https://learn.microsoft.com/dotnet/core/extensions/data-redaction
- NuGet auditing: https://learn.microsoft.com/nuget/concepts/auditing-packages
- Package Source Mapping: https://learn.microsoft.com/nuget/consume-packages/package-source-mapping
- dotnet package list: https://learn.microsoft.com/dotnet/core/tools/dotnet-package-list
- .NET containers: https://learn.microsoft.com/dotnet/core/docker/introduction
- OIDC web authentication (BFF guidance): https://learn.microsoft.com/aspnet/core/security/authentication/configure-oidc-web-authentication

## OWASP cheat sheets

- .NET: https://cheatsheetseries.owasp.org/cheatsheets/DotNet_Security_Cheat_Sheet.html
- File Upload: https://cheatsheetseries.owasp.org/cheatsheets/File_Upload_Cheat_Sheet.html
- Password Storage: https://cheatsheetseries.owasp.org/cheatsheets/Password_Storage_Cheat_Sheet.html
- Mass Assignment: https://cheatsheetseries.owasp.org/cheatsheets/Mass_Assignment_Cheat_Sheet.html
- HTML5 Security: https://cheatsheetseries.owasp.org/cheatsheets/HTML5_Security_Cheat_Sheet.html
- HTTP Headers: https://cheatsheetseries.owasp.org/cheatsheets/HTTP_Headers_Cheat_Sheet.html
- SSRF Prevention: https://cheatsheetseries.owasp.org/cheatsheets/Server_Side_Request_Forgery_Prevention_Cheat_Sheet.html
- Threat Modeling: https://cheatsheetseries.owasp.org/cheatsheets/Threat_Modeling_Cheat_Sheet.html

## Frontend

- Angular security: https://angular.dev/best-practices/security
- AngularJS security: https://docs.angularjs.org/guide/security

## Integrations

- Valkey ACL: https://valkey.io/topics/acl/
- RabbitMQ access control: https://www.rabbitmq.com/docs/access-control
- Elasticsearch security: https://www.elastic.co/guide/en/elasticsearch/reference/current/get-started-enable-security.html
- Dapper: https://github.com/DapperLib/Dapper/blob/main/Readme.md
- MailKit: https://github.com/jstedfast/MailKit (FAQ, ReleaseNotes)
- MimeKit advisory GHSA-g7hc-96xr-gvvx: https://github.com/advisories/GHSA-g7hc-96xr-gvvx
- PuppeteerSharp: https://github.com/hardkoded/puppeteer-sharp
- Puppeteer troubleshooting (sandbox): https://pptr.dev/troubleshooting
- FileSignatures: https://github.com/neilharvey/FileSignatures and https://www.nuget.org/packages/FileSignatures

## Containers and Kubernetes

- Pod Security Standards: https://kubernetes.io/docs/concepts/security/pod-security-standards/
- Secrets good practices: https://kubernetes.io/docs/concepts/security/secrets-good-practices/
- Network policies: https://kubernetes.io/docs/concepts/services-networking/network-policies/
- Docker build secrets: https://docs.docker.com/build/building/secrets/
- dotnet-docker non-root sample: https://github.com/dotnet/dotnet-docker/blob/main/samples/kubernetes/non-root/README.md

## Supply chain and tools

- Trivy releases: https://github.com/aquasecurity/trivy/releases
- Trivy compromise (CVE-2026-33634): https://www.aquasec.com/blog/trivy-supply-chain-attack-what-you-need-to-know
- Gitleaks: https://github.com/gitleaks/gitleaks
- Semgrep C#: https://docs.semgrep.dev/languages/csharp.md
- Opengrep comparison: https://docs.semgrep.dev/faq/comparisons/opengrep.md
- CodeQL CLI: https://docs.github.com/en/code-security/concepts/code-scanning/codeql/about-the-codeql-cli
- pnpm settings: https://pnpm.io/settings/dependency-resolution
- Dependabot options: https://docs.github.com/en/code-security/dependabot/working-with-dependabot/dependabot-options-reference
- SecurityCodeScan on NuGet: https://www.nuget.org/packages/security-scan

## Research on AI-generated code

- Veracode GenAI Code Security Report 2025: https://www.veracode.com/press-release/ai-generated-code-poses-major-security-risks-in-nearly-half-of-all-development-tasks-veracode-research-reveals/
- BaxBench: https://arxiv.org/abs/2502.11844
- Security smells in agent pull requests: https://arxiv.org/abs/2607.12428
- Package hallucinations: https://arxiv.org/abs/2406.10279
- OpenSSF guide for AI code assistants: https://best.openssf.org/Security-Focused-Guide-for-AI-Code-Assistant-Instructions

## Checked again for this skill

- Checked: EF Core 10 raw SQL analyzer IDs. EF1002 flags an interpolated string and EF1003 flags concatenation in `FromSqlRaw`. Source: build of a sample with Microsoft.EntityFrameworkCore.SqlServer 10.0.13, and the Microsoft Learn page on new features in EF Core 10, 2026-10-16.
- Checked: `AnalysisModeSecurity=All` is a valid MSBuild property and turns on CA2326, CA2327, CA5351, CA5359, CA5394, CA2300, CA2301 as warnings. Source: Microsoft Learn MSBuild reference, and a build with SDK 10.0.402, 2026-10-16.
- Checked: `UseAntiforgery` validates POST, PUT and PATCH only, and `HttpMethodOverrideMiddleware` in form mode can bypass it. Source: Microsoft Learn antiforgery page (aspnetcore-10.0), 2026-10-16.
- Checked: `KnownIPNetworks` takes `System.Net.IPNetwork`, and `KnownNetworks` gives ASPDEPR005. Source: Microsoft Learn breaking change page, 2026-10-16.
- Checked: CA2254 is enabled as a suggestion in .NET 10. Source: Microsoft Learn CA2254, 2026-10-16.
- Checked: `ILoggingBuilder.EnableRedaction` is in the package Microsoft.Extensions.Telemetry 10.11.0. Source: compile error without it, 2026-10-16.
- Checked: `IPage.SetContentAsync(string, NavigationOptions)` is obsolete in PuppeteerSharp 25.14.0. Use `SetContentOptions`. Source: CS0618 in the build, 2026-10-16.
- Checked: `dotnet package update --vulnerable` exists in SDK 10.0.402 and fixed a transitive package. Source: command run, 2026-10-16.
- Checked: .NET 10.0.14 and SDK 10.0.403 (2026-10-25, security release). Source: .NET release metadata, 2026-10-27.
- Checked: 8 ASP.NET Core advisories of 2026-10-13, fixed in 10.0.13, 9.0.21 and 8.0.32. Source: `gh api "/advisories?ecosystem=nuget&affects=Microsoft.AspNetCore.App.Runtime.linux-x64"`, 2026-10-27.
- Checked: MailKit before 4.16.0 has CVE-2026-41319 (GHSA-9j88-vvj5-vhgr). Source: GitHub advisory database, 2026-10-27.
- Checked: latest versions MailKit and MimeKit 4.18.2, PuppeteerSharp 25.15.0, Trivy 0.76.1 (fix for the eMudhra CA incident). Source: nuget.org and the Trivy release notes, 2026-10-27.
- Checked: EF1002 shipped in EF Core 8, EF1003 in EF Core 10. Source: `AnalyzerReleases.Shipped.md` in dotnet/efcore, from the audit of 2026-10-25.
- Checked: gitleaks subcommands `git` and `dir`, flags `--redact` and `--log-opts`, exit codes 0 and 1. Source: github.com/gitleaks/gitleaks README, 2026-10-16.

## Credits

This skill paraphrases ideas, not text, from these sources:

- Anthropic `claude-code-security-review` (MIT): a separate verify step per finding and a confidence threshold. https://github.com/anthropics/claude-code-security-review
- OpenSSF Security-Focused Guide for AI Code Assistant Instructions: the package existence check and the self-review loop.
- OWASP Cheat Sheet Series (CC BY-SA 4.0): ideas paraphrased and linked, no text copied.
- Trail of Bits skills (CC BY-SA 4.0): ideas only (insecure defaults, sharp edges). No text copied, so the ShareAlike terms do not apply.
