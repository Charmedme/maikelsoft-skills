# Sources

Checked on 2026-11-19. The facts of this skill come from these primary sources. Look them up again when `metadata.verified` in `SKILL.md` is older than 6 months.

## Contents

- Versions
- Documentation
- Checked by build and run
- Facts that are not verified
- Credits

## Versions

| Item | Version on 2026-11-19 | Source |
|------|-----------------------|--------|
| ASP.NET Core and Blazor | 11.0.0 (2026-11-10, STS) and 10.0.15 (LTS). Both end support on 2028-11-14. | .NET release metadata |
| .NET SDK | 11.0.100 and 10.0.404 | .NET release metadata |
| Microsoft.AspNetCore.Components.QuickGrid | 11.0.0 | [nuget.org](https://www.nuget.org/packages/Microsoft.AspNetCore.Components.QuickGrid) |
| Microsoft.AspNetCore.Components.Server.AutoPause | .NET 11 package, opt-in | [What's new in ASP.NET Core 11](https://learn.microsoft.com/aspnet/core/release-notes/aspnetcore-11) |
| bUnit | 2.15.2 | [bUnit 1 to 2 migration](https://bunit.dev/docs/migrations/1to2.html), NuGet |

## Documentation

- [Render modes](https://learn.microsoft.com/aspnet/core/blazor/components/render-modes): the 4 modes, `RendererInfo`, `[ExcludeFromInteractiveRouting]`.
- [Prerender](https://learn.microsoft.com/aspnet/core/blazor/components/prerender): the double load, `prerender: false`.
- [Prerendered state persistence](https://learn.microsoft.com/aspnet/core/blazor/state-management/prerendered-state-persistence): `[PersistentState]`, `AllowUpdates`, `RegisterPersistentService`.
- [Server-side state management](https://learn.microsoft.com/aspnet/core/blazor/state-management/server): circuit pause, server-triggered pause, AutoPause, the file input trap.
- [Validation](https://learn.microsoft.com/aspnet/core/fundamentals/validation): `AddValidation()`, `[ValidatableType]`, models in `.cs` files.
- [Forms binding](https://learn.microsoft.com/aspnet/core/blazor/forms/binding): `FormName`, `[SupplyParameterFromForm]`, no `[BindNever]`.
- [Navigation](https://learn.microsoft.com/aspnet/core/blazor/fundamentals/navigation): enhanced navigation and form handling.
- [Blazor security](https://learn.microsoft.com/aspnet/core/blazor/security/): auth state serialization.
- [Blazor SignalR guidance](https://learn.microsoft.com/aspnet/core/blazor/fundamentals/signalr): session affinity for more than one server.
- [SignalR hosting and scaling](https://learn.microsoft.com/aspnet/core/signalr/scale): sticky sessions, Azure SignalR Service.
- [What's new in ASP.NET Core 10](https://learn.microsoft.com/aspnet/core/release-notes/aspnetcore-10.0): `NotFoundPage`, `NavigationManager.NotFound()`, the reconnect component.
- [What's new in ASP.NET Core 11](https://learn.microsoft.com/aspnet/core/release-notes/aspnetcore-11): client validation, async validators, QuickGrid links, `Virtualize`, circuit pause, TempData and Session parameters.
- [Antiforgery change in .NET 11](https://learn.microsoft.com/aspnet/core/breaking-changes/11/blazor-server-side-rendering-deferred-cross-site-request-forgery-protection).
- [Removed Blazor APIs in .NET 11](https://learn.microsoft.com/aspnet/core/breaking-changes/11/blazor-obsolete-apis-removed).
- [Virtualize issues aspnetcore#70553](https://github.com/dotnet/aspnetcore/issues/70553) and [aspnetcore#70554](https://github.com/dotnet/aspnetcore/issues/70554).

## Checked by build and run

On 2026-11-19 with the .NET SDK 11.0.100, `dotnet new blazor --interactivity Server`, target `net11.0`:

| Fact | Result |
|------|--------|
| The template has `MapStaticAssets()`, `NotFoundPage`, `UseStatusCodePagesWithReExecute`, and no `UseAntiforgery()` or `UseStaticFiles()`. | pass |
| `[PersistentState]` on an Interactive Server page builds. The prerendered HTML has the state comment. | pass |
| `[PersistentState(AllowUpdates = true)]`, `RegisterPersistentService<T>(RenderMode.InteractiveServer)`, `[ExcludeFromInteractiveRouting]`, `NavigationManager.NotFound()`, `RendererInfo.IsInteractive`, `RegisterAsyncFieldValidator`, `IsValidationPending`, `IsValidationFaulted`, and `DisableClientValidation` build with 0 warnings. | pass |
| The examples of `forms.md` (order form, async validator) and the JS interop example of `render-modes.md` build with `-warnaserror`. | pass |
| A POST with 4 bad nested fields gives 4 messages. A POST without line fields gives the `MinLength` message, not a `NullReferenceException`, with `Lines ??= []`. | pass |
| `NotFound()` gives 404 in a static SSR page and in a prerendered Interactive Server page. | pass |
| `<NotFound>` in a `Router` gives no build warning. With `NotFoundPage` too, each request fails with `InvalidOperationException`. | observed |
| `[ValidateComplexType]` without its package gives CS0246. | observed |
| JS interop in `OnInitializedAsync` gives no warning. `InvokeAsync<object>` gives BL0010. A JS interop call without `try`/`catch` gives BL0016. | observed |
| Without `UseAntiforgery()`, forms have no token field. A same-origin POST gives 200. A cross-site POST gives 400. | observed (`docs/work/stack-split/compile-check.md`) |
| `AddServerSideBlazor` and `MapBlazorHub` are in the .NET 11 reference assemblies, without an obsolete text. The assembly holds the obsolete text for the `NotFound` parameter, which points to `NotFoundPage`. | observed (reference XML and assembly strings) |

## Facts that are not verified

- `[PersistentState]` in an app with `_Host.cshtml`.
- The exact HTML form of the .NET 11 client validation rules. A GET of the form page showed no `data-val-*` attributes on the inputs, only `data-valmsg-summary`.
- A page that called `NotFound()` after it made an `EditContext` gave status 500 ("The renderer does not have a component with ID"). One run, cause not known.
- `required` or `init` on a `[Parameter]` as BL0007: from the Microsoft agent skills, not from a build.

## Credits

- The Microsoft `dotnet-blazor` agent skills ([dotnet/skills](https://github.com/dotnet/skills/tree/main/plugins/dotnet-blazor/skills)): the order of the render mode table, the list of agent mistakes, the `OperationCanceledException` trap.
- Probes with Haiku 5.5 and Sonnet 5.5 on 2026-11-19 (`docs/work/stack-split/research-mistakes-dotnet.md`): `RegisterOnPersisting`, `[ValidateComplexType]` without its validator, `UseStaticFiles`, default rows on each POST.
