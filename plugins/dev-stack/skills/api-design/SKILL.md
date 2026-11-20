---
name: api-design
description: HTTP API contract design, independent of the framework - resources and URLs, methods (also QUERY, RFC 10008), status codes, RFC 9457 problem details with a stable code, ETag, If-Match, 412 and 428, the Idempotency-Key header, cursor pagination, JSON Merge Patch, 202 with a status resource, deprecation headers (RFC 9745, RFC 8594), 429 with Retry-After, signed webhooks, and a design-first OpenAPI 3.1 or 3.2 file with a lint step. Use when the user designs, writes, or reviews an API contract, an OpenAPI or Swagger YAML file, endpoint URLs, error responses, paging, retries, partial updates, long-running jobs, rate-limit headers, webhooks, or the end of an API version. Not for the ASP.NET Core wiring of endpoints, ProblemDetails, or the rate limiter (dev-stack:aspnetcore), the generated OpenAPI document, Asp.Versioning, or oasdiff (dev-stack:aspnetcore-openapi), authentication, authorization, or BOLA (dev-stack:secure-coding), or MCP servers (dev-stack:dotnet-mcp).
metadata:
  verified: "2026-11-19"
  versions: "RFC 9110, RFC 9457, RFC 10008 (QUERY), RFC 9745, RFC 8594, RFC 8288, RFC 7396, RFC 6585, Idempotency-Key draft -08, RateLimit draft -11, OpenAPI 3.2.1 and 3.1.2, Spectral CLI 6.17.1, Redocly CLI 2.82.0, Standard Webhooks 1.3.0"
---

# API design

The HTTP contract of an API, before and apart from the code: what a client sends, what it gets back, and what it can trust. The framework wiring is in `dev-stack:aspnetcore`. The generated OpenAPI document, Asp.Versioning, and the breaking-change gate are in `dev-stack:aspnetcore-openapi`. Authentication, authorization, object-level access (BOLA), and mass assignment are in `dev-stack:secure-coding`.

## First

1. Read `references/stack-rules.md`.
2. Find the contract. Look for `openapi*.yaml`, `openapi*.json`, or an `openapi:` line in a YAML file. Read the `openapi:` version.
   - A new contract: write OpenAPI 3.1 or 3.2 (APD-08).
   - An existing 3.0 contract: keep 3.0 (stack rule 3). Use `nullable: true` in that file, not the 3.1 form.
   - No contract file, only code: the contract is the code. Apply the rules to the endpoints, and give the OpenAPI changes to `dev-stack:aspnetcore-openapi`.
3. Find the clients: one internal front end, other teams, partners, or agents (MCP, tool calls). An external client needs decisions APD-01 to APD-04 before the first endpoint.
4. Find the lint tool: a `.spectral.yaml`, `.spectral.json`, `redocly.yaml`, or a lint step in CI. When none exists, the tool is decision APD-08.

Done when you can name the OpenAPI version, the clients, and the lint tool (or "none").

## Defaults for new code

Ask the human before you apply a default that has no accepted decision (stack rule 4). The full entries are in `references/decisions.md`.

| Area | Default | ID |
|------|---------|----|
| URLs | Lowercase plural nouns, kebab-case, actions as `POST /api/orders/{id}/cancel` | APD-01 |
| Errors | RFC 9457 `application/problem+json` on each 4xx and 5xx, with a stable `code` and `traceId` | APD-02 |
| Paging | Offset with a maximum page size for a small set. Cursor for a large or changing set, an export, or a feed | APD-03 |
| Retries of POST | Natural idempotency (client ID or unique key). `Idempotency-Key` only for external side effects | APD-04 |
| Partial update | `PATCH` with `application/merge-patch+json` (RFC 7396) | APD-05 |
| Concurrency | `ETag` on GET, `If-Match` on PUT, PATCH, DELETE, 412 on a mismatch, 428 when it is missing | APD-06 |
| Long work | `202 Accepted`, `Location` of a status resource | APD-07 |
| Contract | Design-first OpenAPI 3.1 file in the repository, with a lint step in CI | APD-08 |
| Rate limit | 429 with `Retry-After`. No draft `RateLimit` fields | APD-09 |
| Webhooks | Standard Webhooks headers, HMAC-SHA256 with a timestamp | APD-10 |
| Complex search | `POST /orders/search`, documented as safe. `QUERY` when each part of the stack supports it | APD-11 |
| Version end | `Deprecation` (RFC 9745) and `Sunset` (RFC 8594) headers, plus a `Link` with `rel="deprecation"` | APD-12 |

