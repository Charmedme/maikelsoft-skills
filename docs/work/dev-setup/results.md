# Results: dev-setup 1.0.0

Brief: `brief.md` (G1 approved on 2026-11-05). Eval set: `evals/dev-setup/`. Model of the runs: Sonnet. 3 runs for each variant.

## Round 1 (not used)

Date: 2026-11-05. Cost: $8.93. The runs used the real home folder of the owner. Two baseline runs of `po-global` wrote the role into `~/.claude/CLAUDE.md` and `~/.claude/dev-team/profile.md`. The third baseline run read that role. Claude removed the two changes the same day.

`run.py` now gives each run its own home folder, with only the login file of Claude Code.

The skill runs of round 1 showed two faults, fixed before round 2:

- One run ran `rolestore.py` from another folder, so the role went to the wrong repository. Now `set --scope project` needs `--cwd`, and the script prints the project key.
- Two `starter` runs wrote to the existing `CLAUDE.md` after the "yes". Now the starter never writes to an existing `CLAUDE.md`.

## Round 2

Date: 2026-11-05. Cost: $7.61. Script checks by `score.py`, judge assertions by one blind grader for each eval (the runs in a random order, without the variant).

| Eval | Assertion | Baseline | Skill |
|------|-----------|----------|-------|
| solo-setup | Role for the project, no global role | - | 3/3 |
| solo-setup | `delivery.md`, `REVIEW.md`, PR template, CI check, `.dev-flow/speclock.py` | 0/3 | 3/3 |
| solo-setup | No existing file changed | 1/3 | 3/3 |
| solo-setup | Questions in one message, each with a recommended answer | 0/3 | 3/3 |
| solo-setup | No question about a fact that the repository shows | 1/3 | 1/3 |
| solo-setup | File list, and writes only after the "yes" | 0/3 | 3/3 |
| solo-setup | Report with the role store and `Next:` | 0/3 | 3/3 |
| po-global | Global role, no project role | - | 3/3 |
| po-global | No existing file changed | 3/3 | 3/3 |
| po-global | No new file | 3/3 | 2/3 |
| po-global | No question about the route or the project facts | 3/3 | 0/3 |
| po-global | Report says "all projects" and gives `Next:` | 0/3 | 3/3 |
| starter | No existing file changed, except `.gitignore` | 0/3 | 3/3 |
| starter | `.gitignore` keeps its lines and gets the 2 new lines | 3/3 | 3/3 |
| starter | Valid `.claude/settings.json` | 1/3 | 3/3 |
| starter | The folders `skills`, `agents`, `hooks`, `rules` | 0/3 | 3/3 |
| starter | No `.claude/commands/` | 2/3 | 3/3 |
| starter | `.claude/README.md` names each part | 0/3 | 3/3 |
| starter | `CLAUDE.md` unchanged, additions only proposed | 0/3 | 3/3 |
| starter | Each README section short, with purpose and setup | 0/3 | 3/3 |
| starter | File list, and writes only after the "yes" | 0/3 | 3/3 |

Trigger queries: the 5 must-trigger queries pass 15 of 15 tries. The 5 must-not queries pass 15 of 15 tries. The laya router ran the 3 tries on Sonnet, not on Haiku.

Fixes after round 2:

- `po-global`: the skill answered a request for the role only with the full setup, and asked project facts. Now a request about only the role or the scope does steps 1, 3, and 8, and offers the project files in one line.
- `solo-setup`: two runs asked for the Azure DevOps project, which the remote shows. Now `questions.md` says that the path of the remote gives the organization and the project.

## Round 3

Date: 2026-11-05. Cost: $2.63. The 6 skill runs of `po-global` and `solo-setup` again, after the two fixes. One grader. The baseline of round 2 stays the compare point.

| Eval | Assertion | Baseline (round 2) | Skill round 2 | Skill round 3 |
|------|-----------|--------------------|---------------|---------------|
| solo-setup | Each script check | 0/3 to 1/3 | 3/3 | 3/3 |
| solo-setup | Questions in one message, each with a recommended answer | 0/3 | 3/3 | 3/3 |
| solo-setup | No question about a fact that the repository shows | 1/3 | 1/3 | 2/3 |
| solo-setup | File list, and writes only after the "yes" | 0/3 | 3/3 | 3/3 |
| solo-setup | Report with the role store and `Next:` | 0/3 | 3/3 | 3/3 |
| po-global | Each script check | 3/3 | 2/3 to 3/3 | 3/3 |
| po-global | No question about the route or the project facts | 3/3 | 0/3 | 3/3 |
| po-global | Report says "all projects" and gives `Next:` | 0/3 | 3/3 | 3/3 |

The one open fail: a run gave the Azure DevOps project of the remote as the recommended answer of the tracker question. That is a question to confirm a fact that the repository shows.

Total cost of the 3 rounds: $19.17.
