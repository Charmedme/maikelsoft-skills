# Edit log: dev-setup

| No | Date | Change | Test | Verdict |
|----|------|--------|------|---------|
| E1 | 2026-11-05 | New plugin `dev-setup` 1.0.0: the skill `setup` (modes `/setup`, `route`, `starter`), `rolestore.py`, and the session-start hook. | Round 1, not used (`results.md`) | Two faults found |
| E2 | 2026-11-05 | `set --scope project` needs `--cwd`. The starter never writes to an existing `CLAUDE.md`. `run.py` gives each run its own home folder. | Round 2 | Kept. Two new faults found |
| E3 | 2026-11-05 | A request about only the role does steps 1, 3, and 8. The remote gives the Azure DevOps organization and project. | Round 3 | Kept. The owner approved the results |
