---
name: migrate-fluentassertions
description: Migration of a .NET test project from FluentAssertions (version 5, 6, or 7) to AwesomeAssertions 9, step by step, with a build, a test run, and an assertion count after each step. A person starts it, because it changes packages. Use when the user asks to replace FluentAssertions with AwesomeAssertions, to move off FluentAssertions because of the version 8 license, or to remove the FluentAssertions MSTest 4 bridge. Not for writing new tests (dev-stack:dotnet-testing), an upgrade to FluentAssertions 8, or a move to Shouldly or MSTest Assert.
disable-model-invocation: true
metadata:
  verified: "2026-10-27"
  versions: "From FluentAssertions 5.x, 6.x, 7.x (7.2.2) to AwesomeAssertions 9.6.0. MSTest 4.5.2, .NET 10 (SDK 10.0.403)."
---

# Migrate FluentAssertions to AwesomeAssertions

A procedure that moves a test project from FluentAssertions to AwesomeAssertions 9. Rules for test code after the move: `dev-stack:dotnet-testing`.

## First

1. Read `references/stack-rules.md`. It has the common rules of all stack skills.
2. Read the versions. Done when you can name each one:

   | Item | Where to look |
   |------|---------------|
   | FluentAssertions version per test project | `*.csproj`, `Directory.Packages.props` |
   | Companion packages | `FluentAssertions.Web`, `FluentAssertions.Json`, `FluentAssertions.Analyzers` |
   | Test framework and version | `MSTest.Sdk` in `global.json`, `MSTest*`, `xunit*`, `NUnit*` packages |
   | Target framework | `TargetFramework` (AwesomeAssertions 9.6 targets .NET 6, .NET 8, .NET Standard 2.0 and 2.1, .NET Framework 4.7) |

3. Read `references/api-changes.md` before step 4. A version 5 project also needs the "From FluentAssertions 5" section.
4. Read `references/decisions.md`. TEST-02 must have the answer "AwesomeAssertions" before you change a package.

## Defaults for new code

| Area | Default | Decision |
|------|---------|----------|
| Assertion library after the migration | AwesomeAssertions 9.6.0 | TEST-02 |
| Version pin | Exact version `9.6.0`, updates by the dependency bot. On .NET 6, .NET 7, or .NET Framework below 4.7.2: `[9.6.0,10.0.0)`, because the next major version drops these targets. | TEST-02 |

## Rules

1. Ask the human before you change a package (stack rule 5). Give the name, the version, the license, and the reason in one message. Wait for the approval. Reason: the package change touches each test file.
2. Migrate one test project at a time. A project with FA 7 and a project with AwesomeAssertions can live in one solution. Reason: a small step is easy to check and to revert.
3. Do not reference both packages in one project at the end. Both define `Should()` for the same types, and a file that imports both namespaces does not compile (CS0121).
4. Keep the number of assertions the same. Do not delete, skip, or weaken an assertion to make the build pass. Reason: a migration must not change what the tests check.
5. Fix each compile error with the table in `references/api-changes.md`. Do not guess a new name from FluentAssertions 8 docs: AwesomeAssertions 9.6 does not have all FA 8 members.
6. Remove the MSTest 4 bridge (`Services.ThrowException`). AwesomeAssertions 9.3 and later detect MSTest 4.
7. Commit after each green step. Reason: when a later step fails, you can go back to the last green commit.

### When not to migrate

Stop and tell the human when one of these is true:

- The project targets a framework that .NET Standard 2.0 does not support (for example .NET Framework 4.6 or older), and cannot move. AwesomeAssertions 9 does not support it.
- The project has many custom assertion classes built on `Execute.Assertion`. Each class needs a manual rewrite to `AssertionChain`. Give the human the count first.
- The project uses FluentAssertions 8 with a paid license. The license is not the problem then.
- A release is close, and the test suite is the safety net for it. Migrate after the release.

