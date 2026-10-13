# Brief: skill-eval

Status: draft

## Need
Step 3 of the four-step workflow. Each plugin has its own eval format and its own `score.py` today. The graders could see the variant name. Each new skill builds the harness again.

## Workflow
1. Read the brief of the skill. Make the assertions from its success criteria.
2. Write the cases in the `claude plugin eval` format: `prompt.md` and `graders/*.md`.
3. Run `claude plugin eval` with the baseline arm and 3 runs.
4. For the gaps, use a small own runner. It runs the Python checkers, a simulated human from an answer sheet, and a compare of two versions.
5. Hide the variant from the grader: random labels A and B, no variant name in a path. Grade a pair in both orders only for a version compare.
6. Merge all scores in one results table per assertion.

## Output
A results table (assertion against baseline, v1, v2) with one line per change between versions, and the eval files in the new format.

## Triggers
- Must start: "test this skill", "run the evals", "compare v1 and v2 of the skill".
- Must not start: "write unit tests for this code", "validate the plugin manifest".

## Success criteria
- Each case has a baseline arm and 3 runs.
- No grader input contains the variant name.
- The Python checkers score each output file.
- The table shows each assertion, not only a total.
- A multi-turn case uses the answer sheet and stops at the gates.

## Limits
- No fake human reply outside the answer sheet, because it makes the result worthless.
- A cost limit on each run (`--max-cost-usd`), because each run uses the account of the human.

## Language and runtime
Output in English (ASD-STE100). Dutch (B1) when the human asks. Claude Code and claude.ai sessions with a shell.

## Open facts for the test step
- Does `claude plugin eval` keep the output files, so the Python checkers can score them? If not, report to the human.

## Decisions
| No | Decision | Who |
|----|----------|-----|
| Q1 | A skill in dev-build | human |
| Q8 | `claude plugin eval` as base, own runner only for the gaps | human |
| Q9 | Move the eval sets of all plugins to the new format, when the new sets are of high quality | human |
| Q10 | Hide the variant. Grade both orders only for a version compare | human |
| Q14 | English STE, Dutch B1 on request | human |
