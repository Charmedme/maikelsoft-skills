# dev-flow

dev-flow is a Claude Code plugin for developers who build an approved work item: a user story, a bug, a task, or a chore. Its skill `plan-and-build` reads the item and sets a tier from the size and the risk of the change. It turns the examples of the item into acceptance tests first. After you approve the tests, it locks them and builds until they pass. The product owner (PO) keeps the product decisions on the item. You make the technical decisions.

English output follows ASD-STE100 Simplified Technical English. Dutch output follows Begrijpelijk Nederlands (taalniveau B1).

## Install

You need Claude Code and Python 3.9 or later.

```sh
claude plugin marketplace add Charmedme/maikelsoft-skills
claude plugin install dev-flow@maikelsoft-skills
```

The skill starts `dev-team:deliver-story` only when you ask for the team. Install `dev-team` too when you want that path.

## Quick start

1. Open Claude Code in a repository.
2. Give the skill an approved item:

   ```
   Plan and build AB#1234
   ```

3. The skill reads the item and tells you the tier in one line, for example: "Tier: small bug. One method in `OrderService`, no contract change."
4. Answer the questions at the gates of that tier. At the end, the skill gives a report with the tests, the checks, and the commits.

Do you have an idea, and no item yet? Make the item first with `product-flow:idea-to-backlog`. That skill can write the items to files when you have no tracker.

## Usage

### Tiers

| Tier | Signal | Gates |
|------|--------|-------|
| Patch | One file, a clear cause, no contract or data change | Done |
| Small | One behaviour, up to about 3 files, one layer | Spec, Done |
| Medium | One feature over more than one layer | Spec (with the ticket list), Done |
| Large | Two or more counted signals, for example more than one feature and a schema change | Design, Spec, Done |

Each tier runs in one session.

A risk sets the tier to medium or higher, whatever the size. The risks are:

- A schema change or a data migration.
- Sign-in, authorization, secrets, or file upload.
- A contract change that the item does not ask for, or that breaks existing clients.
- A new package or a new external service.
- Money.
- The deletion of data.
- Another step that is hard to undo.

The type of the change also changes the steps. A bug starts with a test that fails because of the bug. A refactor runs the tests before and after. A chore is patch or small.

The skill moves the tier up when the facts show more work. It moves the tier down only when you say so.

### Spec first

For a small, medium, or large item, the skill does these steps before it writes code:

1. **Example mapping.** It writes `examples.md` with the rules of each criterion. Each limit gets an example below, at, and above it. The file also lists the words with more than one meaning. It searches the existing decisions first, for example a Decisions table in the backlog context file. Each rule without an answer becomes a question for the PO. `examplecheck.py` checks the file.
2. **Acceptance tests.** It turns the answered examples into tests. The tests have four layers: the test case, a DSL, a protocol driver, and the system. The tests fail first.
3. **Gate Spec.** You approve `examples.md` and the tests. `speclock.py` then locks the test cases and watches the DSL. A PreToolUse hook blocks each edit of a locked test, also through Bash.
4. **Build.** One scenario at a time: red, green, a review of the step, a commit.
5. **Review.** `speclock.py check` must show 0 changes. `mutate.py` flips each comparison in the changed code. A mutation that survives is a rule without an example. When the project has Stryker, the skill runs it too. In the medium and large tier, a review sub-agent on Sonnet reads the diff.

### Product questions go to the PO

A question about the user, the value, the scope, or the visible behaviour is a product question. The skill does not decide it, and it does not ask you to decide it unless you are the PO. It drafts one comment for the item, with a recommended answer for each question. After you approve the exact text, it posts the comment with the tracker tool. The work that depends on an answer waits. The technical decision record (`decisions.md`) holds only technical decisions, so a dev decision cannot change a PO decision.

### The work folder

Small, medium, and large items get a work folder, by default `docs/work/<slug>/`:

```
docs/work/<slug>/
  examples.md             rules, examples, words with more than one meaning, open questions
  plan.md                 medium and large: the plan, the questions for the PO, the gates
  research.md             medium
  decisions.md            medium: technical decisions only
  tickets/NN-<slug>.md    medium: one file for each ticket
```

A patch has no work folder. For a patch and a small item, the report holds the plan and the result. To continue later, ask the skill to continue the run. It reads `plan.md` and starts at the first open step.

When the repository has a team decision index (`docs/dev-team/decisions.md`), the skill records a new team decision with `dev-team:team-decisions`, with an ADR.

### The checkers

The scripts use only the Python standard library.

