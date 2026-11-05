# Brief: shared parts of plan version 7

Status: approved (G1, 2026-11-04)

## Need
Plan version 7 gives product-flow, dev-flow, and dev-team one process. The steps 5 to 7 change these plugins. Each of them needs the same cycle, the same review rules, the same spec format, and the same lock. When each plugin has its own copy of a rule, the copies drift apart. So step 3 writes each part one time in `shared/`, and `scripts/sync_shared.py` copies it into each skill that names it.

## Form
Not a skill. Shared text files and scripts, copied into the skills of steps 4 to 8. The steps 4 to 8 use the parts. No skill uses them before those steps.

## Parts
| Part | File in `shared/` | What it holds |
|------|-------------------|---------------|
| Cycle | `cycle.md` | Research, design, write, test, review, improve. The cap of each phase, the early exit, and the step back to the human at the cap. |
| Review rules | `review-rules.md` | A fresh reviewer for each review, with the documents, the change history, and the earlier findings and their status. No finding posted again. A check that a fixed mistake did not come back. A search for the same class of mistake in other places. The finding classes. |
| `REVIEW.md` template | `review-template.md` | The template that `/setup` writes into a project: what to flag, what to skip, the risk tiers. |
| Context step | `context.md` | The first step of each command: read the role line and `.claude/rules/delivery.md`. When a fact is missing, ask only for that fact, name `/setup` when the project has dev-setup, and go on. |
| Run header | `runheader.py` | Reads and writes the header of `run.md`: `flow` (solo or team), `item`, `size`, `step`, `baseline`. Gives a header to an old run. |
| Spec copy | `specsync.py` | Turns the "Behaviour spec" section of a ticket into plain text, checks it, and keeps a fingerprint for each rule. Compares a new copy with the baseline and names the rules that changed. |
| Docs check | `docscheck.py` | Names the docs of a change before the final gate. |
| Lock | `speclock.py` | Moved from dev-flow. One locked test file for each rule. |
| Mutation check | `mutate.py` | Moved from dev-flow, without a change to its behaviour. |

## Decisions
| No | Decision | Who |
|----|----------|-----|
| S1 | The shared parts are text files and scripts in `shared/`, not a skill. Each goes only into a skill whose `SKILL.md` names it. | recommended, accepted |
| S2 | The spec on the ticket is Gherkin. The `Rule:` keyword groups the scenarios, with the IDs R1, R2, and so on. At most 7 rules, and at most 4 scenarios for each rule. A rule with a limit has a scenario below, at, and above the limit. | human |
| S3 | A bug spec has the broken rule with a `Now:` line (the current behaviour) and the expected scenarios, which fail today. A second rule, "Unchanged", holds the scenarios that must stay green. | human |
| S4 | The Gherkin is the spec, not the test. `/build` writes one plain test file for each rule, in the test framework of the project, with each test named after its scenario. When the project already uses Reqnroll or Cucumber, `/build` uses that. | human |
| S5 | The lock works on files. Each rule has one test file. A change of the PO to one rule unlocks only that file. A person approves the file again, at each risk level. | human |
| S6 | `specsync.py` does not call the tracker. The agent reads the ticket with the tracker tool and gives the raw text to the script: Jira ADF JSON, Azure DevOps HTML, or Markdown. The script makes one plain text: no tags, decoded entities, one bullet form, NFC, collapsed white space. The fingerprint is a SHA-256 of each rule. The user never sees a fingerprint. | recommended, accepted |
| S7 | The run header is a YAML front matter block at the top of `run.md`. dev-flow and dev-team keep their own body. In step 6 and 7, `workcheck.py` and `runcheck.py` read the header. An old run gets a header with `baseline: legacy` and keeps its locks. | recommended, accepted |
| S8 | The lock file stays `.dev-flow/spec-lock.json`, so a running item does not move. dev-team needs dev-flow, so it can use the same file. | recommended, accepted |
| S9 | The docs check lists each doc that names a changed file, a command, a setting, or an endpoint. When the project has `CHANGELOG.md`, it asks for a line. When the project has no docs, it proposes a README. | human |
| S10 | Each script gets a unit test file with fixtures: ADF, Azure DevOps HTML, Markdown, an old run, and a bug spec. The text files get their test through the evals of steps 5 to 7 and the comparison of step 9. | human |
| S11 | The numbers of the cycle and the size table come from plan version 7 without a change. | recommended, accepted |

## Success criteria
- `python3 -m unittest discover -s tests` passes, with a test file for each new script.
- `specsync.py` gives the same fingerprints for the same spec from ADF JSON, Azure DevOps HTML, and Markdown.
- `specsync.py` names the changed rules when one rule changes, and only that rule.
- `specsync.py` gives an error for more than 7 rules, and for more than 4 scenarios in a rule.
- `specsync.py` gives an error for a limit rule without below, at, and above, and for a bug spec without `Now:` or an "Unchanged" rule.
- `speclock.py unlock --rule R3` unlocks only the test file of R3. The old commands still work.
- `python3 scripts/sync_shared.py --check` passes, and `tests/` has a test for each new mapping.
- `doccheck.py --level standard` shows 0 errors on each new Markdown file.

## Limits
- No change to a skill in this step. Steps 4 to 8 name the files in their skills.
- No tracker calls in a script. The tracker tools stay in the skills.
- `workcheck.py` and `runcheck.py` read the header in step 6 and 7, not here.

## Sources
- Plan version 7, approved on 2026-11-04, and its three review rounds.
- Gherkin reference, the `Rule` keyword (Gherkin 6 and later): https://cucumber.io/docs/gherkin/reference/
- Farley, "Modern Software Engineering": tests in domain words through a DSL, in four layers.
- Wiegers and Beatty, "Software Requirements Essentials": a requirement with its examples at the limits.
