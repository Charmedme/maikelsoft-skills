# Existing Blazor Server and hosted WebAssembly apps

An app that runs in production keeps its hosting model (BLZ-09). This file tells which rules of `SKILL.md` apply to such an app, and which do not.

## Contents

- Find the hosting model
- Blazor Server with _Host.cshtml
- Hosted Blazor WebAssembly
- Rules that apply to each model
- When the team decides to move

## Find the hosting model

| Signal | Model |
|--------|-------|
| `Pages/_Host.cshtml` (or `_Layout.cshtml`), `AddServerSideBlazor()`, `MapBlazorHub()`, `MapFallbackToPage("/_Host")`, `blazor.server.js` | Blazor Server |
| Projects `*.Client`, `*.Server`, `*.Shared`, `UseBlazorFrameworkFiles()`, `MapFallbackToFile("index.html")`, `blazor.webassembly.js` | Hosted Blazor WebAssembly |
| `App.razor` with `<Routes />`, `MapRazorComponents<App>()`, `blazor.web.js` | Blazor Web App (`SKILL.md` applies in full) |

On .NET 11, `AddServerSideBlazor` and `MapBlazorHub` still exist and are not obsolete (the .NET 11 reference assemblies, checked 2026-11-19).

## Blazor Server with _Host.cshtml

- All components are interactive over the circuit. There is no static SSR and no `@rendermode`. The mode comes from the `<component>` tag helper in `_Host.cshtml`, for example `render-mode="ServerPrerendered"`.
- Prerendering with `ServerPrerendered` loads data two times, the same as in a Blazor Web App. Fix it with the `<persist-component-state />` tag helper in `_Host.cshtml` and `PersistentComponentState` (`RegisterOnPersisting`, `TryTakeFromJson`) in the component. That is the documented form for this model. TODO: `[PersistentState]` in a `_Host.cshtml` app is not verified. Do not use it there until a build and a test show that it restores the value.
- `UseStaticFiles()` is correct in this model. The stackcheck rule BLZ-STATIC-FILES does not fire, because the file has no `MapRazorComponents`.
- Forms: `EditForm` with a private model field. No `FormName`, no `[SupplyParameterFromForm]`.
- Nested validation: an app on .NET 10 or later can use `AddValidation()` and `[ValidatableType]`. An app that uses `ObjectGraphDataAnnotationsValidator` with `[ValidateComplexType]` (both from the experimental package) keeps them until a task changes the form.
- Not-found: the `<NotFound>` fragment of `App.razor` is the form of this model. Keep it. Do not add `NotFoundPage` next to it: the two together fail at run time.
- The circuit rules (memory, sticky sessions, reconnect UI) apply in full.

## Hosted Blazor WebAssembly

- The Server project is an ASP.NET Core API that also serves the WebAssembly files. Its endpoints follow `dev-stack:aspnetcore` and `dev-stack:secure-coding`.
- The Client project runs in the browser. Each secret, connection string, and business rule that must hold is on the server. The user can read and change the Client code.
- Auth: the Client uses `AddOidcAuthentication` or `AddMsalAuthentication`, or a cookie from the server (BFF). Token storage in the browser is in `frontend-stack:web-security`.
- Prerendering is off unless the Server project renders the Client root component. Without prerendering there is no double load.
- JS interop in `OnInitializedAsync` works without prerendering. Still put it in `OnAfterRenderAsync`: a later move to prerendering then does not break it.

## Rules that apply to each model

| Rule of `SKILL.md` | Blazor Server (`_Host`) | Hosted WebAssembly |
|--------------------|-------------------------|--------------------|
| 1, 2 render mode and `HttpContext` | No `@rendermode`. No `HttpContext` in components. | No `@rendermode`. No `HttpContext` in the Client. |
| 3 persisted state | Old `PersistentComponentState` form | Only with prerendering |
| 5 no secrets in state | Yes | Yes, and no secrets in the Client at all |
| 7, 8 static SSR forms | No | No |
| 9 form model, no entity | Yes | Yes |
| 10 nested validation | Yes on .NET 10 or later | Yes on .NET 10 or later, `AddValidation()` in the Client |
| 13 `NotFoundPage` | No, keep `<NotFound>` | No, keep `<NotFound>` |
| 15 error boundaries | Yes | Yes |
| 16 circuit | Yes | No circuit |
| 17 JS interop | Yes | Yes |
| 18 QuickGrid, `Virtualize` | Yes | Yes |
| 19 `MapStaticAssets()` | Optional. `UseStaticFiles()` is correct. | Optional |

## When the team decides to move

A move is its own task (BLZ-09). The order that keeps the app working at each step:

1. Upgrade the target framework first, without a change to the hosting model.
2. Add `App.razor` and `Routes.razor`. Copy the head of `_Host.cshtml` into `App.razor`. Replace `blazor.server.js` with `blazor.web.js`.
3. In `Program.cs`, replace `AddServerSideBlazor()` with `AddRazorComponents().AddInteractiveServerComponents()`. Replace `MapBlazorHub()` and `MapFallbackToPage("/_Host")` with `MapRazorComponents<App>().AddInteractiveServerRenderMode()`.
4. Set the global mode `<Routes @rendermode="InteractiveServer" />`, so each page keeps its behavior. Move pages to static SSR later, one by one, with BLZ-01.
5. Replace `<NotFound>` with `NotFoundPage`, and `UseStaticFiles()` with `MapStaticAssets()`.
6. Open each page and run the tests after each step.

For hosted WebAssembly, the Client project becomes the `.Client` project of a Blazor Web App with `InteractiveWebAssembly`. Move the auth to `AddAuthenticationStateSerialization()` in the same task.
