# Edit log: dev-stack

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

### E1: efcore rule 32a: non-blocking index from a pre-deploy SQL file
- Date and version: 2026-10-17, dev-stack 1.0.0
- Change: with one idempotent migration script on PostgreSQL, build the index from a pre-deploy SQL file, and use `CREATE INDEX IF NOT EXISTS` in the migration.
- Test: efcore-trap, skill variant only, before and after the rule. Opus.
- Observed: clean in 1 of 3 runs before, 5 of 5 runs after.
- Verdict: helped
- Evidence: project doc `claude/dev-stack-batch1-results-2026-10-17.md`, and the test results of 1.0.0 in `plugins/dev-stack/README.md`.

### E2: csharp rule 36a: bind a map or an enum as a string dictionary
- Date and version: 2026-10-17, dev-stack 1.0.0
- Change: bind a map or an enum from configuration as `Dictionary<string, string>` and convert each entry.
- Test: csharp-trap, skill variant only, before and after the rule. Opus.
- Observed: clean in 2 of 5 runs before, 5 of 5 runs after.
- Verdict: helped
- Evidence: as E1.

### E3: the 11 new skills of dev-stack 1.1.0, as a whole
- Date and version: 2026-10-20, dev-stack 1.1.0
- Change: 11 new skills.
- Test: 22 cases, baseline and skill, 3 runs each, 132 runs. Opus.
- Observed: 13 wins, 8 ties, 1 loss per case (p = 0.002). Trap checks failed: 29 baseline, 1 skill.
- Verdict: helped (for each skill as a whole)
- Evidence: `docs/work/dev-stack/batch2/results.md`

### E4: the single rules inside the 11 new skills
- Date and version: 2026-10-20, dev-stack 1.1.0
- Change: each rule of the 11 new skills.
- Test: as E3. The test adds a whole skill, so it does not isolate a rule.
- Observed: 38 items pass in 3 of 3 skill runs and fail in at least one baseline run. Each item names a trap, so its rule is the likely cause. Examples, each from 0/3 to 3/3: wolverine C6 (quorum queues), puppeteersharp C6 (no `--no-sandbox`), and mailkit C7 (the outbox while the relay is down).
- Verdict: confounded
- Evidence: `docs/work/dev-stack/batch2/divergence.md`, class skill-win.

### E5: eval probe observability-pii: a test HMAC key
- Date and version: 2026-10-20, eval set of 1.1.0
- Change: the probe host gives the HMAC redactor a test key.
- Test: the 6 observability trap runs scored again, and the selftest.
- Observed: skill runs 2 and 3 went from 5/7 to 7/7. Selftest 14/14 as before.
- Verdict: helped (the probe no longer fails a correct solution)
- Evidence: `docs/work/dev-stack/batch2/results.md`, faults.

### E6: eval probe rabbitmq-interop: topology at the first publish
- Date and version: 2026-10-20, eval set of 1.1.0
- Change: the probe waits for the queue, and places an order when the queue is still missing.
- Test: the 6 rabbitmq trap runs scored again, and the selftest.
- Observed: skill runs 1 and 3 went from 5/8 to 8/8, baseline run 3 from 4/8 to 7/8. Selftest 13/13.
- Verdict: helped
- Evidence: as E5.

### E7: fluentvalidation tool rule: cascade before a dependent check
- Date and version: proposed for dev-stack 1.1.1
- Change: move checklist item 12 from "judge" to a tool rule in `rules.json`.
- Test: fluentvalidation-review, skill variant, 5 runs. The rule test cases in `rules.json`.
- Observed: before, P7 found in 2 of 3 skill runs. After (1.1.1 round, 2026-10-24): P7 found in 5 of 5 skill runs. Each review found all 8 planted mistakes, with 0 false positives.
- Verdict: helped
- Evidence: `docs/work/dev-stack/batch2/divergence.md`

### E8: rabbitmq report line: the owner of each dead-letter queue
- Date and version: proposed for dev-stack 1.1.1
- Change: one line in the report section of `SKILL.md`.
- Test: rabbitmq-interop-trap, skill variant, 5 runs.
- Observed: before, J1 passed in 2 of 3 skill runs. After: J1 passed in 5 of 5 skill runs.
- Verdict: helped
- Evidence: as E7.

### E9: sqlserver report line: how a database setting change is deployed
- Date and version: proposed for dev-stack 1.1.1
- Change: one line in the report section of `SKILL.md`.
- Test: sqlserver-trap, skill variant, 5 runs.
- Observed: before, J4 passed in 2 of 3 skill runs. After: J4 passed in 4 of 5 skill runs. The failed run ended with a message about a background command, not with its report.
- Verdict: helped (one run did not give a report)
- Evidence: as E7.

