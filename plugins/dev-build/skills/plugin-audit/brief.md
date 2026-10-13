# Brief: plugin-audit

Status: draft

## Need
A plugin goes out of date when practice, Claude Code, or the model changes. The dev-docs 1.3 and 1.4 releases came from a manual audit. Each plugin needs the same audit.

## Workflow
1. Run the deterministic checks: `skillcheck.py`, `doccheck.py`, `claude plugin validate --strict`, `claude plugin details` (token cost).
2. Read the latest eval results of the plugin. Find each assertion below 3 of 3.
3. Research current practice on the subject of each skill, from more than one source.
4. Write one gap table per skill: gap, change, priority (High, Medium, Low, Question for you), source.
5. Add "What not to add" and a proposed release split.
6. Stop. The human selects the items.

## Output
`docs/audits/<plugin>-<date>.md` in the repo, and a copy in the claude.ai project when the session has one.

## Triggers
- Must start: "audit dev-docs", "what can be better in this plugin", "check the plugin against best practice".
- Must not start: "review this skill" (build-skill review mode), "review this PR".

## Success criteria
- Each finding has a source: a checker line, an eval result, or a link.
- Each finding has a priority from the fixed set.
- The checker results are in the report, with counts.
- The audit changes no file except the audit report.
- There is a "What not to add" section.

## Limits
- Read-only, because the human selects the items.
- No priority High without a source, because a guess must not drive a release.

## Language and runtime
Output in English (ASD-STE100). Dutch (B1) when the human asks. Claude Code and claude.ai sessions with a shell.

## Decisions
| No | Decision | Who |
|----|----------|-----|
| Q1 | A skill in dev-build | human |
| Q11 | Inputs and output form as above | human |
| Q12 | In the repo and in the project | human |
| Q13 | Stop at the list | human |
| Q14 | English STE, Dutch B1 on request | human |
