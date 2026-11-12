# Results: product-flow 2.0.0

Brief: `brief.md` (G1 approved on 2026-11-05). Eval set: `evals/product-flow/v2/`. Model of the runs: Sonnet. 3 runs for each variant. Each run has its own home folder.

## Core commands

Date: 2026-11-05 and 2026-11-06. Cost: $19.92 for round 1, and $10.28 for the second run of `to-backlog-medium`. Script checks by `score.py`. Judge assertions by blind graders: the runs in a random order, without the variant.

| Eval | Assertion | Baseline | Skill |
|------|-----------|----------|-------|
| user-story | Behaviour spec, `specsync.py check` 0 errors | 0/3 | 3/3 |
| user-story | `backlogcheck.py` 0 errors | 0/3 | 3/3 |
| user-story | Questions in one message, each with a recommended answer | 0/3 | 3/3 |
| user-story | The 30-day rule has a scenario below, at, and above the limit | 0/3 | 3/3 |
| user-story | Each criterion has a rule, and each rule names its criterion | 0/3 | 3/3 |
| user-story | Ready only after the approval | 1/3 | 3/3 |
| bug-item | Bug spec, `specsync.py check --kind bug` 0 errors | 0/3 | 3/3 |
| bug-item | The current behaviour apart from the expected behaviour | 3/3 | 3/3 |
| bug-item | A rule that must not change | 3/3 | 3/3 |
| bug-item | Ready only after the approval | 2/3 | 3/3 |
| to-backlog-small | One bug item with a valid bug spec | 0/3 | 3/3 |
| to-backlog-small | One bug item, and the reason for the level | 3/3 | 3/3 |
| to-backlog-small | No research, no story map, no intent file | 3/3 | 3/3 |
| to-backlog-small | Final item only after the approval | 0/3 | 3/3 |
| to-backlog-medium | Intent file `docs/backlog/<slug>/intent.md` | 0/3 | 3/3 |
| to-backlog-medium | Stories with a valid Behaviour spec | 0/3 | 3/3 |
| to-backlog-medium | A level (feature or epic) and the reason | 0/3 | 3/3 |
| to-backlog-medium | Asks before a prototype, and makes none after a "no" | 3/3 | 3/3 |
| to-backlog-medium | Final items only after the approval of the list | 0/3 | 2/3 |
| refine | Behaviour spec, `specsync.py check` 0 errors | 0/3 | 3/3 |
| refine | Asks the fee and the meaning of "30 days", and does not decide them | 2/3 | 3/3 |
| refine | Questions in one message, each with a recommended answer | 0/3 | 2/3 |
| refine | AC3 has a rule, and the 30-day rule has the 3 limit cases | 0/3 | 3/3 |
| refine | Ready only after the approval | 0/3 | 3/3 |
| **Total** | | **20/72** | **70/72** |

### Findings

- **Baseline.** Without the skill, 0 of 15 runs wrote a Behaviour spec that the checker accepts. In 12 of 15 runs, the agent set `spec:ready` or wrote the final item after "Take your recommended answers", before the approval of the human.
- **refine, run 3 (skill).** The agent gave no recommended answer for the fee: "a business value". Candidate change: for a value that only the business knows, recommend the value that the item or the code uses now, and say so. Not made. The owner decides.
- **to-backlog-medium, run 1 (skill).** After "Approved. Write the items.", the refine pass found 4 new product questions. The agent asked them together with the G4 question, and wrote no item before the next answer. The grader gave a FAIL, because no final item exists. The agent broke no rule.
- **to-backlog-medium, round 1.** The scripted human had only 3 replies. The medium flow has gates G1, G3, and G4, so 2 of 3 skill runs stopped at G1. The script now has 5 replies. The table shows the second run.

## Trigger queries

13 queries (`evals/product-flow/v2/triggers.json`): 8 for the commands, 5 that must not start a product-flow command. 3 tries each. Result: 13 of 13 pass. No try picked an old name. "Clickable mock of the returns flow" picked `ux-design:ux-prototype` in 1 of 3 tries. Limit: one agent did the 3 tries of a query, and the router ran it on Opus, not on Haiku.

## Smoke runs

