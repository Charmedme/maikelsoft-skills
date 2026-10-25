# Results: the hard cases of dev-stack (2026-10-25)

Input: the 22 hard cases of `concept.md`, a write case and a review case for each of the 11 batch 2 skills. Model: Claude Sonnet (the owner chose Sonnet to keep the cost down). Each case ran 3 times without the skill (baseline) and 3 times with it. A run scores the check items with `score.py` and the judge items with one blind grader each (`grade.py`). The divergence table is in `divergence.md`.

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
| puppeteersharp-hard | 16/57 (28%) | 39/57 (68%) |
| puppeteersharp-review-hard | 11/24 (46%) | 22/24 (92%) |
| rabbitmq-hard | 31/54 (57%) | 52/54 (96%) |
| rabbitmq-review-hard | 16/24 (67%) | 23/24 (96%) |
| redis-hard | 26/45 (58%) | 38/45 (84%) |
| redis-review-hard | 18/24 (75%) | 20/24 (83%) |
| sqlserver-hard | 30/51 (59%) | 34/51 (67%) |
| sqlserver-review-hard | 19/24 (79%) | 24/24 (100%) |
| wolverine-hard | 52/54 (96%) | 49/54 (91%) |
| wolverine-review-hard | 16/24 (67%) | 16/24 (67%) |
| **All** | **549/852 (64%)** | **749/852 (88%)** |

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

## The 18 gaps

Both variants fail these items in each run. Most facts are in the skill already, often only in a reference file. The model does not apply them to existing code, or does not put them in the report.

| Skill | Items | The fact | In the skill now |
|-------|-------|----------|------------------|
| dotnet-observability | review J2, J5, P1 | `SamplingRatio = 1.0F` alone keeps 5 traces per second, because `TracesPerSecond` wins | In `references/appinsights.md`, not in `SKILL.md` |
| elasticsearch | review J2, J5, P1 | One `IndexUpdateAliasesActionDescriptor` holds one action: `RemoveIndex` and `Add` on it send only the add | No |
| fluentvalidation | write J4 | The report lists the error codes of the endpoint | No report rule |
| fluentvalidation | review P6 | `ClassLevelCascadeMode.Stop` stops the whole validator | In `references/validators.md` |
| mailkit | write J1 | The report names the suppressed advisories and why the reason is gone | Rule 2 forbids the suppression. No report rule |
| postgresql | write C7 | The existing daily report counts UTC days (`::date`) | Yes, for new code |
| puppeteersharp | write C5, C6, J6 | `PdfOptions` without `Format` prints Letter. `PrintBackground` is false | Only one mention in `SKILL.md` |
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
- `sqlserver-hard` is not stable on this machine (2 CPUs, 7 GB). SQL Server sometimes does not start in the test container. Two runs (baseline 1, skill 3) failed each probe for that reason, also after a second scoring. Their scores are in the table, so `sqlserver-hard` is too low for both variants.
- The judge items depend on the grader. A two-part judge item can look like a gap.