The version scheme (API-10) and the deprecation window (API-18) are decisions of `dev-stack:aspnetcore-openapi`. This skill gives only the headers that tell a client about them.

## Existing code

The defects of stack rule 3 include these in a contract. A client that the team does not control depends on each one, so a fix is a breaking change. Name it under "Open points" with the fix, and do not change it without a new version.

| Defect | Rule |
|--------|------|
| `200 OK` with `{ "success": false }` | 6 |
| An action endpoint (cancel, approve) without the object-level check of the GET | 22 |
| A `limit` or an array with no `maximum` | 15 |
| PUT or PATCH with no `If-Match`: the last write wins | 11 |
| A webhook signature over the body only: a replay works | 26 |

A new endpoint that copies one of these patterns is a defect of the change. Fix it in the new endpoint.

## Rules

### Resources and methods

1. Name resources with plural nouns: `/orders`, `/orders/{id}/line-items`. Put an action that is not create, read, update, or delete on a sub-resource with POST: `POST /orders/{id}/cancel`. A verb in the path (`/getOrders`, `/orders/delete`) hides the method, and clients and agents guess wrong.
2. Use each method for its meaning (RFC 9110). GET reads. PUT creates or replaces at a known URL. PATCH changes part of a resource. DELETE removes. POST creates under a collection or runs an action. GET, PUT, DELETE, and QUERY are idempotent. POST and PATCH are not.
3. Keep simple filters as query parameters on GET. For a search with a large or nested filter, use `POST /orders/search`. Write "safe and idempotent" in its description. `QUERY` (RFC 10008, OpenAPI 3.2 `query:`) is the correct method. Use it only when the server, the proxies, the client generator, and the lint tool support it (APD-11).
4. Give each operation an `operationId`, a `summary`, each status code that it can return, and the error schema. An agent that calls the API reads only the contract.

### Status codes and errors

5. Return errors as `application/problem+json` (RFC 9457, which obsoletes RFC 7807). Each problem has `type`, `title`, `status`, `detail`, a stable `code` extension (for example `order.not_found`), and `traceId`. Clients switch on `code` and never parse `title` or `detail`.
6. Send the status that matches the result:
    - 201 with `Location` after a create, 204 with no body.
    - 400 for a bad request, 401 and 403 for auth, 404 for an unknown resource.
    - 409 for a state conflict, 412 and 428 for preconditions.
    - 422 for a valid request that breaks a business rule, 429 for a rate limit.

    A 200 with an error flag makes each cache, proxy, retry policy, and monitor count a failure as a success.
7. Give each problem `type` a stable URI and one page that explains it. Keep the `code` values in one list in the contract.

### Retries and concurrency

8. Make each create safe to retry. Prefer natural idempotency: the client sends the ID with `PUT /orders/{id}`, or a unique business key rejects the duplicate.
9. Accept an `Idempotency-Key` header (IETF draft -08, not an RFC) on a POST with an external side effect. Examples are a payment, a mail, or a call to a partner. Answer 400 when the key is missing. Answer 422 when the key comes again with another payload, and 409 while the first request runs. Return the stored response for a repeat. Publish how long the API keeps a key.
10. Send an `ETag` on each GET of a resource that clients change. Use a strong ETag from the row version.
11. Require `If-Match` on PUT, PATCH, and DELETE of that resource. Answer 412 when the ETag does not match, and 428 when the header is missing.
12. Document both 412 and 428 in the contract, with the problem schema. Without them, two users who edit the same order lose a change and see no error.

### Lists

13. Use an opaque cursor for a large or changing set (`?cursor=...&limit=50`). Return the next cursor in the body (`next`) or in a `Link: <...>; rel="next"` header (RFC 8288). Offset paging skips or repeats rows when the set changes, and a deep offset gets slow.
14. Bind the cursor to the filter and the sort order. A cursor with another filter gets a 400 problem.
15. Give each `limit` parameter a `maximum`, and each array in a response or request a `maxItems`. Answer 400 with a problem when a client asks for more. A list with no maximum returns the whole table (OWASP API4:2023).
16. Sort by a unique, stable key, or add the ID as the last sort key.

