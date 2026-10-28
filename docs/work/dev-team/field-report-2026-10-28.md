# Field report of 2026-10-28: what the plugin takes in

Source: the feedback on a run of dev-team 0.4.0 on a real .NET service (a run of 2026-10-27). This note sorts each point of the report. A point is **plugin** when any project that uses dev-team can hit it. A point is **project** when it depends on the tools, the rules, or the code of one repository.

## Rule for the sort

The plugin gives the mechanism. The project gives the content. Example: the plugin must run the checks that the pipeline of the project runs. Which checks those are (for example an analyzer step) is a fact of the project. So the plugin reads them from the project, and it names none of them.

## Taken in (plugin)

| # | Point of the report | Cause in the plugin | Change |
|---|---------------------|---------------------|--------|
| P1 | Work after G2 was done inline: grid, delete, review comments | The steps end at G2. Nothing says what happens after it. `deliver-story` excludes small changes and points to a skill that was not used. | `deliver-story`: new section "After G2". A SessionStart hook names the entry skill for each kind of work. |
| P2 | No skill for PR review comments | Missing skill | New skill `dev-team:process-review`. |
| P3 | Existing pattern of sibling modules not found. Feature folders followed the tables, not the aggregates. | The design step learns the system. It does not look at how sibling code does the same job. It has no step for aggregates. | `senior-practice`: design steps 2 and 3 get a search for sibling patterns and a step to name the aggregates. The reflection list gets one line. |
| P4 | Local checks were not equal to the pipeline | `checks.py` knows only the default tools. It does not read what the pipeline runs. | `conventions.md` gets a section "CI checks". `checks.py` runs the commands in it. Without the section, the output says that parity is unknown. |
| P5 | UX review skipped because `ux-design.md` was missing, with a mock-up in the story | Step 9 starts only on `ux-design.md`. `story-intake` does not record a mock-up. | `story-intake` records mock-up links. Step 9 starts on a mock-up too. |
| P6 | The logging hook failed without a message on Windows | `hooks.json` calls `python3`. On Windows this can be a Store alias that is not Python. | A command with a fallback: `python3`, then `python`, then `py -3`. |
| P7 | Nine RUN-EVIDENCE findings that nobody read | One finding for each log row hides the cause. `deliver-story` does not tell the lead what to do. | `runcheck.py` gives one finding for a missing or empty `commands.jsonl`. Step 10 stops on it. |
| P8 | `decisioncheck` gave 51 findings, 38 of them for existing patterns, and did not stop the duplicate ID | With `--changed` it reads whole files, not the new lines. IDs compare as exact text. ADR files are not checked for the same ID twice. | New lines only. IDs compare without case and dash differences. New rule `DEC-ADR-DUPLICATE`. The findings are sorted by rule, with the rules that matter first. |
| P9 | Each agent made its own temporary `global.json` | The way to build is not written in one place. | `senior-practice` finds a working way one time, with its undo step, and writes it with the build and test commands in `conventions.md`. Later agents use that entry. |
| P10 | The run was lost after a cherry-pick. `run.md` stayed on `G2_SHOWN`. | The run folder is not committed. `run.md` has no state after G2. | The lead commits the run folder at G1 and at G2. New status words for follow-ups. Step 1 finds a run in a folder by its `run.md`. |

## Not taken in (project)

| Point of the report | Reason |
|---------------------|--------|
| The analyzer rules of one pipeline (rule IDs and counts) | A fact of that pipeline. P4 gives the way to declare it in the repository. The plugin names no analyzer. |
| One SDK version and its path | A fact of that machine. P9 gives the place to write it. |
| A code pattern of one repository (an update by specification) | Code of one repository. P3 finds a pattern like it, in any repository. |
| The text of the `CLAUDE.md` of that repository | The repository owns it. The hook of P1 does the job for each repository. |
| A duplicate decision ID, a domain error, and an early delete | Mistakes of one run. They show the causes P1, P3, P8. |
| The comparison with another plugin | Not a requirement. The hook of P1 is the same idea. |
| The retro of the author | Notes of a person. No plugin change. |

## Check of the claims

- `hooks.json` calls `python3`: true.
- `checks.py` has no analyzer rules: true, and it stays so.
- `decisioncheck` has `DEC-DUPLICATE`, but only for two rows with the status `accepted` or `assumed` and the same exact ID. The report says that a duplicate was not found. Possible causes: an ID with another spelling, a second ADR file, or the finding was in the noise. P8 closes the three.
