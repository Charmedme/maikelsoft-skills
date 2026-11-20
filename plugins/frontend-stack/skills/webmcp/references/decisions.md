# WebMCP decisions

The team decisions that WebMCP code needs. The order is the order in which a new app meets them. The recommendation is the catalog default. Ask the human before you apply it (stack rule 4).

Ask WMCP-01, WMCP-02, and WMCP-04 together as one "first tool" question.

## Contents

- WMCP-01. Use of WebMCP
- WMCP-02. Polyfill
- WMCP-03. Angular 22 API
- WMCP-04. Human step for consequential tools
- WMCP-05. Tools for other origins
- WMCP-06. TypeScript types

## WMCP-01. Use of WebMCP

- Question: Does the app give tools to browser agents, while the specification is a Community Group draft?
- Ask when: the first tool.
- Options: No tools. Tools with a feature check, and the page works the same without them. Tools that the app needs to work.
- Recommended: Tools with a feature check, only when the human asks for agent tools. Only Chrome and Edge have origin trials, and each API name can change.
- Choose another option when: never "the app needs them" while the API is a draft.
- Depends on: none.
- Revisit when: the specification leaves the Community Group, or Firefox or Safari ship it.
- Check: judge.
- Detect: none.

## WMCP-02. Polyfill

- Question: Does the app load a polyfill so that agents can use the tools in browsers without WebMCP?
- Ask when: the first tool, or a request for agents in Firefox, Safari, or an older Chrome.
- Options: No polyfill: the feature check skips the tools. `@w3c/webmcp-polyfill` 0.1.1 (W3C Open Source Program, first release 2026-11-03). `@mcp-b/global` or `@mcp-b/webmcp-polyfill` (community, its authors deprecated the polyfill package).
- Recommended: No polyfill. An agent in a browser without WebMCP has no way to call the tools anyway, unless the human adds a bridge.
- Choose another option when: the human wants agents in browsers without WebMCP. Then `@w3c/webmcp-polyfill`, loaded before the first registration, as a new package (stack rule 5). WebMCP outside Chromium can need the response header `Origin-Agent-Cluster: ?1` (open spec issue 318).
- Depends on: WMCP-01.
- Revisit when: the W3C polyfill reaches 1.0, or a browser without WebMCP gets an agent.
- Check: tool (WMCP-MCPB-POLYFILL), judge.
- Detect: none.

## WMCP-03. Angular 22 API

- Question: In an Angular 22 app, does the code use the built-in WebMCP API or a manual `registerTool`?
- Ask when: the first tool in an app on `@angular/core` 22 or later.
- Options: `provideExperimentalWebMcpTools` in route providers with `withAutoCleanupInjectors()`. `declareExperimentalWebMcpTool` in an injection context. `experimentalWebMcpTool` on a Signal Form. A manual `registerTool` with an `AbortController` in the component.
- Recommended: `provideExperimentalWebMcpTools` in the providers of the route of the view, with `withAutoCleanupInjectors()`. Angular then handles the lifecycle and the DI. App-wide entry tools go in `app.config.ts`.
- Choose another option when: the app is on Angular 21 or earlier (manual code). The tool belongs to one Signal Form (`experimentalWebMcpTool`).
- Depends on: WMCP-01.
- Revisit when: Angular removes the "experimental" prefix.
- Check: judge.
- Detect: none.

## WMCP-04. Human step for consequential tools

- Question: How does the human approve a tool that pays, books, deletes, or sends?
- Ask when: the first tool with `consequentialHint: true`.
- Options: The tool stops at a review screen. Then the human finishes with a step that the agent cannot do (a payment sheet, a new sign-in, a passkey). A confirm button in the page. A `confirm: true` argument. No step.
- Recommended: The review screen and a step that the agent cannot do. A page cannot tell an agent click from a human click (spec issue 288), and the agent fills in each argument itself.
- Choose another option when: the action can be undone and costs nothing (then the tool is not consequential).
- Depends on: WMCP-01.
- Revisit when: the specification adds a user-interaction API again. The draft removed `requestUserInteraction` on 2026-06-11.
- Check: judge.
- Detect: none.

## WMCP-05. Tools for other origins

- Question: Do other origins see the tools of the page?
- Ask when: a tool must work for an agent in another frame or on another origin.
- Options: None: the own document, same-origin documents, and the built-in browser agent only. `exposedTo` with a list of secure origins. A local relay (`@mcp-b/webmcp-local-relay`) to a desktop MCP client.
- Recommended: None. Each origin in `exposedTo` can call each tool with the session of the user.
- Choose another option when: the human names the origin and the reason. A local relay exposes the tools to each local process: write a threat model first.
- Depends on: WMCP-01.
- Revisit when: never.
- Check: judge.
- Detect: none.

## WMCP-06. TypeScript types

- Question: Where do the TypeScript types for `document.modelContext` come from?
- Ask when: the first tool in a TypeScript project.
- Options: A small declaration file in the project. The `webmcp-types` package (0.1.11). `any`.
- Recommended: A small declaration file (`references/api.md`). It has only what the code uses, needs no new package, and makes `document.modelContext` optional, so the compiler asks for the feature check.
- Choose another option when: the code uses much of the API (`getTools`, `executeTool`, events). Then `webmcp-types`, as a new package (stack rule 5).
- Depends on: WMCP-01.
- Revisit when: TypeScript `lib.dom` gets the types.
- Check: judge.
- Detect: none.
