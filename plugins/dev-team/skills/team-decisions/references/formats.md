# Decision formats

## Contents

- decision-sheet.md
- docs/dev-team/decisions.md
- ADR
- Profile

## decision-sheet.md

One sheet per story, in the run folder. The senior writes the first five columns. The main session writes Answer and By after G1.

```markdown
# Decision sheet: <story title>

## Decisions
| ID | Question | Recommendation | Reason | Source | Answer | By |
|----|----------|----------------|--------|--------|--------|----|
| API-01 | Minimal APIs or controllers for new endpoints? | Minimal APIs | Default of `dotnet new webapi`; `AddValidation` works only for minimal APIs | profile | | |

## New packages
| Package | Version | License | Reason | Exists | Answer | By |
|---------|---------|---------|--------|--------|--------|----|

## Reopened
| ID | Old answer | New recommendation | Reason | Answer | By |
|----|------------|--------------------|--------|--------|----|
```

- Source: `profile` (an earlier answer of this human) or `skill` (the recommendation of the stack skill).
- Answer: the chosen option. By: `human`, or `answer sheet` in a test run.
- Write "None." under an empty section.

## docs/dev-team/decisions.md

The index of the repository. `decisioncheck.py` reads the first table, with exactly these columns.

```markdown
# Team decisions

| ID | Question | Answer | Status | By | ADR | Check | Date |
|----|----------|--------|--------|----|-----|-------|------|
| API-01 | Minimal APIs or controllers for new endpoints? | Minimal APIs | accepted | human | `../decisions/0001-minimal-apis.md` | ASP-DET-API-01 | 2026-10-18 |
| TEST-01 | Test framework | MSTest 4 | assumed | senior | - | tests/Orders.Tests/Orders.Tests.csproj | 2026-10-18 |
```

- Status: `accepted`, `assumed`, `superseded`, or `rejected`.
- ADR: the path of the ADR file relative to `docs/dev-team/`, as a Markdown link or in backticks. Required for `accepted` and `superseded`.
- Check: the detect rule ID, or the file that shows an `assumed` choice, or `-`.

## ADR

Use `dev-docs:doc-adr` when it is installed. Else use MADR 4.0.0 in the ADR folder of the repository (default `docs/decisions/NNNN-<title>.md`).

```markdown
---
status: accepted
date: 2026-10-18
decision-makers: <human>
---

# Use minimal APIs for new endpoints

## Context and Problem Statement
<why the team must choose, and the story that needed it>

## Considered Options
- Minimal APIs
- Controllers

## Decision Outcome
Chosen option: "Minimal APIs", because <reason>.

### Consequences
- Good, because ...
- Bad, because ...

## More Information
Decision ID: API-01. Source: `dev-stack:aspnetcore` references/decisions.md.
```

A superseded ADR gets `status: superseded by ADR-NNNN` and, in More Information, a line with the file of the new ADR, for example `Superseded by ADR-0007 (0007-controllers-for-odata.md).` The new ADR links back to the old file. `decisioncheck.py` checks both links.

## Profile

File `~/.claude/dev-team/profile.md`. It is personal: it is not in the repository.

```markdown
# Dev-team profile

| ID | Answer | Date |
|----|--------|------|
| API-01 | Minimal APIs | 2026-10-18 |
```

One row per ID. A new answer replaces the row.
