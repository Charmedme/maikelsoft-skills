# Changelog

This file records all notable changes to the dev-stack plugin in the maikelsoft-skills marketplace.

The format follows [Keep a Changelog 1.1.0](https://keepachangelog.com/en/1.1.0/). The plugin uses [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [1.0.0] - 2026-10-18

### Added

- First release of the dev-stack plugin for Claude Code, with seven skills: `csharp`, `aspnetcore`, `efcore`, `dotnet-testing`, `dotnet-analyzers`, `secure-coding`, and `migrate-fluentassertions`.
- Shared stack rules in each skill (`references/stack-rules.md`): package approval, no MediatR and no AutoMapper, branch and commit without push, team decisions, and one report format.
- A check step in each skill with `stackcheck.py` and the tool rules in `rules.json`. `dotnet-analyzers` also has `sarif.py`.
- The report rules: run the checks again after the last change, and show only the results of that last run. A "Cause:" line for a fix of a problem that is not obvious. Each recommendation says what to change, how, and why.
- `secure-coding` reviews use the common review template, with the verdicts PASS, PASS_WITH_FINDINGS, and BLOCKED.
- `secure-coding` rule 10: antiforgery only for an endpoint with cookie sign-in. An API with bearer or API-key sign-in uses `.DisableAntiforgery()` with a reason. Rule 10a: when the sign-in decision is open, keep the current sign-in setup and put the missing sign-in as a blocking open point.
- `dotnet-analyzers` rule 18a: the report explains each rule that the agent raises, lowers, or turns off. It says what the rule prevents and why the change fits the project.
- `efcore` rule 32a: with one idempotent migration script on PostgreSQL, build a non-blocking index from a pre-deploy SQL file, and use `CREATE INDEX IF NOT EXISTS` in the migration.
- `csharp` rule 36a: bind a map or an enum from configuration as `Dictionary<string, string>` and convert each entry. The configuration binder skips entries that it cannot convert, without an error.
- An eval set in `evals/dev-stack/`: 18 cases, fixtures, probe tests, `score.py`, `prepare.py`, `consistency.py`, and the grader prompt.
