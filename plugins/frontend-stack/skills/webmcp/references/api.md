# WebMCP API detail

The API shape of the Community Group draft of 2026-11-13, with code for the cases in `SKILL.md`. Each name can change. Check the spec date first (`SKILL.md`, section First).

## Contents

- API shape
- TypeScript declaration
- Consequential tool
- Angular 21 and earlier
- Angular 22
- Polyfill
- Tests and tools
- API history (old method)

## API shape

Source: `index.bs` and `README.md` in github.com/webmachinelearning/webmcp.

- Entry point: `document.modelContext`. It exists only in a secure context (`[SecureContext]`).
- `registerTool(tool, options)` returns a Promise. It rejects on a duplicate name, an empty name or description, or an invalid schema.
- The tool has `name`, `title`, `description`, `inputSchema`, `outputSchema`, `execute(input, { signal })`, and `annotations`.
- Annotations: `readOnlyHint`, `untrustedContentHint`, `consequentialHint`, `debugging`. Each one defaults to `false`.
- Options: `signal` (an `AbortSignal`: abort it to remove the tool) and `exposedTo` (a list of secure origins).
- `getTools({ fromOrigins })` and `executeTool(tool, input, { signal })` are for agents in the page or in a frame.
- Events: `toolchange`, `toolactivated`, `toolcancel`.
- Permissions Policy feature `tools`, default allowlist `self`. A cross-origin iframe needs `allow="tools"`. `Permissions-Policy: tools=()` turns the API off, and `registerTool` then rejects with `NotAllowedError`. The header itself belongs to `frontend-stack:web-security`.

The `execute` function gets a `signal` too. Stop long work when the agent cancels it.

## TypeScript declaration

TypeScript `lib.dom` has no WebMCP types. This declaration has only the parts that a page uses to register tools (WMCP-06). The optional `modelContext` makes the compiler ask for the feature check.

```ts
// webmcp.d.ts: types for the WebMCP draft of 2026-11-13. Check them when the draft changes.
export interface WebMcpToolAnnotations {
  readOnlyHint?: boolean;
  consequentialHint?: boolean;
  untrustedContentHint?: boolean;
}

export interface WebMcpTool<I = unknown, O = unknown> {
  name: string;
  title?: string;
  description: string;
  inputSchema: object;
  outputSchema?: object;
  annotations?: WebMcpToolAnnotations;
  execute(input: I, context: { signal: AbortSignal }): Promise<O> | O;
}

export interface ModelContext {
  registerTool(tool: WebMcpTool, options?: { signal?: AbortSignal; exposedTo?: string[] }): Promise<void>;
}

declare global {
  interface Document {
    readonly modelContext?: ModelContext;
  }
}
```

The resolved type of the `registerTool` Promise is not in the research. `Promise<void>` is an assumption. The code does not use the value.

## Consequential tool

The tool fills the cart and opens the review screen. The human pays in the page with a step that the agent cannot do (WMCP-04).

```ts
await modelContext.registerTool({
  name: "checkout_cart",
  title: "Check out the cart",
  description: "Opens the checkout review of the current cart. The user pays on that screen. The tool does not pay.",
  inputSchema: { type: "object", properties: {} },
  outputSchema: {
    type: "object",
    properties: { status: { type: "string", enum: ["waiting_for_user", "empty_cart"] } },
    required: ["status"],
  },
  annotations: { consequentialHint: true },
  execute: async () => {
    if (cart.isEmpty()) return { status: "empty_cart" };
    router.navigate(["/checkout/review"]); // the human pays here, with the payment sheet
    return { status: "waiting_for_user" };
  },
}, { signal });
```

An agent that also automates the page can click a button in the page. So the step after the review must be one that the agent cannot do. Examples are a payment sheet of the browser or the payment provider, or a new sign-in. Do not write "approved by you" in the record of a step that a click finished.

## Angular 21 and earlier

Register in the component of the view. Abort on destroy.

