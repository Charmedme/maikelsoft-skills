# Sources

Checked on 2026-11-19. The specification is a preview. Check the spec date before each change (`SKILL.md`, section First), and look up the browser status again when `metadata.verified` is older than 3 months.

## Contents

- Specification and browsers
- Packages
- Framework and tools
- Mistakes that this skill stops
- Facts that are not verified

## Specification and browsers

| Item | Status on 2026-11-19 | Source |
|------|----------------------|--------|
| WebMCP specification | Community Group draft (`CG-DRAFT`), Web Machine Learning Community Group. Last commit of `index.bs`: 2026-11-13 | [webmachinelearning/webmcp](https://github.com/webmachinelearning/webmcp) |
| Chrome developer trial | From Chrome 146, flag `#enable-webmcp-testing` | [Chrome Platform Status entry 5117755740913664](https://chromestatus.com/feature/5117755740913664) |
| Chrome origin trial | Chrome 149 to 156, extended to 162. Status "Proposed" | same |
| Edge | Origin trial in Edge 150 | `implementation-status.md` in the spec repository |
| Firefox, Safari | Standards position requested, no implementation | same, and Chrome Platform Status |
| Chromium removal of `navigator.modelContext` | 2026-07-09 (WPT PR 61186) | spec repository |

## Packages

| Package | Version | Note |
|---------|---------|------|
| `@w3c/webmcp-polyfill` | 0.1.1 (2026-11-16, first release 2026-11-03) | [w3c-oss/webmcp-polyfill](https://github.com/w3c-oss/webmcp-polyfill) |
| `webmcp-types` | 0.1.11 (2026-11-12) | TypeScript types |
| `@mcp-b/webmcp-polyfill` | 6.2.0, deprecated: "use @w3c/webmcp-polyfill" | npm |
| `@mcp-b/global`, `@mcp-b/webmcp-local-relay` | 6.2.0 | Community, not part of the specification |

## Framework and tools

- [Angular WebMCP guide](https://angular.dev/ai/webmcp): `provideExperimentalWebMcpTools`, `declareExperimentalWebMcpTool`, `withAutoCleanupInjectors`, from Angular 22.0.0. Checked in `core.d.ts` of `@angular/core` 22.3.0.
- [Chrome DevTools WebMCP pane](https://developer.chrome.com/docs/devtools/application/webmcp): the pane, and the DevTools MCP server option `--categoryExperimentalWebmcp`.
- [GoogleChromeLabs/webmcp-tools](https://github.com/GoogleChromeLabs/webmcp-tools): evals CLI and the `build-webmcp-tools` skill. Not an official Google product.
- [Model Context Tool Inspector](https://github.com/beaufortfrancois/model-context-tool-inspector): community extension.

## Mistakes that this skill stops

The research of 2026-11-19 ran a todo page and an Angular cart on two models. All 4 answers used `navigator.modelContext`. None used an `AbortSignal` or `await` on `registerTool`. None set `consequentialHint` on a card payment. Other answers used `provideContext`, `unregisterTool`, `requestUserInteraction`, or a `confirm: true` input that the agent fills in.

Reports in the spec and tools repositories:

- An agent called a proposal tool, then clicked the "Approve" button of the page itself (spec issue 288).
- A navigation tool marked `consequentialHint: true` (webmcp-tools issue 511).
- Search tools as hidden `fetch()` calls marked read-only (webmcp-tools issue 552).
- A tool for one route registered for the whole app, and entry tools on one page only (webmcp-tools issue 551).
- Agents that refused a checkout tool because "hand off to the UI" and "mark consequential" looked like a conflict (webmcp-tools issue 549).
- `inputSchema` as a JSON string, which Chrome rejects (Lumar agentic readiness docs).
- `undefined` results (spec issue 361).
- `inputSchema` as a string from `getTools()` in Chrome (spec issue 287).
- Gemini in Chrome that does not see page tools (spec issue 388).
- Sites that use both `navigator.modelContext` and `document.modelContext` (spec issue 319).
- Hidden characters in tool text (spec issue 360).

## Facts that are not verified

- The Chrome milestone that removed `navigator.modelContext`. Secondary sources name Chrome 150.
- Whether Chrome still ships the declarative form attributes.
- Whether Modern Web Guidance has WebMCP content.
- The calendar end date of the Chrome origin trial. The research found only the milestones.
- The exact parameters of the Angular 22 functions.
- The resolved value of the `registerTool` Promise.
- Whether the DevTools WebMCP pane needs a flag. The docs say no. A search snippet names `#devtools-webmcp-support`.
