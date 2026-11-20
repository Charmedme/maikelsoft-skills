# Sources

Checked on 2026-11-19. The facts of this skill come from these primary sources. Look them up again when `metadata.verified` in `SKILL.md` is older than 6 months, and before you use a draft standard.

## Contents

- Standards
- Guidelines
- Tools
- Facts that are not verified

## Standards

| Item | Status on 2026-11-19 | Source |
|------|----------------------|--------|
| RFC 9457 Problem Details | Proposed Standard, 2023-08. Obsoletes RFC 7807 | [RFC 9457](https://www.rfc-editor.org/rfc/rfc9457) |
| RFC 9110 HTTP Semantics | Internet Standard, 2022-06. Section 13: conditional requests | [RFC 9110](https://www.rfc-editor.org/rfc/rfc9110) |
| RFC 6585 (428, 429) | Proposed Standard, 2012-04 | [RFC 6585](https://www.rfc-editor.org/rfc/rfc6585) |
| RFC 10008 `QUERY` method | Proposed Standard, 2026-06 | [RFC 10008](https://www.rfc-editor.org/rfc/rfc10008) |
| RFC 9745 `Deprecation` header | Proposed Standard, 2025-03 | [RFC 9745](https://www.rfc-editor.org/rfc/rfc9745) |
| RFC 8594 `Sunset` header | Informational, 2019-05 | [RFC 8594](https://www.rfc-editor.org/rfc/rfc8594) |
| RFC 8288 Web Linking | Proposed Standard, 2017-10 | [RFC 8288](https://www.rfc-editor.org/rfc/rfc8288) |
| RFC 7396 JSON Merge Patch | Proposed Standard, 2014-10 | [RFC 7396](https://www.rfc-editor.org/rfc/rfc7396) |
| RFC 6902 JSON Patch | Proposed Standard, 2013-04 | [RFC 6902](https://www.rfc-editor.org/rfc/rfc6902) |
| RFC 9651 Structured Fields | Proposed Standard, 2024-09. Obsoletes RFC 8941 | [RFC 9651](https://www.rfc-editor.org/rfc/rfc9651) |
| `Idempotency-Key` header | IETF draft -08, 2026-10-22, expires 2027-04-25. Not an RFC | [draft-ietf-httpapi-idempotency-key-header](https://datatracker.ietf.org/doc/draft-ietf-httpapi-idempotency-key-header/) |
| `RateLimit` and `RateLimit-Policy` | IETF draft -11, 2026-05-23, expires 2026-11-24. Not an RFC | [draft-ietf-httpapi-ratelimit-headers](https://datatracker.ietf.org/doc/draft-ietf-httpapi-ratelimit-headers/) |
| OpenAPI | 3.2.1 (2026-09-10), 3.2.0 (2025-09-19), 3.1.2 | [OpenAPI releases](https://github.com/OAI/OpenAPI-Specification/releases) |
| Standard Webhooks | 1.3.0 (2026-11-17) | [Standard Webhooks repository](https://github.com/standard-webhooks/standard-webhooks) |

## Guidelines

The three guidelines do not agree on errors, versions, and case. This skill takes a default from them and gives the other choices as options in `decisions.md`.

- [Microsoft REST API Guidelines](https://github.com/microsoft/api-guidelines): merge patch, `If-Match`, 202 with `Operation-Location`, own error envelope.
- [Zalando RESTful API Guidelines](https://github.com/zalando/restful-api-guidelines): RFC 9457 problem JSON (rule 176), cursor pagination (rule 160), verb-free URLs.
- [Google API Improvement Proposals](https://google.aip.dev/): AIP-136 custom methods, AIP-151 long-running operations, AIP-158 page tokens, AIP-161 field masks, AIP-233 and AIP-234 bulk methods.
- [OWASP API Security Top 10 2023](https://owasp.org/API-Security/): API1 object-level authorization, API4 resource consumption.

## Tools

| Tool | Version on 2026-11-19 | Note |
|------|-----------------------|------|
| Spectral CLI (`@stoplight/spectral-cli`) | 6.17.1 | OpenAPI 3.2 since 2026-03, with `@stoplight/spectral-formats` 1.9.0. False positives on 3.2 examples: [issue 3100](https://github.com/stoplightio/spectral/issues/3100), [issue 3110](https://github.com/stoplightio/spectral/issues/3110) |
| `@stoplight/spectral-owasp-ruleset` | 2.0.1 (last release 2024-03) | OWASP API Top 10 checks |
| Redocly CLI (`@redocly/cli`) | 2.82.0 | Lint and bundle, OpenAPI 3.2 |
| oasdiff | owned by `dev-stack:aspnetcore-openapi` | Breaking-change gate |

## Mistakes that this skill stops

The research of 2026-11-19 ran the orders contract and a search contract on two models. The answers had these mistakes:

- the old `RateLimit-*` trio, and `Deprecation: 'true'`
- `openapi: 3.0.3` with `nullable`
- `POST /search` with no word on safety
- a webhook HMAC over the body only

 Reports of AI app builders found APIs with no object-level check, a page size with no maximum, and mass assignment of `role`. A study of 600 endpoints ([arXiv 2605.14312](https://arxiv.org/abs/2605.14312)) lists the contract smells that confuse an agent.

## Facts that are not verified

- ASP.NET Core 10 or 11 support for `QUERY` in routing and in the OpenAPI generator.
- Support for OpenAPI 3.2 in client generators (Kiota, NSwag, openapi-generator).
- The Standard Webhooks header names in version 1.3.0. The research checked only the release date.
- A report of an AI agent that cancelled the booking of another customer through an endpoint with no object-level check. Only a secondary article reports it.
