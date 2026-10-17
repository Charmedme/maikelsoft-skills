# Team decisions: secure-coding

The security decisions that a team makes one time for new code. Ask each decision when the work first needs it (stack rule 4). Existing code keeps its own style. Source: the defaults research of 2026-10-16 (SEC-01 to SEC-23). The blocks follow the order in which a new project meets them.

Batch the questions: ask SEC-15, SEC-17 and SEC-07 together at the first commit. Ask SEC-01, SEC-02, SEC-04, SEC-05, SEC-12, SEC-14 and SEC-18 together as one "first protected endpoint" question. Ask the others one by one when the work needs them.

## Contents

- SEC-15. Dependency policy
- SEC-17. Secrets scanning
- SEC-07. Secrets in development and CI
- SEC-01. Identity provider
- SEC-02. API token model
- SEC-04. Authorization model
- SEC-05. Multi-tenant isolation check
- SEC-12. Input size limits
- SEC-14. Data classification and PII logging policy
- SEC-18. Threat model practice
- SEC-16. SAST tool
- SEC-19. Security review gate
- SEC-03. Sign-in model for Angular apps
- SEC-20. Data Protection key ring storage
- SEC-09. CORS defaults
- SEC-10. Security headers set
- SEC-08. HTTPS, HSTS and TLS termination
- SEC-21. Kubernetes workload hardening baseline
- SEC-11. Rate limiting defaults
- SEC-13. File upload policy
- SEC-22. Service-to-service authentication
- SEC-23. Security event and audit logging
- SEC-06. Password storage

## SEC-15. Dependency policy

- Question: Which package sources are allowed, and how do new versions enter the build?
- Ask when: the first package added to a new project.
- Options: A, mapped sources: nuget.org and npmjs.org plus internal feeds with Package Source Mapping. Lock files in Git, locked restore in CI, NuGetAudit errors for high and critical, Renovate or Dependabot with a cooldown. B, A without lock files. C, one proxy feed (Azure Artifacts upstream, Artifactory) as the only source.
- Recommended: A, with a 3-day cooldown (Dependabot default) and `min-release-age=3` for npm. Lock files and source mapping stop changed versions and dependency confusion. A cooldown keeps freshly published malware out of the build.
- Choose another option when: a policy requires one curated feed (C).
- Depends on: none.
- Revisit when: a new supply-chain incident hits the ecosystem.
- Check: `tool (SC-NUGETAUDIT-OFF, SC-NPM-INSTALL-CI, NuGetAudit NU1901 to NU1904)`
- Detect: SC-DET-SEC-15

## SEC-17. Secrets scanning

- Question: Which tool stops secrets from entering Git?
- Ask when: the first commit to a new repository.
- Options: A, gitleaks in pre-commit and CI. B, Betterleaks (new since 2026-03-15, by the gitleaks author, MIT). C, GitHub secret scanning with push protection.
- Recommended: A, plus C when licensed. Gitleaks is mature. It gets security patches only, which is enough for a scanner with a stable rule set.
- Choose another option when: Betterleaks proves stable (review in Q1 2027).
- Depends on: none.
- Revisit when: gitleaks stops security patches.
- Check: `tool (gitleaks, SC-PRIVATE-KEY, SC-APPSETTINGS-SECRET)`
- Detect: SC-DET-SEC-17

## SEC-07. Secrets in development and CI

- Question: Where do developers and CI keep secrets?
- Ask when: the first secret value in any environment.
- Options: A, `dotnet user-secrets` or Aspire parameters for development, the platform secret store plus OIDC federation to the cloud for CI (no long-lived keys). B, a git-ignored `.env` file for development, stored service principal keys in CI. C, password manager entries that people copy by hand.
- Recommended: A. Hard-coded credentials are 99.6% of the critical findings in agent pull requests. User-secrets are plain JSON outside the repository, for development only.
- Choose another option when: tools outside .NET need the values locally (B for development only, never commit `.env`).
- Depends on: OPS-02.
- Revisit when: a secret leaks. Rotate it and review the path.
- Check: `tool (SC-APPSETTINGS-SECRET, SC-DOCKER-SECRET-ARG, gitleaks)`
- Detect: SC-DET-SEC-07

