# Review checklist

Each check is `tool (<rule ID>)` when a tool finds it, or `judge` when a reviewer must decide. Run the tools first (`SKILL.md`, section Check). In a review, cite the rule number of `SKILL.md` or the rule ID.

## Contents

- Spec date and versions
- Feature check
- Registration and lifecycle
- Tool design
- Confirmation and security
- Angular
- Polyfill and tests
- Severity
- Do not flag

## Spec date and versions

| # | Check | Type |
|---|-------|------|
| 1 | The code follows the draft of 2026-11-13, or the report names a newer date that the author checked. | judge |
| 2 | The report says that WebMCP is a preview (Community Group draft). | judge |

## Feature check

| # | Check | Type |
|---|-------|------|
| 3 | The code checks `window.isSecureContext` and `document.modelContext` before each use. | judge |
| 4 | The page works the same without WebMCP. Each tool calls the function of a UI control. No feature is only for agents. | judge |
| 5 | No `navigator.modelContext`, also not as a fallback, and no `Navigator.modelContext` type. | tool (WMCP-NAVIGATOR) |

## Registration and lifecycle

| # | Check | Type |
|---|-------|------|
| 6 | Each `registerTool` has `await` and sits in a `try` with a `catch` for `NotAllowedError` and schema errors. | tool (WMCP-NO-AWAIT), judge |
| 7 | Each registration has a `signal`. The code removes a tool with `abort()`. | judge |
| 8 | No `provideContext`, `clearContext`, `unregisterTool`, or a returned `unregister()`. | tool (WMCP-OLD-API), judge |
| 9 | A tool that needs one view lives only while that view is open. | judge |
| 10 | Entry tools (search, navigation) live for the whole app. | judge |

## Tool design

| # | Check | Type |
|---|-------|------|
| 11 | Names are `snake_case` verb and noun. Each tool has a `title` and a `description` that says what changes. | judge |
| 12 | `inputSchema` is an object, not a JSON string, with limits and `required`. | tool (WMCP-SCHEMA-STRING), judge |
| 13 | `execute` returns a JSON value, never `undefined`. `outputSchema` describes it. | judge |
| 14 | The result shows on the screen. No hidden background call. | judge |
| 15 | `readOnlyHint: true` only when nothing visible changes. | judge |
| 16 | `consequentialHint: true` on each tool that pays, books, deletes, or sends, and on no other tool. | judge |
| 17 | `untrustedContentHint: true` when the output has user text. | judge |
| 18 | No hidden characters (zero-width, bidi controls, Unicode tags) in names, descriptions, or schemas. | tool (WMCP-HIDDEN-CHARS) |

## Confirmation and security

| # | Check | Type |
|---|-------|------|
| 19 | A consequential tool stops at a review screen. The human finishes with a step that the agent cannot do. | judge |
| 20 | No `confirm: true` argument, no `requestUserInteraction`, and no claim that a page button stops the agent. | tool (WMCP-OLD-API), judge |
| 21 | The server checks authorization for each action that a tool starts. | judge, then `dev-stack:secure-coding` |
| 22 | No `exposedTo` without a decision (WMCP-05). No local relay without a threat model. | judge |
| 23 | The `tools` Permissions Policy and the CSP fit the tools. | judge, then `frontend-stack:web-security` |

## Angular

| # | Check | Type |
|---|-------|------|
| 24 | Angular 22: `provideExperimentalWebMcpTools` in route providers with `withAutoCleanupInjectors()`, or `declareExperimentalWebMcpTool`. No manual `registerTool` in a component. | judge |
| 25 | Angular 21 and earlier: `abort()` in `DestroyRef.onDestroy`. | judge |

## Polyfill and tests

| # | Check | Type |
|---|-------|------|
| 26 | No `@mcp-b/webmcp-polyfill`. A polyfill only with WMCP-02, and then `@w3c/webmcp-polyfill`, loaded before the first registration. | tool (WMCP-MCPB-POLYFILL), judge |
| 27 | A unit test covers the page with no `document.modelContext`. | judge |
| 28 | The manual test uses the DevTools WebMCP pane or the DevTools MCP server, not Gemini in Chrome. | judge |

## Severity

- Blocking: a consequential tool that the agent can finish alone. A tool action that the server does not authorize. An error in Firefox or Safari because the feature check is missing.
- Major:
  - `navigator.modelContext`, an old API call, or `registerTool` with no `await`
  - a missing or wrong `consequentialHint`
  - a tool that returns `undefined`, or a view tool with no cleanup
- Minor: a name that is not `snake_case`, a missing `title` or `outputSchema`.

## Do not flag

- `registerTool` with no `await` inside `Promise.all(...)` that the code awaits.
- `provideExperimentalWebMcpTools` in `app.config.ts` for entry tools in Angular 22.
- A missing polyfill (WMCP-02 default).
