# Prove

After the last rule, prove that the change does what the spec says, and only that. At most 2 fix rounds (`cycle.md`).

## Steps

1. **Full suite.** Run the full test suite. Compare the failures with the list of `run.md` from before the change. Each new failure is a finding of this build.
2. **Lock.** Run `speclock.py check`. It must show 0 changes of a locked file. Give each WATCH line to the code reviewer.
3. **Held-back tests.** Medium and large. Run them in a new folder of the test project, with the command that runs that folder:

   ```
   python3 ${CLAUDE_SKILL_DIR}/scripts/heldback.py run --item <key> --dest <test project>/HeldBack --test "<command>"
   ```

   The script copies the tests in, runs them, and removes them. A failure shows a rule that the build does not meet. Fix the code, not the test. When the failure names a value that the spec does not give, it is a product question.
4. **Mutation check.** Run the boundary mutation check on the changed code:

   ```
   python3 ${CLAUDE_SKILL_DIR}/scripts/mutate.py --base <base commit> --test "<command that runs the locked tests>"
   ```

   Each mutation that survives is a limit that no scenario checks. Look for the answer in the decisions of the project. With an answer: give it to the PO as a new scenario. Without an answer: it is a product question (`po-questions.md`). Do not add a locked test without the approval. When the project has Stryker (`dotnet-stryker` or `@stryker-mutator`), also run it on the changed files, and give its score.
5. **Fix round.** Fix each new failure of steps 1 and 3. Then run steps 1 to 4 again. After 2 fix rounds, stop. Give each open hole to the human with the options of `cycle.md`.

Done when the suite has no new failure, the lock shows 0 changes, and the held-back tests pass. Each surviving mutation also needs an answer or a question to the PO.
