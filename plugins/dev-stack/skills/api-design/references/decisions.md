# API contract decisions

The team decisions that an HTTP contract needs. The order is the order in which a new API meets them. The recommendation is the catalog default. Ask the human before you apply it (stack rule 4).

Batch the questions: ask APD-01, APD-02, APD-08, and APD-03 together as one "first contract" question. Ask the others one by one when the work needs them.

APD-01 to APD-04 came from `dev-stack:aspnetcore` (API-17, API-04, API-12, API-13). A project that accepted the old ID keeps that answer. Write both IDs in the decision index.

## Contents

- APD-01. URL style
- APD-02. Error contract
- APD-08. Contract source, OpenAPI version, and lint tool
- APD-03. Pagination style
- APD-04. Idempotency for POST
- APD-05. Partial update format
- APD-06. Concurrency control
- APD-07. Long-running work
- APD-09. Rate-limit headers
- APD-10. Webhook signatures
- APD-11. Complex search
- APD-12. Deprecation signals

## APD-01. URL style

- Question: Do routes use lowercase plural nouns with kebab-case segments, for example `/api/orders/{id}/line-items`?
- Ask when: the first endpoint.
- Options: Lowercase plural nouns, kebab-case, `/api` prefix, actions as `POST /api/orders/{id}/cancel`. camelCase or PascalCase segments. RPC verbs (`/api/cancelOrder`). Google style custom methods (`POST /orders/{id}:cancel`).
- Recommended: Lowercase plural nouns with kebab-case. Nouns name the resources, and an action that is not CRUD becomes a sub-resource with POST.
- Choose another option when: the organization has an API style guide.
- Depends on: none.
- Revisit when: never.
- Check: tool (APD-VERB-PATH), judge.
- Detect: none.
- Moved from: API-17 in `dev-stack:aspnetcore`.

## APD-02. Error contract

- Question: Do all errors use RFC 9457 problem details, with a stable machine-readable `code`?
- Ask when: the first response that is not 2xx.
- Options: Problem details with an extension `code` and `traceId`. Problem details with the framework defaults only. An own error envelope (for example the Azure `error` object with `x-ms-error-code`).
- Recommended: Problem details everywhere, with `code` (for example `order.not_found`) on each 4xx and `traceId`. Clients act on the code and do not parse text.
- Choose another option when: the API must match an error contract that is already published.
- Depends on: none.
- Revisit when: never.
- Check: tool (APD-RFC7807), judge.
- Detect: none.
- Moved from: API-04 in `dev-stack:aspnetcore`. The wiring (`AddProblemDetails`, `CustomizeProblemDetails`) stays there.

## APD-08. Contract source, OpenAPI version, and lint tool

- Question: Is the OpenAPI file the source of the contract (design-first), which OpenAPI version does it use, and which tool lints it?
- Ask when: the first contract, or the first external client.
- Options: Design-first: a hand-written file in the repository, linted in CI, then the code. Code-first: the framework generates the file (`dev-stack:aspnetcore-openapi`). OpenAPI 3.1 (3.1.2). OpenAPI 3.2 (3.2.1): adds `query:`, `additionalOperations`, `in: querystring`, and `itemSchema` for streams. Lint with Spectral CLI 6.17.1 (`spectral:oas`, optional `@stoplight/spectral-owasp-ruleset` 2.0.1, last release 2024-03), or with Redocly CLI 2.82.0 (`recommended` rules).
- Recommended: Design-first for an API with an external client. OpenAPI 3.1, because more tools read it. Spectral with `spectral:oas` and the OWASP ruleset, because the OWASP rules find a missing `maximum`, a missing 401 or 429, and a missing security scheme.
- Choose another option when: the API has one internal client and the team generates the file from code (code-first, with the same lint step). The contract needs `QUERY` or a stream schema (OpenAPI 3.2, after you check that each tool in the chain reads 3.2). The team already uses Redocly.
- Depends on: none.
- Revisit when: the client generators of the team read OpenAPI 3.2 (not verified on 2026-11-19).
- Check: tool (lint, APD-OAS30, APD-NULLABLE).
- Detect: none.

## APD-03. Pagination style

- Question: Do list endpoints page by offset (page number and size) or by cursor (continuation token)?
- Ask when: the first list endpoint.
- Options: Offset with a maximum size. Cursor (keyset) with an opaque token. Both: offset for UI tables, cursor for exports and feeds.
- Recommended: Offset with a hard maximum page size (for example 100) and a unique, stable sort order. It fits page-number UIs and is simple.
- Choose another option when: large or fast-growing tables, infinite scroll, sync or export endpoints (cursor).
- Depends on: APD-01.
- Revisit when: a list query passes about 100 ms on deep pages, or a table passes about 1 million rows.
- Check: tool (APD-OFFSET-PARAM), judge (a maximum page size exists).
- Detect: none.
- Moved from: API-12 in `dev-stack:aspnetcore`.

## APD-04. Idempotency for POST

- Question: Do create and action endpoints accept an idempotency key, so that a client can retry safely?
- Ask when: the first POST with a side effect outside the database (payment, mail, external call), or the first client with automatic retries.
- Options: Natural idempotency: the client sends the resource id and a unique constraint rejects duplicates. An `Idempotency-Key` header with a stored response per key (own filter and table). A library (for .NET: IdempotentAPI, last release 2024). None.
- Recommended: Natural idempotency (a client id or a unique business key). Add an `Idempotency-Key` header only on POSTs with external side effects. The IETF header is still a draft (-08 on 2026-10-22). It answers 400 for a missing key, 422 for a key with another payload, and 409 while the first request runs.
- Choose another option when: a public API with payment-like operations (a key on each POST).
- Depends on: APD-02.
- Revisit when: the IETF draft becomes an RFC.
- Check: judge.
- Detect: none.
- Moved from: API-13 in `dev-stack:aspnetcore`. The filter code stays there.

