# Wolverine 5

Read this file only when the project uses Wolverine 5. Wolverine 5 gets fixes for severe bugs only, and its end of life is October 2026. Tell the human. Upgrade only when the task asks for it. The source is the Wolverine migration guide (checked on 2026-10-19).

## Contents

- What differs in version 5
- Upgrade from 5 to 6

## What differs in version 5

The rules of `SKILL.md` apply, with these differences:

| Area | Wolverine 5 | Wolverine 6 |
|------|-------------|-------------|
| Target framework | .NET 8, 9, 10 | .NET 9 and 10 only |
| Service location | `AllowedButWarn`: a plain `AddDbContext` works, with a warning | `NotAllowed`: a plain `AddDbContext` throws on the first message |
| Runtime code generation | Roslyn in the core package | The package `WolverineFx.RuntimeCompilation` for `Dynamic` and `Auto` |
| Message forwarders | Found by an assembly scan | `opts.RegisterMessageForwarder<TFrom, TTo>()` for each one |
| Newtonsoft.Json | `UseNewtonsoftForSerialization` in the core package | An extension in `WolverineFx.Newtonsoft` (`using Wolverine.Newtonsoft;`) |
| JasperFx packages | 1.x line | 2.x line |

In version 5, follow rule 4 of `SKILL.md` anyway. It removes the service location warnings and prepares the upgrade.

## Upgrade from 5 to 6

1. Move the target framework to .NET 9 or 10.
2. Update all `WolverineFx*` packages together. Remove direct references to `JasperFx*` packages, or update them to the same line.
3. Add `WolverineFx.RuntimeCompilation` when the app uses `TypeLoadMode.Dynamic` or `Auto`. Or pre-generate the code (`codegen write`) and use `Static`.
4. Fix each registration that the code generator cannot see through (rule 4 and rule 5 of `SKILL.md`). Start the host and handle one message of each type: the errors show at the first message.
5. Register each message forwarder with `RegisterMessageForwarder`.
6. Do not keep `opts.RestoreV5Defaults()`. It is a temporary bridge for the upgrade only, and it hides the registration problems.
