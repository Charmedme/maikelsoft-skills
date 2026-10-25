---
name: fluentvalidation
description: Validators with FluentValidation 12 in .NET code. Covers rules, cascade mode, error codes, child and collection validators, async rules, DI registration, the call from a minimal API endpoint filter, a controller, or a worker, the error response with stable codes, and validator tests. Use when code or a review touches AbstractValidator, RuleFor, RuleForEach, MustAsync, WithErrorCode, CascadeMode, SetValidator, IValidator, ValidateAsync, AddValidatorsFromAssemblyContaining, TestValidate, InlineValidator, FluentValidation.AspNetCore, or a move from DataAnnotations or AddValidation to FluentValidation. Not for the built-in AddValidation with DataAnnotations alone, ProblemDetails wiring, or endpoint design (dev-stack:aspnetcore), options validation at startup, or domain invariants inside entities (dev-stack:csharp).
metadata:
  verified: "2026-10-25"
  versions: "FluentValidation 12.2.0 and FluentValidation.DependencyInjectionExtensions 12.2.0 (net8.0 and later), .NET 10 (SDK 10.0.402)"
---

# FluentValidation

Validators and the call that runs them. The endpoint, ProblemDetails, and the built-in `AddValidation` are in `dev-stack:aspnetcore`. This skill also works in a worker or a library without a web host.

## First

1. Read `references/stack-rules.md`.
2. Read the version gate:
   - The `FluentValidation*` packages in `*.csproj` or `Directory.Packages.props`.
   - 12.x: use this file.
   - 11.x: the rules apply, but the 12.0 removals still compile there. Tell the human about the upgrade. Do not upgrade unless the task asks for it. The upgrade steps are in `references/validators.md`.
   - `FluentValidation.AspNetCore` in the project: its owner deprecated it. Plan its removal (rule 1).
3. Find the other validation in the same request path. Look for `builder.Services.AddValidation()`, DataAnnotations on the request records, `[ApiController]` model validation, and manual checks in the handler.

Done when you can name the FluentValidation version, the call site (filter, controller, worker), and each other validation that runs for the same request type.

## Defaults for new code

Existing code keeps its style. For new code, use the accepted decision in `docs/dev-team/decisions.md`. When a decision has no accepted answer, ask the human (stack rule 4). The detail is in `references/decisions.md`.

| Area | Default | ID |
|------|---------|----|
| Library | FluentValidation when the project uses it. Built-in `AddValidation` for shape rules only | API-08 |
| Error codes to the client | `code = "validation.failed"` plus an `errorCodes` map per field, the same shape for both validation paths | API-20 |
| Code format | `WithErrorCode` in the format of the ProblemDetails `code` (`order.sku_unknown`) | API-04 |
| Call site | One generic endpoint filter for minimal APIs. An explicit `ValidateAsync` in controllers and workers | API-08 |
| Auto-validation packages | None: no `FluentValidation.AspNetCore`, no SharpGrip | API-08 |
| Lifetime | The library default (scoped) | - |
| Uniqueness and state | A unique index with 409, and the rule in the domain. A validator check is only an early message | - |

## Rules

### Packages and registration

1. Reference `FluentValidation` and `FluentValidation.DependencyInjectionExtensions` with the same 12.x version. Do not add `FluentValidation.AspNetCore`: its owner deprecated it, it works for MVC only, and it runs async rules synchronously.
2. Register with `services.AddValidatorsFromAssemblyContaining<T>()` and `using FluentValidation;`. The namespace `FluentValidation.DependencyInjectionExtensions` does not exist, although a docs page shows it (CS0234).
3. Make validators `public`, or pass `includeInternalTypes: true`. The scan skips `internal` validators, and the gap shows only at runtime as a DI error.
4. Keep the default lifetime (scoped). A singleton validator that takes a `DbContext` or another scoped service holds one instance for all requests.

### The call

5. Call `await validator.ValidateAsync(model, ct)` in async code. `Validate` on a validator with a `MustAsync` rule throws `AsyncValidatorInvokedSynchronouslyException`. A later async rule then turns each request into a 500.
6. Check the model for null before the call. `ValidateAsync(null)` throws `InvalidOperationException`. In Production, minimal APIs run the endpoint filters with a JSON `null` body, so a filter gets null (tested). In Development, `ThrowOnBadRequest` throws `BadHttpRequestException` before the filters, so a test in Development does not show the problem.
7. Validate one request type with one library. With `AddValidation()` in the project, remove the DataAnnotations from a type with a validator. Or call `.DisableValidation()` on the endpoint. The built-in filter runs first and returns its own 400 (tested). The validator then never runs for that field, and the client sees two error styles.
8. Return `TypedResults.ValidationProblem(result.ToDictionary(), extensions: ...)` with the codes in an extension (decision API-20). `ToDictionary()` carries the messages only. The default `ErrorCode` is the validator name (`NotEmptyValidator`), which is not a contract.