## APD-05. Partial update format

- Question: How does a client change some fields of a resource?
- Ask when: the first update endpoint.
- Options: `PATCH` with JSON Merge Patch (RFC 7396, `application/merge-patch+json`). `PATCH` with JSON Patch (RFC 6902, `application/json-patch+json`). `PUT` with the full resource. Field masks (Google AIP-134, `update_mask`).
- Recommended: Merge patch. It is the plain JSON of the changed fields, and `null` removes a field. Use JSON Patch only for changes to array items, and PUT only for a full replace.
- Choose another option when: the API follows Google AIP (field masks), or most changes are to array items (JSON Patch).
- Depends on: APD-01.
- Revisit when: never.
- Check: judge.
- Detect: none.

## APD-06. Concurrency control

- Question: How does the API stop two clients from overwriting each other?
- Ask when: the first PUT, PATCH, or DELETE of a resource that more than one user or process changes.
- Options: `ETag` and `If-Match`, 412 on a mismatch, 428 when `If-Match` is missing. `ETag` and `If-Match`, but the header is optional. A version field in the body. None: the last write wins.
- Recommended: A strong `ETag` from the row version, `If-Match` required on each change, 412 and 428 (RFC 9110 section 13, RFC 6585). The header works for each media type and each method, also DELETE.
- Choose another option when: the resource has one writer (an optional header is enough).
- Depends on: APD-02.
- Revisit when: never.
- Check: judge.
- Detect: none.

## APD-07. Long-running work

- Question: How does the API answer a request that takes more than a few seconds?
- Ask when: the first export, import, report, or call to a slow partner.
- Options: `202 Accepted` with `Location` of a status resource. `202` with `Operation-Location` (Azure style). Google AIP-151 operation resources. A webhook at the end. A long request.
- Recommended: `202` with `Location: /exports/{id}`. The status resource returns `status` (`running`, `succeeded`, `failed`), `Retry-After` while it runs, a problem on failure, and a link to the result at the end.
- Choose another option when: the organization follows Azure or Google guidelines. Add a webhook when clients cannot poll.
- Depends on: APD-02.
- Revisit when: never.
- Check: judge.
- Detect: none.

## APD-09. Rate-limit headers

- Question: Which headers tell a client about a rate limit?
- Ask when: the first rate limit on a public or partner endpoint. Where the limiter runs is SEC-11 (`dev-stack:secure-coding`) and API-14 (`dev-stack:aspnetcore`).
- Options: `429` with `Retry-After` only (RFC 6585, RFC 9110). Also the draft fields `RateLimit` and `RateLimit-Policy` (IETF draft -11, 2026-05-23, expires 2026-11-24). The old `X-RateLimit-*` headers.
- Recommended: 429 with `Retry-After` and a problem body. Both are standards, and each HTTP client library handles `Retry-After`.
- Choose another option when: clients need the remaining quota before a 429. Then add the draft fields, and write "draft -11" in the contract.
- Depends on: APD-02.
- Revisit when: the RateLimit draft becomes an RFC, or a new draft changes the field names.
- Check: tool (APD-RATELIMIT-OLD), judge.
- Detect: none.

## APD-10. Webhook signatures

- Question: How does a receiver know that a webhook is from us and not a replay?
- Ask when: the first webhook.
- Options: Standard Webhooks 1.3.0: `webhook-id`, `webhook-timestamp`, `webhook-signature`, HMAC-SHA256 over `id.timestamp.body`. An own HMAC header with a timestamp. HMAC over the body only. Mutual TLS.
- Recommended: Standard Webhooks. Libraries exist for many languages, and the ID and the timestamp stop a replay. The receiver rejects a timestamp outside a tolerance window and drops a repeated ID. Send at least once and retry with backoff.
- Choose another option when: the receivers already check an own format.
- Depends on: none.
- Revisit when: the Standard Webhooks spec changes its header names. The header names were not checked against version 1.3.0 (not verified).
- Check: judge.
- Detect: none.

## APD-11. Complex search

- Question: How does a client send a search with a large or nested filter?
- Ask when: a filter does not fit in simple query parameters.
- Options: GET with query parameters. `POST /orders/search`, documented as safe and idempotent. The `QUERY` method (RFC 10008, Proposed Standard 2026-06, OpenAPI 3.2 `query:`).
- Recommended: `POST /orders/search` with "safe and idempotent" in the description. `QUERY` is the correct method, but each part of the chain must support it. The parts are the server framework, the proxies, the client generator, and the lint tool.
- Choose another option when: you checked that each part supports `QUERY`. ASP.NET Core support for `QUERY` in routing and OpenAPI is not verified.
- Depends on: APD-08.
- Revisit when: the framework and the client generator support `QUERY`.
- Check: judge.
- Detect: none.

## APD-12. Deprecation signals

- Question: How does a client learn that an endpoint or a version will go away?
- Ask when: the first deprecated endpoint or version. The window is API-18 (`dev-stack:aspnetcore-openapi`).
- Options: `Deprecation` (RFC 9745) and `Sunset` (RFC 8594) headers with a `Link` of `rel="deprecation"`. `deprecated: true` in OpenAPI only. A changelog only.
- Recommended: The headers and `deprecated: true` in the contract. `Deprecation: @1767225600` is a structured date in Unix seconds. `Sunset` is an HTTP date. A client sees the headers on each call, also when nobody reads the changelog.
- Choose another option when: never for an external client.
- Depends on: APD-08.
- Revisit when: never.
- Check: judge.
- Detect: none.
