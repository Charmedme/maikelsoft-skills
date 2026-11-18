# test-stack

test-stack is a Claude Code plugin with stack skills for tests in .NET. It has 2 skills. Each skill tells the agent what is true for this version of the stack and what the agent usually gets wrong. Each skill also has a check step with a tool and one report format.

English output follows ASD-STE100 Simplified Technical English. Dutch output follows Begrijpelijk Nederlands (taalniveau B1).

## Install

You need Claude Code, the .NET 10 SDK, and Python 3.9 or later. For integration tests, you also need Docker.

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
| `/test-stack:dotnet-testing` | MSTest 4 on Microsoft.Testing.Platform, FluentAssertions 7.2.2 or AwesomeAssertions 9, fakes, `FakeTimeProvider`, `WebApplicationFactory`, and Testcontainers. | .NET tests written, reviewed, or fixed, or a test project set up |
| `/test-stack:migrate-fluentassertions` | The step-by-step move of a test project from FluentAssertions 5, 6, or 7 to AwesomeAssertions 9. | Never: it changes packages, so only its command `/test-stack:migrate-fluentassertions` starts it |

## Moved from dev-stack

Both skills come from dev-stack 1.3.0, without a change to their rules. The old names in dev-stack 2.x send the request here.

## Test results

The test results of `dotnet-testing` are in the README of dev-stack (versions 1.0.0 to 1.2.0). `migrate-fluentassertions` has no eval case yet.
