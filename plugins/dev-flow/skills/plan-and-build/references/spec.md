# Executable specification

The examples on the item are the specification. Turn them into acceptance tests before you write production code. The human approves the test cases at the gate Spec. After the gate, you do not change them: the approved test cases are the contract.

## Two test streams

| Stream | Who owns it | What it tests | Can you change it after the gate Spec? |
|--------|-------------|---------------|----------------------------------------|
| Acceptance tests | The human and the PO | The examples of the item, through the API or the UI | No. A hook blocks the edit. Ask the human |
| Unit tests | You | The parts inside, while you build | Yes |

## Four layers

Make the layers one time for each project. A later item adds test cases, and adds DSL methods when it needs them.

| Layer | Content | Example (.NET) |
|-------|---------|----------------|
| 1. Test case | One test for each example of the item. Domain words only: no URL, no JSON, no table name | `CancelInsideWindow_OrderIsCancelled()` calls `Orders.Place(...)` and `Orders.Cancel(...)` |
| 2. DSL | Domain verbs and checks. It hides IDs, waits, and protocols. You can add to it. Never make a check weaker | `Orders.Cancel(order)`, `Orders.ShouldBeCancelled(order)` |
| 3. Protocol driver | Talks to the real system: HTTP through the test host of the project, or the UI | `HttpOrdersDriver` with `HttpClient` from the `WebApplicationFactory` |
| 4. System under test | The application | |

Find the place first: the project guide, or an acceptance folder that exists. Else make one folder in the test project, for example `tests/<Project>.Tests/Acceptance/` with `Specs/`, `Dsl/`, and `Drivers/`. Use the test framework, the assertion library, and the test host that the project has. Do not add a package for this.

## Steps

1. **Map the examples.** List each example of the item (Given/When/Then, or a row of an example table) with its acceptance criterion. An example that is not clear is a product question (`po-questions.md`).
2. **Write the test cases.** One test for each example. The test name says the example in domain words. The body calls only the DSL.
3. **Write the DSL and the driver.** Only what the test cases need.
4. **Run them: red.** Each new test case must fail, for the right reason (for example 404 because the endpoint does not exist yet). A test that passes before the change tests nothing new. Change it, or tell the human that the behaviour exists already.
5. **Gate Spec.** Show the human each test case: the file, the test name, the body, and the example that it checks. Ask: "Do these tests say what the item means?"
6. **Lock.** After the approval, run:

   ```
   python3 ${CLAUDE_SKILL_DIR}/scripts/speclock.py approve --item <key> --by <who> <test case files> --watch <DSL files>
   ```

   The test cases are locked: the hook blocks each edit. The DSL is watched: you can add methods, and `check` lists each change for the review. The driver is free: it is plumbing.
7. **Build with TDD.** Write unit tests and code until all acceptance tests and all unit tests pass. Change the driver when the protocol needs it.
8. **Check the lock.** Before each commit and before the gate Done, run `speclock.py check`. It must show 0 changes.

## When a locked test seems wrong

Stop. Do not change the test, do not skip it, and do not change the DSL to make it pass. Tell the human which test, what it expects, and why you think it is wrong. When the example of the item is wrong, it is a product question. After the human approves the change, run `speclock.py unlock --by <who> <file>`, change the test, and lock it again.

## Patch tier

A bug fix needs no new layers. Its specification is one test that fails because of the bug. Put it in the acceptance tests when the project has them. Else put it in the unit tests. Show it in the report at the gate Done.

## No human to ask

With an answer sheet, the sheet approves the gate Spec. Without a human and without a sheet, the gate Spec can be "assumed". That is only true when each test case maps to one example of the item, word for word. Write `--by "assumed (examples of the item)"`. Else stop at the gate Spec.
