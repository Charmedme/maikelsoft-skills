# Validators

Rules, codes, cascade, child and collection validators, async rules, options, and the 12.0 removals. Each C# example compiled with SDK 10.0.402, FluentValidation 12.2.0, and `AnalysisLevel` `latest-recommended`, and the tests in `testing.md` ran against it.

## Contents

- A complete validator
- Error codes
- Cascade mode
- Child and collection validators
- Async rules
- Limits from options
- What a validator does not check
- Two contracts
- Upgrade from 11.x

## A complete validator

```csharp
using FluentValidation;
using Microsoft.Extensions.Options;

namespace Orders.Api.Orders;

public sealed class CreateOrderRequestValidator : AbstractValidator<CreateOrderRequest>
{
    public CreateOrderRequestValidator(IOptions<OrdersOptions> options, IValidator<CreateOrderLineRequest> lineValidator)
    {
        var maxLines = options.Value.MaxLinesPerOrder;
        RuleLevelCascadeMode = CascadeMode.Stop;

        RuleFor(x => x.CustomerId)
            .NotEmpty().WithErrorCode("order.customer_required");

        RuleFor(x => x.Lines)
            .NotEmpty().WithErrorCode("order.lines_required")
            .Must(lines => lines.Count <= maxLines).WithErrorCode("order.lines_too_many")
            .WithMessage($"An order can have at most {maxLines} lines.")
            .Must(HaveUniqueSkus).WithErrorCode("order.sku_duplicate")
            .WithMessage("Each SKU can appear only once in an order.");

        RuleForEach(x => x.Lines).SetValidator(lineValidator);
    }

    private static bool HaveUniqueSkus(IReadOnlyList<CreateOrderLineRequest> lines)
    {
        var skus = lines.Where(line => line is not null && !string.IsNullOrWhiteSpace(line.Sku)).Select(line => line.Sku.Trim()).ToList();
        return skus.Distinct(StringComparer.OrdinalIgnoreCase).Count() == skus.Count;
    }
}

public sealed class CreateOrderLineRequestValidator : AbstractValidator<CreateOrderLineRequest>
{
    public CreateOrderLineRequestValidator(IProductCatalog catalog)
    {
        RuleLevelCascadeMode = CascadeMode.Stop;

        RuleFor(x => x.Sku)
            .NotEmpty().WithErrorCode("order.sku_required")
            .MaximumLength(32).WithErrorCode("order.sku_too_long")
            .MustAsync(catalog.ExistsAsync).WithErrorCode("order.sku_unknown")
            .WithMessage("The SKU is not a known product.");

        RuleFor(x => x.Quantity)
            .InclusiveBetween(1, 1000).WithErrorCode("order.quantity_out_of_range");

        RuleFor(x => x.UnitPrice)
            .InclusiveBetween(0m, 100_000m).WithErrorCode("order.unit_price_out_of_range");
    }
}
```

Register both with one scan: `builder.Services.AddValidatorsFromAssemblyContaining<CreateOrderRequestValidator>();`. The scan finds the line validator too, and DI gives it to the constructor.

When the request records move from DataAnnotations to this validator, remove the attributes from the records (rule 7 of `SKILL.md`).

## Error codes

- `WithErrorCode` sets the code of the validator just before it. In `.NotEmpty().MaximumLength(5).WithErrorCode("x")`, an empty value still has the code `NotEmptyValidator` (tested). Put a code after each validator.
- The default code is the validator class name (`NotEmptyValidator`, `PredicateValidator`, `AsyncPredicateValidator`). It is not a contract: a client must not read it.
- Use the format of decision API-04: `<area>.<problem>` in lowercase with underscores (`order.sku_unknown`).
- List the codes in the report of the task (the table of codes, section Report of `SKILL.md`) and in the API documentation. For each code, give the field, the status, and the place in the response. A client team builds on them.
- A changed code breaks clients like a renamed field (`dev-stack:aspnetcore-openapi`, breaking changes).

## Cascade mode

