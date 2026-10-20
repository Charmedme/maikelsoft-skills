# FluentValidation decisions

The team decisions that this skill owns. The order is the order in which a project meets them. The recommendation is the catalog default. Ask the human before you apply it (stack rule 4). API-04 (error contract) is in `dev-stack:aspnetcore`. API-08 is also in `dev-stack:aspnetcore`, from the side of the built-in validation.

## Contents

- API-08. Request validation
- API-20. Validation error codes to the client

## API-08. Request validation

- Question: Do we validate requests with the built-in `AddValidation` (DataAnnotations), or with FluentValidation, and how does the validator run?
- Ask when: the first request type with a rule, or the first rule that DataAnnotations cannot express (async, cross-field, a rule from options).
- Options: Built-in `AddValidation` with DataAnnotations and `IValidatableObject`. FluentValidation with one generic endpoint filter (minimal APIs) or an explicit `ValidateAsync` call (controllers, workers). FluentValidation with an auto-validation package (`FluentValidation.AspNetCore`, deprecated; SharpGrip.FluentValidation.AutoValidation, MIT). Manual checks in the handler.
- Recommended: FluentValidation when the project uses it, with an own endpoint filter and no auto-validation package. Else the built-in `AddValidation` for shape rules. One library per request type. The filter has about 15 lines and no extra dependency, and one library per type gives one error style.
- Choose another option when: a small API with simple shape rules and no FluentValidation yet (built-in `AddValidation`). Native AOT (the built-in validation is source-generated).
- Depends on: API-01, API-04.
- Revisit when: .NET 11 (async and localized built-in validation), or FluentValidation changes its license.
- Check: tool (FV-PKG-ASPNETCORE, FV-AUTOVALIDATION, FV-SYNC-VALIDATE) and judge (one library per request type).
- Detect: DET-API-08

## API-20. Validation error codes to the client

- Question: How do stable error codes per field reach the client in a validation 400?
- Ask when: the first client that acts on a validation error (a webshop, a mobile app, a partner), or the first `WithErrorCode`.
- Options: `code = "validation.failed"` plus an `errorCodes` map per field next to the standard `errors` messages. Codes instead of messages in `errors`. A custom error body. Messages only.
- Recommended: `code = "validation.failed"` plus an `errorCodes` map (`field -> codes`), the same shape for the built-in and the FluentValidation path. The standard `errors` keep working for generic clients, and the client acts on codes, not on text that can change.
- Choose another option when: an existing API already sends codes in another shape (keep it). A partner contract prescribes a body.
- Depends on: API-04, API-08.
- Revisit when: the client team asks for codes on other 4xx responses in the same shape.
- Check: judge (a test asserts a code in the response).
- Detect: none
