# Divergence analysis: dev-stack batch 2

Date: 2026-10-21. Input: the 132 runs of the batch 2 round (`results.md`). Script: `python3 evals/dev-stack/divergence.py <runs>`.

The script looks at each item of each case: a check (C), a judge assertion (J), or a planted review mistake (P). It counts the passing runs of each variant and gives the item a class. The idea comes from Harness² (google-research/harness2): where all runs agree, the skill already decides the behavior. Where runs disagree, the skill leaves a choice to chance.

## Result

| Class | Meaning | Items |
|-------|---------|-------|
| gap | The skill variant fails in every run | 1 |
| worse | The skill variant passes less often than the baseline | 1 |
| chance | The skill variant passes in some runs only | 4 |
| skill-win | The skill variant passes in every run, the baseline does not | 38 |
| no-effect | Both variants pass in every run | 254 (214 not generic) |

## The six items that are not a win

I read the grader evidence of each failed run, and the text of the skill.

| Item | Baseline | Skill | Cause | Action |
|------|----------|-------|-------|--------|
| redis-trap J2 | 0/3 | 0/3 | All 6 runs cache a DTO, which is correct. No run says why. The judge asks for the code and the reason in one item. The skill has the rule with its reason (rule 11). | Eval change: split J2 into "caches a DTO" and "says why". No skill change. |
| redis-review J6 | 3/3 | 2/3 | One run gives the correct fix (DisableLocalCache, a short L1 time) but does not use the words "no backplane". | No action. One run of 3 is noise. |
| fluentvalidation-review P7 | 0/3 | 2/3 | The skill has the cascade rule (rule 10, checklist item 12), but the checklist marks it "judge". The model finds it in 2 of 3 reviews. | Move it to a tool rule in `rules.json`: `NotEmpty()` and then `Matches(` in one rule chain, without `Cascade(` or `RuleLevelCascadeMode`. |
| mailkit-confirmation-trap C12 | 2/3 | 2/3 | One skill run has no test against Mailpit. Rule 16 asks for one. | Add "a test sends a mail through Mailpit" to the check step of the skill. |
| rabbitmq-interop-trap J1 | 0/3 | 2/3 | One skill run marks the decisions as assumed but names no owner for the dead-letter queue. The owner rule is only in `references/broker.md`. | Add one line to the report section of `SKILL.md`: name the owner of each dead-letter queue. |
| sqlserver-trap J4 | 1/3 | 2/3 | One skill run does not say how the RCSI change is deployed. The skill shows how to deploy it, but does not ask the report to state the choice. | Add one line to the report section: state how a database setting change is deployed (migration with `suppressTransaction`, or a DBA step). |

None of the six is missing knowledge. Five are rules that the model applies in some runs only, or rules that do not reach the report. One is an eval item that tests two things at once.

## The no-effect items: what Opus already does

In the trap cases, Opus without the skill passes most traps in 3 of 3 runs. Examples: the HybridCache backplane trap (redis C4), the Pkcs pin (mailkit C1), SSRF in the renderer (puppeteersharp C4), and the outbox for Wolverine (wolverine J1). In the review cases, both variants find 80 of the 89 planted mistakes in every run.

This has two meanings:

1. Cut candidates. The rules behind these items add nothing for Opus on these cases. Do not cut them on this evidence alone. The dev-team mediors and other users can run a smaller model. A rule can also prevent mistakes that no case tests. Test a cut on the smaller model first.
2. Weak cases. Most trap items do not separate the variants on Opus. The 38 skill-win items carry the whole result. A next round needs harder cases, or a smaller model, to measure more.

## Limits

- 3 runs per variant. A class of "chance" or "worse" with one failed run can be noise. Confirm with 5 runs on that case before a change.
- The class of an item depends on the grader. A strict or two-part judge item can look like a skill gap (redis J2).
- The analysis is for one model (Opus).