## SEC-01. Identity provider

- Question: Which identity provider owns users and issues tokens?
- Ask when: the first endpoint or page that needs a signed-in user.
- Options: A, Keycloak, self-hosted. B, Microsoft Entra ID (staff) or Entra External ID (customers). C, another managed provider (Auth0, Okta). D, ASP.NET Core Identity inside the app.
- Recommended: A for customer-facing, multi-tenant products: Organizations, themes, self-hosting, no fee per user. Run the official image with an external PostgreSQL, behind the gateway.
- Choose another option when: the users are only staff with Microsoft 365 accounts (B). The team cannot operate Keycloak upgrades and backups (B or C). Avoid D: it puts passwords and MFA in each app.
- Depends on: none.
- Revisit when: user count, compliance, or operations cost changes the build-or-buy balance.
- Check: judge
- Detect: SC-DET-SEC-01

## SEC-02. API token model

- Question: How do APIs authenticate callers?
- Ask when: the first protected API endpoint.
- Options: A, JWT bearer, validated locally against the provider keys (issuer, audience, lifetime, signature, pinned algorithms). B, opaque tokens with introspection on each call. C, API keys.
- Recommended: A with plain `AddJwtBearer`, one audience per API, short-lived access tokens. Local validation is fast and Keycloak advises it over introspection.
- Choose another option when: instant revocation is a hard requirement (B for that API). Machine partners cannot do OAuth (C: store a hash, compare in constant time, rate limit).
- Depends on: SEC-01.
- Revisit when: token theft becomes a threat in the threat model. Then consider DPoP or mTLS-bound tokens.
- Check: `tool (SC-JWT-NO-LIFETIME, SC-JWT-NO-AUDIENCE, SC-JWT-NO-ISSUER, SC-JWT-UNSIGNED)`
- Detect: SC-DET-SEC-02

## SEC-04. Authorization model

- Question: How do we express who may do what?
- Ask when: the first endpoint that not each signed-in user may call.
- Options: A, a fallback policy (authenticated user) plus named policies that check permissions, and resource-based checks for objects. B, role attributes on endpoints (`[Authorize(Roles = "...")]`). C, an external policy engine (OPA, OpenFGA).
- Recommended: A. Map provider roles to permissions in one policy registry. New endpoints are closed by default, and the rules live in one place that a test can check.
- Choose another option when: users share objects with many relationships (documents, folders). Then use C, for example OpenFGA.
- Depends on: SEC-01.
- Revisit when: rules start to depend on relationships rather than roles.
- Check: `tool (SC-ALLOW-ANONYMOUS, authorization inventory test)` + judge
- Detect: SC-DET-SEC-04

## SEC-05. Multi-tenant isolation check

- Question: How do we guarantee that one tenant never sees the data of another tenant?
- Ask when: the first table, cache key, index or queue that holds tenant data.
- Options: A, shared schema with `TenantId`, tenant from the token claim, EF Core global query filter, and an automated cross-tenant test per endpoint. B, A plus database row-level security. C, schema per tenant. D, database per tenant.
- Recommended: A. It is the simplest model that a test can prove, and the test is part of the definition of done. Apply the tenant to each store: SQL, cache keys, search filters, messages, logs.
- Choose another option when: contracts or audits demand isolation that the database enforces (B). Large tenants need separate backup, restore or data residency (D).
- Depends on: SEC-01, SEC-04.
- Revisit when: a tenant asks for data residency or its own encryption key.
- Check: `judge` (cross-tenant test in `references/testing.md`)
- Detect: SC-DET-SEC-05

## SEC-12. Input size limits

- Question: Which request size limits apply by default?
- Ask when: the first endpoint that accepts a body.
- Options: A, lower the global `MaxRequestBodySize` (for example to 1 MB) and raise it per endpoint that needs more. Add a max length on each string and a max page size. B, keep the Kestrel defaults (30,000,000 bytes body, 128 MB multipart).
- Recommended: A. Use a 1 MB global body limit and `[RequestSizeLimit]` on upload endpoints. Cap the page size at 100. Microsoft advises to lower the body size for APIs without large uploads.
- Choose another option when: most endpoints accept documents (keep a higher global limit, but set it explicitly).
- Depends on: none.
- Revisit when: valid requests fail with 413.
- Check: `tool (SC-NO-SIZE-LIMIT)` + judge
- Detect: SC-DET-SEC-12