Filter, controller, worker, and the error shape: `references/call-sites.md`.

### Rules in a validator

9. Set `WithErrorCode` after each validator that a client acts on. It applies to the last validator before it, not to the whole chain. Use the format of API-04 (`order.lines_too_many`).
10. Set `RuleLevelCascadeMode = CascadeMode.Stop` in the constructor when a later check needs an earlier one (`NotEmpty`, then `Matches`, then `MustAsync`). It stops a second message and a null access in `Must`.
11. Do not set `ClassLevelCascadeMode = CascadeMode.Stop`. It stops the whole validator after the first rule that fails, not each field. The client then gets one field error at a time (tested).
12. Compose with `RuleForEach(x => x.Lines).SetValidator(lineValidator)` and the child validator from the constructor. FluentValidation 12.0 removed `InjectValidator` (CS1061).
13. `SetValidator` skips a null item or child. When the value is required, put `.NotNull().WithErrorCode(...)` before `SetValidator`, else the handler throws. When null is a valid value (for example an optional field on an update, where null keeps the current value), `SetValidator` without `NotNull()` is correct: do not flag it. With `CascadeMode.Stop`, a `RuleForEach` stops at the first bad item and does not check the other items. Add `.Cascade(CascadeMode.Continue)` to that rule when the client needs an error for each item (tested).
14. Validate the shape of the input: required, length, range, format, and cross-field rules inside one request. Keep state rules in the domain, for example "only a placed order can ship". Keep uniqueness in a unique index with a 409. A validator runs only on the paths that call it. Two requests can pass the same check at the same time.
15. Read limits from the options class that holds them (`IOptions<T>` in the validator constructor). Do not copy a number into a second place. When a request names a limit that differs from the option, ask which one wins.
16. Write two validators for two contracts. Do not use rule sets for it: they hide which rules run, and they pass into child validators.

Rule examples and the 12.0 removals: `references/validators.md`.

### Tests

17. Test the real validator with `TestValidateAsync` (valid and invalid case per rule, and the code). Use `InlineValidator<T>` when a consumer test needs a validator that fails. Do not mock `IValidator<T>`: the mock hides a missing registration and a sync call.

Test examples: `references/testing.md`.

## Never write

| Wrong | Correct |
|-------|---------|
| `AddFluentValidationAutoValidation()`, `AddMvc().AddFluentValidation(...)` | `AddValidatorsFromAssemblyContaining<T>()` and an endpoint filter |
| `using FluentValidation.DependencyInjectionExtensions;` | `using FluentValidation;` |
| `internal sealed class CreateOrderValidator` with a default scan | `public sealed class`, or `includeInternalTypes: true` |
| `AddValidatorsFromAssemblyContaining<T>(ServiceLifetime.Singleton)` with a scoped dependency | `AddValidatorsFromAssemblyContaining<T>()` |
| `var result = validator.Validate(request);` in an endpoint or filter | `var result = await validator.ValidateAsync(request, ct);` |
| `context.GetArgument<T>(0)` passed to the validator as is | `context.Arguments.OfType<T>().FirstOrDefault() is not { } argument` and a 400 |
| `CascadeMode = CascadeMode.StopOnFirstFailure;` | `RuleLevelCascadeMode = CascadeMode.Stop;` |
| `ClassLevelCascadeMode = CascadeMode.Stop;` to get one message for each field | `RuleLevelCascadeMode = CascadeMode.Stop;` (rule 11) |
| `RuleForEach(x => x.Lines).SetValidator(lineValidator)` when an item can be null | `RuleForEach(x => x.Lines).NotNull().WithErrorCode("order.line_required").SetValidator(lineValidator)` |
| `RuleFor(x => x.Address).InjectValidator();` | Inject `IValidator<Address>` and call `SetValidator(addressValidator)` |
| `Transform(x => x.Name, n => n.Trim())` | Project the value in the rule expression or in the request mapping |
| `.NotEmpty().MaximumLength(32).WithErrorCode("sku_invalid")` for both checks | `.NotEmpty().WithErrorCode("order.sku_required").MaximumLength(32).WithErrorCode("order.sku_too_long")` |
| `TypedResults.ValidationProblem(result.ToDictionary())` when the client reads codes | The same plus the `code` and `errorCodes` extensions (API-20) |
| `result.Errors.GroupBy(e => e.PropertyName).ToDictionary(..., e.ErrorMessage ...)` | `result.ToDictionary()` |
| `[Required]` on the record and a validator for the same field | One of the two (rule 7) |
| `new Mock<IValidator<CreateOrder>>()`, `Substitute.For<IValidator<...>>()` | `new CreateOrderValidator(...)` or `new InlineValidator<CreateOrder>()` |
| `ShouldHaveAnyValidationError()` | `ShouldHaveValidationErrors()` (12.x) |
| `MustAsync(EmailNotTakenAsync)` as the only guard | A unique index, catch the violation, return 409 |

