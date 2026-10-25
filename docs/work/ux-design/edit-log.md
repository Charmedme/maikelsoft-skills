# Edit log: ux-design

This log records each change to a skill or to an eval case, the test that measured it, and a verdict. The format comes from the experience step of Harness² (google-research/harness2). The goal: each claim in a CHANGELOG or README can point to its evidence, and no lesson rests on a guess.

## Rules

- One entry for each change: one rule, one tool rule, one report line, or one eval item.
- Test one change at a time when you can. When one test covers more than one change, the verdict of each change is `confounded`.
- Verdicts:
  - `helped`: the test isolates the change, and the result is better.
  - `hurt`: the test isolates the change, and the result is worse.
  - `no observed effect`: the test isolates the change, and the result is the same.
  - `confounded`: the test does not isolate the change, or one run is all the evidence.
  - `proposed`: not tested yet.
- Prefer `confounded` when you are not sure. A wrong `helped` misleads each later change.
- A count of 1 of 3 runs is not enough for `helped` or `hurt`. Use 5 runs on the case.
- At most one lesson for each round. It has a cue (the kind of situation) and the lesson (what to change, in which component). It also has the evidence (the entries) and a confidence.

## Entry format

```
### E<n>: <skill or eval> <component>: <short change>
- Date and version: <date>, <plugin version>
- Change: <what changed>
- Test: <cases, variants, runs, model>
- Observed: <counts, before and after>
- Verdict: <helped | hurt | no observed effect | confounded | proposed>
- Evidence: <path to the results>
```

## Entries

### E1: ux-practice states: Default state is the start of the main task
- Date and version: 2026-10-24, v2 (released as 0.1.0)
- Change: Default state is the start of the main task.
- Test: Iteration 2, all cases, baseline and v2, 3 runs, Opus.
- Observed: The form no longer starts filled in. Other v2 changes ran in the same test.
- Verdict: confounded
- Evidence: `evals/ux-design/results-2026-10-24.md`

### E2: ux-practice opportunities: 1 to 3 opportunities from product facts
- Date and version: 2026-10-24, v2 (released as 0.1.0)
- Change: 1 to 3 opportunities from product facts, as proposed decisions.
- Test: Iteration 2, all cases, 3 runs, Opus.
- Observed: Opportunities appear in each run. Product detail of the mockups did not improve (5-30).
- Verdict: confounded
- Evidence: `evals/ux-design/results-2026-10-24.md`

### E3: ui-craft foundation: Foundation recipe for a project without a design system
- Date and version: 2026-10-24, v2 (released as 0.1.0)
- Change: Foundation recipe for a project without a design system.
- Test: Iteration 2, empty fixture, baseline and v2, 3 runs, Opus.
- Observed: Shared style files: 0 of 6 runs before, 6 of 6 after. Determinism 0.72-0.78 before, 0.94-1.00 after.
- Verdict: confounded
- Evidence: `evals/ux-design/results-2026-10-24.md`

### E4: ui-craft system audit: Audit of an existing design system
- Date and version: 2026-10-24, v2 (released as 0.1.0)
- Change: Audit of an existing design system, with design-system-issues.md and ds-fixes.css.
- Test: Iteration 2, faulty fixture, baseline and v2, 3 runs, Opus.
- Observed: Faults copied into the mockups: 6-7 before, 0 after.
- Verdict: confounded
- Evidence: `evals/ux-design/results-2026-10-24.md`

### E5: ui-craft method: Business rules at the point of decision
- Date and version: 2026-10-24, v3 (released as 0.1.0)
- Change: Business rules at the point of decision, a result summary, numbered sections, realistic data.
- Test: Iteration 3, 5 cases, v2 and v3, 3 runs, Sonnet.
- Observed: Clarity v3 19-11, product detail v3 21-7. States v2 18-9.
- Verdict: confounded
- Evidence: `evals/ux-design/results-2026-10-24.md`

### E6: ux-states error kinds: Each error kind is its own state
- Date and version: 2026-10-24, v4 (released as 0.1.0)
- Change: Each error kind is its own state.
- Test: Iteration 4, 5 cases, v3 and v4, 3 runs, Sonnet.
- Observed: Mockup states v4 26-2.
- Verdict: confounded
- Evidence: `evals/ux-design/results-2026-10-24.md`

### E7: ux-practice failure paths: Failure paths table
- Date and version: 2026-10-24, v4 (released as 0.1.0)
- Change: Failure paths table, checked by uxcheck.py against the states.
- Test: Iteration 4, 5 cases, v3 and v4, 3 runs, Sonnet.
- Observed: Design thought through v4 22-8, buildable v4 20-9.
- Verdict: confounded
- Evidence: `evals/ux-design/results-2026-10-24.md`

### E8: ui-craft opportunities: Each opportunity in its own proposed-op frame
- Date and version: 2026-10-24, v4 (released as 0.1.0)
- Change: Each opportunity in its own proposed-op frame.
- Test: Iteration 4, 5 cases, v3 and v4, 3 runs, Sonnet.
- Observed: MK-PROPOSED issues: 0 in v4. Clarity v3 18-12.
- Verdict: confounded
- Evidence: `evals/ux-design/results-2026-10-24.md`

