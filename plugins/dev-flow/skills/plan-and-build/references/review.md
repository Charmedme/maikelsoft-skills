# Phase 6: Review

Check the work against the specs and the tickets. The agent that wrote the code does not review its own code: use a new sub-agent when you have one.

## Steps

1. **Spec.** For each user story and each acceptance criterion, find the code and the test that make it work. List each item that is missing or different.
2. **Standards.** Check the changes against the project guide and the conventions of the code around them.
3. **Risks.** Look for input without a check and errors without a handler. Also look for secrets in the code, data that a change can lose, and queries in a loop.
4. **Fix.** Fix each finding that is clearly a bug. Commit the fix. Put each other finding in the report.
5. **Run.** Run the type check and the full test suite.

## Gate G4

Give the report in less than 250 words. Copy each number from the command output of step 5.

- The result of the full test suite.
- For each spec: covered, or the items that are missing.
- The findings that you fixed, and the findings for the human, each with the file and the reason.
- The commits.
- The open questions, each with your recommended answer.

Write "G4: waiting" in `plan.md`. The human decides when to close the work. Then write "G4: approved".
