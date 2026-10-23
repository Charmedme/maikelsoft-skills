# dev-flow

dev-flow is a Claude Code plugin for developers who build an approved work item: a user story, a bug, a task, or a chore. Its skill `plan-and-build` reads the item and sets a tier from the size and the risk of the change. Then it runs only the steps and gates of that tier. The product owner (PO) keeps the product decisions on the item. You make the technical decisions.

English output follows ASD-STE100 Simplified Technical English. Dutch output follows Begrijpelijk Nederlands (taalniveau B1).

## Install

You need Claude Code and Python 3.9 or later.

```sh
claude plugin marketplace add Charmedme/maikelsoft-skills
claude plugin install dev-flow@maikelsoft-skills
```

For a large item, the skill starts `dev-team:deliver-story`. Install `dev-team` too when you want that path.

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
| Small | One behaviour, up to about 3 files, one layer | Done |
| Medium | One feature over more than one layer | Plan, Done |
| Large | More than one feature, or more than 10 tickets | Runs with `dev-team:deliver-story` |

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

### Product questions go to the PO

A question about the user, the value, the scope, or the visible behaviour is a product question. The skill does not decide it, and it does not ask you to decide it unless you are the PO. It drafts one comment for the item, with a recommended answer for each question. After you approve the exact text, it posts the comment with the tracker tool. The work that depends on an answer waits. The technical decision record (`decisions.md`) holds only technical decisions, so a dev decision cannot change a PO decision.

### The work folder

Small and medium items get a work folder, by default `docs/work/<slug>/`:

```
docs/work/<slug>/
  plan.md                 the item, the tier, the plan, the questions for the PO, the gates
  research.md             medium
  decisions.md            medium: technical decisions only
  tickets/NN-<slug>.md    medium: one file for each ticket
```

A patch has no work folder: the report holds the result. To continue later, ask the skill to continue the run. It reads `plan.md` and starts at the first open step.

When the repository has a team decision index (`docs/dev-team/decisions.md`), the skill records a new team decision with `dev-team:team-decisions`, with an ADR.

### The checker

`scripts/workcheck.py` checks the work folder. It uses only the Python standard library.

```sh
python3 workcheck.py docs/work/ord-45           # check the folder
python3 workcheck.py docs/work/ord-45 --next    # which tickets can start now?
```

The exit code is 1 when there is one or more errors. The checker finds these errors:

- A missing or wrong tier, or a gate out of order.
- A gate Plan that is "assumed" while a question waits for the PO.
- An acceptance criterion that is in no ticket.
- A missing blocker, or a cycle of blockers.
- A ticket table that does not agree with the ticket files.

### No human to answer

In a scheduled or unattended run, the skill takes its recommended answer for a technical question and marks it "assumed". It never assumes a new package, a new service, or a fact that only you or the PO know. It never posts a comment. While a question waits, it stops before the build.

## Test results

24 runs in round 1 and 24 runs in rounds 2 to 4, on the orders-api fixture, model Opus, headless, with an answer sheet. Cost is the median per run.

| Item | Checks | Tier | dev-flow 2.0 | dev-team | No plugin |
|------|--------|------|--------------|----------|-----------|
| Bug (patch) | 9/9 x3 | small | $0.84 | $3.40 | $0.36 |
| Small story | 9/9 x3 | small | $1.28 | $4.14 | $0.40 |
| Medium story | 9/9 x3 | small | $1.54 | $8.01 | $0.57 |
| Story with a missing fact (must stop) | stops 3 of 3 | medium | $1.18 | $3.12 | builds anyway |

Each delivered run passes the build, the tests, the package policy, and each hidden acceptance test. The details, the verdict of each change, and the gaps are in the [test results](../../docs/work/flow-scale/results.md).

## Limits

- The skill uses sub-agents for the review of a small item and for each ticket of a medium item, when Claude Code has them.
- The checker checks the structure of the work folder. It does not check if a plan is good.
- The product question path is tested only unattended: no test posted a comment to a real tracker.
- No test item reached the large tier, so the hand-off to `dev-team` is not tested end to end.

## Documentation

- [Changelog](CHANGELOG.md)
- [Credits](CREDITS.md)
- [Concept](../../docs/work/flow-scale/concept.md): the reasons for the tiers and for one product front end.
- [Test results of 1.0.0](../../evals/dev-flow/results-2026-10-09.md).

## License

MIT. Refer to the [LICENSE](../../LICENSE) file. The credits file has the license of the source skills.
