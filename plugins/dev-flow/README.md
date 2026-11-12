# dev-flow

dev-flow is a Claude Code plugin for developers who build an approved work item: a user story, a bug, a task, or a chore. Its skill `build` takes the item from its ticket to tested commits, spec first.

The Behaviour spec on the ticket is the contract. The product owner (PO) approved it, and the build does not change a rule of it. The skill copies the spec into `spec.md`, locks one test file for each rule, and builds until the tests pass. The size of the change sets how much process the work gets. The PO keeps the product decisions on the ticket. You make the technical decisions.

English output follows ASD-STE100 Simplified Technical English. Dutch output follows Begrijpelijk Nederlands (taalniveau B1).

## Install

You need Claude Code and Python 3.9 or later.

```sh
claude plugin marketplace add Charmedme/maikelsoft-skills
claude plugin install dev-flow@maikelsoft-skills
```

Optional plugins:

- `dev-team` 1.0.0 or later: the team mode of `build` for a large change with a risk. The route step `team` of your role decides: `ask` offers the team, `on` takes the team without the question, and `off` builds in one session.
- `ux-design`: the UX review of an item with UI work needs it, and Playwright.
- `product-flow`: drafts a bug item for a fix without a ticket. Without it, `build` drafts the item itself.
- `dev-stack`: the security reviewer loads `dev-stack:secure-coding`. Without it, the reviewer uses a short checklist at the level of the OWASP Top 10.

## Quick start

1. Open Claude Code in a repository.
2. Give the skill an approved item:

   ```
   /dev-flow:build AB#1234
   ```

   A request in plain words also starts the skill:

   ```
   Plan and build AB#1234
   ```

3. The skill reads the ticket and tells you the size in one line, for example: "Size: small bug. One method in `OrderService`, no contract change."
4. Answer the questions at the gates of that size. At the end, the skill gives a report: testing, risk, how to undo, and the commits.

Do you have an idea, and no item yet? Make the item first with `product-flow:idea-to-backlog`. That skill can write the items to files when you have no tracker.

## Commands

Claude Code starts a skill by itself when your request matches its trigger. You can also start a skill with its command. Text after the command goes to the skill, for example `/dev-flow:build AB#1234`.

| Command | What it does | Starts by itself when you ask for |
|---------|--------------|-----------------------------------|
| `/dev-flow:build` | Builds one work item from its ticket to tested commits. It locks one test file for each rule of the Behaviour spec, and builds with TDD, held-back tests, and fresh reviews. | An item, a ticket, a bug fix, or a story built, "build" or "plan and build", or a run with a `docs/work/<item>/run.md` to continue |
| `/dev-flow:review-comments` | Handles the review comments on a pull request of a solo build. It sorts and fixes each comment, and replies in each thread. | Review comments on a pull request to process, fix, or answer |

## Usage

### The old command

Version 3.0.0 gives the skill `plan-and-build` the new name `build`. The old command still works: it sends your request to `build`. A later major version removes the old command, so use `/dev-flow:build`.

### The steps of a build

