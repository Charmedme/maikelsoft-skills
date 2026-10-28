# Results: the hard cases of dev-stack (2026-10-25)

Input: the 22 hard cases of `concept.md`, a write case and a review case for each of the 11 batch 2 skills. Model: Claude Sonnet (the owner chose Sonnet to keep the cost down). Each case ran 3 times without the skill (baseline) and 3 times with it. A run scores the check items with `score.py` and the judge items with one blind grader each (`grade.py`). The divergence table is in `evals/dev-stack/divergence-hard-2026-10-25.txt` (output of `divergence.py`).

## Result

| Case | Baseline | Skill |
|------|----------|-------|
| aspnetcore-openapi-hard | 24/48 (50%) | 46/48 (96%) |
| aspnetcore-openapi-review-hard | 21/24 (88%) | 23/24 (96%) |
| dotnet-observability-hard | 51/66 (77%) | 60/66 (91%) |
| dotnet-observability-review-hard | 17/24 (71%) | 18/24 (75%) |
| elasticsearch-hard | 31/51 (61%) | 49/51 (96%) |
| elasticsearch-review-hard | 13/24 (54%) | 17/24 (71%) |
| fluentvalidation-hard | 43/54 (80%) | 49/54 (91%) |
| fluentvalidation-review-hard | 22/24 (92%) | 24/24 (100%) |
| mailkit-hard | 26/54 (48%) | 51/54 (94%) |
| mailkit-review-hard | 14/24 (58%) | 24/24 (100%) |
| postgresql-hard | 35/54 (65%) | 51/54 (94%) |
| postgresql-review-hard | 17/24 (71%) | 20/24 (83%) |
| puppeteersharp-hard | 22/57 (39%) | 54/57 (95%) |
| puppeteersharp-review-hard | 11/24 (46%) | 22/24 (92%) |
| rabbitmq-hard | 31/54 (57%) | 52/54 (96%) |
| rabbitmq-review-hard | 16/24 (67%) | 23/24 (96%) |
| redis-hard | 26/45 (58%) | 38/45 (84%) |
| redis-review-hard | 18/24 (75%) | 20/24 (83%) |
| sqlserver-hard | 30/51 (59%) | 34/51 (67%) |
| sqlserver-review-hard | 19/24 (79%) | 24/24 (100%) |
| wolverine-hard | 52/54 (96%) | 49/54 (91%) |
| wolverine-review-hard | 16/24 (67%) | 16/24 (67%) |
| **All** | **555/852 (65%)** | **764/852 (90%)** |

- The skill variant scores higher in 20 of 22 cases, equal in 1, and lower in 1 (wolverine-hard).
- The hard cases measure what batch 2 could not. In batch 2 on Opus, both variants passed most items. Here the baseline fails 36% of the items.
- The median cost of a run is $0.20 to $0.90. The 132 runs cost about $80 in total. Two cases cost more with the skill: elasticsearch-hard ($2.88 against $0.42) and wolverine-hard ($2.74 against $1.21).

## Divergence classes

| Class | Items |
|-------|-------|
| skill-win (skill 3/3, baseline less) | 119 |
| no-effect (both 3/3) | 246 |
| chance (skill in some runs) | 39 |
| worse (skill less than baseline) | 21 |
| gap (both 0/3) | 18 |

## The gaps

The first scoring found 18 gap items. Two of them (puppeteersharp C5 and C6) were a fault of the scorer.

Both variants fail these items in each run. Most facts are in the skill already, often only in a reference file. The model does not apply them to existing code, or does not put them in the report.

| Skill | Items | The fact | In the skill now |
|-------|-------|----------|------------------|
| dotnet-observability | review J2, J5, P1 | `SamplingRatio = 1.0F` alone keeps 5 traces per second, because `TracesPerSecond` wins | In `references/appinsights.md`, not in `SKILL.md` |
| elasticsearch | review J2, J5, P1 | One `IndexUpdateAliasesActionDescriptor` holds one action: `RemoveIndex` and `Add` on it send only the add | No |
| fluentvalidation | write J4 | The report lists the error codes of the endpoint | No report rule |
| fluentvalidation | review P6 | `ClassLevelCascadeMode.Stop` stops the whole validator | In `references/validators.md` |
| mailkit | write J1 | The report names the suppressed advisories and why the reason is gone | Rule 2 forbids the suppression. No report rule |
| postgresql | write C7 | The existing daily report counts UTC days (`::date`) | Yes, for new code |
| puppeteersharp | write J6 | The report names the existing defects of the document. (C5 and C6 were a fault of the scorer, refer to Limits) | No report rule |
| sqlserver | write C10, C11, J4 | The existing report counts UTC days. An interpolated string on `varchar` gives `CONVERT_IMPLICIT`. `Guid.CreateVersion7()` is not ordered in SQL Server | Yes, in references |
| wolverine | review J2, P2 | A handler that injects a typed `HttpClient` throws in Wolverine 6 | In `references/configuration.md` |

Two patterns:

1. **Existing code.** 7 of the 18 gaps are defects in existing code next to the change. The skill rules are written for new code. The agent does not check the existing code that the change touches.
2. **Facts in a reference file.** A fact that lives only in `references/` reaches the output less often than a fact in `SKILL.md`. This is the same lesson as round 1.1.0.

## The cases that measure little