## Check

1. Build with the analyzers. Pass: 0 warnings and 0 errors. The 12.0 removals fail here (CS0117, CS0118, CS0103, CS1061).

   ```
   dotnet build -warnaserror
   ```

2. Find deprecated packages. Pass: no `FluentValidation.AspNetCore`.

   ```
   dotnet list package --deprecated
   ```

3. Run the rule check of this skill. Pass: `stackcheck: 0 errors`. In an existing project, add `--changed <base branch>`.

   ```
   python3 <skill folder>/scripts/stackcheck.py --root <repo>
   ```

4. Run the tests. Pass: all green. Each rule has a `TestValidateAsync` test (valid and invalid, with the code). Each endpoint has one HTTP test that asserts the 400 and its codes.

   ```
   dotnet test
   ```

## Review checklist

The full list is in `references/review-checklist.md`.

1. No `FluentValidation.AspNetCore`, no `AddFluentValidation*`. tool (FV-PKG-ASPNETCORE, FV-AUTOVALIDATION)
2. Each call is `ValidateAsync` with the token. tool (FV-SYNC-VALIDATE)
3. A filter checks the argument for null before the call. judge
4. Validators are `public`, or the scan has `includeInternalTypes: true`. judge
5. The lifetime is the default, or no validator takes a scoped service. tool (FV-SINGLETON) and judge
6. One library per request type: no DataAnnotations and no `AddValidation` filter next to a validator. judge
7. Each rule that a client acts on has its own `WithErrorCode`. The codes reach the response (API-20). tool (FV-MANUAL-DICTIONARY) and judge
8. `RuleLevelCascadeMode = CascadeMode.Stop` where a check needs an earlier one. No `ClassLevelCascadeMode = CascadeMode.Stop`. tool (FV-CASCADE-DEPENDENT, FV-CLASS-CASCADE-STOP)
9. Child validators come in through the constructor and `SetValidator`. A `RuleForEach` with items that can be null has `NotNull()`. tool (CS1061 for `InjectValidator`) and judge
10. No state rule or uniqueness check as the only guard in a validator. judge
11. Limits come from the options class, not from a second literal. judge
12. Tests use the real validator or `InlineValidator<T>`, not a mock. tool (FV-MOCK-VALIDATOR)
13. The endpoint documents its 400 (`ProducesValidationProblem`, `dev-stack:aspnetcore-openapi`). judge

## Existing code

Stack rule 3 applies. For this skill, check these defects in the existing code that the change touches:

- A sync `Validate` call on a validator that gets a `MustAsync` rule, also in an import or a worker without an HTTP test (rule 5).
- A filter without a null check, or that returns `ToDictionary()` without the codes (rules 6 and 8).
- DataAnnotations on a type that gets a validator while `AddValidation()` is on (rule 7).
- One `WithErrorCode` after a chain of validators (rule 9), and a limit that the handler checks a second time (rule 15).

## Report

End with the report of stack rule 8 (`references/stack-rules.md`). Output text follows `references/language.md`.

After a change to an endpoint, add a table of the error codes below the "Changes" table. Give each code that a client can get from the endpoint, with its field (the property path, for example `Lines[0].Sku`) and the HTTP status. Also give where the code is in the response (the `errorCodes` extension, decision API-20). Include the existing codes that the endpoint keeps. The client team builds on this list.

## References

- `references/validators.md`: read when you write a validator, a child or collection rule, an async rule, or upgrade from 11.x.
- `references/call-sites.md`: read when you call a validator from an endpoint filter, a controller, or a worker, or shape the error response.
- `references/testing.md`: read before you write a validator test or an endpoint test that involves validation.
- `references/decisions.md`: read when new code needs decision API-08 or API-20.
- `references/review-checklist.md`: read when you review a change.
- `references/sources.md`: the primary sources and the date of each check.
- `references/stack-rules.md`: the common rules of each dev-stack skill.
- `references/language.md`: the rules for text output.
- `scripts/stackcheck.py`: the rule check of this skill.
