---
name: blazor
description: Blazor code on ASP.NET Core 10 and 11. Covers render modes with a decision table (static SSR, Interactive Server, WebAssembly, Auto), prerendering and persisted state, forms and nested validation, enhanced navigation and not-found pages, the circuit, auth state in each mode, JS interop, QuickGrid, Virtualize, and error boundaries. Also existing Blazor Server apps with _Host.cshtml and hosted WebAssembly apps. Use when code or a review touches a .razor component, @rendermode, MapRazorComponents, PersistentState, EditForm, SupplyParameterFromForm, NavigationManager, IJSRuntime, QuickGrid, Virtualize, a circuit, AddServerSideBlazor, or a Blazor WebAssembly project. Not for Razor Pages or MVC views, Program.cs outside the Blazor parts (dev-stack:aspnetcore), FluentValidation (dev-stack:fluentvalidation), server auth (dev-stack:secure-coding), XSS, CSP, or tokens in the browser (frontend-stack:web-security), plain HTML and CSS (frontend-stack:modern-web), or bUnit and Playwright tests (test-stack:dotnet-testing).
metadata:
  verified: "2026-11-19"
  versions: "ASP.NET Core 10.0.15 and 11.0.0 (Blazor Web App), Microsoft.AspNetCore.Components.QuickGrid 11.0.0, examples built on SDK 11.0.100"
---

# Blazor

Razor components in a Blazor Web App on .NET 10 and .NET 11. The skill covers render modes, state, forms, and navigation. It also covers the circuit, auth state, JS interop, and the built-in UI components. Existing Blazor Server apps (`_Host.cshtml`) and hosted WebAssembly apps keep their form. The rest of `Program.cs` and the middleware order are in `dev-stack:aspnetcore`. HTML, CSS, and DOM patterns without Blazor are in `frontend-stack:modern-web`.

## First

1. Read `references/stack-rules.md`.
2. Find the target framework (`*.csproj`, `global.json`) and the hosting model:
   - `MapRazorComponents<App>()` and `blazor.web.js`: a Blazor Web App. Use this file.
   - `_Host.cshtml`, `AddServerSideBlazor()`, `MapBlazorHub()`, `blazor.server.js`: Blazor Server. Read `references/existing-code.md` first.
   - Client, Server, and Shared projects with `UseBlazorFrameworkFiles()`: hosted WebAssembly. Read `references/existing-code.md` first.
3. Find the render mode of the change: `@rendermode` on the page, on `<Routes>` in `App.razor` (global), or none (static SSR). Find out if prerendering is on.
4. On `net11.0`, read the column ".NET 11" of the version table in this file.

Done when you can name the target framework, the hosting model, the render mode of each changed component, and the prerender state.

## Defaults for new code

Ask the human before you apply a default that has no accepted decision (stack rule 4). The full entries are in `references/decisions.md`.

| Area | Default | ID |
|------|---------|----|
| Render mode | Static SSR for content and plain forms. Interactive Server for interactive pages. WebAssembly only with a reason. Auto only when Server and WebAssembly both fail. | BLZ-01 |
| Interactivity location | Per page or per component. Global only when most pages are interactive. | BLZ-02 |
| Prerendering | On, with `[PersistentState]` for loaded data. `prerender: false` only with a reason. | BLZ-03 |
| Validation | `DataAnnotationsValidator`, `AddValidation()`, and `[ValidatableType]` on the form model | BLZ-04 |
| Antiforgery on .NET 11 | New app: the template (no `UseAntiforgery()`). Upgraded app: keep `UseAntiforgery()`. | BLZ-05 |
| State | Built-in parts (parameters, cascading values, scoped services, `[PersistentState]`). No Fluxor or Blazored.LocalStorage. | BLZ-06 |
| Grids and UI library | QuickGrid and the built-in input components. A component library only with approval. | BLZ-07 |
| Interactive Server on more instances | Sticky sessions at the load balancer. Azure SignalR Service on Azure with many connections. | BLZ-08 |
| Old hosting model | Keep `_Host.cshtml` and hosted WebAssembly. Move only as its own task. | BLZ-09 |

### Render mode decision table

Select the first row that is true for the page or the component.

| Fact of the page | Render mode | Reason |
|------------------|-------------|--------|
| It shows content, or it has a form that can post the full page | Static SSR (no `@rendermode`) | No circuit, no download. Works with cookies and `HttpContext`. |
| It reads or writes a cookie, or it signs the user in or out | Static SSR, `[ExcludeFromInteractiveRouting]` in a global app | `HttpContext` exists only during the static render. |
| It reacts to each click or key, and the users have a stable connection | `InteractiveServer` | Fast start. Data and secrets stay on the server. Costs server memory for each user. |
| It must work offline or with high latency, or the server must hold no state for each user | `InteractiveWebAssembly` | Runs in the browser. Large first download. All its code and data are visible to the user. |
| It needs a fast first page and also client work later, and the code runs on both sides | `InteractiveAuto` | Server first, then WebAssembly. Each service needs an implementation on both sides. |