- `wolverine-hard`: the baseline scores 96%. The case is too easy for Sonnet, or the traps do not need the skill. The skill run costs 2.3 times as much.
- `wolverine-review-hard`: both 67%. The skill does not help to find the planted mistakes.

## Limits

- 3 runs per variant. An item with 1 failed run of 3 can be noise.
- One model (Sonnet). The batch 2 results are on Opus, so the two rounds are not comparable item by item.
- `sqlserver-hard` was not stable in the first round. SQL Server sometimes did not start in the test container. The scorer has a fix, refer to the section after the audit. Two runs (baseline 1, skill 3) failed each probe for that reason, also after a second scoring. Their scores are in the table, so `sqlserver-hard` is too low for both variants.
- The judge items depend on the grader. A two-part judge item can look like a gap.
- The first scoring of `puppeteersharp-hard` ran without the Chrome of the case (`EVAL_CHROME` and `PUPPETEER_EXECUTABLE_PATH`). A second scoring with Chrome changed the skill variant from 39/57 to 54/57 and the baseline from 16/57 to 22/57. The table shows the second scoring.

## After the reference audit (2026-10-25)

The owner asked to fix the reference files: up to date, and correct against the primary sources (Microsoft Learn, DeepWiki, the official docs, a run). One agent for each skill checked each version and fact. Each fact that prevents a silent wrong result also got a short rule in `SKILL.md`. Stack rule 3 now says that the style of existing code wins, but its defects do not.

Then the 9 cases with a gap ran again, skill variant only, 3 runs each on Sonnet.

| Case | Baseline | Skill before | Skill after | Gap items, before and after |
|------|----------|--------------|-------------|-----------------------------|
| dotnet-observability-review-hard | 17/24 (71%) | 18/24 (75%) | 24/24 (100%) | J2 0/3 to 3/3, J5 0/3 to 3/3, P1 0/3 to 3/3 |
| elasticsearch-review-hard | 13/24 (54%) | 17/24 (71%) | 24/24 (100%) | J2 0/3 to 3/3, J5 0/3 to 3/3, P1 0/3 to 3/3 |
| fluentvalidation-hard | 43/54 (80%) | 49/54 (91%) | 54/54 (100%) | J4 0/3 to 3/3 |
| fluentvalidation-review-hard | 22/24 (92%) | 24/24 (100%) | 21/24 (88%) | P6 0/3 to 3/3, but decoy D1 flagged in 3 of 3 |
| mailkit-hard | 26/54 (48%) | 51/54 (94%) | 52/54 (96%) | J1 0/3 to 3/3 |
| postgresql-hard | 35/54 (65%) | 51/54 (94%) | 50/54 (93%) | C7 0/3 to 3/3 |
| puppeteersharp-hard | 22/57 (39%) | 54/57 (95%) | 50/50 (100%) | J6 0/3 to 2/2 |
| sqlserver-hard | 30/51 (59%) | 34/51 (67%) | 30/51 (59%) | C10 0/3 to 1/3, C11 0/3, J4 0/3 |
| wolverine-review-hard | 16/24 (67%) | 16/24 (67%) | 21/24 (88%) | J2 0/3 to 3/3, P2 0/3 to 3/3 |

- 14 of the 16 real gap items now pass in each valid run. The 2 that stay are in `sqlserver-hard`.
- `fluentvalidation-review-hard` went down. The new rule "put `NotNull()` before `SetValidator`" made each review flag an optional child on an update (decoy D1). The rule now says that `SetValidator` without `NotNull()` is correct when null is a valid value. A new run of 3 reviews (2026-10-25, skill variant) tested the fix. No review flags decoy D1, and each review finds the missing `NotNull()` on create.
- I read the reviews of that run by hand, with no grader. No review flags D2, D3, or D4.
- `sqlserver-hard` ran again on a machine with 12 CPUs and 17 GB (2026-10-25, 3 runs for each variant, check items only). The skill variant scored 11/12 in each run, and the baseline scored 10/12. C10 (the Amsterdam day) went from 0/3 to 3/3 with the skill.
- In `sqlserver-hard`, C11 (implicit conversion in the SQL plans) still fails in all 6 runs. The skill runs fix the SKU parameter in `StockReservationService`, but the probe still finds a plan with a conversion, so C11 stays an open gap.
- Then the skill got a rule to search for the same defect after a fix. A new run (3 skill runs) scored 36/36 on the check items, and C11 passed in 3 of 3 runs. The baseline scored 30/36.
- SQL Server on Linux sometimes crashes when it starts (a fatal error in `sqlpal.dll`), more often when several containers start together. `score.py` now starts the tests again, up to 3 times, when a container does not stay up. Two scorings after this change gave the same score for each run.
- Run 3 of `puppeteersharp-hard` has no grade: the disk filled up during the run, and its output stops before the end.
- `redis-review-hard` has 3 new decoys, D6 to D8. The "Do not flag" list of the checklist does not name them. D1 to D3 and D5 stay in the answer sheet, but the checklist names them, so they do not count. A new run (3 per variant) found no false positive on D6 to D8 in either variant. I counted the planted mistakes by keyword, with no grader. The baseline found 7, 7, and 5 of 10. The skill found 8, 10, and 8.
- In `redis-review-hard`, the skill finds P2 (cache removal before `SaveChanges`) in 3 of 3 runs. The baseline finds it in none.
