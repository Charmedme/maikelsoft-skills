# Sources

All sources checked on 2026-10-16. The researcher read the Microsoft Learn pages. The versions and licenses come from nuget.org (registration API and nuspec files). The author compiled and ran the code in `examples/` with the .NET SDK 10.0.402 and a real Docker daemon.

On 2026-10-27, the author checked the versions again. The four test projects of `examples/` ran with MSTest.Sdk 4.5.2 and Microsoft.AspNetCore.Mvc.Testing 10.0.14 (SDK 10.0.113, real Docker daemon). All 54 tests passed.

## MSTest and Microsoft.Testing.Platform

- MSTest releases: https://github.com/microsoft/testfx/releases
- MSTest changelog (4.0.0, 4.5.1, 4.5.2, 4.5.0 not published): https://github.com/microsoft/testfx/blob/main/docs/Changelog.md
- MSTest overview and support policy: https://learn.microsoft.com/dotnet/core/testing/unit-testing-mstest-intro
- MSTest 3 to 4 migration: https://learn.microsoft.com/dotnet/core/testing/unit-testing-mstest-migration-v3-v4
- MSTest.Sdk: https://learn.microsoft.com/dotnet/core/testing/unit-testing-mstest-sdk
- Run tests (EnableMSTestRunner): https://learn.microsoft.com/dotnet/core/testing/unit-testing-mstest-running-tests
- dotnet test and the global.json runner: https://learn.microsoft.com/dotnet/core/tools/dotnet-test
- MTP v1 to v2: https://learn.microsoft.com/dotnet/core/testing/microsoft-testing-platform-migration-from-v1-to-v2 (only MTP v2 projects fail on the VSTest path)
- MTP code coverage: https://learn.microsoft.com/dotnet/core/testing/microsoft-testing-platform-code-coverage
- MTP retry extension: https://learn.microsoft.com/dotnet/core/testing/microsoft-testing-platform-retry
- Assertions: https://learn.microsoft.com/dotnet/core/testing/unit-testing-mstest-writing-tests-assertions
- Lifecycle: https://learn.microsoft.com/dotnet/core/testing/unit-testing-mstest-writing-tests-lifecycle
- Data-driven tests: https://learn.microsoft.com/dotnet/core/testing/unit-testing-mstest-writing-tests-data-driven
- Execution control (parallel, timeout): https://learn.microsoft.com/dotnet/core/testing/unit-testing-mstest-writing-tests-controlling-execution
- TestContext: https://learn.microsoft.com/dotnet/core/testing/unit-testing-mstest-writing-tests-testcontext
- MSTest analyzers: https://learn.microsoft.com/dotnet/core/testing/mstest-analyzers/overview (MSTEST0074 to MSTEST0077, checked 2026-10-27)
- Analyzer severities in Recommended mode: `globalconfigs/mstest-recommended.globalconfig` in the MSTest.Analyzers 4.5.1 package

## FluentAssertions

- Releases (7.2.2 on 2026-03-16, 8.11.0 on 2026-09-14): https://github.com/fluentassertions/fluentassertions/releases
- Release notes, main branch: https://github.com/fluentassertions/fluentassertions/blob/main/docs/_pages/releases.md
- Upgrading to version 8: https://github.com/fluentassertions/fluentassertions/blob/main/docs/_pages/upgradingtov8.md
- Version 7 object graphs: https://github.com/fluentassertions/fluentassertions/blob/support-7.0/docs/_pages/objectgraphs.md
- Version 7 exceptions: https://github.com/fluentassertions/fluentassertions/blob/support-7.0/docs/_pages/exceptions.md
- Version 7 test framework detection: https://github.com/fluentassertions/fluentassertions/blob/support-7.0/Src/FluentAssertions/Execution/TestFrameworkProvider.cs
- Version 7 `Services.ThrowException`: https://github.com/fluentassertions/fluentassertions/blob/support-7.0/Src/FluentAssertions/Common/Services.cs
- MSTest 4 support in version 8.8 (PR 3111): https://github.com/fluentassertions/fluentassertions/pull/3111
- License of 7.2.2 (Apache-2.0) and 8.11.0 (Xceed, license acceptance): nuspec files on nuget.org