| Setting | Effect |
|---|---|
| `RuleLevelCascadeMode = CascadeMode.Stop` in the constructor | Each rule chain stops at its first failure |
| `.Cascade(CascadeMode.Stop)` on one rule | The same for that rule only |
| `ClassLevelCascadeMode = CascadeMode.Stop` | The whole validator stops after the first rule that fails, for all properties. An invalid email and an empty name give only the email error. It does not set the rule level. Do not use it for "one message for each field" |
| `ValidatorOptions.Global.DefaultRuleLevelCascadeMode` | A global default. Prefer the setting in each validator, so that a reader sees it |

Without `Stop`, an empty SKU gives "must not be empty" and "is not a known product", and the async catalog call runs with an empty value.

In a `RuleForEach` rule, `Stop` ends the rule at the first item that fails: the other items are not checked (`CollectionPropertyRule`, tested). For an error on each item, add `.Cascade(CascadeMode.Continue)` to the `RuleForEach` rule. The child validator keeps its own cascade mode.

## Child and collection validators

- `RuleForEach(x => x.Lines).SetValidator(lineValidator)` validates each item. The property name of an error is `Lines[0].Sku`.
- `RuleFor(x => x.Address).SetValidator(addressValidator)` validates a child object. It skips a null child. Add `NotNull()` when the child must have a value.
- Take the child validator from the constructor (`IValidator<T>`), so that its dependencies come from DI.
- `ChildRules(...)` is fine for a small inline rule set that no other validator uses.
- A null collection gives no item errors. Add `NotEmpty()` or `NotNull()` on the collection.
- `RuleForEach(...).SetValidator(...)` skips a null item: `"lines": [null]` passes. Put `.NotNull().WithErrorCode(...)` before `SetValidator`. The error has the property name `Lines[0]`.

## Async rules

- `MustAsync((value, ct) => ...)` passes the cancellation token. Use it for an I/O check (a catalog, a lookup service).
- A validator with one async rule needs `ValidateAsync` at each call site. `Validate` throws `AsyncValidatorInvokedSynchronouslyException`.
- Put async rules last in a chain with `CascadeMode.Stop`, so that the I/O call runs only for a value of the correct shape.
- An async rule that reads the database is a hint that a domain rule or a constraint is better (next sections).

## Limits from options

```csharp
public CreateOrderRequestValidator(IOptions<OrdersOptions> options, IValidator<CreateOrderLineRequest> lineValidator)
{
    var maxLines = options.Value.MaxLinesPerOrder;
    // ...
}
```

The validator reads the limit from the options class that the rest of the code uses. Remove the same check from the handler, so that the limit has one source. When a task names a different number than the option, ask which number wins, and change the option value, not a second literal.

## What a validator does not check

| Check | Where it belongs | Reason |
|---|---|---|
| Required, length, range, format, cross-field rules of one request | The validator | Shape of the input |
| "Only a placed order can change" | The domain method, with a 409 | A validator runs only on its path, and the state can change between the check and the save |
| "The email is not taken" | A unique index, catch the violation, 409 | Two requests can pass the check at the same time |
| Authorization ("the user owns the order") | An authorization policy or the handler | Validation errors are 400, not 403 or 404 |

An early check in the validator can give a friendly message, but the guard stays in the domain or the database.

## Two contracts

Create and update often have different rules. Write `CreateOrderRequestValidator` and `UpdateOrderRequestValidator`, and share common rules through a child validator. Rule sets (`RuleSet("Update", ...)`) hide which rules run at a call site, and they pass into child validators.

## Upgrade from 11.x

FluentValidation 12.0 needs .NET 8 or later. The removed APIs fail to compile:

| 11.x | 12.x | Error |
|---|---|---|
| `CascadeMode = CascadeMode.StopOnFirstFailure;` | `RuleLevelCascadeMode = CascadeMode.Stop;` | CS0118, CS0117 |
| `RuleFor(x => x.Child).InjectValidator();` | `SetValidator(childValidator)` from the constructor | CS1061 |
| `Transform(x => x.Name, n => n.Trim())` | Project the value in the rule or the mapping | CS0103 |
| `ShouldHaveAnyValidationError()` | `ShouldHaveValidationErrors()` | CS1061 |

Remove `FluentValidation.AspNetCore` in the same change, and move the call to an endpoint filter or an explicit `ValidateAsync` (`call-sites.md`). The full list is in the upgrade guide (`sources.md`).
