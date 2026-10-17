# Reference lists

The lists that a finding maps to, with the typical code mistake in this stack. Status on 2026-10-16.

## Contents

- Status of the lists
- OWASP Top 10:2025
- OWASP API Security Top 10:2023
- ASVS 5.0 chapters
- CWE Top 25:2025
- OWASP LLM Top 10:2026

## Status of the lists

| List | Version | Note |
|------|---------|------|
| OWASP Top 10 | 2025 | Current released version. New: A03 Supply Chain, A10 Exceptional Conditions. SSRF is now in A01. |
| OWASP API Security Top 10 | 2023 | No newer edition. |
| OWASP ASVS | 5.0.0 (2025-05-30) | 345 requirements, 17 chapters, levels 1 to 3. Target here: level 2 (SEC-19). No direct CWE mapping. |
| CWE Top 25 | 2025 (2025-12) | Uses the CWE mappings of the CNAs. Memory-safety entries apply only to `unsafe` code, P/Invoke and native parts (Chromium). |
| OWASP LLM Top 10 | 2026 (2026-08-03) | Replaces 2025. See `llm-apps.md`. |

## OWASP Top 10:2025

| ID | Name | Code mistake in this stack |
|----|------|----------------------------|
| A01 | Broken Access Control (with SSRF) | Load by id without a tenant filter. No fallback policy. `HttpClient.GetAsync(userUrl)`. |
| A02 | Security Misconfiguration | Developer exception page outside Development. `AllowAnyOrigin` with credentials. Valkey without a password. Swagger UI in production. |
| A03 | Software Supply Chain Failures | No lock file. Audit warnings suppressed. Action pinned by tag. An invented package name. |
| A04 | Cryptographic Failures | MD5 or SHA1, AES ECB, a hard-coded key, own password hash, accept-all certificate callback. |
| A05 | Injection (with XSS) | `FromSqlRaw($"...")`, Dapper with `+`, `query_string` with user input, `bypassSecurityTrustHtml(userHtml)`. |
| A06 | Insecure Design | Reset token that never expires. Export without a limit. Status taken from the client. |
| A07 | Authentication Failures | `ValidateAudience = false`. No rate limit on sign-in. Tokens in `localStorage`. |
| A08 | Software or Data Integrity Failures | `TypeNameHandling.All` on queue messages. CDN script without SRI. |
| A09 | Security Logging and Alerting Failures | Failed sign-ins not logged. Tokens in logs. Interpolated log lines. |
| A10 | Mishandling of Exceptional Conditions | `catch { return true; }` in an authorization check. `ex.ToString()` in a response. No rollback. |

## OWASP API Security Top 10:2023

| ID | Name | Code mistake |
|----|------|--------------|
| API1 | Broken Object Level Authorization | `FindAsync(id)` without an owner or tenant filter |
| API2 | Broken Authentication | HS256 with a shared key from `appsettings.json`. API keys compared with `==` |
| API3 | Broken Object Property Level Authorization | Entity in the response. Request bound to the entity |
| API4 | Unrestricted Resource Consumption | No rate limiter. No max page size. No upload limit |
| API5 | Broken Function Level Authorization | Admin endpoint protected only by a hidden menu |
| API6 | Unrestricted Access to Sensitive Business Flows | No limit on "send verification mail" or "create label" |
| API7 | Server Side Request Forgery | Webhook or logo URL fetched without an allowlist |
| API8 | Security Misconfiguration | Detailed errors, open CORS, no TLS between services |
| API9 | Improper Inventory Management | Old `/api/v1` live without the new checks. Test endpoints in production |
| API10 | Unsafe Consumption of APIs | Partner response used in SQL, HTML or a path without validation |

## ASVS 5.0 chapters

