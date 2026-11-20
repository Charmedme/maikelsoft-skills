# Forms and validation

## Contents

- A static SSR form with nested fields
- Form model and overposting
- Enhanced form posts
- Interactive forms
- .NET 11: validation in the browser
- .NET 11: async validators
- Antiforgery
- Static SSR state between requests

## A static SSR form with nested fields

The model is in a `.cs` file. The source generator of `AddValidation()` reads `[ValidatableType]` only there.

```csharp
using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Validation;

namespace Shop.Orders;

[ValidatableType]
public sealed class OrderForm
{
    [Required, StringLength(100)] public string Customer { get; set; } = "";
    [Required] public AddressForm ShippingAddress { get; set; } = new();
    [Required, MinLength(1)] public List<OrderLineForm> Lines { get; set; } = [];
}

public sealed class AddressForm
{
    [Required] public string Street { get; set; } = "";
    [Required, RegularExpression(@"^\d{4}\s?[A-Z]{2}$")] public string PostalCode { get; set; } = "";
}

public sealed class OrderLineForm
{
    [Required] public string Sku { get; set; } = "";
    [Range(1, 100)] public int Quantity { get; set; } = 1;
}
```

`Program.cs`:

```csharp
builder.Services.AddValidation();
```

The page:

```razor
@page "/order"
@using Shop.Orders
@inject IOrderService Orders

<EditForm Model="Model" FormName="order" OnValidSubmit="SubmitAsync" Enhance>
    <DataAnnotationsValidator />
    <ValidationSummary />
    <InputText @bind-Value="Model!.Customer" />
    <InputText @bind-Value="Model!.ShippingAddress.Street" />
    <InputText @bind-Value="Model!.ShippingAddress.PostalCode" />
    @for (var i = 0; i < Model!.Lines.Count; i++)
    {
        var line = Model.Lines[i];
        <InputText @bind-Value="line.Sku" />
        <InputNumber @bind-Value="line.Quantity" />
    }
    <button type="submit">Place order</button>
</EditForm>

@code {
    [SupplyParameterFromForm(FormName = "order")]
    public OrderForm? Model { get; set; }

    protected override void OnInitialized()
    {
        if (Model is null)
        {
            Model = new OrderForm();
            Model.Lines.Add(new OrderLineForm()); // one empty row for a new form only
        }

        Model.Lines ??= []; // a POST without line fields binds null
    }

    private async Task SubmitAsync() => await Orders.PlaceAsync(Model!.ToCommand());
}
```

- The form binds before `OnInitialized`. So `Model is null` is true only on a GET.
- In the compile check, a POST with 4 bad nested fields gave 4 messages. A POST without `Lines` fields gave a null list and a `NullReferenceException`, also with the initializer `= []` (one run).
- Without `AddValidation()` and `[ValidatableType]`, only `Customer` validates. Nothing reports an error.
- `[ValidateComplexType]` is not in the shared framework. It comes from the experimental package `Microsoft.AspNetCore.Components.DataAnnotations.Validation`, and it works only with `<ObjectGraphDataAnnotationsValidator />` of the same package. Without the package it does not compile (CS0246). In an existing app that uses the package with both parts, keep it until a task moves the form.

## Form model and overposting

`[SupplyParameterFromForm]` binds each posted field that matches a property. `[BindNever]` does not work there. So bind a form model with only the fields that the user can change, and map it by hand:

```csharp
public OrderCommand ToCommand() => new(
    Customer.Trim(),
    new Address(ShippingAddress.Street, ShippingAddress.PostalCode),
    [.. Lines.Select(l => new OrderLine(l.Sku, l.Quantity))]);
```

Never bind the EF entity. A posted `Status` or `Price` field then changes the entity.

## Enhanced form posts

- `Enhance` on `EditForm`, or `data-enhance` on a plain `<form>`, sends the POST with fetch and patches the page. The scroll position and focus stay.
- Enhanced handling does not flow to a child form. Set it on each form.
- A plain `<form method="post">` needs `@formname` and `<AntiforgeryToken />` when the app uses `UseAntiforgery()`.

