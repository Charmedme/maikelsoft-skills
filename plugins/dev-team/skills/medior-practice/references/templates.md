# Medior templates

Use the headings as written, in this order. Write "None." under an empty heading.

## Contents

- NN-plan.md
- NN-report.md
- Log row

## NN-plan.md

```markdown
# Plan: package NN, <title>

Status: PLAN_READY

## Goal
<one sentence>

## Acceptance criteria
<verbatim from the package>

## Files
| File | Change | Owned |
|------|--------|-------|

## Tests
| Test name | Criterion | Kind (unit / integration) |
|-----------|-----------|---------------------------|

## Risks and deviations
<each with the reason, or "None.">
```

## NN-report.md

```markdown
# Report: package NN, <title>

Status: DONE

## Done
<files and what they do, one line each>

## Acceptance criteria
| Criterion (verbatim) | Test (path:line) | Result |
|----------------------|------------------|--------|

## Plan feedback applied
<each change of the feedback and what you did>

## Checks
<the table of NN-checks.md, from the run after the last change>

## Commits
<hash and message>

## Open points
<or "None.">

## Fix
<only after mode fix: each blocking comment and what you changed>
```

## Log row

```markdown
| 11:20 | medior | 01 build | DONE | src/Orders/CancelOrder.cs, tests/Orders/CancelOrderTests.cs | 3f2a9c1 | build pass, test pass, format pass | - | - | package built, all criteria tested |
```