## SEC-14. Data classification and PII logging policy

- Question: Which data classes do we use, and what can appear in logs and traces?
- Ask when: the first log line, span tag or DTO with personal data.
- Options: A, a small taxonomy (Public, Internal, Personal, Secret) with Microsoft.Extensions.Compliance attributes and redaction on. Secret is never logged. Personal is logged only redacted. B, a rule in a document, no code enforcement. C, the company taxonomy, if one exists.
- Recommended: C when the company has a taxonomy, else A. Redaction in code is the only part that a test can check. Redaction works only when Microsoft.Extensions.Logging owns the logger factory.
- Choose another option when: never B alone.
- Depends on: OPS-19.
- Revisit when: a DPIA or a GDPR request shows a gap.
- Check: `tool (SC-LOG-SECRET)` + judge
- Detect: SC-DET-SEC-14

## SEC-18. Threat model practice

- Question: When and how do we threat model?
- Ask when: the first design that adds a trust boundary (new external input, new integration, change to authentication, new data class).
- Options: A, STRIDE light: the five questions of `references/threat-model.md`, answered in the design or the ADR, for each design that adds a boundary. B, a full STRIDE workshop with data-flow diagrams per release. C, no formal practice.
- Recommended: A. A light model that the team repeats finds more than a heavy model that it skips. ASVS 5.0 asks for documented security decisions.
- Choose another option when: the system is high risk, for example customs filings to authorities or payments. Then use B at the first design, and A after that.
- Depends on: none.
- Revisit when: an incident shows a missed threat.
- Check: judge
- Detect: SC-DET-SEC-18

## SEC-16. SAST tool

- Question: Which static analysis runs on each pull request?
- Ask when: the first CI pipeline.
- Options: A, .NET SDK security analyzers on (`AnalysisModeSecurity=All`) plus Semgrep CE with registry rules. B, .NET analyzers plus Opengrep with an own rule set. C, .NET analyzers plus CodeQL.
- Recommended: C when the organization has GitHub Code Security, else A. Most CA security rules are off by default, so the property is the first step in each option.
- Choose another option when: legal rejects the Semgrep Rules License v1.0 (B).
- Depends on: SEC-19.
- Revisit when: the license of the Semgrep rules or of CodeQL changes.
- Check: `tool (dotnet build -p:AnalysisModeSecurity=All)`
- Detect: SC-DET-SEC-16

## SEC-19. Security review gate

- Question: Which findings block a merge or a release?
- Ask when: the first CI pipeline with a scanner.
- Options: A, block Critical and High (when a fix exists). Medium gets a ticket with a due date. Low is advice. Exceptions have an owner and an end date. B, block Critical only. C, report only.
- Recommended: A, with ASVS 5.0 level 2 as the target of the security reviewer. A gate that blocks too much gets bypassed, and a gate that blocks nothing gets ignored.
- Choose another option when: a legacy codebase has a large backlog (B for old code, A for changed lines).
- Depends on: none.
- Revisit when: the exception list grows or gets stale.
- Check: judge (`references/review-report.md`)
- Detect: none

## SEC-03. Sign-in model for Angular apps

- Question: How does the Angular app sign users in and call APIs?
- Ask when: the first Angular screen that needs a signed-in user.
- Options: A, Backend for Frontend: an ASP.NET Core host runs OIDC as a confidential client and keeps the tokens on the server. It gives the browser an `HttpOnly` cookie and forwards API calls with YARP. B, a public client in the browser with Authorization Code and PKCE, access token in memory. C, a public client with tokens in `localStorage`.
- Recommended: A, built with ASP.NET Core OIDC, cookie authentication, antiforgery and `MapForwarder` or YARP. No third-party BFF library is needed. Microsoft no longer advises OIDC public clients for web apps.
- Choose another option when: a static host serves the Angular app without a backend that you control. The threat model must accept tokens in the browser (B). Never C.
- Depends on: SEC-01, SEC-20.
- Revisit when: the IETF browser-based apps guidance or Angular adds a standard pattern.
- Check: `tool (SC-TS-TOKEN-STORAGE)`
- Detect: SC-DET-SEC-03