## Existing code

The existing form wins (stack rule 3). A Blazor Server app with `_Host.cshtml` and a hosted WebAssembly app still compile on .NET 11: `AddServerSideBlazor` and `MapBlazorHub` exist and are not obsolete. Do not move them in a feature change (BLZ-09). The detail is in `references/existing-code.md`.

These are defects, not style. Fix them when the change calls, copies, or edits the code. Else report them (stack rule 3):

| Defect | Rule |
|--------|------|
| A data load in `OnInitializedAsync` of a prerendered interactive component without persisted state: the load runs two times. | 3 |
| `[ValidateComplexType]` or `ObjectGraphDataAnnotationsValidator` that the change relies on, without the package and the validator together: nested fields do not validate. | 10 |
| JS interop in `OnInitialized(Async)`: it fails during prerendering. | 17 |
| A secret, a token, or the data of another user in persisted state. | 5 |
| A form that binds the entity class: overposting. | 9 |

## Rules

### Render modes and prerendering

1. Select the render mode with the decision table, for each page. Put `@rendermode` on the page or the component, not on a component inside an interactive parent. A child gets the mode of its parent.
2. Use `HttpContext` (as `[CascadingParameter]`) and `SignInManager` only in static SSR components. In an interactive component, `HttpContext` is null or stale.
3. Persist the data that a prerendered interactive component loads. Put `[PersistentState]` on a public property and load with `??=`. Without it, `OnInitializedAsync` runs two times: one time for the prerender and one time when the component becomes interactive. The user sees the data blink. For a service, use `RegisterPersistentService<T>(renderMode)`. Use `[PersistentState(AllowUpdates = true)]` only for read-only data that enhanced navigation can refresh.

```razor
@page "/products"
@rendermode InteractiveServer
@inject IProductService ProductService

@if (Products is null)
{
    <p>Loading products.</p>
}
else
{
    <ul>@foreach (var p in Products) { <li>@p.Name</li> }</ul>
}

@code {
    [PersistentState]
    public List<ProductDto>? Products { get; set; }

    protected override async Task OnInitializedAsync()
    {
        Products ??= await ProductService.GetAllAsync(); // restored after prerender, so it loads one time
    }
}
```

4. Turn prerendering off (`@rendermode @(new InteractiveServerRenderMode(prerender: false))`) only when the page needs no first paint from the server. A double load is not a reason: rule 3 fixes it.
5. Keep secrets out of persisted state. Persisted state goes into the HTML of the page. With WebAssembly and Auto, the user can read it. Persist DTOs with only the fields that the page shows.
6. Use `RendererInfo.IsInteractive` to disable a button or an input until the component is interactive. Use `RendererInfo.Name` and `AssignedRenderMode` to find where the component runs.

Detail, the auth state for each mode, and the circuit: `references/render-modes.md`.

### Forms and validation

7. Give each static SSR form a unique `FormName` and a `[SupplyParameterFromForm]` property. Create the model only when the binding gave none (`Model is null`), and add default rows only then. Code that adds rows on each request adds empty rows to each POST.
8. Handle a null list after the binding. A POST without fields for a list gave a null list, also with an initializer `= []` (compile check, one run). Add `[Required]` to the list and set `Model.Lines ??= []` after the binding.
9. Bind a form model with only the fields that the user can change, never the entity. `[SupplyParameterFromForm]` does not support `[BindNever]`. Map the form model to the entity by hand.
10. For nested objects and lists, call `builder.Services.AddValidation()` and put `[ValidatableType]` on the root form model. Declare the model in a `.cs` file, not in the `@code` block of a `.razor` file. The source generator does not see it there. Without this, Blazor validates only the top level, with no error. For models in a `.Client` project, call `AddValidation()` in that project too.

```csharp
using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Validation;

[ValidatableType]
public sealed class OrderForm
{
    [Required, StringLength(100)] public string Customer { get; set; } = "";
    [Required] public AddressForm ShippingAddress { get; set; } = new();
    [Required, MinLength(1)] public List<OrderLineForm> Lines { get; set; } = [];
}
```

11. Turn on enhanced form posts with `Enhance` on `EditForm` (or `data-enhance` on `<form>`). Enhanced handling does not flow to child forms.
12. On .NET 11, read `references/forms.md` before you change a form. Static SSR forms validate in the browser by default. Async validators exist. The antiforgery setup of the template changed (BLZ-05).

