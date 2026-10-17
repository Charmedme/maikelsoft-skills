# Rule catalog

## Contents

- Fix patterns for common IDs
- Rules for the csharp skill
- Duplicates
- Noisy rules
- Severity in the baseline

## Fix patterns for common IDs

The test build of 2026-10-16 showed each ID in this table, except the IDs with the mark "not seen".

| ID | Problem | Fix |
|---|---|---|
| CS8602 | Dereference of a value that can be null | Check for null (`is null`, `?.`, `??`), or make the type not nullable. Do not use `!` to hide it. |
| CA1304, CA1311 | `ToLower()` or `ToUpper()` without culture | `ToLowerInvariant()`, or `ToLower(culture)` for text that a user sees. |
| CA1305 | `int.Parse(s)` or `ToString()` without format provider | `int.Parse(s, CultureInfo.InvariantCulture)`. |
| CA1862 | `a.ToLower() == b` for a case-insensitive compare | `string.Equals(a, b, StringComparison.OrdinalIgnoreCase)`. |
| CA1510 | `if (x == null) throw new ArgumentNullException(...)` | `ArgumentNullException.ThrowIfNull(x);` (CA1511 to CA1513 for the other throw helpers). |
| CA1822 | Member does not use instance data | Make it `static`, or use the instance data. |
| CA1829 | `list.Count() > 0` | `list.Count > 0`. |
| CA2016 | The `CancellationToken` is not passed on | Pass the token to each call that accepts one. |
| CA2200 | `throw ex;` loses the stack trace | `throw;` |
| CA2254 | Log template is not a constant (`$"..."`) | `logger.LogInformation("User {UserId} logged in", userId);` |
| CA1873 | Log argument is costly when the level is off | On SDK 10: suggestion only. Use `[LoggerMessage]` on hot paths. |
| IDE0005 | Unused `using` | Remove it (`dotnet format style --diagnostics IDE0005`). |
| IDE0011 | `if` without braces | Add braces (`dotnet format style --diagnostics IDE0011`). |
| IDE0052, IDE0051 | Private member that is never read or used | Remove it. |
| IDE0059 | Value assigned and never read (not seen) | Remove the assignment. |
| IDE0060 | Unused parameter | Use it or remove it. Often it is a `CancellationToken` that the method must pass on. |
| IDE0161 | Block-scoped namespace | `namespace Shop;` (`dotnet format style --diagnostics IDE0161`). |
| IDE1006 | Name breaks a naming rule | Rename. Private fields: `_camelCase`. |
| RS0030 | Banned API | Use the API in the message, for example inject `TimeProvider` and call `GetUtcNow()`. |
| VSTHRD002 | `.Result`, `.Wait()`, `GetAwaiter().GetResult()` | `await` the task, and make the caller async. |
| VSTHRD100 | `async void` | `async Task`, and await it at the caller. Only UI event handlers can be `async void`. |
| VSTHRD103 | Blocking call in an async method (`File.ReadAllText`) | The async API: `await File.ReadAllTextAsync(path, cancellationToken)`. |
| VSTHRD110 | Task result is not observed | `await` it, or assign it and observe it later. |
| RCS1075 | Empty `catch (Exception)` | Catch the specific exception and handle or log it, or remove the `try`. |
| RCS1155 | String compare without `StringComparison` | `string.Equals(a, b, StringComparison.Ordinal)`. |
| RCS1210 | `return null` from a method that returns `Task` (not seen) | `return Task.FromResult<T?>(null)` or make the method `async`. |
| RCS1229 | Task returned from a `using` block without await (not seen) | `await` the task inside the block. |
| MA0016 | `List<T>` in a public signature | `IReadOnlyList<T>`, `IReadOnlyCollection<T>`, or `IEnumerable<T>`. |
| MA0048 | File name differs from the type name | One public type per file, file named after the type. |
| MSTEST0037 | Assert that can be simpler (`IsTrue(x == true)`) | The specific assert, for example `Assert.IsTrue(x)` or `Assert.AreEqual`. |

## Rules for the csharp skill

`dev-stack:csharp` gives the reason for these code rules. This skill makes sure that the build reports them.

