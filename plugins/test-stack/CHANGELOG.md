# Changelog

This file records all notable changes to the test-stack plugin in the maikelsoft-skills marketplace.

The format follows [Keep a Changelog 1.1.0](https://keepachangelog.com/en/1.1.0/). The plugin uses [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [1.1.0] - 2026-11-21

### Added

- `playwright`: browser tests with Playwright 1.65. It has one guide for Playwright Test in TypeScript and one for Playwright for .NET with MSTest 4 or xUnit v3. A third guide covers Playwright MCP and `playwright-cli` for coding agents. It covers locators, web-first assertions, a sign-in setup project, test data, and the app under test with `UseKestrel(0)` and `StartServer()`. Browser settings on Microsoft.Testing.Platform, traces, and CI are in it too.
- `playwright`, MSTest 4: the skill uses `Microsoft.Playwright.MSTest.v4`. The old package builds, but the run finds 0 tests.
- `reqnroll`: acceptance tests with Reqnroll 3 on MSTest 4 or xUnit v3, with the Gherkin rules. It covers declarative steps, context injection, and a browser context for each scenario with a trace only for a failed scenario. It also covers the move from SpecFlow.

### Changed

- `dotnet-testing` covers xUnit v3 4.x next to MSTest 4. It reads the framework of the project and uses the matching guide. A new project still uses MSTest 4. The xUnit guide has the move from xUnit v2.
- `dotnet-testing`: MSTest 4.5.3. A `DataRow` value for a `decimal` parameter fails at run time, also an `int` value without a warning. The skill gives the forms that work. Also new: analyzers MSTEST0084 to 0089, and `dotnet test --no-build` on Microsoft.Testing.Platform.

## [1.0.0] - 2026-11-18

The first version. The skills come from dev-stack 1.3.0.

### Added

- `dotnet-testing`: moved from dev-stack 1.3.0 without a change to its rules.
- `migrate-fluentassertions`: moved from dev-stack 1.3.0 without a change to its rules.

[Unreleased]: https://github.com/Charmedme/maikelsoft-skills/compare/test-stack-v1.1.0...HEAD
[1.1.0]: https://github.com/Charmedme/maikelsoft-skills/compare/test-stack-v1.0.0...test-stack-v1.1.0
[1.0.0]: https://github.com/Charmedme/maikelsoft-skills/releases/tag/test-stack-v1.0.0
