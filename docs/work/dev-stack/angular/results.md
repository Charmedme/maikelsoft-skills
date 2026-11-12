# Test results: the angular skill (dev-stack 1.3.0)

This page gives the test results of the `angular` skill. The decisions are in `plugins/dev-stack/skills/angular/brief.md`.

## Method

- Cases: `evals/dev-stack/cases/angular-delete.json`, `angular-edit-hard.json`, and `angular-review-hard.json`.
- Variants: both variants get the official skill `angular-developer` (angular/skills, commit 39cea51). The variant "skill" also gets `dev-stack:angular`. So the test measures only what our skill adds.
- Runs: 3 runs for each case and variant, on Sonnet 5.5.
- Script checks: `ng build`, `ng test`, the count of new tests, a search for old APIs, and hidden browser probes with Playwright and axe-core 4.14.0.
- Grader items: one blind grader for each run. The grader does not know the variant.

## Results

Version 1 is the first build. Version 2 adds three lines to the report section: the Angular version, each new UI text, and the focus target of each action. Only the skill variant of the delete and edit cases ran again for version 2.

| Case | Items | Official skill only | Skill v1 | Skill v2 |
|------|-------|---------------------|----------|----------|
| Delete | Script checks (12 for each run) | 8, 8, 8 | 12, 12, 12 | 12, 12, 11 (1) |
| Delete | Grader items (4 for each run) | 6 of 12 | 6 of 12 | 12 of 12 |
| Edit (hard) | Script checks (13 for each run) | 11, 9, 9 | 13, 13, 13 | 13, 13, 13 |
| Edit (hard) | Grader items (4 for each run) | 3 of 12 | 9 of 12 | 11 of 12 |
| Review (hard) | Planted mistakes found (12 planted) | 9, 7, 7 | 11, 11, 11 | not run (2) |
| Review (hard) | Grader items (7 for each run) | 15 of 21 | 21 of 21 | not run (2) |

1. The check "dialog texts are Dutch in Dutch" stopped on a 15-second time-out. The full self-test ran on the same machine at that time. The check passed when it ran again alone.
2. The change of version 2 is in the report section only. The review case does not use that section.

### What the official skill alone did not do

- Delete: after a cancel or a delete, the focus went to the page body (3 of 3 runs). The code made the live region together with its text, so a screen reader does not announce it (3 of 3).
- Edit: no `aria-invalid` on the field after a server error (3 of 3). After a submit with errors, the focus stayed on Save (1 of 3). In the other runs, the code disabled Save while the form was invalid (2 of 3). In those 2 runs, the axe probe also failed, because it could not click Save.
- Edit: the read did not use `httpResource` (3 of 3). The report did not say that the list loaded only once (3 of 3).
- Review: the official skill alone missed the subtle mistakes P5 and P12 in each run, and P3, P4, or P7 in some runs. Both variants missed P10 in each run. One baseline run flagged a decoy, and one gave a wrong reason for `withFetch()`.

### Grader items that version 2 still misses

- Edit J3, 1 of 3 runs: the code made the list fresh, and the probe passed. But the report did not say that the list loaded only once.

## Trigger test

10 queries, 6 runs each, with the description of `SKILL.md`.

| Query type | Result |
|------------|--------|
| 5 Angular tasks (must trigger) | 6 of 6 each |
| ASP.NET Core endpoint, UX design, React | 0 of 6 each |
| AngularJS 1.x | 2 of 6 (the limit is 2) |
| JWT token storage in an Angular app | 4 of 6, next to `secure-coding` in 6 of 6 |

The JWT query loads both skills. We accept this, because `secure-coding` loads too and the rules do not conflict.

## Cost

| Case | Official skill only | Skill v2 |
|------|---------------------|----------|
| Delete | $0.27 to $0.29 | $0.33 to $0.39 |
| Edit (hard) | $0.36 to $0.49 | $0.59 to $0.70 |
| Review (hard) | about $0.24 to $0.28 | about $0.24 to $0.28 |

## Changes to the test tools

- `run.py` sends the prompt on stdin. Before, `pkill -f` in one run matched the prompt text of the other runs, and stopped 4 runs.
- `run.py` clones the pinned official skill into `evals/dev-stack/.cache/` and gives it to both variants (the field `base_skill` of a case).
- The delete probe accepts an inline confirmation in the row, not only a dialog. Before this fix, the 3 baseline runs failed 8 checks for a valid design choice. The probe also accepts the Dutch word "Annuleren".
- `score.py` skips `.claude-flow/` and `.remember/`. The hooks of the plugins of the operator write these folders, and the review check "files unchanged" failed on them.
- The self-test of the two Angular cases gives 58 of 58 expected results.

## Limits

- One fixture app: Angular 22.2 with zone.js, Transloco, and a `BehaviorSubject` service. No test on Angular 21, NgRx, Angular Material, or a zoneless app.
- Sonnet only. The gain can be different on other models.
- 3 runs for each variant. A count of 1 of 3 is not enough for a firm claim.
- The browser probes wait at most 15 seconds. On a busy machine, a probe can stop on a time-out.
- The field test on a real project is still open.