| Code rule | ID | Severity in the baseline |
|---|---|---|
| Pass the `CancellationToken` on | CA2016 | warning (SDK, recommended mode) |
| Constant log templates | CA2254 | warning (SDK, recommended mode) |
| Costly log arguments | CA1873 | suggestion on SDK 10 (noise), SDK default on SDK 11 |
| `LoggerMessage` delegates | CA1848 | none (opt in per hot path) |
| No `async void` | VSTHRD100 | warning (VS Threading) |
| No sync over async | VSTHRD002, VSTHRD103 | warning (VS Threading) |
| Observe each task | VSTHRD110, CS4014 | warning |
| No `DateTime.Now` or `UtcNow` | RS0030 | warning (BannedApiAnalyzers) |
| Throw helpers | CA1510 to CA1513 | warning (SDK) |
| Culture | CA1304, CA1305, CA1311 | warning (SDK, recommended mode) |

## Duplicates

Keep the CA or IDE rule. Turn the other off, so that one problem gives one finding. The baseline does this for each pair in the table.

| Keep | Off | Problem |
|---|---|---|
| CA1307, CA1309, CA1310 | MA0001 | String comparison |
| CA2007 (off in apps) | MA0004 | `ConfigureAwait` |
| CA1862 | MA0006 | String `==` |
| CA1304, CA1305 | MA0011 | Culture |
| CA2200 | MA0027, RCS1044 | `throw ex;` |
| CA1822 | MA0038 | Static member |
| VSTHRD110 | MA0134 | Unobserved task |
| VSTHRD103 | MA0042 (Info) | Blocking call in async code |
| IDE0060 | RCS1163 (Info) | Unused parameter |
| IDE0051 | RCS1213 (Info) | Unused member |
| CA1829 | RCS1077 (Info) | `Count()` |

The Meziantou comparison page lists more pairs (see `sources.md`).

## Noisy rules

Keep these off or at Info. They are not in `latest-recommended`, or they are Info by default.

| ID | Why it is noise |
|---|---|
| CA1062 | Null checks on each public parameter. Nullable reference types do this job. |
| CA2007 | `ConfigureAwait(false)` everywhere. ASP.NET Core has no synchronization context. Turn it on in reusable libraries only. |
| CA1515 | Make public types internal. Noise in apps with many test projects. |
| CA2000 | Dispose before scope loss. Many false positives with DI and factories. |
| CA1031 | Do not catch general exceptions. Top-level handlers must catch them. |
| CA1002 | Do not expose `List<T>`. MA0016 covers it. |
| CA1848 | `LoggerMessage` delegates for each log call. |
| CA1873 | On SDK 10 only. |
| RCS1138, RCS1139, RCS1141 | Doc comment elements. Noise in apps. |
| RCS1046, RCS1047, VSTHRD200 | Async suffix rules. |
| RCS1154 | Sorts enum members. Risky for stored values. |
| RCS0xxx | Roslynator formatting rules. They overlap `dotnet format`. Off by default. |
| VSTHRD111 | `ConfigureAwait` in apps. |

## Severity in the baseline

| Source | What the baseline does |
|---|---|
| SDK CA rules | `latest-recommended`: 145 rules as warnings. CA1848, CA1716 off. CA1873 suggestion. |
| SDK IDE rules | IDE0005, IDE0011, IDE0051, IDE0052, IDE0059, IDE0060, IDE0161, IDE1006 at warning. Other IDE rules stay at suggestion. |
| Roslynator | Info by default. RCS1059, RCS1075, RCS1155, RCS1169, RCS1202, RCS1210, RCS1215, RCS1229 at warning. RCS1044, RCS1251 off. |
| Meziantou | Package defaults. Seven duplicates and MA0026 off. |
| VS Threading | Package defaults. VSTHRD200, VSTHRD111 off. |
| BannedApiAnalyzers | RS0030 at warning (package default). |
| MSTest | `MSTestAnalysisMode` `Recommended`. Details in `dev-stack:dotnet-testing`. |

IDE0079 (unnecessary suppression) does not run on build, only in the IDE. A stale `#pragma` stays. Find suppressions with `sarif.py --suppressed` or with the rule ANA-PRAGMA-REASON.