### E10: ux-prototype: Prototype mode on the shared shell
- Date and version: 2026-10-25, ux-design 0.2.0
- Change: Prototype mode on the shared shell.
- Test: Prototype, FL-31, baseline and agent v1, 3 runs, Opus.
- Observed: Agent v1 lost the question 0-6 and test readiness 0-6.
- Verdict: hurt
- Evidence: `evals/ux-design/results-2026-10-25.md`

### E11: ux-prototype question: Rank the candidate questions by risk
- Date and version: 2026-10-25, ux-design 0.2.0
- Change: Rank the candidate questions by risk.
- Test: Prototype, FL-31, v1 and v2, 3 runs, Opus.
- Observed: Question v2 6-0 against v1, and 4-1 against the baseline. All v2 runs took the risk question. Changed together with E11 and E12.
- Verdict: confounded
- Evidence: `evals/ux-design/results-2026-10-25.md`

### E12: ux-prototype test plan: Test-plan template with pass rules and decisions
- Date and version: 2026-10-25, ux-design 0.2.0
- Change: Test-plan template with pass rules and decisions.
- Test: Same test as E10.
- Observed: Test readiness v2 6-0 against v1, 3-2 against the baseline.
- Verdict: confounded
- Evidence: `evals/ux-design/results-2026-10-25.md`

### E13: ux-prototype states: Add the failure states of the path
- Date and version: 2026-10-25, ux-design 0.2.0
- Change: Add the failure states of the path.
- Test: Same test as E10.
- Observed: States v2 6-0 against v1 and against the baseline. 13 to 16 states, against 7 to 8.
- Verdict: confounded
- Evidence: `evals/ux-design/results-2026-10-25.md`

### E14: product-prototype: Move to the shared shell
- Date and version: 2026-10-25, ux-design 0.2.0
- Change: Move to the shared shell.
- Test: Prototype, FL-31, old and new, 3 runs, Opus.
- Observed: Test readiness new 5-1, states 4-2, fidelity 4-2. Question old 4-0.
- Verdict: confounded
- Evidence: `evals/ux-design/results-2026-10-25.md`

### E15: product-prototype question: Rank the open points by the cost of a wrong answer
- Date and version: 2026-10-25, ux-design 0.2.0
- Change: Rank the open points by the cost of a wrong answer.
- Test: Prototype, FL-31, shell v1 and v2, 3 runs, Opus.
- Observed: Question v2 4-0 against shell v1, and 4-0 against the old version.
- Verdict: helped
- Evidence: `evals/ux-design/results-2026-10-25.md`

### E16: ux-review: Review mode with a walk file, screenshots, axe-core
- Date and version: 2026-10-25, ux-design 0.2.0
- Change: Review mode with a walk file, screenshots, axe-core.
- Test: Review, 6 easy and 6 subtle defects, baselines on Opus and Sonnet, 3 runs.
- Observed: The same defects found as the baselines. Adds a verdict, severities, screenshots, and a walk file.
- Verdict: no observed effect
- Evidence: `evals/ux-design/results-2026-10-25.md`

### E17: ux-review walk: Each state ends with expect_text checks inside the element of the state
- Date and version: 2026-10-25, ux-design 0.2.0
- Change: Each state ends with expect_text checks inside the element of the state.
- Test: Review, subtle defects, Sonnet v1, v2, v3, 3 runs.
- Observed: 14, 16 (v2), 15 (v3, short prompt) of 18. D5 not found: the design is not clear on it.
- Verdict: confounded
- Evidence: `evals/ux-design/results-2026-10-25.md`

### E18: example-mapping: One example for each state of the UX design, source ux:
- Date and version: 2026-10-25, ux-design 0.2.0
- Change: One example for each state of the UX design, source ux:.
- Test: Handoff, 3 stories, old and new, 2 runs, Sonnet.
- Observed: States covered 100% (6 of 6 runs) against 53% to 76%.
- Verdict: helped
- Evidence: `evals/ux-design/results-2026-10-25.md`

### E19: uxcheck.py: Skip invisible elements in the tell checks
- Date and version: 2026-10-25, ux-design 0.2.0
- Change: Skip invisible elements in the tell checks.
- Test: The clean app of the review test.
- Observed: 2 false results (a hidden date input) gone.
- Verdict: helped
- Evidence: `evals/ux-design/results-2026-10-25.md`

## Lesson of batch 1

- Cue: a design skill adds completeness rules (more states, more failure paths).
- Lesson: in ui-craft, keep the visual rules (rules at the point of decision, numbered steps) in the same version. More frames make the screens denser and cost clarity.
- Evidence: E5 to E8.
- Confidence: low. Each round tested more than one change.

## Lesson of batch 2

- Cue: a skill picks one item from a list of candidates, for example the question of a prototype.
- Lesson: give the skill the ranking rule (the cost of a wrong answer), not only the list. Without the rule, the model takes the first clear item.
- Evidence: E10, E14.
- Confidence: medium. Two skills, the same cause, and the same fix.