| Chapter | Topic in this stack | Requirements L1/L2/L3 |
|---------|---------------------|-----------------------|
| V1 | Encoding and sanitization, injection, deserialization | 8/19/3 |
| V2 | Validation and business logic, anti-automation | 4/7/2 |
| V3 | Web frontend: CSP, cookies, headers | 8/11/12 |
| V4 | API and web service | 2/8/6 |
| V5 | File handling | 4/5/4 |
| V6 | Authentication | 13/22/12 |
| V7 | Session management | 6/12/1 |
| V8 | Authorization | 4/3/6 |
| V9 | Self-contained tokens (JWT) | 4/3/0 |
| V10 | OAuth and OIDC | 5/24/7 |
| V11 | Cryptography | 3/11/10 |
| V12 | Secure communication | 3/6/3 |
| V13 | Configuration, secrets | 1/12/8 |
| V14 | Data protection | 2/7/4 |
| V15 | Secure coding and architecture | 3/10/8 |
| V16 | Security logging and error handling | 0/16/1 |
| V17 | WebRTC (not used) | 0/7/5 |

## CWE Top 25:2025

| # | CWE | Code mistake in this stack |
|---|-----|----------------------------|
| 1 | 79 XSS | `bypassSecurityTrustHtml`, `Html.Raw`, `$sce.trustAsHtml` |
| 2 | 89 SQL injection | Raw SQL or Dapper with interpolation |
| 3 | 352 CSRF | Cookie API without antiforgery. `DisableAntiforgery()` |
| 4 | 862 Missing Authorization | Endpoint without a policy and no fallback policy |
| 5 | 787 Out-of-bounds Write | Not in safe C# |
| 6 | 22 Path Traversal | `Path.Combine(root, request.FileName)` |
| 7 | 416 Use After Free | Not in safe C# |
| 8 | 125 Out-of-bounds Read | Not in safe C# |
| 9 | 78 OS Command Injection | `Process.Start("sh", $"-c ... {name}")` |
| 10 | 94 Code Injection | Roslyn scripting on user input. AngularJS expression injection |
| 11 | 120 Classic Buffer Overflow | Not in safe C# |
| 12 | 434 Unrestricted File Upload | Client extension trusted, file under `wwwroot` |
| 13 | 476 NULL Pointer Dereference | Crash that leaks details or stops a consumer |
| 14 | 121 Stack-based Buffer Overflow | Not in safe C# |
| 15 | 502 Deserialization of Untrusted Data | `TypeNameHandling.Auto` on messages. BinaryFormatter |
| 16 | 122 Heap-based Buffer Overflow | Not in safe C# |
| 17 | 863 Incorrect Authorization | Policy checks the role, not the tenant |
| 18 | 20 Improper Input Validation | No max length or range. Negative quantities |
| 19 | 284 Improper Access Control | Parent of 862, 863 and 639 |
| 20 | 200 Sensitive Information Exposure | Entity in the response. Stack trace in an error |
| 21 | 306 Missing Authentication for Critical Function | Admin or state-changing endpoint without authentication |
| 22 | 918 SSRF | `HttpClient` or `GoToAsync` with a user URL |
| 23 | 77 Command Injection | Same as 78 |
| 24 | 639 Authorization Bypass Through User-Controlled Key | `?customerId=` from the query instead of the claims |
| 25 | 770 Allocation Without Limits | No request limit, no page cap, unbounded `ToListAsync()` |

CWE-798 (hard-coded credentials) left the list because of the method change. In agent pull requests it is still 99.6% of the critical findings.

## OWASP LLM Top 10:2026

| 2026 | 2025 | Name |
|------|------|------|
| LLM01 | LLM01 | Prompt Injection |
| LLM02 | LLM02 | Sensitive Information Disclosure |
| LLM03 | LLM06 | Excessive Agency |
| LLM04 | LLM03 | Supply Chain |
| LLM05 | LLM04 | Data and Model Poisoning |
| LLM06 | LLM10 | Unbounded Consumption |
| LLM07 | LLM09 | Misinformation |
| LLM08 | LLM07 | Hidden Context Exposure (was System Prompt Leakage) |
| LLM09 | LLM08 | Vector and Embedding Weaknesses |
| LLM10 | LLM05 | Improper Output Handling |