FluentAssertions 7.2.2 stays a supported choice (TEST-02). Not migrating is a valid answer.

## Procedure

Run the check after each step. Go to the next step only when the check passes.

1. **Measure the baseline.** Run the tests and count the assertions on the old version:

   ```
   dotnet test --project <test project>
   grep -rhoE '\.Should\(\)' --include=*.cs <test project folder> | wc -l
   python3 <skill folder>/scripts/stackcheck.py --root <test project folder>
   ```

   Check: all tests pass. Write down the test count, the assertion count, and the number of `MFA-USING-FA` findings. When a test fails before the migration, stop and tell the human.

2. **Ask for approval.** Send the human one message: "Replace FluentAssertions <version> (Apache-2.0) with AwesomeAssertions 9.6.0 (Apache-2.0) in <project>. Reason: FluentAssertions 8 needs a paid license. Version 7 gets fixes only and does not detect MSTest 4. Companion packages: <list>." Check: the human approves. Without approval, stop.

3. **Optional: move to global usings first.** For a large project, add `<Using Include="FluentAssertions" />` (and `.Execution`, `.Equivalency`, `.Extensions` as needed) to the `.csproj`. Remove the `using FluentAssertions...;` lines from the `.cs` files. Check: build and tests pass, counts unchanged. Then steps 4 and 5 change only the `.csproj` for the namespace.

4. **Change the package and the namespaces.**
   a. Replace `<PackageReference Include="FluentAssertions" .../>` with `<PackageReference Include="AwesomeAssertions" Version="9.6.0" />`. With Central Package Management, change `Directory.Packages.props`.
   b. Replace the companion packages: `FluentAssertions.Web` with `AwesomeAssertions.Web` 3.0.0, `FluentAssertions.Json` with `AwesomeAssertions.Json` 9.0.0.
   c. Replace `FluentAssertions` with `AwesomeAssertions` in each `using` line and each `<Using Include>`.
   d. Remove `Services.ThrowException` and `using FluentAssertions.Common;`. Remove the `[AssemblyInitialize]` method when it is empty.

   Check: `stackcheck.py` shows 0 `MFA-USING-FA`, 0 `MFA-PKG-FA-LEFT`, and 0 `MFA-BRIDGE-LEFT` findings for this project.

5. **Fix the compile errors.** Run `dotnet build -warnaserror`. Fix each error with `references/api-changes.md`. Typical errors: `EquivalencyAssertionOptions<T>` (CS0246), `AssertionOptions` (CS0103), `RespectingRuntimeTypes` and `HaveStatusCode` (CS1061). Check: 0 errors, 0 warnings.

6. **Fix the silent changes.** Run `stackcheck.py` again. Fix each `MFA-XML-HAVEATTRIBUTE` finding. Read each `MFA-EMPTY-ALLSATISFY` finding and add `NotBeEmpty()` where an empty collection is a bug. Check: 0 warnings from this skill.

7. **Run the tests.** Run `dotnet test --project <test project>`. Check: the same number of tests as in step 1, all pass. When a test fails, read "Behavior changes without a compile error" in `references/api-changes.md`. Do not change the expected value to make it pass: find the cause.

8. **Count the assertions again.** Run the `grep` count of step 1. Check: the count is the same as the baseline. A different count is a lost or an added assertion. Find it and explain it.

9. **Prove the failure path.** Add one test that fails on purpose (`1.Should().Be(2);`) and run it. Check: the output shows "Expected ... to be 2, but found 1" as an assertion failure, not "threw exception". Remove the test.

10. **Commit and report.** Commit the project. Report these items:
    - the project, and the old and new package versions
    - the test count and the assertion count, before and after
    - the rows of `references/api-changes.md` that you used
    - each assertion that changed its meaning, with the reason

Go back to step 1 for the next test project. After the last project, `stackcheck.py --root <repo>` shows 0 findings of this skill.

## Never write

