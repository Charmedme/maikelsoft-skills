# Review checklist

Each check is `tool (<rule ID>)` when a tool finds it, or `judge` when a reviewer must decide. Run the tools first (`SKILL.md`, section Check). In a review, cite the rule number of `SKILL.md` or the rule ID.

## Contents

- Packages and registration
- The call
- Rules in a validator
- Error response
- Tests
- Decisions

## Packages and registration

| # | Check | Type |
|---|-------|------|
| 1 | No `FluentValidation.AspNetCore` package. | tool (FV-PKG-ASPNETCORE, `dotnet list package --deprecated`) |
| 2 | No `AddFluentValidation()`, `AddFluentValidationAutoValidation()`, or `AddFluentValidationClientsideAdapters()`. | tool (FV-AUTOVALIDATION) |
| 3 | `FluentValidation` and `FluentValidation.DependencyInjectionExtensions` have the same 12.x version. | judge |
| 4 | Validators are `public`, or the scan passes `includeInternalTypes: true`. | judge |
| 5 | The scan uses the default lifetime, or no validator takes a scoped service (`DbContext`). | tool (FV-SINGLETON) and judge |

## The call

| # | Check | Type |
|---|-------|------|
| 6 | Each call is `await ValidateAsync(model, ct)`, not `Validate`. | tool (FV-SYNC-VALIDATE) |
| 7 | A filter checks its argument for null and returns a 400. | judge |
| 8 | One library per request type: no DataAnnotations on a type with a validator while `AddValidation` is on, or `.DisableValidation()` on the endpoint. | judge |
| 9 | A worker resolves the validator from a scope and does not retry a rejected message. | judge |
| 10 | A limit check is in one place (validator or handler), not in both. | judge |

## Rules in a validator

| # | Check | Type |
|---|-------|------|
| 11 | Each validator that a client acts on has its own `WithErrorCode` in the API-04 format. | judge |
| 12 | `RuleLevelCascadeMode = CascadeMode.Stop` (or `.Cascade(CascadeMode.Stop)`) where a check needs an earlier one. | tool (FV-CASCADE-DEPENDENT) |
| 13 | Child validators come from the constructor and `SetValidator`. No `InjectValidator`, `Transform`, or `CascadeMode =`. | tool (CS1061, CS0103, CS0118) |
| 14 | A required child or collection has `NotNull()` or `NotEmpty()`. | judge |
| 15 | No state rule or uniqueness check as the only guard. The domain or a unique index guards it. | judge |
| 16 | Limits come from the options class. | judge |
| 17 | No rule sets for two contracts. | judge |

## Error response

| # | Check | Type |
|---|-------|------|
| 18 | The 400 carries the codes (decision API-20), not only `ToDictionary()` messages. | judge |
| 19 | No hand-written `GroupBy(...).ToDictionary(... ErrorMessage ...)`: use `result.ToDictionary()` for the messages. | tool (FV-MANUAL-DICTIONARY) |
| 20 | The endpoint has `.ProducesValidationProblem()` for the document. | judge (`dev-stack:aspnetcore-openapi`) |

## Tests

| # | Check | Type |
|---|-------|------|
| 21 | Each rule has an invalid case that asserts its code, and one valid case. | judge |
| 22 | Async validators are tested with `TestValidateAsync`. | judge |
| 23 | No mocked `IValidator<T>`. A consumer test uses `InlineValidator<T>`. | tool (FV-MOCK-VALIDATOR) |
| 24 | One HTTP test per endpoint asserts the 400 and a code, and an async rule gives 400, not 500. | judge |

## Decisions

| # | Check | Type |
|---|-------|------|
| 25 | API-08 and API-20 are accepted in `docs/dev-team/decisions.md`, or the change asks them. | judge |
