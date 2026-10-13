---
name: skill-eval
description: Eval set and test run for the skills of a Claude Code plugin. Use when the user wants a skill tested against a baseline, two versions of a skill compared, eval cases written or moved to the plugin eval format, or a results table. Writes in plain language, ASD-STE100 for English and B1 for Dutch.
allowed-tools: Bash(python3 ${CLAUDE_SKILL_DIR}/scripts/evalrun.py *), Bash(python3 ${CLAUDE_SKILL_DIR}/scripts/simhuman.py *)
---

# Test a skill

This skill tests the skills of a plugin. It runs each case with the skill and without it, 3 times each, and shows one table with a row for each assertion. It uses `claude plugin eval` for one-turn cases. Its own runner adds Python checks on the output files, a simulated human, and a compare of two versions. `plugin eval` does not do these three things.

- Use it for: step 3 of a new skill, a change to a skill, or a new model.
- Not for: unit tests of code, or a check of the manifest (use `claude plugin validate`).

## Steps

1. **Read the brief.** Read `brief.md` of the skill, or ask the human for the success criteria. Each criterion becomes one yes or no assertion. Done when you have 4 to 8 assertions for each case.
2. **Write the cases.** Read `references/cases.md`. Write 3 cases in `plugins/<plugin>/evals/<case>/`. Make one short request, one with detail, and one with a trap (a missing fact or a bad input). Use a simulated human for each skill that asks questions. Done when each case has its files and the assertions are in `graders/` or `checks.json`.
3. **Check the cases.** Run each check command on a known good output and on a known bad output, by hand. A check that passes both is weak: fix it. Done when each check tells good from bad.
4. **Ask before the run.** Tell the human the number of runs and the cost limit. Each run uses the account of the human. Done when the human agrees, or the task says that no human can answer.
5. **Run.** Run `python3 ${CLAUDE_SKILL_DIR}/scripts/evalrun.py plugins/<plugin> --runs 3 --max-cost-usd <limit>`. Add `--allow-tools Write Edit` when the skill writes files, and `--scaffold` when a case has a scaffold script. To compare versions, add `--variant v1=<old copy> --variant v2=plugins/<plugin>`. Done when it prints the table and the results folder.
6. **Read the runs.** Open the transcripts of at least one failed run and one passed run for each case. A failure must be fair. When the grader or the check is wrong, fix it and run again. Do not trust a score that you did not read. Done when each failure has a cause.
7. **Report.** Give the table, and one line for each change between versions with its reason. Name the limits of the test, for example "the human was simulated" or "one model only". Write it to `evals/results-<date>.md` in the plugin folder, with the rules in `references/language.md`. Done when the human has the table.

## Rules for the cases

- The baseline gets the same request, the same files, and the same tools. Only the plugin is different.
- The grader does not see which variant made the output. `evalrun.py` gives each variant a random letter. Keep variant names out of case files, folder names, and file contents.
- Use a script for each assertion that a script can decide. Use an `llm` grader only for the rest, with a concrete pass condition.
- A case with a simulated human ends at the first gate that needs a real decision, or with "done". The answer sheet gives facts only when the agent asks for them.

## When no human is there

Use a cost limit of 5 USD for each variant. Say in the report that the human did not agree on the cost.