### Partial updates and long work

17. Use `PATCH` with `application/merge-patch+json` (RFC 7396) as the default. In merge patch, `null` removes a field, so a schema field that clients can clear must allow `null`. Use JSON Patch (`application/json-patch+json`, RFC 6902) only for changes to array items.
18. Run work that takes more than a few seconds outside the request. Answer `202 Accepted` with `Location` of a status resource. The status resource returns the state, `Retry-After` while it runs, and a link to the result at the end.
19. Give a bulk operation a maximum item count. Return one result for each item, in the body or in the status resource of a 202.

### Versions, deprecation, and rate limits

20. Add fields, never remove or rename them, in one version. Clients ignore unknown fields. A removed field, a new required field, or a changed type is a breaking change. It needs a new version (API-10 in `dev-stack:aspnetcore-openapi`).
21. Tell clients about the end of a version in the response, not only in a changelog. Send `Deprecation: @<unix seconds>` (RFC 9745, a structured date), `Sunset: <HTTP date>` (RFC 8594), and `Link: <https://...>; rel="deprecation"`.
22. Apply the same object-level check on each method of a resource, also on actions. A cancel endpoint that skips the owner check of the GET lets a user cancel the order of another user. The check itself is in `dev-stack:secure-coding`.
23. Answer a rate limit with 429 and `Retry-After`. The `RateLimit` and `RateLimit-Policy` fields are IETF draft -11, not an RFC. Add them only when the team accepts a draft, and write "draft" in the contract. Do not write `X-RateLimit-*` or `RateLimit-Limit`, `RateLimit-Remaining`, `RateLimit-Reset`: they are old forms.

### OpenAPI file

24. Write the contract first, lint it, then build. Use OpenAPI 3.1 (or 3.2, APD-08), with JSON Schema 2020-12: `type: [string, "null"]`, not `nullable: true`.
25. Describe webhooks under the top-level `webhooks:` key (OpenAPI 3.1 and later).
26. Sign each webhook. Use the Standard Webhooks headers `webhook-id`, `webhook-timestamp`, and `webhook-signature`, with HMAC-SHA256 over `id.timestamp.body`. The receiver rejects a timestamp outside a tolerance window (for example 5 minutes) and drops a repeated `webhook-id`. Support two secrets during a rotation. A signature over the body only lets an attacker replay an old event.

Detail and YAML examples for each rule: `references/contract.md`.

## Never write

| Wrong | Correct |
|-------|---------|
| "Errors follow RFC 7807", or `{ "error": "Not found" }` | RFC 9457 `application/problem+json` with `type`, `title`, `status`, `detail`, `code` |
| `200` with `{ "success": false, "message": "..." }` | The 4xx or 5xx status with a problem body |
| `/getOrders`, `/createOrder`, `/orders/{id}/delete` | `GET /orders`, `POST /orders`, `DELETE /orders/{id}` |
| `?page=500&size=50` on a list of millions of rows or an export | `?cursor=...&limit=50` with `next` or `Link: rel="next"` |
| `limit` with no `maximum`, an array with no `maxItems` | `maximum: 100`, `maxItems: 1000`, 400 above it |
| `POST /orders` with no retry safety for a mobile client | Client ID with PUT, or `Idempotency-Key` with 400, 422, 409 |
| `PUT` or `PATCH` with no `If-Match` | `ETag` on GET, `If-Match`, 412, 428 |
| `PUT /orders/{id}` with the full object to change one field | `PATCH` with `application/merge-patch+json` |
| JSON Patch to change one field | Merge patch. JSON Patch only for array items |
| A request that runs a 5-minute export | `202 Accepted` and `Location: /exports/{id}` |
| `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` | `429` with `Retry-After` |
| `RateLimit-Limit`, `RateLimit-Remaining`, `RateLimit-Reset` | Draft -11 `RateLimit: "default";r=50;t=30` and `RateLimit-Policy: "default";q=100;w=60`, marked draft |
| `Deprecation: true`, or `Warning: 299` | `Deprecation: @1767225600`, `Sunset: Thu, 31 Dec 2026 23:59:59 GMT`, `Link: <...>; rel="deprecation"` |
| `openapi: 3.0.3` with `nullable: true` for a new contract | `openapi: 3.1.2` with `type: [string, "null"]` |
| `POST /products/search` with no word on safety | The same path, documented as safe and idempotent, or `QUERY` (APD-11) |
| A long nested filter in the GET query string | `POST .../search` or `QUERY` with a body |
| `X-Signature: HMAC(body)` on a webhook | `webhook-id`, `webhook-timestamp`, `webhook-signature` over `id.timestamp.body` |
| An owner check on `GET /bookings/{id}` but not on `POST /bookings/{id}/cancel` | The same object-level check on each method (`dev-stack:secure-coding`) |
| A request schema that accepts `role`, `ownerId`, or `createdAt` from the client | A separate request schema with only the fields that the client sets |