## AwesomeAssertions

- Repository and FAQ (license promise, maintainers): https://github.com/AwesomeAssertions/AwesomeAssertions
- Release notes (9.0.0 namespace change, 9.3.0 MSTest 4, 9.6.0): https://github.com/AwesomeAssertions/AwesomeAssertions/blob/main/docs/_pages/releases.md
- Upgrading to version 9: https://github.com/AwesomeAssertions/AwesomeAssertions/blob/main/docs/_pages/upgradingtov9.md
- Approved public API of 9.6.0: https://github.com/AwesomeAssertions/AwesomeAssertions/blob/9.6.0/Tests/Approval.Tests/ApprovedApi/AwesomeAssertions/net8.0.verified.txt
- Package (9.6.0, Apache-2.0, published 2026-08-20): https://www.nuget.org/packages/AwesomeAssertions
- Companion packages: https://www.nuget.org/packages/AwesomeAssertions.Web, https://www.nuget.org/packages/AwesomeAssertions.Json, https://www.nuget.org/packages/AwesomeAssertions.Analyzers

## Testcontainers and Respawn

- Releases (4.16.0 on 2026-10-09): https://github.com/testcontainers/testcontainers-dotnet/releases
- Documentation: https://dotnet.testcontainers.org/
- Best practices: https://github.com/testcontainers/testcontainers-dotnet/blob/develop/docs/api/best_practices.md
- Wait strategies: https://github.com/testcontainers/testcontainers-dotnet/blob/develop/docs/api/wait_strategies.md
- Resource reuse: https://github.com/testcontainers/testcontainers-dotnet/blob/develop/docs/api/resource_reuse.md
- CI setup: https://github.com/testcontainers/testcontainers-dotnet/blob/develop/docs/cicd/index.md
- Image argument required since 4.10 (discussion 1470): https://github.com/testcontainers/testcontainers-dotnet/discussions/1470
- Respawn: https://github.com/jbogard/Respawn
- Respawn 7.0.0: https://github.com/jbogard/Respawn/releases/tag/v7.0.0
- SQL Server image tags: https://mcr.microsoft.com/v2/mssql/server/tags/list

## ASP.NET Core and EF Core

- Integration tests (MSTest tab): https://learn.microsoft.com/aspnet/core/test/integration-tests?view=aspnetcore-10.0
- EF Core testing strategy (in-memory provider discouraged): https://learn.microsoft.com/ef/core/testing/choosing-a-testing-strategy

## Test doubles and time

- NSubstitute releases (6.2.0): https://github.com/nsubstitute/NSubstitute/releases
- Moq releases (4.21.0): https://github.com/devlooped/moq/releases
- Moq SponsorLink (issue 1372): https://github.com/devlooped/moq/issues/1372
- FakeTimeProvider: https://learn.microsoft.com/dotnet/core/extensions/timeprovider-testing
- TimeProvider overview: https://learn.microsoft.com/dotnet/standard/datetime/timeprovider-overview

## Design and strategy

- Unit testing best practices (names, AAA, coverage): https://learn.microsoft.com/dotnet/core/testing/unit-testing-best-practices
- The testing trophy: https://kentcdodds.com/blog/the-testing-trophy-and-testing-classifications
- Martin Fowler, testing guide: https://martinfowler.com/testing
- Stryker.NET 5.0.0: https://github.com/stryker-mutator/stryker-net/releases/tag/dotnet-stryker%405.0.0
- Stryker.NET configuration: https://stryker-mutator.io/docs/stryker-net/configuration/
- DORA, trunk-based development: https://dora.dev/capabilities/trunk-based-development/

## Credits

The author copied no text or code. The structure of some rules follows public skills, paraphrased:

- dotnet/skills, `plugins/dotnet-test` (MIT): https://github.com/dotnet/skills/tree/main/plugins/dotnet-test. Idea of a version gate per API and of the anti-pattern list.
- github/awesome-copilot, `skills/csharp-mstest` (MIT): https://github.com/github/awesome-copilot/blob/main/skills/csharp-mstest/SKILL.md. Idea of sealed test classes and constructor setup.