## SEC-20. Data Protection key ring storage

- Question: Where does ASP.NET Core store and protect Data Protection keys?
- Ask when: the first use of cookies, antiforgery, or `IDataProtector` in a service with more than one replica.
- Options: A, the SQL database with `Microsoft.AspNetCore.DataProtection.EntityFrameworkCore`, protected with a certificate or a vault key. B, Azure Blob plus Key Vault. C, Redis or Valkey. D, the default (file system in the container).
- Recommended: A. The database already exists and has backups. Set the same `SetApplicationName` in each replica.
- Choose another option when: the app runs on Azure with Key Vault (B). Valkey has persistence and backups (C). Never D with more than one replica: keys are lost at each restart, and cookies and antiforgery tokens then fail.
- Depends on: SEC-03.
- Revisit when: key rotation or multi-region needs appear.
- Check: judge
- Detect: SC-DET-SEC-20

## SEC-09. CORS defaults

- Question: Which origins can call our APIs from a browser?
- Ask when: the first browser call to an API on another origin.
- Options: A, no CORS: SPA and API on one origin (BFF or path routing on the gateway). B, an explicit origin list from configuration, per API. C, any origin.
- Recommended: A. With the BFF of SEC-03, the browser talks to one origin. CORS is not authorization, so a narrow setting costs nothing.
- Choose another option when: a public API serves third-party web apps (B, without credentials).
- Depends on: SEC-03.
- Revisit when: a partner portal on another domain must call the API.
- Check: `tool (SC-CORS-ANY-CREDENTIALS, SC-CORS-ANY-ORIGIN-FUNC)`
- Detect: SC-DET-SEC-09

## SEC-10. Security headers set

- Question: Which security headers do responses carry, and who sets them?
- Ask when: the first HTML page or the first API response that leaves the cluster.
- Options: A, nginx `add_header` for the Angular static files, a small own middleware for API and BFF. B, a library (`NetEscapades.AspNetCore.SecurityHeaders`, `NWebsec.AspNetCore.Middleware`). C, the gateway sets them for all services.
- Recommended: A with the OWASP set: `X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY` or CSP `frame-ancestors`, `Referrer-Policy: strict-origin-when-cross-origin`, `Permissions-Policy`, `Cross-Origin-Opener-Policy: same-origin`, a CSP for the Angular app, Kestrel `AddServerHeader = false`.
- Choose another option when: many services need complex CSP rules (B). The platform team owns headers centrally (C).
- Depends on: SEC-03.
- Revisit when: a penetration test reports header findings.
- Check: judge (header test in `references/testing.md`)
- Detect: SC-DET-SEC-10

## SEC-08. HTTPS, HSTS and TLS termination

- Question: Where does TLS end, and which HSTS policy do we send?
- Ask when: the first public hostname.
- Options: A, TLS ends at the gateway, HSTS `max-age=31536000; includeSubDomains`, APIs reject plain HTTP, the app uses forwarded headers with known proxies. B, A plus `preload` and two years. C, end-to-end TLS to each Pod, or mesh mTLS.
- Recommended: A. Start HSTS short and raise it to one year. `preload` is hard to undo. Do not use `ASPNETCORE_FORWARDEDHEADERS_ENABLED`: it trusts any proxy.
- Choose another option when: the domain is fully HTTPS and owned for the long term (B). Regulations require encryption inside the cluster (C).
- Depends on: OPS-11, OPS-22.
- Revisit when: a compliance audit asks for encryption inside the cluster.
- Check: `tool (SC-FORWARDED-TRUST-ALL)` + judge
- Detect: SC-DET-SEC-08

## SEC-21. Kubernetes workload hardening baseline