## Check

1. Lint the contract with the tool of APD-08. This command lints a contract with Spectral:

   ```
   npx @stoplight/spectral-cli@6.17.1 lint openapi.yaml --fail-severity warn
   ```

   This command lints it with Redocly:

   ```
   npx @redocly/cli@2.82.0 lint openapi.yaml
   ```

   Pass: 0 errors and 0 warnings. A new tool is a new package (stack rule 5). Spectral needs `@stoplight/spectral-formats` 1.9.0 or later for OpenAPI 3.2, and it gives false positives on 3.2 `dataValue` and `serializedValue` examples. Name each such warning in the report.

2. Run the rules of this skill:

   ```
   python3 <skill folder>/scripts/stackcheck.py --root <repo>
   ```

   Pass: 0 errors. For each warning, fix it or add `# stackcheck: ignore <RULE-ID> <reason>` on the line.

3. For a change to a published contract, run the breaking-change gate of `dev-stack:aspnetcore-openapi` (oasdiff). Pass: no breaking change, or a new version.

## Review checklist

The full list is in `references/review-checklist.md`.

1. Errors use RFC 9457 problem details with a stable `code`. No RFC 7807, no own envelope, no 200 with an error flag. tool (APD-RFC7807), judge
2. Paths are nouns, actions are `POST .../{action}`. tool (APD-VERB-PATH), judge
3. Each operation has an `operationId`, a summary, each status code, and the error schema. tool (lint)
4. Large or changing lists use a cursor. Each `limit` and each array has a maximum. tool (APD-OFFSET-PARAM), judge
5. Each create is safe to retry. `Idempotency-Key` answers 400, 422, and 409 as the draft says. judge
6. Each changeable resource has `ETag`, `If-Match`, 412, and 428. judge
7. PATCH uses merge patch, and fields that clients clear allow `null`. judge
8. Long work returns 202 with a status resource. judge
9. Rate limits return 429 with `Retry-After`. No `X-RateLimit-*` or old `RateLimit-*` trio. Draft fields say "draft". tool (APD-RATELIMIT-OLD), judge
10. Version end uses `Deprecation: @<seconds>`, `Sunset`, and `Link rel="deprecation"`. judge
11. A new contract is OpenAPI 3.1 or 3.2 with no `nullable`. tool (APD-OAS30, APD-NULLABLE)
12. The contract calls a complex search safe and idempotent, or the search uses `QUERY` with support checked. judge
13. Webhooks have a signature with a timestamp and an ID. judge
14. Each method of a resource, also each action, has the same object-level check. Request schemas do not accept server-owned fields. Severity: blocking. judge, then `dev-stack:secure-coding`

## Report

End with the report of stack rule 8 (`references/stack-rules.md`). Also give:

- Under "Changes": each new or changed operation, with its method, path, and status codes.
- Under "Decisions": each APD decision that the change uses. Mark each one accepted or assumed.
- Under "Open points": each breaking change, and each draft standard (Idempotency-Key, RateLimit) that the contract uses.

## References

- `references/decisions.md`: read before new code needs an APD decision. It has the question, the options, and the recommendation.
- `references/contract.md`: read when you write or change a contract. It has a YAML example for each rule.
- `references/review-checklist.md`: read for a full review.
- `references/sources.md`: the primary sources, the status of each standard, and the facts that are not verified.
- `references/stack-rules.md` and `references/language.md`: the common rules and the output language.
- `scripts/stackcheck.py`: the rule check of this skill.
