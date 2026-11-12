# Held-back tests (medium and large)

A separate agent writes extra tests from `spec.md` and the DSL, with new values. The builder never reads them. They show a build that fits the locked tests but not the rule.

## Steps

1. **Start the writer.** Start a new sub-agent. Give it `spec.md`, the DSL files, the item key, and these steps. Do not give it the code of the change or the locked test files. The writer:
   1. For each rule, writes one or more tests that call only the DSL. It uses values that the scenarios do not use. Examples: the other side of each limit, a second example of each result, and an empty or a long input.
   2. Saves each test file with `heldback.py add`. The text goes on stdin:

      ```
      python3 ${CLAUDE_SKILL_DIR}/scripts/heldback.py add --item <key> --name <file> <<'TEST'
      <the test file>
      TEST
      ```

   3. Gives back only the file names and the number of tests. Not the text.
2. **Check.** Run `heldback.py list --item <key>`. Done when it shows one file or more.

The tests stay in the data folder of the plugin, outside the repository. The hook of the plugin blocks each direct read of that folder. Do not try to read them. `references/prove.md` runs them.
