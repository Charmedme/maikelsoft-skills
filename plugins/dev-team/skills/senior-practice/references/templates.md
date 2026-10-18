# Senior templates

Use the headings as written, in this order. Write "None." under an empty heading.

## Contents

- design.md
- Work package
- reflection.md
- core-report.md
- Plan feedback
- Review (core-review.md, NN-review.md)
- Log row

## design.md

```markdown
# Design: <story title>

Status: DESIGN_READY

## Summary
<3 to 5 lines: what changes, where, and why this design>

## Acceptance criteria
| # | Criterion (verbatim from story.md) | Design part | Package | Test |
|---|------------------------------------|-------------|---------|------|

## Current facts
| Fact | Value | Source | Date |
|------|-------|--------|------|

## Design
<components, data, endpoints, and flow. Name each file that changes.>

## Trade-offs
<each real choice: the options, the choice, the reason>

## Threat model
- New input: ...
- Who may call it: ...
- Data it touches: ...
- What can go wrong: ...
- What stops it: ...

## Work split
- Core (senior): <files and reason>
- Packages: <NN title, size, one line each, or "None.">

## Decisions
<ID, decision, status (accepted / open). The open rows are in decision-sheet.md.>

## Questions for the human
<only facts that the human knows. Each with the reason. Or "None.">

## Answers
<written by the team lead after NEEDS_HUMAN>

## Reflection
<written in apply-reflection: each improvement, accepted or rejected, and the reason>
```

## Work package

File `packages/NN-<slug>.md`:

```markdown
# Package NN: <title>

Size: standard | hard
Depends on: <NN or none>

## Goal
<one sentence>

## Acceptance criteria
<the criteria that this package covers, verbatim>

## Owned files
<files that this package may change or add>

## Contracts
<types, endpoints, method signatures that other parts use. They do not change in the build.>

## Skills to load
<dev-stack skills>

## Notes
<facts from the design that the medior needs>
```

## reflection.md

```markdown
# Reflection: <story title>

Status: REFLECTION_READY

## Improvements
| # | Effect | What to change | How | Why |
|---|--------|----------------|-----|-----|

## Good as it is
<one line, or "None.">
```

## core-report.md

```markdown
# Core report: <story title>

Status: CORE_DONE

## Done
<files and what they do, one line each>

## Decisions logged
<ID, ADR path>

## Checks
<the table from checks.py, from the run after the last change>

## Commits
<hash and message>

## Open points
<or "None.">
```

## Plan feedback

File `packages/NN-plan-feedback.md`:

```markdown
# Plan feedback: package NN

Status: PLAN_APPROVED | PLAN_APPROVED_WITH_CHANGES

## Changes
1. <what to change, how, and why>

## Good as it is
<one line>
```

## Review (core-review.md, NN-review.md)

```markdown
# Review: <core or package NN>

Status: APPROVED | APPROVED_WITH_COMMENTS | CHANGES_REQUIRED

## Story verdict
| Criterion (verbatim) | Code (path:line) | Test (path:line) | Met |
|----------------------|------------------|------------------|-----|

## Code verdict
<one paragraph: conventions, readability, logic, tests, security, current APIs>

## Checks
<the result of checks.py that you ran>

## Comments
| # | Label | Location | What | Why | Suggestion |
|---|-------|----------|------|-----|------------|
```

## Log row

```markdown
| 10:42 | senior | design | DESIGN_READY | design.md, decision-sheet.md, packages/01-cancel.md | - | - | API-01 open | - | design done, one decision open |
```