### E10: mailkit check step: a test through Mailpit
- Date and version: proposed for dev-stack 1.1.1
- Change: one item in the check step of `SKILL.md`.
- Test: mailkit-confirmation-trap, skill variant, 5 runs.
- Observed: before, C12 passed in 2 of 3 skill runs. After: C12 passed in 4 of 5 skill runs.
- Verdict: neutral (4 of 5 is within the noise of 2 of 3)
- Evidence: as E7.

### E11: eval redis-trap J2: split code and reason
- Date and version: proposed for the eval set
- Change: J2 becomes two items: "caches a DTO" and "the report says why".
- Test: grade the 6 existing redis-trap runs again.
- Observed: before, J2 failed in 6 of 6 runs. All 6 runs cache a DTO. After the split: J2 (caches a DTO) passed in 3 of 3 runs of each variant. J4 (says why) passed in 0 of 3 runs of each variant.
- Verdict: helped (the eval item now tests one thing). The skill does not bring the reason into the report: refer to E13.
- Evidence: as E7.

### E12: rabbitmq fact: basic.nack with requeue does not count toward the delivery-limit on 4.3
- Date and version: 2026-10-23, proposed for dev-stack 1.1.1
- Change: the transient path of `references/client.md` uses `BasicRejectAsync(requeue: true)`. `broker.md`, `SKILL.md`, and review checklist item 19 state the 4.3 rule. The new tool rule RMQ-NACK-REQUEUE finds `BasicNackAsync(..., requeue: true)`.
- Test: the rule test cases in `rules.json`. No eval run yet.
- Observed: the old text said that the `delivery-limit` stops a nack loop. A hard-case agent saw 2,877 redeliveries in 5 seconds on 4.3. The RabbitMQ docs confirm it: from 4.3 the limit uses `delivery-count`, and `basic.nack` only increments `acquired-count`.
- Verdict: proposed (the fact is verified in the docs, the effect on runs is not tested)
- Evidence: https://www.rabbitmq.com/docs/quorum-queues, https://www.rabbitmq.com/blog/2026/04/23/rabbitmq-4.3-release

### E13: redis report line: why each cached type is a DTO
- Date and version: 2026-10-24, dev-stack 1.1.1
- Change: one sentence in the report section of `SKILL.md`.
- Test: redis-trap, skill variant. Not run yet.
- Observed: before, J4 passed in 0 of 3 runs of each variant. Rule 11 has the reason, but the report does not state it.
- Verdict: proposed

### E14: mailkit facts: CVE-2026-41319 and MimeKit 4.18.2
- Date and version: 2026-10-24, dev-stack 1.1.1
- Change: the version rule, MAIL-PKG-MIMEKIT-CVE, the review checklist, and `sources.md`. MailKit below 4.16.0 has CVE-2026-41319. MimeKit 4.18.2 depends on Pkcs 10.0.13, so the Pkcs pin is only for MimeKit 4.15.1 to 4.18.1. Three judge items of the eval accept the upgrade to 4.18.2 as a fix.
- Test: the rule test cases, and a build of MailKit 4.18.2 without a pin (0 warnings, Pkcs 10.0.13).
- Observed: a hard-case agent found the advisory. The skill knew only CVE-2026-30227.
- Verdict: helped (a missing fact, verified at the source)
- Evidence: https://github.com/advisories/GHSA-9j88-vvj5-vhgr, the NuGet nuspec of MimeKit 4.18.2

### E15: rabbitmq fact: global QoS does not fail on 4.3.6
- Date and version: 2026-10-24, dev-stack 1.1.1
- Change: rule 11 and `broker.md` no longer say that global QoS fails. The rule (a prefetch per consumer) stays.
- Test: a run on RabbitMQ 4.3.6 by a hard-case agent: `BasicQosAsync(0, 10, true)` did not fail.
- Observed: the old text said that 4.3 denies global QoS.
- Verdict: helped (a wrong fact is gone)

## Lessons

### Round 1.1.0 (2026-10-21)
- Cue: a rule is in the skill, and the model applies it in some runs only.
- Lesson: move the rule to the form that the model cannot skip. A pattern that a regex can find goes to `rules.json`. A fact that the report must hold goes to the report section of `SKILL.md`, not to a reference file.
- Evidence: the analysis of E7 to E10 (the rule exists in each case). Not tested yet.
- Confidence: low, until E7 to E10 have a verdict.
