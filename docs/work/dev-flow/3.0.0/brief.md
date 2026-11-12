# Brief: dev-flow 3.0.0

Status: approved (G1, 2026-11-07)

## Need
Plan version 7 makes dev-flow the build phase of the spec-first process. Today `plan-and-build` reads the acceptance criteria and makes its own examples with example mapping. In version 7, the product owner approves the Behaviour spec on the ticket (product-flow 2.0.0). The build copies this spec, locks one test file for each rule, and does not write the examples again.

Review comments on a pull request have no command in dev-flow today. Only a team run has `dev-team:process-review`. Version 3.0.0 gives one command for each part: `/build` and `/review-comments`.

## Form
Two command skills in dev-flow. `plan-and-build` stays one major version as a hidden forwarder to `build`. No new agent. The reviewers are sub-agents with a fresh context.

## Commands
| Command | Folder | Was | What it does |
|---------|--------|-----|--------------|
| `/build` | `build` | `plan-and-build` | Builds one item from its ticket by the spec-first process. Each build has a ticket. |
| `/review-comments` | `review-comments` | new | Handles the review comments on a pull request, fixes, pushes, and replies on the pull request. |
| (no command) | `plan-and-build` | `plan-and-build` | Forwarder to `build`, out of the `/` menu. |

## Workflow of `/build`
1. **Context step** (`shared/context.md`). Goes wrong when: it asks a fact that `/setup` already wrote.
2. **Ticket.** Read the item. Without a ticket, draft the item and push it after the approval (B3). Goes wrong when: it builds without a ticket.
3. **Spec.** A draft spec gets the refine pass (B2). A ready spec goes into `spec.md` with `specsync.py copy`, with a check for each part or "review only". Goes wrong when: the build changes a rule of the product owner.
4. **Size and plan.** The tier from size and risk (`tiers.md`). Medium and up: `plan.md`, a fresh plan reviewer (3 rounds), then the plan gate with the list of scenarios. Goes wrong when: the plan has a part without a check.
5. **Held-back tests.** Medium and up: a separate agent writes them from `spec.md` and the DSL (B4). Goes wrong when: the builder reads them.
6. **Scenario loop.** For each rule: the test file, approve and lock (`speclock.py`), red, green, a step review, commit. 3 tries to get green. Goes wrong when: it changes a locked test.
7. **Prove.** The full suite, the lock check, the held-back tests, and the mutation check. 2 fix rounds. Goes wrong when: a hole does not go to the human.
8. **Reviews.** A fresh code reviewer (`review-rules.md`), 1 to 3 passes by size. A security agent from medium up (B6). Goes wrong when: a reviewer sees the context of the builder.
9. **Docs of the change** (`docscheck.py`). Goes wrong when: a changed command or setting has an old doc.
10. **Final gate.** The report: Testing, Risk, How to undo, the commits. Open findings stop the gate. Then one proposal when there was a miss (B9), and the `Next:` line.

When the spec on the ticket changes after the copy: `specsync.py diff` names the changed rules. The human approves each changed rule again, and `speclock.py unlock --rule` opens only its file.

## Workflow of `/review-comments`
1. Read the run header (`runheader.py`). A team run goes to `dev-team:process-review` (B8).
2. Read the threads. Sort each comment with the classes of `review-rules.md`. A product question goes to the product owner on the ticket.
3. Fix the blocking comments. A fresh reviewer checks the fix diff. Round cap: small 1, medium 2, large 3.
4. Push the fix commits to the branch of the pull request, without a force push.
5. Reply on each thread: what changed and in which commit, or the reason when there is no fix. Do not resolve a thread.

## Decisions
| No | Decision | Who |
|----|----------|-----|
| B1 | Two command skills `build` and `review-comments`. `plan-and-build` is a hidden forwarder for one major version. | human (Q1) |
| B2 | `refine.md` moves from `plugins/product-flow/shared/` to `shared/`. Both plugins use it. Without product-flow, `/build` runs the refine pass itself on a draft spec, and starts only after the approval. | human (Q2) |
| B3 | Each build has a ticket, for traceability. Without one, `/build` drafts the item: a bug with `product-flow:bug-item` when the project has product-flow, else a minimal item of its own template. It pushes the item after the approval, then builds. | human (Q3, Q12) |
| B4 | The held-back tests are outside the repository, in the data folder of the plugin. The guard hook blocks the builder from that folder. A separate step runs them. | human (Q4) |
| B5 | Medium and up: four test layers (test case, DSL, driver, system). Patch and small: plain tests in domain words. One test file for each rule (S4). | human (Q5) |
| B6 | The security review uses `dev-stack:secure-coding`. Without dev-stack, a short checklist at the level of the OWASP Top 10, and the report says that the full rules were not there. | human (Q6) |
| B7 | `/review-comments` pushes the fix commits without a force push, and replies on each thread. It does not resolve a thread. | human (Q7, Q14) |
| B8 | A team run goes to `dev-team:process-review` until step 7. | human (Q8) |
| B9 | After a miss, the report gives one proposal: a line in `CLAUDE.md`, a skill change, or a check. The skill does not make the change. | human (Q9) |
| B10 | Remove `shared/example-mapping.md`, `shared/examplecheck.py`, and the example-mapping step. An old run with `examples.md` keeps its locks (`baseline: legacy`). | human (Q10) |
| B11 | A chore or a task has a ticket, but no Behaviour spec. `spec.md` gives a check for each part, or "review only". | human (Q13) |
| B12 | The tiers, the caps, and the size table come from plan version 7 without a change. | plan version 7 |
| B13 | Version 3.0.0, because the typed name `plan-and-build` stops. Released with the other plugins after step 9. | plan version 7 |

## Tests
- `build`: the delivery suite, 14 stories. 3 runs of the new `build` and 3 runs of `plan-and-build` 2.1.0 on each story: 84 runs. Step 9 uses these runs again.
- `review-comments`: 2 fixture pull requests, 3 runs with and 3 runs without the skill.
- One set of trigger queries for both commands.
- Budget: at most 300 dollars for all runs of this step. Each run has its own cap.

## Success criteria
- Each build has a ticket. A build from plain words drafts the item and waits for the approval.
- `spec.md` has each rule of the ticket, and `specsync.py` shows 0 errors. The build does not change a rule.
- Each rule has one locked test file. `speclock.py check` shows 0 changes at the final gate.
- The builder never reads the held-back tests.
- The new `build` passes at least as many hidden tests as `plan-and-build` 2.1.0 on the suite.
- `/review-comments` replies on each thread, pushes without force, and resolves no thread.
- `python3 -m unittest discover -s tests`, `sync_shared.py --check`, and `claude plugin validate` pass.

## Limits
- No change to dev-team in this step, except the target of B8.
- No change to the spec format of step 3.
- The cut of example mapping does not change product-flow, except the move of `refine.md`.

## Language and runtime
English and Dutch output: ASD-STE100 and Begrijpelijk Nederlands B1. Claude Code only. The build needs a shell and git.
