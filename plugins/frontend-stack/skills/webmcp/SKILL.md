---
name: webmcp
description: WebMCP browser tools for AI agents (preview) - document.modelContext and registerTool in a web page, the feature check with isSecureContext, progressive enhancement, an AbortSignal for each registration, tools for the active view in a SPA, tool names and schemas, the annotations readOnlyHint, consequentialHint, and untrustedContentHint, a real user confirmation for a payment or a delete, the Angular 22 built-in WebMCP API, the W3C polyfill, and tests with Chrome DevTools. Use when a web page or a front-end app must expose tools to a browser agent, or code or a review touches document.modelContext, navigator.modelContext, registerTool, provideContext, unregisterTool, provideExperimentalWebMcpTools, or a WebMCP polyfill. Not for MCP servers (dev-stack:dotnet-mcp), CSP and Permissions-Policy headers (frontend-stack:web-security), general HTML, CSS, and JavaScript work (frontend-stack:modern-web), or Angular component code without tools (frontend-stack:angular).
metadata:
  verified: "2026-11-19"
  label: "preview"
  spec-date: "2026-11-13"
  versions: "WebMCP Community Group draft (index.bs, last commit 2026-11-13), Chrome origin trial 149 to 162, Edge 150 origin trial, @w3c/webmcp-polyfill 0.1.1, webmcp-types 0.1.11, Angular 22"
---

# WebMCP

Label: preview. WebMCP lets a web page give tools to an AI agent in the browser. The page registers each tool with a name, a schema, and a function. The agent calls the tool instead of a click on the screen.

The specification is a Community Group draft of the Web Machine Learning Community Group, not a W3C Standard. This skill follows the draft of 2026-11-13. Chrome and Edge have origin trials. Firefox and Safari have no implementation. Each API name in this skill can change.

## First

1. Read `references/stack-rules.md`.
2. Check the spec date: the date of the last commit of `index.bs` in github.com/webmachinelearning/webmcp. When it is newer than 2026-11-13, read the commit log. Find each change to the IDL before you write code (stack rule 2). Done when you can say "the API is the same as on 2026-11-13", or you can name each change.
3. Read the versions in `package.json`:
   - `@angular/core` 22 or later: use the built-in API (rule 15).
   - Angular 21 or earlier, another framework, or plain JavaScript: use `document.modelContext` directly.
   - `@mcp-b/webmcp-polyfill`, `@mcp-b/global`, or `@w3c/webmcp-polyfill`: rule 17.
4. List the tools. For each tool, name the application function that the UI button calls, and the view of the tool. Done when each tool has a function and a view.

## Defaults for new code

Ask the human before you apply a default that has no accepted decision (stack rule 4). The full entries are in `references/decisions.md`.

| Area | Default | ID |
|------|---------|----|
| Use | Only when the human asks for agent tools. Feature check, the page works without WebMCP | WMCP-01 |
| Polyfill | None. `@w3c/webmcp-polyfill` only when the human wants agents in browsers without WebMCP | WMCP-02 |
| Angular 22 | `provideExperimentalWebMcpTools` in route providers with `withAutoCleanupInjectors()` | WMCP-03 |
| Pay, book, delete, or send | The tool stops at a review screen. The human finishes the step in the page | WMCP-04 |
| Other origins | None: no `exposedTo` | WMCP-05 |
| Types | A small declaration in the project, no types package | WMCP-06 |

## Rules

### Feature check

1. Check `window.isSecureContext` and `document.modelContext` before each use. The API exists only in a secure context, and only in Chromium browsers with the origin trial or the flag. Without the check, the page breaks in Firefox and Safari.
2. Keep the page the same without WebMCP. A tool calls the same application function as the UI button. Do not add a feature that only an agent can use.
3. Use `document.modelContext`. Chromium removed `navigator.modelContext` on 2026-07-09, and the draft moved the getter to `Document` on 2026-05-27. Do not write a fallback to `navigator`.

### Registration

4. Call `registerTool(tool, { signal })` one time for each tool, and `await` it. The call returns a Promise. It rejects with `NotAllowedError` when a Permissions Policy blocks `tools`. It also rejects a duplicate name, an empty name or description, and an invalid schema. A `try` without `await` does not catch the rejection.
5. Remove a tool with its `AbortController`: `controller.abort()`. The draft has no `unregisterTool()`, `provideContext()`, or `clearContext()`.
6. Register a tool that needs one view when the view opens, and abort it when the view leaves. A tool for a view that is not on the screen does nothing and gives the agent no error.
7. Register entry tools (search, navigation to a main area) for the whole app. An entry tool on one page only costs the agent a navigation turn.

