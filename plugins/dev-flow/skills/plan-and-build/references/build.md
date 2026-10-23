# Build

Medium tier: after the gate Plan, set the status of each ticket to `todo`. Then build the tickets one by one. Patch and small tier: use "Before the first ticket" and the test-first steps for the one change, and commit with the item key and title.

## Before the first ticket

- **Branch.** Use the branch rules of the project guide. Else, name the branch exactly `feat/<slug>`. In a chat without a repository, write the code in the work folder.
- **Commands.** Find the commands for the type check, one test file, and the full test suite. Write them in the "Build" section of `plan.md`.

## For each ticket

1. Run `workcheck.py <work folder> --next`. Take the first ticket that it gives. Set its status to `doing`.
2. If you have sub-agents, give the ticket to a new sub-agent. Give it the ticket, the item (acceptance criteria and PO decisions from `plan.md`), `decisions.md`, the test commands, and these steps. One sub-agent for each ticket keeps each context small.
3. Use test-driven development at the seams of `decisions.md`:
   1. Write one test for one acceptance criterion. Run it. It must fail.
   2. Write the smallest code that makes the test pass. Run the test.
   3. Clean the code. Run the type check and the test file.
   4. Go to the next criterion.
4. Mark each criterion `[x]` when its test passes.
5. Commit with the message `<ticket number>: <ticket title>`. Follow the commit rules of the project when it has them.
6. Set the status to `done` in the ticket and in `plan.md`. Run `workcheck.py`.

Do not stop between tickets.

## Stop and ask the human only when

- A ticket needs a technical decision that is not in `decisions.md`.
- A ticket needs a product decision that is not in the item. Read `po-questions.md`.
- A test cannot pass without a change to an acceptance criterion. That is a product question.
- A step changes data or a system outside the project, or cannot be undone.

Then set the ticket status to `blocked`, write the reason in the ticket, and ask the human. Continue with other tickets that the checker gives, when they do not depend on it.

## After the last ticket

Run the full test suite one time. Fix each failure.

Remove `spike/` from the branch in a separate commit, unless the project guide says to keep it. The commit message says where the spike is in the history: `Remove spike (see <commit>)`.
