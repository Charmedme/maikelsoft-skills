# Review checklist

Each check is `tool (<rule ID>)` when a tool finds it, or `judge` when a reviewer must decide. Run the tools first (`SKILL.md`, section Check). In a review, cite the rule number of `SKILL.md` or the rule ID. For an app with `_Host.cshtml` or hosted WebAssembly, some checks do not apply. `SKILL.md` (section Existing code) tells where the table of those checks is.

## Contents

- Render modes and prerendering
- State and security
- Forms and validation
- Navigation and errors
- Circuit and JS interop
- UI components
- Pipeline and hosting model
- Tests
- Decisions
- Severity
- Do not flag

## Render modes and prerendering

| # | Check | Type |
|---|-------|------|
| 1 | The render mode of each new or changed page follows the decision table of `SKILL.md` (BLZ-01). The report names the row. | judge |
| 2 | `@rendermode` is on the page or on a top component, not on a child of an interactive parent. | judge |
| 3 | No `HttpContext`, `SignInManager`, or cookie write in an interactive component. | judge |
| 4 | Each data load of a prerendered interactive component uses `[PersistentState]` with `??=`, or a service with `RegisterPersistentService<T>`. | judge |
| 5 | No new `RegisterOnPersisting` or `TryTakeFromJson` in a Blazor Web App. | tool (BLZ-PERSIST-OLD) |
| 6 | `prerender: false` has a reason other than a double load. | judge |
| 7 | `[PersistentState(AllowUpdates = true)]` only on read-only data. | judge |

## State and security

| # | Check | Type |
|---|-------|------|
| 8 | Persisted state holds no secret, token, key, or data of another user. With WebAssembly or Auto, the user can read it. | judge |
| 9 | A WebAssembly or Auto app calls `AddAuthenticationStateSerialization()` on the server and `AddAuthenticationStateDeserialization()` in the `.Client` project. `SerializeAllClaims` is off unless the UI needs each claim. | judge |
| 10 | `AuthorizeView` in the browser is not the only check. Each API that the browser calls checks the user on the server. | judge |
| 11 | Shared state uses built-in parts (BLZ-06). A state or storage package has an approval. | judge |

## Forms and validation

| # | Check | Type |
|---|-------|------|
| 12 | Each static SSR form has a unique `FormName` and a `[SupplyParameterFromForm]` property. | judge |
| 13 | The page creates the model and its default rows only when `Model is null`. | judge |
| 14 | A list in a posted model can be null after the binding. The code handles it (`[Required]` and `??= []`). | judge |
| 15 | The form binds a form model, not the entity. The mapping to the entity is by hand. | judge |
| 16 | Nested objects and lists validate: `AddValidation()` and `[ValidatableType]` on the root model in a `.cs` file. | tool (BLZ-VALIDATE-OLD), judge |
| 17 | No `[ValidateComplexType]` without `ObjectGraphDataAnnotationsValidator` and its package. | tool (BLZ-VALIDATE-OLD) |
| 18 | No ASP0029 suppression on .NET 11. | judge |
| 19 | An async validator (.NET 11): the submit handler checks `IsValidationPending()` and `IsValidationFaulted()`, and the server checks the rule again. | judge |
| 20 | Antiforgery on .NET 11 follows BLZ-05. An upgrade does not remove `UseAntiforgery()` in the same commit. | judge |

## Navigation and errors

| # | Check | Type |
|---|-------|------|
| 21 | A Blazor Web App uses `NotFoundPage` and `NavigationManager.NotFound()`, not `<NotFound>`. Never both. | tool (BLZ-ROUTER-NOTFOUND, BLZ-ROUTER-BOTH) |
| 22 | A link to a page outside Blazor has `data-enhance-nav="false"`. | judge |
| 23 | An `ErrorBoundary` is in the layout, with `Recover()` on navigation. | judge |
| 24 | A load catches `OperationCanceledException` and shows an error state, not an endless loading state. | judge |

## Circuit and JS interop

| # | Check | Type |
|---|-------|------|
| 25 | No large data in component fields of Interactive Server pages. | judge |
| 26 | More than one instance with Interactive Server has sticky sessions or Azure SignalR Service (BLZ-08). | judge |
| 27 | No JS interop in `OnInitialized` or `OnInitializedAsync`. | judge |
| 28 | `InvokeVoidAsync` for a call without a result. | tool (BL0010) |
| 29 | Each JS interop call is in `try`/`catch` for `JSDisconnectedException`. The component disposes each module. | tool (BL0016), judge |
| 30 | No `_ = InvokeAsync(...)` that discards a task. | judge |
| 31 | No `required` or `init` on a `[Parameter]`. | tool (BL0007) |

## UI components

| # | Check | Type |
|---|-------|------|
| 32 | QuickGrid on EF Core uses the EF adapter and a `Paginator`. | judge |
| 33 | `Virtualize` items have whole-pixel heights on .NET 11. | judge |

## Pipeline and hosting model

| # | Check | Type |
|---|-------|------|
| 34 | A Blazor Web App serves static files with `MapStaticAssets()` before `MapRazorComponents<App>()`. | tool (BLZ-STATIC-FILES) |
| 35 | No new `AddServerSideBlazor`, `MapBlazorHub`, or `_Host.cshtml` in a new app. An existing app keeps them (BLZ-09). | tool (BLZ-HOST-OLD), judge |
| 36 | A move of the hosting model is its own task with an accepted BLZ-09. | judge |

## Tests

| # | Check | Type |
|---|-------|------|
| 37 | A form with nested fields has a test that posts bad nested values and expects each message. | judge |
| 38 | Component tests use bUnit 2.x names (`BunitContext`, `Render<T>`). The test rules are in `test-stack:dotnet-testing`. | judge |

## Decisions

| # | Check | Type |
|---|-------|------|
| 39 | New Blazor code follows the accepted BLZ decisions, or the output marks them as assumed. | judge |

## Severity

- Blocking: a secret or the data of another user in persisted state (8). Overposting into an entity (15). Nested fields that do not validate where the data goes to the database (16, 17). `<NotFound>` with `NotFoundPage` (21): each request fails.
- Major: a double load (4), JS interop during the prerender (27), `UseStaticFiles` in a Blazor Web App (34), no sticky sessions with more instances (26).
- Minor: BL0010, a missing `ErrorBoundary`, style.

## Do not flag

These patterns are correct. A finding against them is a false positive.

- `RegisterOnPersisting` and `<NotFound>` in an app with `_Host.cshtml`. That is the form of the old model.
- `UseStaticFiles()` in an app without `MapRazorComponents`, or next to `MapStaticAssets()` for files that are not in the build output (uploads).
- No `app.UseAntiforgery()` in a new .NET 11 app. The template removed it, and the CSRF middleware rejects cross-site POSTs.
- `prerender: false` on a component that needs a browser API to show anything.
- `HttpContext` as a `[CascadingParameter]` in `App.razor` or a static SSR page.
- JS interop in `OnInitializedAsync` of a WebAssembly app without prerendering. It works. Still recommend `OnAfterRenderAsync` as minor.
