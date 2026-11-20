# Navigation, error boundaries, QuickGrid, and Virtualize

## Contents

- Not-found pages
- Enhanced navigation
- Error boundaries
- QuickGrid
- Virtualize

## Not-found pages

The template router:

```razor
<Router AppAssembly="typeof(Program).Assembly" NotFoundPage="typeof(Pages.NotFound)">
    <Found Context="routeData">
        <RouteView RouteData="routeData" DefaultLayout="typeof(Layout.MainLayout)" />
        <FocusOnNavigate RouteData="routeData" Selector="h1" />
    </Found>
</Router>
```

`Program.cs` of the template also has `app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);`.

A page with a route value that has no data calls `NavigationManager.NotFound()`:

```razor
@page "/orders/{Id:int}"
@inject NavigationManager Navigation
@inject IOrderService Orders

@code {
    [Parameter] public int Id { get; set; }
    private OrderDto? order;

    protected override async Task OnInitializedAsync()
    {
        order = await Orders.FindAsync(Id);
        if (order is null)
        {
            Navigation.NotFound();
        }
    }
}
```

- On SDK 11.0.100, `NotFound()` gave status 404 in a static SSR page and in a prerendered Interactive Server page.
- `<NotFound>` and `NotFoundPage` together make each request fail: "Setting NotFound and NotFoundPage properties simultaneously is not supported."
- Test the not-found path of each page that calls `NotFound()`. In one probe, a page that called it after it made an `EditContext` gave status 500 ("The renderer does not have a component with ID"). The cause is not known (one run).

## Enhanced navigation

- Enhanced navigation is on by default in a Blazor Web App. A link to a page of the app loads with fetch and patches the DOM. The scroll position and the focus need care: `FocusOnNavigate` sets the focus on the `h1`.
- Turn it off for one link or a part of the page with `data-enhance-nav="false"`. Examples: a link to a Razor Page, or a file download.
- A script in a page runs only on a full load. Use a JavaScript initializer or the `enhancedload` event for code that must run after each enhanced navigation.
- On .NET 11, enhanced navigation no longer preloads resources.

## Error boundaries

```razor
<ErrorBoundary @ref="errorBoundary">
    <ChildContent>
        @Body
    </ChildContent>
    <ErrorContent>
        <p role="alert">This part of the page failed. Reload the page to try again.</p>
    </ErrorContent>
</ErrorBoundary>

@code {
    private ErrorBoundary? errorBoundary;

    protected override void OnParametersSet() => errorBoundary?.Recover(); // a new page clears the error
}
```

- Put the boundary in the layout around `@Body`, and also around a widget that can fail alone.
- `ComponentBase` swallows an `OperationCanceledException`. A load that times out then leaves the component in its loading state with no error. Catch the exception in the load, and set an error state that the markup shows.
- An unhandled exception in Interactive Server ends the circuit. The boundary keeps the rest of the page alive.

## QuickGrid

- Package: `Microsoft.AspNetCore.Components.QuickGrid` (MIT, the same version as the framework). Ask for the package first (stack rule 5).
- For an `IQueryable` from EF Core, use `Microsoft.AspNetCore.Components.QuickGrid.EntityFrameworkAdapter` and `AddQuickGridEntityFrameworkAdapter()`. Else QuickGrid runs the query in memory.
- Use a `PaginationState` and a `Paginator`. Without them, QuickGrid renders all rows.
- On .NET 11, the sort and page controls are links. The page, the sort column, and the direction go into the query string. So the grid works in static SSR, and a user can share the link. `OnRowClick` gives a row click event.
- On .NET 10, sort and paging need an interactive render mode.

```razor
<QuickGrid Items="orders" Pagination="pagination">
    <PropertyColumn Property="o => o.Number" Sortable="true" />
    <PropertyColumn Property="o => o.PlacedAt" Format="yyyy-MM-dd" Sortable="true" />
</QuickGrid>
<Paginator State="pagination" />

@code {
    private readonly PaginationState pagination = new() { ItemsPerPage = 25 };
    private IQueryable<OrderRow> orders = default!;
}
```

## Virtualize

- `Virtualize` renders only the visible items of a long list. Use `ItemsProvider` to load each window from the server.
- .NET 10: each item must have the same height (`ItemSize`). `OverscanCount` is 3.
- .NET 11: items can have different heights. The default `OverscanCount` is 15. `ScrollToItemAsync(index)` scrolls to an item after the first interactive render. `InitialItemIndex` sets the start position.
- Two .NET 11 issues are open. A fractional row height can leave the view blank (aspnetcore#70553). Home or End in a text input in the list can move the list (aspnetcore#70554). Give items whole-pixel heights until the fix ships.
