# Review checklist

The full list for a review of a migration from FluentAssertions to AwesomeAssertions. `tool` means a tool finds it. `judge` means a reviewer reads the diff.

| # | Check | How |
|---|-------|-----|
| 1 | The human approved the package change: name, version, license, reason. | judge (log or PR) |
| 2 | Each changed project references `AwesomeAssertions` 9.6.0 and no `FluentAssertions*` package. | tool (MFA-PKG-FA-LEFT) |
| 3 | Companion packages moved too: `AwesomeAssertions.Web`, `AwesomeAssertions.Json`. | tool (MFA-PKG-FA-LEFT) |
| 4 | The code has no `using FluentAssertions` and no `<Using Include="FluentAssertions">`. | tool (MFA-USING-FA) |
| 5 | The code has no `Services.ThrowException` and no empty `[AssemblyInitialize]`. | tool (MFA-BRIDGE-LEFT), judge |
| 6 | `dotnet build -warnaserror` passes. | tool (build) |
| 7 | The test count is the same as before. | tool (test run) |
| 8 | The assertion count (`.Should()`) is the same as before, or the PR explains each change. | tool (grep count), judge |
| 9 | No expected value or `because` text changed to make a test pass. | judge |
| 10 | No `HaveAttribute(name, value)` on `XElement`. | tool (MFA-XML-HAVEATTRIBUTE) |
| 11 | `AllSatisfy` and `OnlyContain` on collections that must have items have `NotBeEmpty()`. | tool (MFA-EMPTY-ALLSATISFY), judge |
| 12 | Global options moved to `AssertionConfiguration.Current.Equivalency.Modify` in `[AssemblyInitialize]`. | tool (compiler), judge |
| 13 | `ComparingByMembers<T>()` stays where a class overrides `Equals`. | judge |
| 14 | Custom assertion classes use `AssertionChain`, not `Execute.Assertion`. | tool (compiler), judge |
| 15 | No FluentAssertions 8 member that AwesomeAssertions 9.6 does not have. | tool (compiler) |
| 16 | A test that fails on purpose shows an assertion failure, not "threw exception". | judge |
| 17 | One commit per project, each with green tests. | judge |
