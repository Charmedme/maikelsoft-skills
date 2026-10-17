# Sources

All sources checked on 2026-10-16. The author compiled and ran the code in `examples/` with the .NET SDK 10.0.402, MSTest 4.5.1, FluentAssertions 7.2.2, and AwesomeAssertions 9.6.0.

## AwesomeAssertions

- Repository and FAQ (license promise, maintainers): https://github.com/AwesomeAssertions/AwesomeAssertions
- Release notes (8.0.0 fork, 9.0.0 namespace change, 9.3.0 MSTest 4 and `ExcludingMembersNamed`, 9.6.0): https://github.com/AwesomeAssertions/AwesomeAssertions/blob/main/docs/_pages/releases.md
- Upgrading to version 9 (namespace change, global usings, no alias): https://github.com/AwesomeAssertions/AwesomeAssertions/blob/main/docs/_pages/upgradingtov9.md
- Upgrading to version 8 (`AssertionChain`, `HttpResponseMessage` removed): https://github.com/AwesomeAssertions/AwesomeAssertions/blob/main/docs/_pages/upgradingtov8.md
- Approved public API of 9.6.0: https://github.com/AwesomeAssertions/AwesomeAssertions/blob/9.6.0/Tests/Approval.Tests/ApprovedApi/AwesomeAssertions/net8.0.verified.txt
- MSTest 4 support (issue 312): https://github.com/AwesomeAssertions/AwesomeAssertions/issues/312
- Package 9.6.0 (Apache-2.0, published 2026-08-20, all versions 7.0.0 to 9.6.0): https://www.nuget.org/packages/AwesomeAssertions
- Companion packages: https://www.nuget.org/packages/AwesomeAssertions.Web (3.0.0, Apache-2.0), https://www.nuget.org/packages/AwesomeAssertions.Json (9.0.0, Apache-2.0), https://www.nuget.org/packages/AwesomeAssertions.DataSets (9.0.0, Apache-2.0), https://www.nuget.org/packages/AwesomeAssertions.Analyzers (9.0.8, MIT)

## FluentAssertions

- Releases (7.2.2, 8.11.0): https://github.com/fluentassertions/fluentassertions/releases
- Upgrading to version 6 (enums, non-generic collections, async throw): https://github.com/fluentassertions/fluentassertions/blob/main/docs/_pages/upgradingtov6.md
- Upgrading to version 8 (`AssertionConfiguration`): https://github.com/fluentassertions/fluentassertions/blob/main/docs/_pages/upgradingtov8.md
- Version 7 test framework detection (MSTest 3 assembly name only): https://github.com/fluentassertions/fluentassertions/blob/support-7.0/Src/FluentAssertions/Execution/TestFrameworkProvider.cs
- `FluentAssertions.Web` 3.0.0 dependency range `[6.5.1, 8.0.0)`: nuspec on nuget.org

## Verified in a build

- The compile errors after a pure namespace change from FluentAssertions 7.2.2 to AwesomeAssertions 9.6.0.
- The compile errors of FluentAssertions 5.10.3 code against FluentAssertions 6.12.2.
- CS0121 when one file imports both namespaces.
- CS0246 for `global using FluentAssertions = AwesomeAssertions;`.
- `HaveAttribute("id", "2")` on an `XElement` with `id="1"` passes in AwesomeAssertions 9.6.0.
- `AllSatisfy` on an empty array passes in AwesomeAssertions 9.6.0 and fails in FluentAssertions 7.2.2.
- AwesomeAssertions 9.6.0 reports a failure as `AssertFailedException` under MSTest 4.5.1 without a bridge.