```js
const modelContext = window.isSecureContext ? document.modelContext : undefined;

export async function registerTodoTools(signal) {
  if (!modelContext || typeof modelContext.registerTool !== "function") {
    return; // no WebMCP: the page works as before
  }
  try {
    await modelContext.registerTool({
      name: "list_todos",
      title: "List todos",
      description: "Returns the todos on this page with their id, text, and done state.",
      inputSchema: { type: "object", properties: {} },
      annotations: { readOnlyHint: true },
      execute: async () => ({ todos: listTodos() }),
    }, { signal });
    await modelContext.registerTool({
      name: "add_todo",
      title: "Add a todo",
      description: "Adds one todo to the list on this page and shows it.",
      inputSchema: {
        type: "object",
        properties: { text: { type: "string", minLength: 1, maxLength: 200 } },
        required: ["text"],
      },
      execute: async ({ text }) => ({ id: addTodo(text) }), // the same function as the Add button
    }, { signal });
  } catch (error) {
    console.warn("WebMCP tools not registered", error); // NotAllowedError or a schema error
  }
}

const tools = new AbortController();
registerTodoTools(tools.signal);
// when the view leaves: tools.abort();
```

### Tool design

8. Give each tool a `snake_case` name with a verb and a noun (`add_todo`, `checkout_cart`), and a `title`. Give it a `description` that says what the tool does and what it changes on the screen. Keep the description plain text: no hidden characters (zero-width, bidi controls, Unicode tags). They pass a review and change what the agent reads.
9. Pass `inputSchema` as a JavaScript object with JSON Schema, not as a JSON string. Chrome rejects a string. Give limits (`maxLength`, `maximum`, `enum`) and `required`.
10. Return a JSON value from `execute`, for example `{ id }` or `{ ok: true }`. Never return `undefined` or nothing: the draft and the polyfill reject it, and Chrome returns the text `'undefined'`. Declare `outputSchema` (added to the draft on 2026-11-02).
11. Show the result on the screen. The user must see what the agent did. A search tool that only runs a background `fetch()` hides the result from the user.
12. Set the annotations (all default to `false`):
    - `readOnlyHint: true` on a tool that changes no data and no visible state. A search that updates the results on the screen is not read-only.
    - `consequentialHint: true` only on a tool that pays, books, deletes, or sends a message. Not on navigation or search.
    - `untrustedContentHint: true` when the output has text from users (reviews, comments, mail). That text can hold a prompt injection.

### Confirmation and security

13. Put a real human step between the agent and a consequential action. The tool prepares the action (fills the cart, opens the review screen), sets `consequentialHint: true`, and returns `{ status: "waiting_for_user" }`. The human finishes the step with an action that the agent cannot do. Examples are a payment sheet or a new sign-in. A confirm button in the page is not enough: an agent that also automates the page can click it. A `confirm: true` argument is not a confirmation: the agent fills it in. Build the tool. Do not refuse it, and do not skip the hint.
14. Treat each tool call as a call from an untrusted actor. Chrome extensions can list and run the tools of a page. The server API checks authorization for each call, as for a click (`dev-stack:secure-coding`). The `tools` Permissions Policy header and the CSP belong to `frontend-stack:web-security`. Set `exposedTo` only for an origin that the human names (WMCP-05).

### Angular 22

15. In Angular 22, use the built-in API. Put `provideExperimentalWebMcpTools([...])` in the `providers` of the route of the view, and add the router option `withAutoCleanupInjectors()`. Angular then removes the tools when the route leaves. Use `declareExperimentalWebMcpTool(tool)` in an injection context for one tool. Put app-wide entry tools in `app.config.ts`. The API is experimental from Angular 22.0.0. Read the options at https://angular.dev/ai/webmcp before you write the code. This skill did not verify their exact shape.
16. In Angular 21 and earlier, register in the component. Use an `AbortController`, `await registerTool` in the constructor or `ngOnInit`, and `abort()` in `DestroyRef.onDestroy`. The code is in `references/api.md`.

### Polyfill and tests

17. Add no polyfill by default. When the human wants agents in browsers without WebMCP, use `@w3c/webmcp-polyfill` (W3C, 0.1.1), a new package (stack rule 5). Load it before the first registration. It does not replace a native `document.modelContext`. Replace `@mcp-b/webmcp-polyfill`: its authors deprecated it.
18. Test with one of these tools:
    - the Chrome DevTools WebMCP pane (Application panel)
    - the Chrome DevTools MCP server with `--categoryExperimentalWebmcp` (`list_webmcp_tools`, `execute_webmcp_tool`)
    - the WebMCP evals CLI

    Do not ask Gemini in Chrome to call the tool as a test. Gemini in Chrome 157 does not see page tools.
19. In a unit test, stub `document.modelContext`, and also test that the page works when it is `undefined`. A stub that follows the draft does not prove Chrome: Chrome gives `inputSchema` from `getTools()` as a string.

Detail, the Angular 21 code, TypeScript types, and the API history: `references/api.md`.

## Never write

