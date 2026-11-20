# Blazor decisions

The team decisions that Blazor code needs. The order is the order in which a new app meets them. The recommendation is the catalog default. Ask the human before you apply it (stack rule 4).

Batch the questions: ask BLZ-01, BLZ-02, and BLZ-03 together as one "first page" question. Ask the others one by one when the work needs them.

## Contents

- BLZ-01. Render mode of a page
- BLZ-02. Interactivity location
- BLZ-03. Prerendering
- BLZ-04. Form validation
- BLZ-05. Antiforgery on .NET 11
- BLZ-06. Client state
- BLZ-07. Grids and component library
- BLZ-08. Interactive Server on more than one instance
- BLZ-09. Move from the old hosting model

## BLZ-01. Render mode of a page

- Question: Which render mode does this page or component use?
- Ask when: each new page, and each page that gets its first interactive part.
- Options: Static SSR: no circuit, no download, full page posts. `InteractiveServer`: a SignalR circuit, state in server memory. `InteractiveWebAssembly`: .NET in the browser, a large first download, all code visible. `InteractiveAuto`: Server first, then WebAssembly, code must run on both sides.
- Recommended: The decision table in `SKILL.md`. Static SSR for content and plain forms. `InteractiveServer` for interactive pages. WebAssembly only with a reason (offline, high latency, no server state for each user). Auto only when Server and WebAssembly both fail. Each step to the right adds a failure mode.
- Choose another option when: the app already uses one mode for all pages. Keep it for a new page that fits the same row.
- Depends on: none.
- Revisit when: server memory for circuits is too high (then WebAssembly for the busy pages), or users have unstable connections.
- Check: judge.
- Detect: DET-BLZ-01.

## BLZ-02. Interactivity location

- Question: Does the app set the render mode for each page or component, or one time for the whole app?
- Ask when: a new Blazor Web App (`dotnet new blazor --all-interactive` sets global).
- Options: Per page or component: `@rendermode` on the page, static SSR elsewhere. Global: `<Routes @rendermode="InteractiveServer" />` and the same on `<HeadOutlet>` in `App.razor`, with `[ExcludeFromInteractiveRouting]` for static pages.
- Recommended: Per page or component. Static pages then need no circuit, and the sign-in and cookie pages work without an exception list.
- Choose another option when: almost all pages are interactive and the app has few static pages.
- Depends on: BLZ-01.
- Revisit when: the number of `[ExcludeFromInteractiveRouting]` pages grows.
- Check: judge.
- Detect: DET-BLZ-02.

## BLZ-03. Prerendering

- Question: Do interactive components prerender on the server?
- Ask when: the first interactive page.
- Options: On (the default), with `[PersistentState]` or `RegisterPersistentService<T>` for loaded data. Off for each component (`prerender: false`). Off for the app.
- Recommended: On, with persisted state. The user sees content at once, and search engines see it. Persisted state removes the double load.
- Choose another option when: the page shows nothing useful before it is interactive (a drawing canvas, a page that needs browser APIs first). Then `prerender: false` for that component.
- Depends on: BLZ-01.
- Revisit when: the persisted state makes the HTML too large.
- Check: judge, and the network check (`SKILL.md`, Check step 3).
- Detect: DET-BLZ-03.

## BLZ-04. Form validation

- Question: Which validation do Blazor forms use?
- Ask when: the first form with a nested object or a list.
- Options: DataAnnotations with `DataAnnotationsValidator`, `AddValidation()`, and `[ValidatableType]`. It needs no package. It validates nested objects and lists. On .NET 11 it also validates in the browser in static SSR. FluentValidation with a third-party Blazor adapter: one more package, no browser validation. The experimental `ObjectGraphDataAnnotationsValidator` package: old.
- Recommended: DataAnnotations with `AddValidation()` and `[ValidatableType]`. The same attributes work in minimal APIs, and no package is necessary.
- Choose another option when: the rules already live in FluentValidation validators that the API also uses. Then ask for the adapter package (stack rule 5) and use `dev-stack:fluentvalidation`.
- Depends on: none.
- Revisit when: a rule needs a database call. Then an async validator (.NET 11) or a check in the submit handler.
- Check: tool (BLZ-VALIDATE-OLD), judge.
- Detect: DET-BLZ-04.

## BLZ-05. Antiforgery on .NET 11