| Wrong | Correct |
|-------|---------|
| `global using FluentAssertions = AwesomeAssertions;` | Replace the namespace in each `using` line, or use `<Using Include="AwesomeAssertions" />` |
| Both `using FluentAssertions;` and `using AwesomeAssertions;` in one file | One namespace per file, one package per project |
| `Services.ThrowException = ...` after the move | Remove it |
| `AssertionOptions.AssertEquivalencyUsing(...)` | `AssertionConfiguration.Current.Equivalency.Modify(...)` |
| `EquivalencyAssertionOptions<T>` | `EquivalencyOptions<T>` |
| `RespectingRuntimeTypes()` | `PreferringRuntimeMemberTypes()` |
| `ExcludingNestedObjects()` | `WithoutRecursing()` |
| `response.Should().HaveStatusCode(HttpStatusCode.OK)` | `response.StatusCode.Should().Be(HttpStatusCode.OK)` |
| `element.Should().HaveAttribute("id", "42")` on `XElement` | `element.Should().HaveAttributeWithValue("id", "42")` |
| FA 8: `act.Should().Throw()` (no type) | `act.Should().Throw<Exception>()` |
| FA 8: `.WithoutMessage("*x*")` | `.And.Message.Should().NotContain("x")` |
| A deleted assertion to make the build pass | The renamed member from `references/api-changes.md` |
| `<PackageReference Include="FluentAssertions" Version="8.*" />` as the fix | AwesomeAssertions 9.6.0 |

## Check

1. Build: `dotnet build -warnaserror`. Pass: 0 warnings, 0 errors.
2. Tests: `dotnet test`. Pass: the same test count as the baseline, all pass.
3. Assertion count: `grep -rhoE '\.Should\(\)' --include=*.cs <folder> | wc -l`. Pass: the same count as the baseline.
4. Rule check: `python3 <skill folder>/scripts/stackcheck.py --root <repo>`. Pass: 0 findings of `MFA-` rules. In a repo with more test projects, the `MFA-USING-FA` count shows the remaining work.
5. Rules of the test stack: run `dev-stack:dotnet-testing` stackcheck. Pass: 0 errors.

## Review checklist

The full list is in `references/review-checklist.md`.

| # | Check | How |
|---|-------|-----|
| 1 | The human approved the package change. | judge (the log or the PR) |
| 2 | The migrated project has no FluentAssertions package. | tool (MFA-PKG-FA-LEFT) |
| 3 | The migrated project has no `FluentAssertions` namespace. | tool (MFA-USING-FA) |
| 4 | The MSTest 4 bridge is gone. | tool (MFA-BRIDGE-LEFT) |
| 5 | The build has 0 warnings and 0 errors. | tool (build) |
| 6 | Test count and assertion count are the same as before. | tool (test run, grep count) |
| 7 | No `HaveAttribute(name, value)` on `XElement`. | tool (MFA-XML-HAVEATTRIBUTE) |
| 8 | Each `AllSatisfy` and `OnlyContain` on a collection that must have items has `NotBeEmpty()`. | tool (MFA-EMPTY-ALLSATISFY lists them), judge |
| 9 | No expected value changed to make a test pass. | judge (the diff) |
| 10 | Custom assertion classes use `AssertionChain`. | tool (compiler), judge |
| 11 | A test that fails on purpose shows an assertion failure. | judge (step 9) |

## Report

End with the report of stack rule 8 (`references/stack-rules.md`): the same headings in the same order on each run.

## References

- `references/api-changes.md`: read before step 4. Each API change with the old and new code.
- `references/examples/`: a compiled before project (FluentAssertions 7.2.2) and after project (AwesomeAssertions 9.6.0) with 11 tests and 13 assertions each.
- `references/decisions.md`: TEST-02, the decision that this migration carries out.
- `references/review-checklist.md`: read before a review of a migration PR.
- `references/sources.md`: the primary sources and the date of each check.
- `references/language.md`: the rules for all text that is not code.