Date: 2026-11-06. Cost: $4.99. 1 run for each command, with the skill only. Claude graded the judge assertions. The assertions come from the results of version 1 (`evals/product-flow/results-2026-10-12-*.md`).

| Eval | Assertion | Skill |
|------|-----------|-------|
| smoke-epic | Item files exist | 1/1 |
| smoke-epic | Outcome with a measure, no invented target | 1/1 |
| smoke-epic | 2 to 6 features in delivery order, each with a goal, scope, and criteria | 1/1 |
| smoke-epic | Review, and the open questions before the final items | 1/1 |
| smoke-epic | Final items only after the approval | 1/1 |
| smoke-feature | Stories with a valid Behaviour spec, `backlogcheck.py` 0 errors | 1/1 |
| smoke-feature | Goal with a measure, scope in and out | 1/1 |
| smoke-feature | Each criterion has a rule that names it | 1/1 |
| smoke-feature | `spec:ready` only after the approval | 0/1 |
| smoke-goal | One sentence, an outcome | 1/1 |
| smoke-goal | A measure or a check | 1/1 |
| smoke-goal | Supporting items, the typo fix out of the goal | 1/1 |
| smoke-goal | A time frame, or a proposal | 1/1 |
| smoke-prototype | Prototype file, `protocheck.py` 0 errors | 1/1 |
| smoke-prototype | The question, and marked as a prototype | 1/1 |
| smoke-prototype | An empty state and an error state | 1/1 |
| smoke-prototype | "Is this what you mean?" with options | 1/1 |
| smoke-push | Item in the tracker with the label `spec:ready` | 1/1 |
| smoke-push | The possible duplicate 1204 shown, and a question before the create | 1/1 |
| smoke-push | Criteria and Behaviour spec in the field Acceptance Criteria, criteria first | 1/1 |
| smoke-push | Create only after the "yes" | 1/1 |

### Fault and fix

- **smoke-feature.** The agent asked "Is each story ready to build?" in the same message as the open questions. Then it took "Take your recommended answers" as the approval, and set `spec:ready`. The epic run had the same combined question, but the human answered "Approved".
- **Fix (E2).** In step 6 of `refine.md`, the approval question now has a message of its own. It comes after the changes. An answer to the questions is not the approval.

### Check of E2

Date: 2026-11-06. One run each with the skill. I graded these 3 runs myself, not blind.

| Case | Result |
|------|--------|
| smoke-feature | 5/5. The questions came first. The next message showed the changes and asked "Is this item ready to build?" alone. The agent set `spec:ready` only after "Approved". |
| smoke-epic | 5/5. The agent asked for the approval only after the answers. Three questions about the measure stayed open, so the items stayed `spec:draft`. |
| user-story | 7/8. The status stayed `spec:draft`, so the check "`spec:ready` in the item" failed. |

The user-story run did not break a rule. After the first answers, the refine pass found a new question: the story has no error criterion (warning A6). The agent asked it in the message after the changes. The script answered "Approved" to this question. The agent did not take this as the approval of the item.

The script has 3 replies, so the run had no turn left for the approval question. Three more runs of user-story show if this happens often.

Three more runs of user-story, with the skill: 24/24. Each run asked the open questions first. The next message showed the changes and asked "Is this item ready to build?" alone. Each run set `spec:ready` only after "Approved". The miss of the first run did not come back. So the cause was the short script, not E2.

## Upload to claude.ai

Date: 2026-11-06. The owner did the uploads.

| Zip | Result |
|-----|--------|
| `to-backlog.zip`, with `to-backlog/SKILL.md` one folder down | Refused: "Zip must contain a .claude-plugin/plugin.json file or a top-level SKILL.md declaring plugin components". |
| The plugin zip, with `.claude-plugin/plugin.json` at the top level (138 files, 399 KB) | Installed. |

The support article gives 200 characters for a description, and the platform docs give 1,024. The plugin zip installed with descriptions of up to 511 characters. So `build_claude_ai.py` now makes the plugin zip, and gives an error only above 1,024 characters.

Not tested yet: a command that runs in claude.ai, and a checker script that runs there. The files keep `${CLAUDE_SKILL_DIR}` in their paths.
