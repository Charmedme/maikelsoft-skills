# Spec and tests

The Behaviour spec on the ticket is the contract. The PO approved it. You copy it into `spec.md`, give each part a check, and write one test file for each rule. After the lock, you do not change a test file.

## spec.md

`docs/work/<item>/spec.md` has three parts:

1. **The spec block.** `specsync.py copy` writes it between the lines `<!-- BEGIN behaviour spec: <item> -->` and `<!-- END behaviour spec -->`. It also writes `spec-baseline.json` with a fingerprint of each rule. Do not edit the block by hand.
2. **Design rules.** Only when the item has UI work. Copy the rules of `ux-design.md` that the change must keep. Write one line for each rule, numbered D1, D2.
3. **Checks.** A table with one row for each part. A part is a rule R<n> or a design rule D<n>. For a chore or a task, a part is a part of the change. The column Check names the test file, or `review only` when no test can check the part. When a check and the text disagree, the check wins: fix the check, or ask the PO.

~~~markdown
## Checks

| Part | Check |
|------|-------|
| R1 A return within 30 days is free | `tests/Shop.Tests/Acceptance/Specs/R1FreeReturnTests.cs` |
| D1 The fee shows next to the button | review only |
~~~

Copy the spec:

```
python3 ${CLAUDE_SKILL_DIR}/scripts/specsync.py copy docs/work/<item>/ticket.txt --item <key> --out docs/work/<item>/spec.md
```

Add `--kind bug` for a bug, and `--from adf` or `--from html` for the raw text of Jira or Azure DevOps. Then run `workcheck.py docs/work/<item>`. Done when both show 0 errors. Add a comment on the ticket with the link to the branch or the work folder. Do not change the description: a change there changes the spec.

## Test layers

| Size | Layers |
|------|--------|
| Patch | One plain test that fails because of the bug. |
| Small | Plain tests in domain words. One test file for each rule. |
| Medium, large | Four layers. One test file for each rule. |

The four layers:

| Layer | Content | Example (.NET) |
|-------|---------|----------------|
| 1. Test case | One test for each scenario of the rule. Domain words only: no URL, no JSON, no table name | `ReturnOnDay30_IsFree()` calls `Returns.Return(order, day: 30)` |
| 2. DSL | Domain verbs and checks. It hides IDs, waits, and protocols. You can add to it. Never make a check weaker | `Returns.Return(order)`, `Returns.ShouldBeFree(order)` |
| 3. Protocol driver | Talks to the real system: HTTP through the test host of the project, or the UI | `HttpReturnsDriver` with `HttpClient` from the `WebApplicationFactory` |
| 4. System under test | The application | |

Find the place first: the project guide, or an acceptance folder that exists. Else make one folder in the test project, for example `tests/<Project>.Tests/Acceptance/` with `Specs/`, `Dsl/`, and `Drivers/`. Use the test framework, the assertion library, and the test host that the project has. Do not add a package for this. The test name says the scenario in domain words.

## Lock a test file

Write the test file of one rule, and run it. It must fail for the right reason, for example 404 because the endpoint does not exist yet. A test that passes before the change tests nothing new. Change it, or tell the human that the behaviour exists already. Then approve and lock it:

- **High risk** (a risk of `tiers.md`, or `riskcheck.py` gives `raise: yes`): show the human the test file and the scenarios that it checks. Ask: "Does this test say what rule R<n> means?" Lock after the yes.
- **Else:** the approval of the list of scenarios at the plan gate covers the lock. Write `--by "<who> (plan gate)"`.

```
python3 ${CLAUDE_SKILL_DIR}/scripts/speclock.py approve --item <key> --by <who> --rule R<n> <test file> --watch <DSL files>
```

The hook blocks each edit of the test file. `speclock.py` watches the DSL: you can add methods, and `check` lists each change for the review. The driver is free.

## When a locked test seems wrong

Stop. Do not change the test, do not skip it, and do not change the DSL to make it pass. Tell the human which test, what it expects, and why you think it is wrong. When the scenario on the ticket is wrong, it is a product question. After the human approves the change, run `speclock.py unlock --by <who> --rule R<n>`, change the test, and lock it again.

## Chore and task

A chore or a task has a ticket, but no Behaviour spec. `spec.md` has no spec block. Its table "## Checks" names each part of the change, with a test or `review only`. Example: "Update the package to 9.0" with the check "the full suite and the build pass". The checks are the gate.

## An old run

A run from dev-flow 2 has `examples.md` and no `spec.md`. Run `runheader.py adopt <work folder>`: the header gets `baseline: legacy`. The run keeps its locks. Finish it with the steps of this skill. Do not copy the spec again.

The rules and examples of that run are in `examples.md`. Read `references/example-mapping.md` for its format, and check it with `examplecheck.py <examples.md>`. A large run from dev-flow 2 can have tickets in `docs/work/<item>/tickets/`, in the format of `references/templates/ticket.md`.

## No human to ask

With an answer sheet, the sheet approves the plan gate and the lock. Without a human and without a sheet, the lock can be "assumed". That is only true when each test case checks one scenario of the spec, word for word. Write `--by "assumed (spec of the ticket)"`. Else stop before the lock.
