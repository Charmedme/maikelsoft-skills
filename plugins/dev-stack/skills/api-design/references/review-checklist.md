# Review checklist

Each check is `tool (<rule ID>)` when a tool finds it, or `judge` when a reviewer must decide. Run the tools first (`SKILL.md`, section Check). In a review, cite the rule number of `SKILL.md` or the rule ID.

## Contents

- Resources and methods
- Errors and status codes
- Retries and concurrency
- Lists
- Partial updates and long work
- Versions, deprecation, and rate limits
- OpenAPI file and webhooks
- Security
- Severity
- Do not flag

## Resources and methods

| # | Check | Type |
|---|-------|------|
| 1 | Paths are plural nouns in kebab-case. No verb in a path (`/getOrders`, `/orders/delete`). | tool (APD-VERB-PATH), judge |
| 2 | An action that is not CRUD is `POST /{resource}/{id}/{action}` (or `:action` when APD-01 says so). | judge |
| 3 | Each method has its RFC 9110 meaning. No GET that changes data, no POST for a plain read. | judge |
| 4 | The contract calls a search with a body safe and idempotent, or the search uses `QUERY` after a support check. | judge |
| 5 | Each operation has an `operationId`, a `summary`, each status code, and the error schema. | tool (lint) |

## Errors and status codes

| # | Check | Type |
|---|-------|------|
| 6 | Errors are `application/problem+json` (RFC 9457). No RFC 7807, no own envelope. | tool (APD-RFC7807), judge |
| 7 | Each problem has a stable `code`, and the contract lists the codes. | judge |
| 8 | No `200` with `success: false` or an error message. | judge |
| 9 | 201 with `Location` after a create, 422 for a business rule, 409 for a state conflict. | judge |

## Retries and concurrency

| # | Check | Type |
|---|-------|------|
| 10 | Each create is safe to retry: a client ID with PUT, a unique key, or `Idempotency-Key`. | judge |
| 11 | `Idempotency-Key` answers 400 (missing), 422 (other payload), and 409 (in progress), and the contract says how long a key lives. | judge |
| 12 | Each resource that more than one client changes has `ETag` on GET and `If-Match` on PUT, PATCH, and DELETE. | judge |
| 13 | 412 and 428 are in the contract, with the problem schema. | judge |

## Lists

| # | Check | Type |
|---|-------|------|
| 14 | A large or changing list, an export, or a feed uses a cursor, not `page` and `offset`. | tool (APD-OFFSET-PARAM), judge |
| 15 | Each `limit` has a `maximum`. Each array has a `maxItems`. A larger request gets 400. | tool (lint with the OWASP ruleset), judge |
| 16 | The sort order is unique and stable. The server binds the cursor to the filter. | judge |

## Partial updates and long work

| # | Check | Type |
|---|-------|------|
| 17 | PATCH uses `application/merge-patch+json`. JSON Patch only for array items. No PUT with the full object to change one field. | judge |
| 18 | A field that clients can clear with merge patch allows `null`. | judge |
| 19 | Work longer than a few seconds returns 202 and `Location` of a status resource. | judge |
| 20 | A bulk operation has a maximum item count and a result for each item. | judge |

## Versions, deprecation, and rate limits

| # | Check | Type |
|---|-------|------|
| 21 | No removed field, new required field, or changed type in a published version. | tool (oasdiff, `dev-stack:aspnetcore-openapi`) |
| 22 | A deprecated version sends `Deprecation: @<seconds>`, `Sunset`, and `Link` with `rel="deprecation"`. No `Deprecation: true`, no `Warning: 299`. | judge |
| 23 | A rate limit answers 429 with `Retry-After`. | tool (lint with the OWASP ruleset), judge |
| 24 | No `X-RateLimit-*` and no `RateLimit-Limit`, `RateLimit-Remaining`, `RateLimit-Reset`. Draft -11 fields say "draft". | tool (APD-RATELIMIT-OLD) |

## OpenAPI file and webhooks

| # | Check | Type |
|---|-------|------|
| 25 | A new contract is OpenAPI 3.1 or 3.2. | tool (APD-OAS30) |
| 26 | A 3.1 or 3.2 file uses `type: [string, "null"]`, not `nullable: true`. | tool (APD-NULLABLE) |
| 27 | The lint step runs in CI and passes. | tool (lint) |
| 28 | Webhooks are under `webhooks:` and have an ID, a timestamp, and a signature over both and the body. | judge |
| 29 | The contract names each draft standard that it uses (Idempotency-Key, RateLimit). | judge |

## Security

These checks name the risk. The rules are in `dev-stack:secure-coding`.

| # | Check | Type |
|---|-------|------|
| 30 | Each operation has a security scheme, or a reason why it is public. | tool (lint with the OWASP ruleset), judge |
| 31 | Each method of a resource, also each action (cancel, approve), has the object-level check of the GET (BOLA, OWASP API1:2023). | judge |
| 32 | A request schema does not accept fields that the server owns (`role`, `ownerId`, `createdAt`). | judge |
| 33 | A webhook URL from a customer goes through the SSRF rule. | judge |

## Severity

- Blocking: no object-level check on a method or an action. A list with no maximum that returns data of other users. A request schema that accepts `role`.
- Major:
  - a 200 with an error flag, or no retry safety on a POST with an external side effect
  - last write wins on a shared resource, or offset paging on an export
  - an old rate-limit or deprecation header form, or OpenAPI 3.0 in a new contract
- Minor: a verb in a path of an internal API, a missing `summary`, a missing `code` list.

## Do not flag

- `nullable: true` in an OpenAPI 3.0 file of an existing contract.
- Offset paging on a small, stable set with page numbers in the UI (APD-03).
- `POST /.../search` that the contract documents as safe and idempotent.
- An own error envelope when APD-02 says that the API matches a published contract.
