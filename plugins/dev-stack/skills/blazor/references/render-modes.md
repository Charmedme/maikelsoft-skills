# Render modes, state, auth, the circuit, and JS interop

## Contents

- Where a component runs
- Global interactivity and static pages
- Prerendering and persisted state
- Persisted services
- Auth state in each mode
- The circuit
- JS interop

## Where a component runs

| Mode | Where it runs | Interactive | `HttpContext` |
|------|---------------|-------------|---------------|
| Static SSR (no `@rendermode`) | Server, one render for each request | No | Yes |
| `InteractiveServer` | Server, over a SignalR circuit | Yes | Only during the prerender |
| `InteractiveWebAssembly` | Browser | Yes | Only during the prerender |
| `InteractiveAuto` | Server for the first visit, then browser | Yes | Only during the prerender |

- A component in a `.Client` project can run on the server (prerender, Auto) and in the browser. Each service that it injects needs a registration in both projects.
- `RendererInfo.Name` gives `Static`, `Server`, or `WebAssembly`. `RendererInfo.IsInteractive` is false during the prerender. `AssignedRenderMode` is the mode from `@rendermode`, or null.

```razor
<button disabled="@(!RendererInfo.IsInteractive)" @onclick="SaveAsync">Save</button>
```

## Global interactivity and static pages

In a global app, `App.razor` sets the mode on `<Routes>` and `<HeadOutlet>`. A page that must be static (sign-in, cookie consent) gets `[ExcludeFromInteractiveRouting]`. `App.razor` then reads the mode from `HttpContext`:

```razor
<HeadOutlet @rendermode="PageRenderMode" />
<Routes @rendermode="PageRenderMode" />

@code {
    [CascadingParameter] private HttpContext HttpContext { get; set; } = default!;

    private IComponentRenderMode? PageRenderMode =>
        HttpContext.AcceptsInteractiveRouting() ? InteractiveServer : null;
}
```

```razor
@page "/account/login"
@attribute [ExcludeFromInteractiveRouting]
```

## Prerendering and persisted state

- Without persisted state, `OnInitializedAsync` runs one time for the prerender and one time on the interactive render.
- `[PersistentState]` on a public property stores the value in the prerendered HTML and restores it before `OnInitializedAsync` of the interactive render. Load with `??=`.
- By default, enhanced navigation to the same page does not update a restored value. This keeps the edits of a user. `[PersistentState(AllowUpdates = true)]` allows the update, for read-only data.
- `RestoreBehavior.SkipInitialValue` and `RestoreBehavior.SkipLastSnapshot` control when Blazor restores the value.
- With Interactive Server, the same attribute keeps the value when the circuit pauses and resumes.
- The state is in the page. With WebAssembly and Auto, the user can read it. Persist DTOs with only the shown fields, never a token, a key, or the data of another user.
- When you bind a property with `[PersistentState]` to `<input type="file">`, the circuit crashes on a resume. Persist the file name in a separate property.

Old form, for existing code only:

```csharp
@implements IDisposable
@inject PersistentComponentState ApplicationState

private PersistingComponentStateSubscription subscription;

protected override async Task OnInitializedAsync()
{
    subscription = ApplicationState.RegisterOnPersisting(Persist);
    if (!ApplicationState.TryTakeFromJson<List<ProductDto>>("products", out var restored))
    {
        restored = await ProductService.GetAllAsync();
    }
    products = restored;
}
```

When you edit such a component in a Blazor Web App, replace it with `[PersistentState]`. In a `_Host.cshtml` app, keep it.

## Persisted services

`RegisterPersistentService<T>` persists the `[PersistentState]` properties of a scoped service for the given render mode:

```csharp
builder.Services.AddScoped<CartState>();
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .RegisterPersistentService<CartState>(RenderMode.InteractiveServer);
```

## Auth state in each mode

- Static SSR and Interactive Server: the server has the `ClaimsPrincipal`. Use `[CascadingParameter] Task<AuthenticationState>` or `AuthorizeView`. Add `AddCascadingAuthenticationState()`.
- WebAssembly and Auto: the server sends the auth state to the browser.

```csharp
// Server project
builder.Services.AddRazorComponents()
    .AddInteractiveWebAssemblyComponents()
    .AddAuthenticationStateSerialization();

// .Client project
builder.Services.AddAuthorizationCore();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddAuthenticationStateDeserialization();
```

- Only the name and role claims go to the browser, unless `SerializeAllClaims = true`. Send only the claims that the UI needs.
- The state in the browser does not change for the life of the WebAssembly app. A sign-out on the server shows only after a reload.
- An `AuthorizeView` in the browser hides UI. It protects nothing. Each API that the browser calls checks the user on the server (`dev-stack:secure-coding`).
- On .NET 11, an Interactive Server circuit can get a new `ClaimsPrincipal` without a reconnect.

## The circuit

- A circuit holds the component tree and the scoped services of one user in server memory. After a disconnect, the server keeps the circuit for a time, so the user can reconnect.
- Keep large lists out of component fields. Page them, or load them again.
- The template has a `ReconnectModal` component with its own CSS and JS. Change its text and style there. It reacts to the `components-reconnect-state-changed` event, with a `retrying` state.
- Pause and resume: `Blazor.pauseCircuit()` and `Blazor.resumeCircuit()` in JavaScript. On .NET 11, also `Circuit.RequestCircuitPauseAsync(cancellationToken)` from a `CircuitHandler`, for example before a deploy. `[PersistentState]` values survive the pause.
- On .NET 11, the package `Microsoft.AspNetCore.Components.Server.AutoPause` pauses the circuit of a hidden tab. Ask for the package first (stack rule 5).

```csharp
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .WithBrowserOptions(options => options.AddAutoPause(p => p.HiddenDelay = TimeSpan.FromMinutes(2)));
```

- On a mobile phone, the system stops the page script soon after the app goes to the background. A long `HiddenDelay` then never fires, and the circuit drops as a normal disconnect.
- More than one instance needs sticky sessions or Azure SignalR Service (BLZ-08). Without them, a reconnect goes to a server without the circuit, and the browser shows an "Invocation canceled" error.

## JS interop

- During the prerender there is no browser. A call in `OnInitialized(Async)` fails, and no analyzer finds it (checked by build on SDK 11.0.100).
- Call JavaScript in `OnAfterRenderAsync` when `firstRender` is true. Load a collocated module (`Map.razor.js`) with `import`.
- Use `InvokeVoidAsync` for a call without a result (BL0010).
- Wrap each call in `try`/`catch`. On Interactive Server the circuit can close during the call: `JSDisconnectedException`. SDK 11 gives BL0016 for a call without `try`/`catch`.
- Dispose the module in `DisposeAsync`, and catch `JSDisconnectedException` there too.
- A `[JSInvokable]` method is public. Pass a `DotNetObjectReference` and dispose it.

```razor
@implements IAsyncDisposable
@inject IJSRuntime JS

<div @ref="mapElement"></div>

@code {
    private ElementReference mapElement;
    private IJSObjectReference? module;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
        {
            return;
        }

        try
        {
            module = await JS.InvokeAsync<IJSObjectReference>("import", "./Components/Pages/Map.razor.js");
            await module.InvokeVoidAsync("init", mapElement);
        }
        catch (JSDisconnectedException)
        {
            // The circuit closed. Nothing to clean up in the browser.
        }
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            if (module is not null)
            {
                await module.DisposeAsync();
            }
        }
        catch (JSDisconnectedException)
        {
        }
    }
}
```
