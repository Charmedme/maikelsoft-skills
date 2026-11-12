# Scenario loop

Build one rule at a time, in the order of "## Scenarios" in `plan.md`. Small: in the order of `spec.md`.

## Before the first scenario

- **Branch.** Use the branch rules of the project guide. Else, name the branch `feat/<item>` (a bug: `fix/<item>`).
- **Base.** Write the commit of the start in the header of `plan.md` and `review.md`. Each check of the run uses it.
- **Commands.** Find the commands for the build, one test file, and the full suite. Write them in `plan.md`, or in `run.md` for a small item.
- **Suite before.** Run the full suite one time. Write each test that fails before the change in `run.md`: it is not a failure of this build.

## For each rule

1. **Test file.** Write the test file of the rule, with one test for each scenario (`spec.md`, "Test layers"). Run it: it must fail for the right reason.
2. **Lock.** Approve and lock the file (`spec.md`, "Lock a test file").
3. **Green.** Write unit tests and code until the test file passes. Each test that passed before stays green. At most 3 tries: a try is a change of the code and a run of the tests. At the cap, stop and give the options of `cycle.md`.
4. **Step review.** Start a new sub-agent. Give it the diff of this rule, the rule, and the test file. It answers: is this the smallest change that makes the rule pass, and does a change go outside the rule? Fix a blocking finding.
5. **Commit.** Run `speclock.py check`. Commit with the message `<item key>: R<n> <rule title>`. Follow the commit rules of the project when it has them. Set the status of the scenario to `done` in `plan.md`.

Start the next rule only after the commit. Change the driver when the protocol needs it.

A patch has one rule: the test that fails because of the bug. Steps 2 and 4 do not apply.

## Stop and ask the human only when

- A rule needs a technical decision that is not in `decisions.md`.
- A rule needs a product decision that is not in the ticket. Read `po-questions.md`.
- A test cannot pass without a change of a locked test. That is a product question.
- A step changes data or a system outside the project, or cannot be undone.

Then write the reason in `run.md`, and ask the human. Continue with the next rule when it does not depend on the answer.