## Interactive forms

- In an interactive component, `EditForm` needs no `FormName`. The model is a private field.
- Disable the submit button while a save runs, so one click gives one save.
- `OnValidSubmit` runs only when the sync validators pass. Check the async state too (see the .NET 11 section).

## .NET 11: validation in the browser

- In static SSR, `DataAnnotationsValidator` writes the validation rules into the HTML. The browser shows the messages before the POST.
- The server validates each POST again. Browser validation is for the user, not for security.
- Turn it off for one form with `<DataAnnotationsValidator DisableClientValidation="true" />`, or for the app with `AddRazorComponents(o => o.DisableClientValidation = true)`.
- `[ValidatableType]` and `[SkipValidation]` are stable on .NET 11. Remove a `NoWarn` or `#pragma` for ASP0029.

## .NET 11: async validators

`EditContext.RegisterAsyncFieldValidator(field, token => ...)` starts an async check for one field. A new call for the same field cancels the old one. `IsValidationPending()` and `IsValidationFaulted()` give the state, for the form or for one field.

```razor
<EditForm EditContext="editContext" OnValidSubmit="SaveAsync">
    <DataAnnotationsValidator />
    <InputText @bind-Value="form.UserName" />
    @if (editContext.IsValidationPending(() => form.UserName))
    {
        <span role="status">Checking the name.</span>
    }
    <ValidationMessage For="() => form.UserName" />
    <button type="submit" disabled="@editContext.IsValidationPending()">Save</button>
</EditForm>

@code {
    private readonly SignUpForm form = new();
    private EditContext editContext = default!;
    private ValidationMessageStore messages = default!;

    protected override void OnInitialized()
    {
        editContext = new EditContext(form);
        messages = new ValidationMessageStore(editContext);
        editContext.OnFieldChanged += (_, e) =>
        {
            if (e.FieldIdentifier.FieldName != nameof(SignUpForm.UserName))
            {
                return;
            }

            var field = e.FieldIdentifier;
            messages.Clear(field);
            editContext.RegisterAsyncFieldValidator(field, async token =>
            {
                if (await Users.NameExistsAsync(form.UserName, token))
                {
                    messages.Add(field, "This user name is taken.");
                    editContext.NotifyValidationStateChanged();
                }
            });
        };
    }

    private async Task SaveAsync()
    {
        if (editContext.IsValidationPending() || editContext.IsValidationFaulted())
        {
            return;
        }

        await Users.CreateAsync(form); // the service checks the name again: two users can post at once
    }
}
```

An async validator runs in an interactive component. In static SSR there is no `EditContext` between requests, so check the rule in the submit handler.

## Antiforgery

| Version | Template | Effect |
|---------|----------|--------|
| .NET 10 | `app.UseAntiforgery()` after `UseHttpsRedirection()` | `EditForm` writes a token field. A POST without a valid token gets 400. |
| .NET 11 | No `UseAntiforgery()` | The CSRF middleware checks the `Origin` and `Sec-Fetch-Site` headers. A cross-site POST gets 400. A same-origin POST without a token gets 200. Forms have no token field. |

- The compile check on SDK 11.0.100 showed each row of the .NET 11 line.
- When you upgrade an app to .NET 11, keep `app.UseAntiforgery()` in the upgrade (BLZ-05).
- The rules for cookies, sign-in, and CSRF of API endpoints are in `dev-stack:secure-coding`.

## Static SSR state between requests

- .NET 11 adds `[SupplyParameterFromTempData]` and `[SupplyParameterFromSession]` for static SSR components. Use TempData for a message after a redirect ("Order saved").
- `[SupplyParameterFromSession]` needs the session middleware (`AddSession`, `UseSession`). The session is in server memory or a distributed cache. Keep it small.
- In the template, `BlazorDisableThrowNavigationException` is `true`. `NavigationManager.NavigateTo` in static SSR then does not throw a `NavigationException`. Remove old `catch (NavigationException)` code only when the project sets this property.