```ts
@Component({ selector: "app-cart", templateUrl: "./cart.component.html" })
export class CartComponent {
  private readonly cart = inject(CartService);
  private readonly router = inject(Router);
  private readonly tools = new AbortController();

  constructor() {
    inject(DestroyRef).onDestroy(() => this.tools.abort());
    void this.registerTools();
  }

  private async registerTools(): Promise<void> {
    const modelContext = window.isSecureContext ? document.modelContext : undefined;
    if (!modelContext) return;
    try {
      await modelContext.registerTool(checkoutTool(this.cart, this.router), { signal: this.tools.signal });
    } catch (error) {
      console.warn("WebMCP tool not registered", error);
    }
  }
}
```

The component rules (`inject`, signals, tests) are in `frontend-stack:angular`.

## Angular 22

Angular 22.0.0 added an experimental WebMCP API in `@angular/core`. Source: https://angular.dev/ai/webmcp and `core.d.ts` of `@angular/core` 22.3.0.

- `provideExperimentalWebMcpTools([...])` in `app.config.ts` (entry tools) or in the `providers` of a route (view tools).
- The router option `withAutoCleanupInjectors()` (also `withExperimentalAutoCleanupInjectors`). With it, the route injector and its tools go away when the route leaves.
- `declareExperimentalWebMcpTool(tool)` in an injection context, for one tool.
- `experimentalWebMcpTool` on a Signal Form.

This skill did not verify the exact parameters of these functions. Read the Angular page before you write the code, and write "Checked: angular.dev/ai/webmcp, <date>" in the report. The rules of `SKILL.md` for names, schemas, return values, hints, and the human step stay the same.

## Polyfill

Only with WMCP-02. `@w3c/webmcp-polyfill` 0.1.1 is from the W3C Open Source Program.

- Load it before the first registration: `import "@w3c/webmcp-polyfill/auto";`, or call `installWebMCP()`.
- It does not replace a native `document.modelContext`, also not a partial one.
- `@mcp-b/webmcp-polyfill` 6.2.0 says "Deprecated: use @w3c/webmcp-polyfill".
- `@mcp-b/webmcp-local-relay` 6.2.0 bridges page tools to a desktop MCP client over a localhost WebSocket. It is not part of the specification, and it exposes the tools to local processes (WMCP-05).

## Tests and tools

| Tool | Use | Status |
|------|-----|--------|
| Chrome developer trial | `chrome://flags/#enable-webmcp-testing`, from Chrome 146 | Flag |
| Chrome DevTools, **Application** > **WebMCP** | Tools, call log, schema errors, "Run tool" | Experimental |
| Chrome DevTools MCP server with `--categoryExperimentalWebmcp` | A coding agent runs `list_webmcp_tools` and `execute_webmcp_tool` | Experimental, off by default |
| Model Context Tool Inspector extension | The tools of any page in a side panel | Community, needs the flag |
| WebMCP evals CLI (GoogleChromeLabs/webmcp-tools) | Checks that an agent picks the right tool for a request | Not an official Google product |

Known differences between Chrome and the draft:

- Chrome returns a string result without quotes, and `undefined` as `'undefined'`. The draft rejects `undefined` (spec issue 361). Return an object.
- `getTools()` gives `inputSchema` as a string in Chrome and as an object in the draft (spec issue 287). A test stub that follows the draft passes where Chrome fails.
- Gemini in Chrome 157 does not see page tools (spec issue 388).

## API history (old method)

Code from before these dates is old. Replace it.

| Date | Change |
|------|--------|
| 2026-03-05 | `provideContext()` and `clearContext()` removed |
| 2026-03-26 | `registerTool()` takes an optional `AbortSignal` |
| 2026-05-27 | The `modelContext` getter moved from `Navigator` to `Document` |
| 2026-06-08 | `registerTool()` returns a Promise |
| 2026-06-11 | `ModelContextClient` and `requestUserInteraction` removed |
| 2026-07-09 | Chromium removed `navigator.modelContext` (WPT PR 61186) |
| 2026-10-08 | The declarative API (form attributes `toolname`, `tooldescription`) removed from the specification |
| 2026-11-02 | `outputSchema` added |

The declarative API is now a separate explainer (`declarative-api-explainer.md`). The Chrome docs of 2026-10-07 still describe a declarative API for forms. Whether Chrome still ships the form attributes is not verified. Use `registerTool`.
