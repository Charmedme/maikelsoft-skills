# Brief: product-flow 2.0.0

Status: approved (G1, 2026-11-05)

## Need
Plan version 7 makes product-flow the start of the spec-first process. Today a product owner must know eight skill names, and `idea-to-backlog` runs the full research for each request, also for one bug. A story has acceptance criteria, but no spec that the build can lock. The examples come later, in `example-mapping`, as a separate file. Version 2.0.0 gives one entry for each phase. It writes the Gherkin "Behaviour spec" of each story and bug on the ticket, and it ends each item command with the refine pass.

## Form
The plugin keeps its skills, with new names and new steps. The old names stay one major version as hidden forwarders. No new agent.

## Commands
| Command | Folder | Was | What changes |
|---------|--------|-----|--------------|
| `/to-backlog` | `to-backlog` | `idea-to-backlog` | One entry for an idea, a feature request, or a bug report. It selects the level and the depth by size (P2). |
| `/epic` | `epic` | `backlog-epic` | Ends with the refine pass and the push. |
| `/feature` | `feature` | `backlog-feature` | Ends with the refine pass and the push. |
| `/user-story` | `user-story` | `backlog-story` | Writes the Behaviour spec next to the acceptance criteria (P1). Ends with the refine pass and the push. |
| `/bug-item` | `bug-item` | new | One bug item with the bug spec of `shared/` (decision S3 of step 3). |
| `/refine` | `refine` | `example-mapping` | Makes a draft spec ready: one review, the open questions to the product owner, the approval. |
| `/prototype` | `prototype` | `product-prototype` | Only the name. |
| `/product-goal` | `product-goal` | `backlog-goal` | Only the name. |
| (no command) | `backlog-push` | `backlog-push` | Out of the `/` menu. The other skills start it. It sets the label `spec:draft` or `spec:ready`. |

Plan version 7 checked the short names against the built-in commands of Claude Code on 2026-11-04.

## Workflow of each item command
1. **Context step** (`shared/context.md`): the role, `.claude/rules/delivery.md`, the tracker. Goes wrong when: it asks a fact that `/setup` already wrote.
2. **Write** the item with the rules of its level. A story or a bug gets the Behaviour spec. Goes wrong when: a rule has no scenario at its limit.
3. **Check**: `backlogcheck.py`, `specsync.py check`, `doccheck.py`. Goes wrong when: the acceptance criteria and the spec do not agree.
4. **Refine pass** (`/refine`): a new reviewer checks INVEST and the scenarios (`shared/review-rules.md`). The open questions go to the product owner in one message. At most 3 rounds (`shared/cycle.md`). Goes wrong when: the agent answers a product question itself.
5. **Approval and push**: the product owner approves. Then a duplicate check, and the item goes to the tracker with the label `spec:ready`, or `spec:draft` with open questions. Without a tracker, the item file. Goes wrong when: a hand-set "ready" passes without the check.
6. **Next** line of `shared/context.md`.

## Decisions
| No | Decision | Who |
|----|----------|-----|
| P1 | A story keeps its list of acceptance criteria, and gets the Behaviour spec below it. Each rule names its criterion: `Rule: R1 Free return within 30 days (AC1)`. `backlogcheck.py` checks that each criterion has one or more rules, and that each rule names a criterion that exists. In Azure DevOps both go in the Acceptance Criteria field. | human |
| P2 | `/to-backlog` sets the depth by size. A small request (one story or one bug): no research and no story map. A medium or large request: research and a story map. A prototype only after a yes, when the idea has UI and the level is a feature or an epic. It says which level it chose and why, and asks when it is not sure. | human |
| P3 | Each item command pushes after the approval of the product owner, with the duplicate check of `backlog-push`. Without a tracker, it writes the item file. | human |
| P4 | Test: the full build-skill method for `/to-backlog`, `/user-story`, `/bug-item`, and `/refine`. One smoke run each for `/epic`, `/feature`, `/product-goal`, `/prototype`, and the push, against their earlier evals. One set of trigger queries for all commands. | human |
| P5 | Each old folder name stays as a forwarder for one major version: `user-invocable: false`, a one-line description "Old name of `<new>`", and it passes `$ARGUMENTS` to the new skill. A test checks that each forwarder names one new skill that exists, and that no forwarder is the target of another skill. | plan version 7 |
| P6 | The backlog folder is `docs/backlog/<slug>/` with `intent.md`. It holds the problem, the outcome, the users, and the scope. It also holds the story map, the decisions, and the status for a resume. `/to-backlog` also reads an old `backlog-<slug>.md`. | plan version 7 |
| P7 | Only the tool sets the label. `spec:ready` means: no open question, `specsync.py check` 0 errors, and the approval of the product owner. When a person sets the label by hand, the next command checks the item again. | plan version 7 |
| P8 | The refine pass has its rules in `plugins/product-flow/shared/refine.md`. `shared/example-mapping.md` and `examplecheck.py` stay for dev-flow until step 6, so this step changes no dev-flow behaviour. | recommended, accepted |
| P9 | A build script `scripts/build_claude_ai.py` makes the plugin zip for claude.ai, in `dist/` (not committed). A test checks the zip. The README gets the block for the Project instructions. Changed on 2026-11-06: the first version made one zip for each skill, and claude.ai refused that layout. The plugin zip installed. | plan version 7, changed after the upload test |
| P10 | The version is 2.0.0, because the old typed names stop. The release waits for the joint release with dev-setup 1.0.0 after step 9. | plan version 7 |

## Success criteria
- Each story and each bug has a Behaviour spec with 0 errors of `specsync.py check`. A bug passes `--kind bug`.
- Each criterion has a rule, and each rule names a criterion (`backlogcheck.py`).
- The refine pass asks the product owner each open question in one message, each with a recommended answer. It does not answer a product question itself.
- `/to-backlog` runs no research for one small story or bug, and asks before a prototype.
- No item goes to the tracker before the approval of the product owner.
- The trigger queries start the right command, and the old names do not show in the `/` menu.
- `python3 -m unittest discover -s tests`, `sync_shared.py --check`, and `claude plugin validate` pass.

## Limits
- No change to dev-flow, dev-team, or ux-design in this step.
- No new tracker. The push keeps the five trackers and the item files.
- No change to the rules of an epic, a feature, or a goal, except the refine pass and the push at the end.

## Language and runtime
English and Dutch output, as today: ASD-STE100 and Begrijpelijk Nederlands B1. Claude Code, and claude.ai through the zip.
