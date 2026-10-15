# Test results: idea-to-backlog (2026-10-12)

## Contents

- Set-up
- Results
- Problems found and fixes
- Trigger test

## Set-up

- 3 generic requests: a status dashboard, a search box, a required login.
- Each request ran 2 times in a separate agent: once with the skill (v1), once without a skill (baseline).
- The work folder was a small shop repository with brand tokens in `design/`. No ticket system and no claude.ai Project were connected.
- A simulated human: an answer sheet (`answers.md`) gave "ok" to recommended answers, "yes" at each gate, and one combined Markdown file as output.
- One blind grader (Sonnet) for each backlog file, with 9 checks.

## Results

### Blind grade (9 checks for each file)

| Request | Baseline | v1 |
|---------|----------|----|
| Status dashboard | 5 | 8 |
| Search box | 3 | 7 |
| Required login | 1 | 7 |
| **Total** | **9 of 27** | **22 of 27** |

| Check | Baseline | v1 |
|-------|----------|----|
| Specific role in each story | 2 of 3 | 3 of 3 |
| No Gherkin | 0 of 3 | 3 of 3 |
| Edge case in each story | 0 of 3 | 1 of 3 (the other 2: 1 story short) |
| Scope In and Out on each feature | 1 of 3 | 3 of 3 |
| Goal with a measure, no invented target | 0 of 3 | 3 of 3 |
| First release and later marked | 0 of 3 | 3 of 3 |
| No scope creep | 1 of 3 | 2 of 3 |
| Short (5-minute read) | 2 of 3 | 1 of 3 (1 fail came from a bug, see fix 1) |
| Testable criteria | 3 of 3 | 3 of 3 |

### Deterministic checks

| Check | Baseline | v1 |
|-------|----------|----|
| One context file with all sections | 0 of 3 (4 to 5 separate files) | 3 of 3 |
| `backlogcheck.py`: items found, 0 errors | 0 items found in 3 of 3 | 0 errors in 3 of 3 |
| `backlogtree.py tree`: 0 errors | not readable | 3 of 3 |
| `protocheck.py` on the prototype: 0 errors | not run | 3 of 3 |
| Gates in order G1, G2, G3, G4 | approvals in order, not named | 3 of 3 |
| Push log made | 0 of 3 | 3 of 3 |

The baseline is good at research, prototypes, and testable criteria. It writes Gherkin, adds scope (filters, synonym admin, CSV export, IP throttling), gives no measures, and has no release slice.

## Problems found and fixes

1. **Nested items.** `backlogtree.py` and `backlogcheck.py` read a section heading ("## Backlog") as an item when the items were `###` under it. A feature block also held its child stories. Thus the export showed each story 2 times: 1963 words, and 1183 words after the fix. Fix: an item block ends at the next item. 2 tests added.
2. **"Parent: None" in the export.** An item without a parent line got "Parent: None". Fix: no parent line in the export. 1 test added.
3. **"As the <role>".** The checker accepted only "As a" or "As an". Fix: "As the" is accepted. 1 test added.
4. **Where the product goal goes.** 2 of 3 runs wrote a separate goal item and also the goal in the features. Fix: idea-to-backlog puts the goal in the "Goal" section of each feature. It writes a separate goal item only when the team keeps goals as items.
5. **Push log for files.** The runs made up a remote id. Fix: `files.md` gives `<file>#<item id>`.
6. **Long status line.** The G4 line in the template was too long for the language check. Fix: the push log has its own line.

Not changed: the "No human to ask" rules stop the run at the prototype. The test answer sheet acts as the human, so the runs continued. This is the correct behaviour for a real unattended run.

## Trigger test

Haiku, 10 requests, 18 skills in the list (the 7 product-flow skills and 11 similar skills): 10 of 10 correct. The 5 requests for the full loop (also in Dutch, and "continue the run") went to idea-to-backlog. A single story, a push, a prototype, a plan-and-build request, and a PRD went to the other skills.

## Follow-up 2026-10-15: one goal for each feature

With 2 levels, each feature had a copy of the product goal. Now each feature has its own goal and a "Supports:" line.

| Run | Before | After |
|-----|--------|-------|
| Status dashboard, phase 5 again | Separate goal item, and the product goal in both features | Product goal one time in the Spec, own goal in both features |
| Required login, phase 5 again | Separate goal item, and one feature goal copied the product goal | Product goal one time in the Spec, own goal in both features |
| `backlog-feature` direct, 2 features | Not run | Own goal in both features, "Supports:" line in both |

`backlogcheck.py`: 0 errors and 0 warnings in all 3 runs. The direct run made up 2 targets in its measures (rule F2a). This is a different problem, and this change does not fix it.
