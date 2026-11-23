# test-stack

test-stack is a Claude Code plugin with 4 stack skills for tests. They cover .NET unit and integration tests, browser tests with Playwright, and Gherkin scenarios with Reqnroll. Each skill tells the agent what is true for this version of the stack and what the agent usually gets wrong. Each skill also has a check step with a tool and one report format.

English output follows ASD-STE100 Simplified Technical English. Dutch output follows Begrijpelijk Nederlands (taalniveau B1).

## Install

You need Claude Code, the .NET 10 SDK, and Python 3.9 or later. For integration tests, you also need Docker. For Playwright in TypeScript, you need Node.js 22 or later.

```sh
claude plugin marketplace add Charmedme/maikelsoft-skills
claude plugin install test-stack@maikelsoft-skills
```

The rules for the code under test are in dev-stack.

## Quick start

1. Open Claude Code in a .NET repository.
2. Ask for tests in your own words:

   ```
   Write the tests for the cancel endpoint, with a real database.
   ```

3. Claude Code loads `dotnet-testing`. The agent writes the tests, runs them, and writes a report.

## Commands

| Command | What it does | Starts by itself when you ask for |
|-------|------------|-----------------------------------|
| `/test-stack:dotnet-testing` | MSTest 4 or xUnit v3 on Microsoft.Testing.Platform, FluentAssertions 7.2.2 or AwesomeAssertions 9, fakes, `FakeTimeProvider`, `WebApplicationFactory`, and Testcontainers. The move from xUnit v2 to v3. | .NET tests written, reviewed, or fixed, or a test project set up |
| `/test-stack:playwright` | Playwright 1.65 in TypeScript and in .NET (MSTest 4 or xUnit v3): locators, web-first assertions, sign-in, test data, the app under test, traces, and CI. Playwright MCP and `playwright-cli` for coding agents. | Browser or end-to-end tests, a flaky browser test, or a browser for the agent |
| `/test-stack:reqnroll` | Reqnroll 3 on MSTest 4 or xUnit v3, with the Gherkin rules. Declarative steps, context injection, hooks, Playwright in the steps, and the move from SpecFlow. | Feature files, step definitions, or acceptance tests in Gherkin |
| `/test-stack:migrate-fluentassertions` | The step-by-step move of a test project from FluentAssertions 5, 6, or 7 to AwesomeAssertions 9. | Never: it changes packages, so only its command `/test-stack:migrate-fluentassertions` starts it |

## Moved from dev-stack

`dotnet-testing` and `migrate-fluentassertions` come from dev-stack 1.3.0. The old names in dev-stack 2.x send the request here.

## Test results

The test results of `dotnet-testing` 1.x are in the README of dev-stack (versions 1.0.0 to 1.2.0). The behaviour test of version 1.1.0 is in `docs/work/stack-split/results.md` (phase 3). `migrate-fluentassertions` has no eval case yet.