- Question: Does the app keep `app.UseAntiforgery()` on .NET 11?
- Ask when: a new .NET 11 app with forms, and each upgrade of a Blazor app from .NET 10 to .NET 11.
- Options: The .NET 11 template: no `UseAntiforgery()`. The cross-origin CSRF middleware rejects a cross-site POST with 400, and forms have no token field. Keep `UseAntiforgery()`: forms keep the token field, the same as on .NET 10.
- Recommended: A new .NET 11 app follows the template. An upgraded app keeps `UseAntiforgery()` in the upgrade commit, so the upgrade does not change the security behavior. The removal is a separate change with a review in `dev-stack:secure-coding`.
- Choose another option when: other endpoints of the app need the token (`IAntiforgery` in an API, a JavaScript client that sends the token). Keep `UseAntiforgery()`.
- Depends on: none.
- Revisit when: Microsoft documents a case where the CSRF middleware alone is not enough.
- Check: judge.
- Detect: DET-BLZ-05.

## BLZ-06. Client state

- Question: Where does the state live that more than one component shares?
- Ask when: the first state that two components, or two pages, share.
- Options: Built-in parts (parameters, cascading values, a scoped service with an event, `[PersistentState]`, and `ProtectedLocalStorage` on Interactive Server). A state library (Fluxor). Browser storage packages (Blazored.LocalStorage).
- Recommended: Built in. A scoped service lives for one circuit (Server) or one browser tab (WebAssembly). It needs no package.
- Choose another option when: the app is large, already uses a library, or needs a full action log. Ask for the package (stack rule 5).
- Depends on: BLZ-01.
- Revisit when: the scoped services get events in many directions.
- Check: judge.
- Detect: none.

## BLZ-07. Grids and component library

- Question: Does the app use QuickGrid and the built-in inputs, or a component library?
- Ask when: the first data table, or the first control that the built-in components do not have.
- Options: QuickGrid (`Microsoft.AspNetCore.Components.QuickGrid`, MIT) and the built-in input components. A component library (MudBlazor, Radzen, Fluent UI Blazor, or a paid library).
- Recommended: QuickGrid and the built-in inputs. On .NET 11, QuickGrid sorts and pages with links, so it works in static SSR too. A library adds a large package and its own update cycle.
- Choose another option when: the app already uses a library. Keep it, and use its grid.
- Depends on: BLZ-01.
- Revisit when: the product needs many controls that QuickGrid and HTML do not give (a tree, a scheduler).
- Check: judge.
- Detect: DET-BLZ-07.

## BLZ-08. Interactive Server on more than one instance

- Question: How does the app run Interactive Server on more than one instance, and how does it limit the server memory for each user?
- Ask when: the second instance of an app with Interactive Server, or a memory problem with circuits.
- Options: Sticky sessions (session affinity) at the load balancer. Azure SignalR Service: no sticky sessions in the app, scales to many connections. On .NET 11, the AutoPause package (`Microsoft.AspNetCore.Components.Server.AutoPause`) pauses the circuit of a hidden tab after 2 minutes.
- Recommended: Sticky sessions at the load balancer. Azure SignalR Service when the app runs on Azure with many connections. AutoPause only after a measurement shows the memory problem, and with the approval of the package. A circuit lives only in the memory of one server, so without affinity a reconnect fails.
- Choose another option when: the platform team already runs Azure SignalR Service.
- Depends on: BLZ-01.
- Revisit when: a deploy drops many circuits. Then server-triggered pause (`Circuit.RequestCircuitPauseAsync`, .NET 11).
- Check: judge.
- Detect: DET-BLZ-08.

## BLZ-09. Move from the old hosting model

- Question: Does an existing Blazor Server app (`_Host.cshtml`) or a hosted WebAssembly app move to the Blazor Web App model?
- Ask when: a task needs static SSR pages, a second render mode, or `[PersistentState]` in an old app.
- Options: Keep the old model: it compiles and runs on .NET 10 and .NET 11. Move to a Blazor Web App. Then `App.razor` replaces `_Host.cshtml`, and `MapRazorComponents<App>()` replaces `MapBlazorHub()` and the fallback page. A hosted WebAssembly app gets a `.Client` project.
- Recommended: Keep the old model. Move only as its own task, with a test of each page, when the app needs a feature of the new model. A move in a feature change mixes two risks in one review.
- Choose another option when: the team plans a large rework of the app.
- Depends on: none.
- Revisit when: Microsoft marks `AddServerSideBlazor` or `MapBlazorHub` obsolete.
- Check: tool (BLZ-HOST-OLD), judge.
- Detect: none.
