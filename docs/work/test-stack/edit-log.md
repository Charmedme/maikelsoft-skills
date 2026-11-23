# Edit log: test-stack

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

Entries for `dotnet-testing` and `migrate-fluentassertions` before test-stack 1.1.0 are in `docs/work/dev-stack/edit-log.md`.

### E1: playwright
- Date and version: 2026-11-21, test-stack 1.1.0
- Change: a new skill for Playwright 1.65 with 3 guides. The guides are TypeScript, .NET (MSTest 4 with `Microsoft.Playwright.MSTest.v4`, or xUnit v3), and Playwright MCP or `playwright-cli`. The .NET code comes from the compile check.
- Test: scenarios S1, S2, and S3 of the behaviour workflow. Each arm had 6 runs (2 Haiku, 2 Sonnet, 2 Opus), with script checks and a blind judge. Also the trigger check (5 start and 5 skip queries, Sonnet).
- Observed: S1 1.00 and 8.7 (baseline) against 1.00 and 9.0 (new). S2 0.71 and 4.5 against 1.00 and 9.0. S3 0.77 and 4.8 against 0.98 and 7.5. Trigger 4 of 5, quiet 5 of 5.
- Verdict: helped (S2, S3). No observed effect on S1
- Evidence: `docs/work/stack-split/results.md` (phase 3)

### E2: playwright rules 16 and 17: pinned versions in permission rules, and the boundary sentence in the docs
- Date and version: 2026-11-21, test-stack 1.1.0
- Change: rule 16 names the permission rules of `.claude/settings.json`. Rule 17 asks for the sentence "Playwright MCP is not a security boundary" in the setup docs.
- Test: the 6 new-arm runs of S3 again, with the changed text.
- Observed: before, 1 of 6 runs did not write the sentence. After, 6 of 6 wrote it, and no run used `@latest` outside a `deny` rule. Grade 7.5 before, 7.8 after. The first `@latest` hit was in a `deny` rule, so it was not a real miss.
- Verdict: confounded (one rerun of 6 runs, a small change)
- Evidence: `docs/work/stack-split/results.md` (phase 3)

### E3: reqnroll
- Date and version: 2026-11-21, test-stack 1.1.0
- Change: a new skill for Reqnroll 3.3.4 on MSTest 4 or xUnit v3, with the Gherkin rules. It has Playwright hooks with a browser context for each scenario, and the move from SpecFlow.
- Test: scenarios S4 and S5 of the behaviour workflow, and the trigger check.
- Observed: S4 0.80 and 5.0 (baseline) against 1.00 and 8.3 (new), with the second judge. S5 0.96 and 8.3 against 1.00 and 8.0. Trigger 4 of 5, quiet 5 of 5.
- Verdict: helped (S4). No observed effect on S5: the models write good Gherkin without the skill
- Evidence: `docs/work/stack-split/results.md` (phase 3)

### E4: dotnet-testing xUnit v3 part
- Date and version: 2026-11-21, test-stack 1.1.0
- Change: `references/xunit-v3.md`, framework routing in `SKILL.md`, decisions TEST-19 and TEST-20, and 4 rules (`TST-XUNIT-V2-PKG`, `TST-XUNIT-ABSTRACTIONS`, `TST-XUNIT-COLLECTION-PARALLEL`, `TST-DATATESTMETHOD`).
- Test: scenarios S6 and S7, and the trigger check.
- Observed: S6 0.64 and 2.8 (baseline) against 1.00 and 9.0 (new). S7 0.47 and 2.8 against 1.00 and 9.0. The text of dev-stack 1.3.0 gave 0.69 and 3.5, and 0.47 and 3.2. Trigger 4 of 5, quiet 5 of 5.
- Verdict: helped
- Evidence: `docs/work/stack-split/results.md` (phase 3)

### E5: dotnet-testing MSTest 4 changes
- Date and version: 2026-11-21, test-stack 1.1.0
- Change: MSTest 4.5.3, the `decimal` `DataRow` rule (also `int` values), MSTEST0084 to 0089, and `dotnet test --no-build` on Microsoft.Testing.Platform.
- Test: scenario S8.
- Observed: baseline 0.85 and 7.3, the text of dev-stack 1.3.0 1.00 and 9.0, new 1.00 and 9.3. No run of the 1.3.0 arm or the new arm wrote a `decimal` `DataRow`.
- Verdict: no observed effect against the 1.3.0 text
- Evidence: `docs/work/stack-split/results.md` (phase 3)

## Lessons

### Round test-stack 1.1.0 (2026-11-21)

- Cue: a judge grades code against facts that a compile check proved.
- Lesson: put each compile-checked fact that changes a verdict into the judge key of each scenario that touches it. One missing fact made the judge call correct code a build error.
- Evidence: E3. The S4 grade went from 5.3 to 8.3 with the second judge, and the other arms did not change.
- Confidence: high