The full order form and the .NET 11 detail: `references/forms.md`.

### Navigation and errors

13. Set `NotFoundPage="typeof(Pages.NotFound)"` on the `Router`, and call `NavigationManager.NotFound()` when a route value has no data. Microsoft deprecated the `<NotFound>` render fragment. The build gives no warning for it in a `.razor` file, so stackcheck finds it (BLZ-ROUTER-NOTFOUND). With `NotFoundPage` and `<NotFound>` together, each request fails with `InvalidOperationException`, so remove the fragment when you add the page (BLZ-ROUTER-BOTH).
14. Enhanced navigation is on by default. Turn it off for one link with `data-enhance-nav="false"`, for example for a link to a page outside Blazor.
15. Put an `ErrorBoundary` around a part of the page that can fail. `ComponentBase` swallows an `OperationCanceledException`, so a timeout leaves the component in its loading state. Catch it and set an error state.

### Circuit, JS interop, and UI components

16. In Interactive Server, each user holds a circuit in server memory, also after a disconnect for a time. Keep large data out of component fields. Keep the template `ReconnectModal` component. For more than one instance, see BLZ-08.
17. Call JS interop in `OnAfterRenderAsync` when `firstRender` is true, or after a check of `RendererInfo.IsInteractive`. No analyzer finds a call in `OnInitializedAsync`. Use `InvokeVoidAsync` for a call without a result (BL0010). Wrap the call in `try`/`catch` for `JSDisconnectedException` (BL0016 on SDK 11). Do not discard a task with `_ = InvokeAsync(...)`: it hides the exception.
18. Use QuickGrid for tables and `Virtualize` for long lists. On .NET 11, QuickGrid sort and page controls are links with the state in the query string. So they work in static SSR. Give `Virtualize` items whole-pixel heights on .NET 11 (aspnetcore#70553).

Detail of the circuit, JS interop, QuickGrid, and `Virtualize`: `references/render-modes.md` and `references/ui-components.md`.

### Pipeline

19. In a Blazor Web App, serve static files with `app.MapStaticAssets()` before `MapRazorComponents<App>()`, and reference them with `@Assets["app.css"]`. `UseStaticFiles()` gives no fingerprint and no compression. Keep `UseStaticFiles()` only for files that are not in the build output, for example uploads.

```csharp
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddValidation();

var app = builder.Build();
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();
app.UseAntiforgery(); // .NET 10 template. The .NET 11 template has no line here (BLZ-05).
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();
```

### .NET 10 and .NET 11

| Area | .NET 10 | .NET 11 |
|------|---------|---------|
| Antiforgery in the template | `app.UseAntiforgery()`, token field in each form | No `UseAntiforgery()`. The CSRF middleware rejects a cross-site POST (400). Forms have no token field. |
| Static SSR validation | Server only | `DataAnnotationsValidator` sends the rules to the browser. `DisableClientValidation` turns it off. |
| Async validation | Own code in the submit handler | `EditContext.RegisterAsyncFieldValidator`, `IsValidationPending`, `IsValidationFaulted` |
| `[ValidatableType]` | Experimental (ASP0029) | Stable. Remove the ASP0029 suppression. |
| QuickGrid sort and paging | Buttons, interactive mode only | Links with the query string, also static SSR. `OnRowClick`. |
| `Virtualize` | One item height, `OverscanCount` 3 | Different heights, `OverscanCount` 15, `ScrollToItemAsync` |
| Circuit pause | `Blazor.pauseCircuit()` in JavaScript | Also `Circuit.RequestCircuitPauseAsync` and the AutoPause package |
| Static SSR state | Cookies or query string | Also `[SupplyParameterFromTempData]` and `[SupplyParameterFromSession]` |
| Removed APIs | - | `Router.PreferExactMatches`, `AddDataAnnotationsValidation(EditContext)`, `SignOutSessionStateManager`, and others |

## Never write

| Wrong | Correct |
|-------|---------|
| `ApplicationState.RegisterOnPersisting(...)` and `TryTakeFromJson` in a new component | `[PersistentState] public T? Data { get; set; }` and `Data ??= await ...` |
| `[ValidateComplexType]` with only `<DataAnnotationsValidator />` | `AddValidation()` and `[ValidatableType]` on the root model in a `.cs` file |
| `<ObjectGraphDataAnnotationsValidator />` in new code | `<DataAnnotationsValidator />` with `AddValidation()` |
| `app.UseStaticFiles();` in a Blazor Web App | `app.MapStaticAssets();` |
| `<NotFound><p>Not found</p></NotFound>` in the `Router` | `NotFoundPage="typeof(Pages.NotFound)"` |
| `await JS.InvokeVoidAsync(...)` in `OnInitializedAsync` | The call in `OnAfterRenderAsync(firstRender)` |
| `await JS.InvokeAsync<object>("focus", el)` | `await JS.InvokeVoidAsync("focus", el)` |
| `prerender: false` to fix a double load | `[PersistentState]` (rule 3) |
| `Model.Lines.Add(new())` in `OnInitialized` on each request | Add rows only when `Model is null` |
| `[SupplyParameterFromForm] public Order? Order` with the EF entity | A form model with only the editable fields |
| `AddServerSideBlazor()`, `MapBlazorHub()`, or `_Host.cshtml` in a new app | `AddRazorComponents().AddInteractiveServerComponents()` and `MapRazorComponents<App>()` |
| `dotnet new blazorwasm` for a new web app | `dotnet new blazor --interactivity WebAssembly` (a Blazor Web App) |
| `[Parameter] public required int Id { get; init; }` | `[Parameter] public int Id { get; set; }` (BL0007) |
| Blazored.LocalStorage or Fluxor as the default state | Scoped services and cascading values (BLZ-06) |
| `TestContext`, `RenderComponent<T>` (bUnit 1.x) | `BunitContext`, `Render<T>` (bUnit 2.x, see test-stack:dotnet-testing) |

## Check

1. Build with the analyzers as errors:

   ```
   dotnet build -warnaserror
   ```

   Pass: `Build succeeded` with 0 warnings. BL0007, BL0010, and BL0016 come from the build.

2. Run the rules of this skill:

   ```
   python3 <skill folder>/scripts/stackcheck.py --root <repo>
   ```

   Pass: 0 errors. For each warning, fix it or add `// stackcheck: ignore <RULE-ID> <reason>` (in a `.razor` file: `@* stackcheck: ignore <RULE-ID> <reason> *@`).

3. Run the app and open each changed page. Check the network tab: one data request for each prerendered page (rule 3). For a form, send one POST with bad nested fields: each nested field shows its message (rule 10).
4. Run the tests. Component tests with bUnit and end-to-end tests are in `test-stack:dotnet-testing`. Pass: all tests green.

## Review checklist

The full list is in `references/review-checklist.md`.

1. The render mode of each page follows the decision table (BLZ-01). judge
2. No `HttpContext` or `SignInManager` in an interactive component. judge
3. Each prerendered interactive load has `[PersistentState]` or a persisted service. No `RegisterOnPersisting` in new code. tool (BLZ-PERSIST-OLD), judge
4. No secret, token, or data of another user in persisted state. Severity: blocking. judge
5. Static SSR forms have `FormName`, `[SupplyParameterFromForm]`, a form model, and default rows only for a new model. judge
6. Nested validation uses `AddValidation()` and `[ValidatableType]` on a model in a `.cs` file. tool (BLZ-VALIDATE-OLD), judge
7. `NotFoundPage` and `NavigationManager.NotFound()`, no `<NotFound>`. tool (BLZ-ROUTER-NOTFOUND, BLZ-ROUTER-BOTH)
8. JS interop runs after the first render, with `InvokeVoidAsync` and a `try`/`catch`. tool (BL0010, BL0016), judge
9. `MapStaticAssets()` in a Blazor Web App. tool (BLZ-STATIC-FILES)
10. Antiforgery on .NET 11 follows BLZ-05. judge
11. No new code in the old hosting model, and no move without BLZ-09. tool (BLZ-HOST-OLD), judge

## Report

End with the report of stack rule 8 (`references/stack-rules.md`): the same headings in the same order on each run. Also give:

- Under "Changes": the render mode of each changed page and the row of the decision table that selected it.
- Under "Checks": the result of the network check for a double load (Check step 3), or "not run".
- Under "Decisions": each BLZ decision that the change uses. Give at least BLZ-01 and BLZ-03 for a new page, and BLZ-05 for a form on .NET 11. Mark each one accepted or assumed.

## References

- `references/decisions.md`: read before new code needs a BLZ decision. It has the question, the options, and the recommendation.
- `references/render-modes.md`: read when you select a render mode, persist state, or set up auth state for WebAssembly or Auto. Also read it for JS interop and for the circuit.
- `references/forms.md`: read before you write or change a form, a validator, or the antiforgery setup.
- `references/ui-components.md`: read when you use QuickGrid, `Virtualize`, `ErrorBoundary`, or navigation.
- `references/existing-code.md`: read before you change a Blazor Server app with `_Host.cshtml` or a hosted WebAssembly app.
- `references/review-checklist.md`: read for a full review.
- `references/sources.md`: the primary sources and the checks by build.
- `references/stack-rules.md` and `references/language.md`: the common rules and the output language.
- `scripts/stackcheck.py`: the rule check of this skill.