1. **Context.** The skill reads the role line and `.claude/rules/delivery.md`. When `run.md` exists, it continues at the open step.
2. **Ticket.** It reads the ticket and the PO decisions on it. Without a ticket, it drafts the item first (see [No ticket](#no-ticket)).
3. **Spec.** It copies the Behaviour spec into `spec.md` (see [Spec first](#spec-first)).
4. **Size and plan.** It sets the size. Medium and large get `plan.md` and a fresh plan reviewer, then the plan gate.
5. **Held-back tests.** Large, or medium with a risk: a separate agent writes them.
6. **Scenario loop.** For each rule: the test file, the lock, red, green, a commit.
7. **Prove.** The full suite, the lock check, the held-back tests, and the mutation check.
8. **Reviews.** Fresh reviewers check the code and the security. An item with UI work also gets the UX review.
9. **Docs.** `docscheck.py` names each doc that the change makes old.
10. **Final gate.** The report: Testing, Risk, How to undo, and the commits.

### Sizes

| Size | Signal |
|------|--------|
| Patch | One file. The cause is clear. No change to a contract, the data, or the configuration of another system. |
| Small | One behaviour, up to about 3 files, one layer. |
| Medium | One feature over more than one layer, for example an endpoint, its logic, and its storage. |
| Large | Two or more of the large signals. |

When two sizes fit, the skill takes the higher one. The large signals are:

1. Three or more endpoints, screens, or message handlers are new or change.
2. The schema changes, or a migration must change the data.
3. The item has more than 8 acceptance criteria.
4. The plan has more than 10 files that change.

Each size runs in one session.

A risk sets the size to medium or higher. A risk alone does not make the size large. The risks are:

- A schema change or a data migration.
- Sign-in, authorization, secrets, or file upload.
- A contract change that the item does not ask for, or that breaks existing clients.
- A new package or a new external service.
- Money, invoices, or prices.
- The deletion of data.
- Another step that is hard to undo.

The type of the change also changes the steps. A bug starts with a test that fails because of the bug. A refactor runs the tests before and after. A chore or a task is patch or small, unless a risk applies. It has no Behaviour spec: the table "## Checks" of `spec.md` is its gate.

The skill moves the size up when the facts show more work or a risk. It moves the size down only when you say so. When the size is large and a risk applies, the skill offers the team of `dev-team`, or takes it when your route says `team=on`. The team works in the same work folder, with the same `spec.md` and locks.

The size sets the steps:

| Step | Patch | Small | Medium | Large |
|------|-------|-------|--------|-------|
| Plan gate | None | The list of scenarios | `plan.md`, 1 plan review round | `plan.md`, at most 3 plan review rounds |
| Test layers | One plain test | Plain tests in domain words | Four layers | Four layers |
| Held-back tests | No | No | When a risk applies | Yes |
| Code review passes | 1 (quick) | 1 | 1 (code and security) | 2 (pass 2: security) |
| Code review round cap | 1 | 2 | 2 | 3 |
| Security review | When `riskcheck.py` raises it | When `riskcheck.py` raises it | In the code review | In pass 2 |

When a phase reaches its cap, the skill stops. It gives you the open points and three options: one more round, accept the open points, or send them back to an earlier phase.

### Spec first

Before the skill writes code, it does these steps:

1. **Draft spec.** A spec with the status `spec:draft`, or a story or bug without a spec, gets the refine pass first. The build waits for the approval of the PO. This works without the `product-flow` plugin.
2. **Copy.** `specsync.py copy` writes the spec block into `docs/work/<item>/spec.md`, and a fingerprint of each rule into `spec-baseline.json`. Do not edit the block by hand.
3. **Checks.** The table "## Checks" of `spec.md` gives each rule a test file, or "review only" when no test can check it. An item with UI work also gets the design rules D1, D2, and so on.
4. **Test file.** For each rule, the skill writes one test file with one test for each scenario. The test fails first, for the right reason. Medium and large use four layers: the test case, a DSL, a protocol driver, and the system.
5. **Lock.** `speclock.py approve` locks the test file and watches the DSL. For a high risk, you approve each test file first. Else, your approval of the list of scenarios at the plan gate covers the lock.

A PreToolUse hook (`hooks/guard.py`) blocks each edit of a locked test, also through Bash. When a locked test seems wrong, the skill stops and tells you. It does not change, skip, or delete the test.

When the PO changes the spec during the build, `specsync.py diff` names each changed rule. Each changed rule needs a person to approve it again. Then the skill unlocks only the test file of that rule, changes it, and locks it again.

### No ticket

Each build has a ticket. When you give a fix in plain words, without a key or a file, the skill drafts the item first:

1. For a bug, it starts `product-flow:bug-item` when the project has `product-flow`. Else it writes the item from a template.
2. It shows you the item and asks: "Is this the item to build?" It builds nothing before your yes.
3. It creates the item in the tracker of the project. Without a tracker, it writes the item to `docs/backlog/<slug>.md`.

### Held-back tests

For a large change, or a medium change with a risk, the plan reviewer of the last round writes extra tests. It writes them from `spec.md` and the DSL, before any code of the change exists. The tests use values that the scenarios do not use, for example the other side of each limit.

`heldback.py` keeps the tests in the data folder of the plugin, outside the repository: `~/.claude/plugins/data/dev-flow/heldback`, or the folder in `$DEV_FLOW_HELDBACK`. The guard hook blocks each direct read, search, or write of that folder, so the builder cannot see the tests. In the prove step, `heldback.py run` copies the tests into the test project, runs them, and removes them.

### Prove and review

After the last rule, the prove step runs:

- the full test suite, compared with the failures from before the change,
- `speclock.py check`, which must show 0 changes of a locked file,
- the held-back tests,
- `mutate.py` one time, with a limit (`--max 12`) and a timeout, while the reviews run.

A mutation that survives is a limit that no scenario checks. It becomes a new scenario for the PO, or a product question. When the project has Stryker, the skill runs it too. The prove step has at most 2 fix rounds.

Each review is a new sub-agent that did not share the context of the builder. For an item with UI work, the `ux-design` agent then reviews the built screens against the approved design. The agent runs on Sonnet and uses Playwright.

### Final report

`docscheck.py` runs before the final gate and names each doc that the change makes old. Then the skill runs `workcheck.py --final` and `speclock.py check`. An open blocking finding, or an open security finding of Critical or High, stops the gate.

The report has these parts:

- **Testing**: the locked tests, the full suite, the held-back tests, and the mutation check.
- **Risk**: the risks of the change, the result of the security review, and the open findings.
- **How to undo**: the revert of the commits, and each step that a revert does not undo, for example a migration.
- **Commits**: the hash and the message of each commit.

After a miss, the report gives one proposal: a line for `CLAUDE.md`, a change of a skill, or a new check. You decide about the change.

### Review comments

`/dev-flow:review-comments` handles the comments on one pull request of a solo build:

1. It gives each comment one class and writes the table in `docs/work/<item>/review.md`.
2. It fixes each blocking comment with a test first. A fresh agent reviews the fix diff.
3. It pushes the fix commits without a force push.
4. It replies in each thread with what changed, or why not. It never resolves a thread.

A comment that is a product question goes to the PO on the ticket. A comment never changes a locked test. A run of the `dev-team` flow goes to `dev-team:process-review`.

### Product questions go to the PO

A question about the user, the value, the scope, or the visible behaviour is a product question. The skill does not decide it, and it does not ask you to decide it unless you are the PO. It drafts one comment for the item, with a recommended answer for each question. After you approve the exact text, it posts the comment with the tracker tool. The work that depends on an answer waits. The technical decision record (`decisions.md`) holds only technical decisions, so a dev decision cannot change a PO decision.

### The work folder

Each build gets a work folder `docs/work/<item>/`:

```
docs/work/<item>/
  run.md                  the header and the log of the run
  ticket.txt              the raw text of the ticket, for specsync.py
  spec.md                 the spec block, the design rules, and the checks
  spec-baseline.json      the fingerprint of each rule
  plan.md                 medium and large: the design choices, the files, and the scenarios
  research.md             medium and large
  decisions.md            technical decisions only
  review.md               the findings of each review, with their round and status
```

To continue later, ask the skill to continue the run. `runheader.py read` reads the header of `run.md`, and the skill starts at the open step.

A run from dev-flow 2 has `examples.md` and no `spec.md`. `runheader.py adopt` marks it as an old run. The run keeps its locks, and the skill finishes it with the steps of 3.0.0. `examplecheck.py` still checks its `examples.md`.

When the repository has a team decision index (`docs/dev-team/decisions.md`), the skill records a new team decision with `dev-team:team-decisions`, with an ADR.

### The checkers

The scripts use only the Python standard library.

| Script | Checks or does |
|--------|----------------|
| `specsync.py` | Copies the Behaviour spec into `spec.md`, checks it, and names each changed rule (`copy`, `check`, `diff`) |
| `speclock.py` | Locks the approved test files and checks that they and the DSL did not change (`approve`, `check`, `unlock`) |
| `heldback.py` | Keeps the held-back tests outside the repository and runs them (`add`, `list`, `run`) |
| `mutate.py` | A locked test catches each changed comparison |
| `riskcheck.py` | The path and pattern risks of the change |
| `docscheck.py` | The docs that the change makes old |
| `runheader.py` | Reads and writes the header of `run.md` (`read`, `write`, `adopt`) |
| `workcheck.py` | The structure of the work folder |
| `examplecheck.py` | The `examples.md` of an old run from dev-flow 2 |

`workcheck.py` examples:

```sh
python3 workcheck.py docs/work/ord-45           # check the folder
python3 workcheck.py docs/work/ord-45 --final   # also check that each test file exists
```

The exit code is 1 when there is one or more errors. The checker finds these errors:

- A `run.md` without the header, the item, or the size.
- A missing `spec.md`, or a `spec.md` without the table "## Checks".
- A rule of the spec without a row in the table, or a row without a check.
- Medium and large: a missing `plan.md`, or a rule with a test file that is not in "## Scenarios".
- With `--final`: a test file of the table that does not exist.

### No human to answer

In a scheduled or unattended run, the skill takes its recommended answer for a technical question and marks it "assumed". When the prompt names an answer sheet, an answer in the sheet counts as your answer. The skill never assumes a new package, a new external service, or a fact that only you or the PO know. It never posts a comment: it gives the drafted comment in the report. While a question waits, it does not build the rule that depends on it.

## When it helps

Use it when:

- The PO approved a Behaviour spec, and the build must keep each rule of it. The lock and the hook stop an agent that changes a test to make it pass.
- The item has business rules with limits, for example a period, an amount, a count, or a time. The held-back tests and the mutation check find a limit that no scenario checks.
- A wrong result costs more than the extra time and money.
- The item can miss facts. The skill stops and asks the PO. Without the skill, the model guesses and builds.
- You need a record of the work: the spec, the decisions, the reviews, and a report.

Do not use it when:

- The change is small and clear, for example a typo, a config value, or a bug with a known cause. Plain Claude Code gives the same result faster and at a lower cost.
- You explore or make a prototype. The gates slow you down, and the tests get thrown away. Use `product-flow:product-prototype` or plain Claude Code.
- Nobody reads the gates. Their value is the human check.
- There is no PO to approve the spec. Make the item first with `product-flow`.

## Test results

### Evidence for 3.0.0

The delivery suite of the repository compared 3.0.0 with 2.1.0 on 14 stories. Both versions got the same Behaviour spec.

- Two blind graders compared 28 pairs of runs. They agreed on 20 pairs: 3.0.0 was better in 15, 2.1.0 in 3, and 2 were equal.
- Confirmed defects (high, medium, low): 9, 4, and 45 for 3.0.0, against 10, 19, and 59 for 2.1.0.
- A 3.0.0 run costs about 1.7 times as much as a 2.1.0 run, and takes longer.

The details are in entries E5 to E7 of the [dev-flow edit log](../../docs/work/dev-flow/edit-log.md).

### Results of 2.0

24 runs in round 1 and 30 runs in rounds 2 to 5, on the orders-api fixture, model Opus, headless, with an answer sheet. Cost is the median per run. The column Tier is the size.

| Item | Checks | Tier | dev-flow 2.0 | dev-team | No plugin |
|------|--------|------|--------------|----------|-----------|
| Bug (patch) | 9/9 x3 | small | $0.84 | $3.40 | $0.36 |
| Small story | 9/9 x3 | small | $1.28 | $4.14 | $0.40 |
| Medium story | 9/9 x3 | small | $1.54 | $8.01 | $0.57 |
| Medium story with a DST rule | 9/9 x3 | medium | $2.72 | $11.11 | $0.77 |
| Large story (3 behaviours, schema change) | 9/9, 8/9, 9/9 | medium x2, large x1 | $4.57 | $9.72 | $1.12 |
| Story with a missing fact (must stop) | stops 3 of 3 | medium | $1.18 | $3.12 | builds anyway |

Each delivered run passes the build, the tests, the package policy, and each hidden acceptance test. The details, the verdict of each change, and the gaps are in the [test results of 2.0](../../docs/work/flow-scale/results.md).

## Limits

- The skill uses sub-agents for the plan review, the held-back tests, and each review, when Claude Code has them.
- The checker checks the structure of the work folder. It does not check if a plan is good.
- The tests ran the product question path only unattended: no test posted a comment to a real tracker.
- The hand-off to `dev-team` ran in one test run only.
- A run costs more and takes longer than a run without the plugin. The value of the flow is in the spec, the lock, the held-back tests, and the reviews.
- The spec does not find each ambiguity. The PO still reads the spec, and you still read the scenarios at the plan gate.

## Documentation

- [Changelog](CHANGELOG.md)
- [Credits](CREDITS.md)
- [Edit log of dev-flow](../../docs/work/dev-flow/edit-log.md): each change of the skill, with its test and its verdict.
- [Concept](../../docs/work/flow-scale/concept.md): the reasons for the tiers and for one product front end.
- [Executable specifications](../../docs/work/flow-scale/executable-specs-concept.md) and [BA and small steps](../../docs/work/flow-scale/ba-and-small-steps-concept.md): the reasons for spec first and example mapping.
- [Cost analysis](../../docs/work/flow-scale/cost-analysis.md).
- [Test results of 1.0.0](../../evals/dev-flow/results-2026-10-09.md).

## License

MIT. Refer to the [LICENSE](../../LICENSE) file. The credits file has the license of the source skills.
