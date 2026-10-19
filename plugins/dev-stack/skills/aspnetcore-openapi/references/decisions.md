# OpenAPI and versioning decisions

The team decisions that this skill owns. The order is the order in which a project meets them. The recommendation is the catalog default. Ask the human before you apply it (stack rule 4). API-04 (error contract) and API-11 (JSON settings) are in `dev-stack:aspnetcore`. They change the document too, so ask them in the same message when the work needs them.

## Contents

- API-09. OpenAPI generator and UI
- API-10. Versioning scheme
- API-18. API version format and deprecation window
- API-19. Breaking-change gate

## API-09. OpenAPI generator and UI

- Question: Which library makes the OpenAPI document, which UI shows it, where is it reachable, and is the document committed to the repository?
- Ask when: the first request for API documentation, a UI for developers, or a client generator.
- Options: `Microsoft.AspNetCore.OpenApi` (built in, OpenAPI 3.1) with Scalar. The same with Swagger UI. Swashbuckle.AspNetCore 10. NSwag. Each with or without the build-time document in the repository.
- Recommended: `Microsoft.AspNetCore.OpenApi` with Scalar, both mapped in Development only, and the build-time document (`Microsoft.Extensions.ApiDescription.Server`) committed in a tracked folder. It is first-party, works with `Asp.Versioning.OpenApi`, and a pull request shows each contract change.
- Choose another option when: the project already uses Swashbuckle or NSwag (keep it). The team knows Swagger UI and does not want a new UI (Swagger UI has the same function). A partner must read the document in Production (protect it with `RequireAuthorization`, or send the build-time file).
- Depends on: API-01.
- Revisit when: .NET 11 (Microsoft.OpenApi 3.x), or the UI package changes its license.
- Check: tool (OAS-UI-UNGUARDED, ASP-WITH-OPENAPI) and judge (the document is committed).
- Detect: DET-API-09

## API-10. Versioning scheme

- Question: Does this API have versions, and if so, where is the version: in the URL segment, a query string, or a header?
- Ask when: the first endpoint of an API that an external system or more than one team will call.
- Options: No versioning (one internal consumer that deploys with the API). URL segment (`/api/v1/orders`) with Asp.Versioning. Query string (`?api-version=1.0`). Header (`api-version`). Media type.
- Recommended: No versioning for an internal API with one consumer. A URL segment with Asp.Versioning for each external or multi-consumer API, from the first endpoint. A path segment added later breaks each client.
- Choose another option when: the organization has an API standard with a query string or header (Azure-style `api-version`). A gateway already routes on a header.
- Depends on: API-09, API-17.
- Revisit when: a second consumer appears for an unversioned API.
- Check: tool (OAS-URL-ASSUME-DEFAULT, AV0029, AV0030) and judge.
- Detect: DET-API-10

## API-18. API version format and deprecation window

- Question: Which version format do we use, and how long does a deprecated version stay available?
- Ask when: the first versioned API (API-10), or the first deprecation.
- Options: Major versions (`v1`, `v2`) with group name format `'v'VVV`. Major and minor (`v1.1`). Date versions (`2026-10-01`). Windows: 3, 6, or 12 months after the deprecation.
- Recommended: Major versions only (`v1`). A deprecated version stays at least 6 months, and 12 months when an external partner uses it. The sunset date and a policy link go into the `Sunset` and `Link` headers. A new field does not need a new version, so a minor number adds no information.
- Choose another option when: a contract with a partner names another window. The organization uses date versions.
- Depends on: API-10.
- Revisit when: a partner asks for a longer window, or the team supports more than two versions at the same time.
- Check: judge (the sunset date is in the future and at least the window after the deprecation).
- Detect: none

## API-19. Breaking-change gate

- Question: Does CI stop a pull request that breaks the API contract?
- Ask when: the build-time document is committed (API-09), or the first external consumer.
- Options: oasdiff `breaking --fail-on ERR` as a gate. oasdiff as a report in the pull request. A review only. No check.
- Recommended: oasdiff in CI against the document on `main`. A gate on `ERR` for each API with an external consumer, a report for internal APIs. A reviewer does not see a renamed field in a large diff, and the tool does.
- Choose another option when: the API has no committed document yet (first make API-09 true). The organization already runs another diff tool with the same function.
- Depends on: API-09, API-10.
- Revisit when: the gate stops a change on purpose too often (the API needs a new major version), or a breaking change reached a client.
- Check: judge (the pipeline runs the check).
- Detect: none