| Script | Checks |
|--------|--------|
| `examplecheck.py` | Each rule has an example, each limit has the cases below, at, and above, and each open example has a question |
| `speclock.py` | The approved test cases and the DSL did not change |
| `mutate.py` | Each changed comparison is caught by an acceptance test |
| `workcheck.py` | The structure of a medium or large work folder |

`workcheck.py` examples:

```sh
python3 workcheck.py docs/work/ord-45           # check the folder
python3 workcheck.py docs/work/ord-45 --next    # which tickets can start now?
```

The exit code is 1 when there is one or more errors. The checker finds these errors:

- A missing or wrong tier, or a gate out of order.
- A gate that is "assumed" while a question waits for the PO.
- An acceptance criterion that is in no ticket.
- A missing blocker, or a cycle of blockers.
- A ticket table that does not agree with the ticket files.

### No human to answer

In a scheduled or unattended run, the skill takes its recommended answer for a technical question and marks it "assumed". It never assumes a new package, a new service, or a fact that only you or the PO know. It never posts a comment. While a question waits, it stops before the build.

## When it helps

Use it when:

- The item has business rules with limits, for example a period, an amount, a count, or a time. Example mapping gives an example below, at, and above each limit. Without it, a model often does not test the exact limit.
- A wrong result costs more than the extra time. The gate Spec shows the rules, the examples, and the source of each answer on one page, before code exists.
- The tests must stay the contract. The lock and the hook stop an agent that changes a test to make it pass.
- The item can miss facts. The skill stops and asks the PO. Without the skill, the model guesses and builds.
- You need a record of the work: examples, decisions, and a report.

Do not use it when:

- The change is small and clear, for example a typo, a config value, or a bug with a known cause. Plain Claude Code gives the same result faster and at a lower cost.
- You explore or make a prototype. The gates slow you down, and the tests get thrown away. Use `product-flow:product-prototype` or plain Claude Code.
- Nobody reads the gate Spec. Its value is the human check. A model does not see each word with two meanings, and it can take a related decision as the answer. Without a reader, you pay about 2 times the cost for a small gain.
- There is no item and no PO. Make the item first with `product-flow`.

## Test results

24 runs in round 1 and 30 runs in rounds 2 to 5, on the orders-api fixture, model Opus, headless, with an answer sheet. Cost is the median per run.

| Item | Checks | Tier | dev-flow 2.0 | dev-team | No plugin |
|------|--------|------|--------------|----------|-----------|
| Bug (patch) | 9/9 x3 | small | $0.84 | $3.40 | $0.36 |
| Small story | 9/9 x3 | small | $1.28 | $4.14 | $0.40 |
| Medium story | 9/9 x3 | small | $1.54 | $8.01 | $0.57 |
| Medium story with a DST rule | 9/9 x3 | medium | $2.72 | $11.11 | $0.77 |
| Large story (3 behaviours, schema change) | 9/9, 8/9, 9/9 | medium x2, large x1 | $4.57 | $9.72 | $1.12 |
| Story with a missing fact (must stop) | stops 3 of 3 | medium | $1.18 | $3.12 | builds anyway |

Each delivered run passes the build, the tests, the package policy, and each hidden acceptance test. The details, the verdict of each change, and the gaps are in the [test results](../../docs/work/flow-scale/results.md).

## Limits

- The skill uses sub-agents for the review of a small item and for each ticket of a medium item, when Claude Code has them.
- The checker checks the structure of the work folder. It does not check if a plan is good.
- The product question path is tested only unattended: no test posted a comment to a real tracker.
- The hand-off to `dev-team` ran in one test run only.
- On the synthetic fixture, a run without a plugin passes the same hidden tests at a lower cost. The value of the flow is in the gates, the lock, and the questions, not in a higher test score.
- Example mapping does not find each ambiguity. The human at the gate Spec still reads the examples and their sources.

## Documentation

- [Changelog](CHANGELOG.md)
- [Credits](CREDITS.md)
- [Concept](../../docs/work/flow-scale/concept.md): the reasons for the tiers and for one product front end.
- [Executable specifications](../../docs/work/flow-scale/executable-specs-concept.md) and [BA and small steps](../../docs/work/flow-scale/ba-and-small-steps-concept.md): the reasons for spec first and example mapping.
- [Cost analysis](../../docs/work/flow-scale/cost-analysis.md).
- [Test results of 1.0.0](../../evals/dev-flow/results-2026-10-09.md).

## License

MIT. Refer to the [LICENSE](../../LICENSE) file. The credits file has the license of the source skills.
