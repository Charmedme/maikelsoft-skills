# Sources: fluentvalidation

The author checked all facts on 2026-10-19, unless a line says otherwise. Look them up again when `metadata.verified` in `SKILL.md` is older than 6 months.

## Contents

- Versions
- Documentation
- Checked by compile and run
- Credits

## Versions

| Item | Version on 2026-10-19 | Source |
|------|-----------------------|--------|
| FluentValidation | 12.2.0 (2026-10-10), target `net8.0`, Apache-2.0 | https://www.nuget.org/packages/FluentValidation |
| FluentValidation.DependencyInjectionExtensions | 12.2.0 | https://www.nuget.org/packages/FluentValidation.DependencyInjectionExtensions |
| FluentValidation.AspNetCore | 11.3.1, deprecated on NuGet (reason "Legacy") | https://www.nuget.org/packages/FluentValidation.AspNetCore |
| SharpGrip.FluentValidation.AutoValidation.Endpoints | 2.0.0, MIT (not used, decision API-08) | https://www.nuget.org/packages/SharpGrip.FluentValidation.AutoValidation.Endpoints |
| Microsoft.Extensions.Validation (built-in `AddValidation`) | 10.0.13 | https://learn.microsoft.com/aspnet/core/fundamentals/minimal-apis |

## Documentation

- 12.0 upgrade guide: https://docs.fluentvalidation.net/en/latest/upgrading-to-12.html
- Cascade mode: https://docs.fluentvalidation.net/en/latest/cascade.html
- Dependency injection (scan, lifetime, `includeInternalTypes`): https://docs.fluentvalidation.net/en/latest/di.html
- ASP.NET Core (the deprecated auto-validation and the manual call): https://docs.fluentvalidation.net/en/latest/aspnet.html
- Async validation: https://docs.fluentvalidation.net/en/latest/async.html
- Test extensions: https://docs.fluentvalidation.net/en/latest/testing.html
- Source repository: https://github.com/FluentValidation/FluentValidation
- Minimal API filters and `ThrowOnBadRequest`: https://learn.microsoft.com/aspnet/core/fundamentals/minimal-apis/min-api-filters

## Checked by compile and run

The author compiled and ran these on 2026-10-19 with SDK 10.0.402 and FluentValidation 12.2.0, in lab projects and in the `orders-api` eval fixture:

- `using FluentValidation.DependencyInjectionExtensions;` gives CS0234. `AddValidatorsFromAssemblyContaining<T>()` is in the namespace `FluentValidation`.
- The scan does not register an `internal` validator.
- `Validate` on a validator with `MustAsync` throws `AsyncValidatorInvokedSynchronouslyException`. `ValidateAsync(null)` throws `InvalidOperationException` ("Cannot pass a null model").
- `ToDictionary()` holds the messages only. The default codes are `NotEmptyValidator` and so on.
- `WithErrorCode` sets the code of the validator just before it: `.NotEmpty().MaximumLength(5).WithErrorCode("x")` with an empty value gives `NotEmptyValidator`.
- With `RuleLevelCascadeMode = CascadeMode.Stop`, an empty SKU gives one error (`.Only()` passed).
- `SetValidator` on a null child and `RuleForEach` on a null collection give no error.
- `CascadeMode = CascadeMode.StopOnFirstFailure` (CS0118, CS0117), `InjectValidator` (CS1061), `Transform` (CS0103), and `ShouldHaveAnyValidationError` (CS1061) do not compile with 12.2.0.
- With `AddValidation()` and DataAnnotations on the request, the built-in filter answered the 400 before the FluentValidation filter: no codes in the response.
- A minimal API with an endpoint filter and a JSON `null` body: in Production the filter ran with a null argument. In Development `BadHttpRequestException` ("Implicit body inferred ... but no body was provided") came first, and `UseExceptionHandler` returned 500. A nullable body parameter reached the filter as null in both environments.
- `FluentValidation.AspNetCore` 11.3.1 restores and runs next to FluentValidation 12.2.0, and gives no build warning. Only `dotnet list package --deprecated` shows it.

## Credits

- FluentValidation documentation (Apache-2.0): the upgrade guide, cascade mode, async rules, and the test extensions. Paraphrased.
- github.com/dotnet/skills, skill `dotnet-webapi` (MIT, .NET team): ProblemDetails and `TypedResults.ValidationProblem` in minimal APIs. Paraphrased.