| Wrong | Correct |
|-------|---------|
| `navigator.modelContext.registerTool(...)` | `document.modelContext.registerTool(...)` |
| `document.modelContext ?? navigator.modelContext` | `document.modelContext` only |
| `navigator.modelContext.provideContext({ tools: [...] })`, `clearContext()` | One `registerTool` for each tool |
| `modelContext.unregisterTool("add_todo")`, or a returned `unregister()` | `controller.abort()` on the signal of the registration |
| `modelContext.registerTool(tool)` with no `await`, or in a sync `try` | `await modelContext.registerTool(tool, { signal })` in a `try` |
| A call with no check of `isSecureContext` and `document.modelContext` | The feature check, then return when it fails |
| All tools of all views registered at start | View tools while the view is open, entry tools for the app |
| `agent.requestUserInteraction(...)` in `execute` | A review screen in the page and a human step the agent cannot do |
| A `confirm: true` input, or "the page has an Approve button, so it is safe" | `consequentialHint: true` and a payment sheet or a new sign-in |
| No `consequentialHint` on a card payment | `consequentialHint: true` |
| `consequentialHint: true` on `navigate_to` or `search_products` | No hint. `readOnlyHint: false` when the screen changes |
| `readOnlyHint: true` on a search that only runs `fetch()` | The search updates the visible results, and returns them |
| `inputSchema: JSON.stringify({...})` | `inputSchema: { type: "object", ... }` |
| `execute: () => { addTodo(text); }` (returns `undefined`) | `execute: async ({ text }) => ({ id: addTodo(text) })` |
| `import "@mcp-b/webmcp-polyfill"` | `@w3c/webmcp-polyfill`, and only on request (WMCP-02) |
| `<form toolname="..." tooldescription="...">` | `registerTool`: the draft removed the declarative API on 2026-10-08 |
| A manual `registerTool` in an Angular 22 component | `provideExperimentalWebMcpTools` in route providers |
| "Ask Gemini in Chrome to call the tool" as the test | The DevTools WebMCP pane or the DevTools MCP server |

## Check

1. Run the rules of this skill:

   ```
   python3 <skill folder>/scripts/stackcheck.py --root <repo> --changed <base branch>
   ```

   Pass: 0 errors. For each warning, fix it or add `// stackcheck: ignore <RULE-ID> <reason>`.

2. Build and run the unit tests of the project. Pass: green, also the test with no `document.modelContext`.
3. Open the page in Firefox or Safari, or in Chrome without the flag. Pass: no error in the console, and each feature works.
4. Open the page in Chrome with `chrome://flags/#enable-webmcp-testing` (or the origin trial). In DevTools, open **Application** > **WebMCP**. Run each tool. Pass: each tool shows, runs, shows its result on the screen, and leaves the list when its view leaves.

## Review checklist

The full list is in `references/review-checklist.md`.

1. No `navigator.modelContext`, also not as a fallback. tool (WMCP-NAVIGATOR)
2. No `provideContext`, `clearContext`, `unregisterTool`, or `requestUserInteraction`. tool (WMCP-OLD-API)
3. Each `registerTool` has `await`, a `catch`, and a `signal`. tool (WMCP-NO-AWAIT), judge
4. The feature check runs first, and the page works without WebMCP. judge
5. View tools leave with their view. Entry tools stay for the app. judge
6. Each schema is an object with limits. Each tool returns a JSON value. tool (WMCP-SCHEMA-STRING), judge
7. Hints: `readOnlyHint` only with no visible change, `consequentialHint` only on pay, book, delete, send, `untrustedContentHint` on user text. judge
8. A consequential tool stops at a review screen, and the human finishes with a step that the agent cannot do. Severity: blocking. judge
9. The server checks authorization for each call. Severity: blocking. judge, then `dev-stack:secure-coding`
10. No hidden characters in names, descriptions, or schemas. tool (WMCP-HIDDEN-CHARS)
11. No deprecated polyfill, and no polyfill without the decision WMCP-02. tool (WMCP-MCPB-POLYFILL), judge
12. Angular 22 uses the built-in API with cleanup on route leave. judge

## Report

End with the report of stack rule 8 (`references/stack-rules.md`). Also give:

- Under "Changes": each tool with its name, view, annotations, and the application function that it calls.
- Under "Assumptions": the spec date that the code follows (2026-11-13, or the newer date that you checked).
- Under "Open points": each consequential tool and its human step, and each tool that the human must test in Chrome.

## References

- `references/decisions.md`: read before new code needs a WMCP decision.
- `references/api.md`: read when you write the code. It has the API shape, TypeScript types, the Angular 21 component, the Angular 22 notes, the polyfill, the test tools, and the API history.
- `references/review-checklist.md`: read for a full review.
- `references/sources.md`: the primary sources, the browser status, and the facts that are not verified.
- `references/stack-rules.md` and `references/language.md`: the common rules and the output language.
- `scripts/stackcheck.py`: the rule check of this skill.