- Question: Which Pod security baseline do all namespaces enforce?
- Ask when: the first namespace for the service.
- Options: A, Pod Security Admission `restricted`, a default-deny NetworkPolicy with explicit allows (DNS too), `automountServiceAccountToken: false`. B, A plus Kyverno for rules that admission cannot express (digest pinning, labels). C, Pod Security `baseline` only.
- Recommended: A. It blocks the common escalation paths with built-in Kubernetes features and no extra component.
- Choose another option when: the platform needs policy beyond Pod Security Admission (B).
- Depends on: OPS-08.
- Revisit when: a Pod needs an exception. Record it.
- Check: `tool (SC-K8S-PRIVILEGED, Trivy config)` + judge
- Detect: SC-DET-SEC-21

## SEC-11. Rate limiting defaults

- Question: Which limits protect our endpoints from abuse?
- Ask when: the first sign-in, export, render, upload or other costly endpoint.
- Options: A, the ASP.NET Core rate limiter with strict policies on sensitive or costly endpoints. Partition by user id (fallback client IP), status 429. B, limits at the gateway only. C, both.
- Recommended: A. Add gateway limits only when the volume of abuse threatens the Pods. Configure forwarded headers first, so that the client IP is correct.
- Choose another option when: a public anonymous API has a high abuse risk (C).
- Depends on: SEC-08.
- Revisit when: abuse incidents or cost spikes occur.
- Check: judge
- Detect: SC-DET-SEC-11

## SEC-13. File upload policy

- Question: How do we accept, check and store uploaded files?
- Ask when: the first upload endpoint.
- Options: A, extension allowlist, file signature check, size limit, server-generated name, object storage outside the web root. Add a malware scan for files from external parties and download with `Content-Disposition: attachment`. B, A without malware scan. C, store on a Pod volume.
- Recommended: A. Use FileSignatures for the signature step (`references/file-upload.md`).
- Choose another option when: files come only from trusted internal systems (B).
- Depends on: SEC-12.
- Revisit when: the app allows new file types.
- Check: `tool (SC-CLIENT-FILENAME-PATH, SC-INSPECTOR-ALL-FORMATS, SC-NO-SIZE-LIMIT)` + judge
- Detect: SC-DET-SEC-13

## SEC-22. Service-to-service authentication

- Question: How do internal services prove their identity to each other?
- Ask when: the first call from one service to another, or the first RabbitMQ consumer.
- Options: A, an OAuth client credentials JWT from the provider for HTTP calls, one RabbitMQ user and vhost per service, NetworkPolicy always. B, network isolation only. C, a service mesh with mTLS (Istio, Linkerd, Cilium).
- Recommended: A. Zero trust between services when calls carry authority, without the operations cost of a mesh.
- Choose another option when: calls are read-only and inside one namespace (B). Many services and a platform team that runs a mesh (C).
- Depends on: SEC-01, SEC-21.
- Revisit when: more than about 10 services call each other.
- Check: judge
- Detect: SC-DET-SEC-22

## SEC-23. Security event and audit logging

- Question: Which security events and business actions do we record, where, and for how long?
- Ask when: the first sign-in flow, admin action, or state change that a user could deny later.
- Options: A, security events (failed sign-ins, denied access, admin actions) as structured logs with stable `EventId`s in the observability backend. Plus a business audit trail (who changed what, when) in an append-only table. B, application logs only. C, a dedicated audit service or SIEM.
- Recommended: A, with a retention that the business agrees (customs records often need years). It answers the STRIDE repudiation question.
- Choose another option when: the organization runs a SIEM (C as the destination for security events).
- Depends on: OPS-18, SEC-14.
- Revisit when: an audit or a legal hold asks for records that you do not keep.
- Check: judge
- Detect: SC-DET-SEC-23

## SEC-06. Password storage

- Question: Does any service store passwords?
- Ask when: the first user or credential table.
- Options: A, no: the identity provider owns all passwords. B, ASP.NET Core Identity `PasswordHasher<T>` with more iterations. C, Argon2id with a third-party package.
- Recommended: A. No own password code means no own password bugs.
- Choose another option when: a legacy or offline system must verify passwords itself. Then use B: set `PasswordHasherOptions.IterationCount` to 220,000 or more (OWASP for PBKDF2-HMAC-SHA512) after a load test.
- Depends on: SEC-01.
- Revisit when: SEC-01 changes.
- Check: `tool (SC-WEAK-HASH-SECRET)` + judge
- Detect: SC-DET-SEC-06
