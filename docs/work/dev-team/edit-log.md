# Edit log: dev-team

This log records each change to a skill, an agent, or a script of the plugin, the test that measured it, and a verdict.

## Edits

### E1: the field report of 2026-10-28
- Date and version: 2026-10-28, dev-team 0.5.0
- Change: the 10 plugin points of the field report.
  - The entry point and the work after G2 changed. A new skill handles review comments.
  - The senior looks for sibling patterns and aggregates. The project declares its CI checks.
  - A mock-up counts as a design source. The hook has a fallback command.
  - A missing command log gives one finding. `decisioncheck` reads added lines and finds duplicate IDs.
  - The build commands are in one place. A run moves with the branch.
- Scope: the points of one project are not in the plugin. The sort is in `field-report-2026-10-28.md`.
- Test: `tests/test_devteam.py` (26 new tests) for `decisioncheck.py`, `runcheck.py`, `checks.py`, and the hooks. The language checker on each changed Markdown file. No run of the team on a story.
- Observed: the scripts and hooks pass their tests. The skill text (`deliver-story`, `senior-practice`, `story-intake`, `process-review`) has no eval run yet. The fallback command was tested on Linux only. The Windows case is not tested.
- Verdict: proposed
- Evidence: `docs/work/dev-team/field-report-2026-10-28.md`
