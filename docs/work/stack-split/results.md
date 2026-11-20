# Results: the stack split, phase 1

Phase 1 of `brief.md`: dev-stack 2.0.0, frontend-stack 1.0.0, test-stack 1.0.0, and dev-team 1.1.0. The moved skills (`angular`, `dotnet-testing`, `migrate-fluentassertions`) have no change to their rules, so their earlier results stay valid. Phase 1 has a trigger check only. The behaviour workflow of the brief starts in phase 2.

## Trigger check

Set: `evals/frontend-stack/triggers.json`. Each query ran one time on Sonnet, with frontend-stack and dev-stack loaded, so that `web-security` and `dev-stack:secure-coding` compete.

| Skill | Version of the description | Starts (of 5) | Quiet (of 5) |
|-------|----------------------------|---------------|--------------|
| `web-security` | first | 5 | 5 |
| `modern-web` | first: "Sends HTML, CSS, and client-side JavaScript work to ... Use when ..." | 0 | 5 |
| `modern-web` | second: "Run first for each task that writes or changes HTML, CSS, or client-side JavaScript ..." | 4 | 5 |

- In the first version of `modern-web`, no run called a skill. The description said what the skill does, not when it must run.
- The second version names the task kinds first, as the description of Modern Web Guidance does. The one miss is "Add a confirm dialog before the user deletes a file on this page. Plain HTML and JavaScript."
- The skip queries are back-end, SQL, CI, and test requests. No skip query started a skill of frontend-stack.

## Limits

- One run for each query, on one model.
- The check did not have Modern Web Guidance installed. It does not show what happens when both skills are in the session.

# Results: the stack split, phase 2

Phase 2 of `brief.md`: dev-stack 2.1.0 (`dotnet-11`, `orleans`, `aspire`, `blazor`, `dotnet-mcp`, `api-design`), frontend-stack 1.1.0 (`webmcp`), and dev-team 1.1.1. The concept is `concept-phase2.md`. The research is in `research-*.md`, and the compile check on SDK 11.0.100 is in `compile-check.md`.

## Trigger check, phase 2

Set: `evals/stack-split/triggers.json`. Each query ran one time on Sonnet, with dev-stack, frontend-stack, and test-stack loaded.

| Skill | Starts (of 5) | Quiet (of 5) |
|-------|---------------|--------------|
| `dotnet-11` | 4 | 5 |
| `orleans` | 5 | 5 |
| `aspire`, first description | 3 | 5 |
| `aspire`, second description ("Run first for each task that adds, changes, or tests an Aspire AppHost ...") | 5 | 5 |
| `blazor` | 4 | 5 |
| `dotnet-mcp` | 5 | 5 |
| `api-design` | 4 | 5 |
| `webmcp` | 5 | 5 |

In each miss, the agent called no skill. No skip query started a skill.

## Behaviour test

The workflow ran 8 scenarios (`evals/stack-split/scenarios.json`) with 6 agents (2 Haiku 5.5, 2 Sonnet 5.5, 2 Opus 5.5) and 3 arms: 144 runs. Each run wrote files in its own folder, without a build. Script checks (`evals/stack-split/score.py`) look for the current pattern and for the old pattern. A blind Sonnet judge graded each result from 0 to 10, with reference facts for each scenario. The folder name of a run hides its arm. Each run gave its model ID, and each ID was the expected one.

Arms: "baseline" has no skill. "current" reads the nearest skill of dev-stack 1.3.0 (`csharp`, `aspnetcore`, `aspnetcore-openapi`, or `angular`). "new" reads the new skill.

### For each model

Each cell gives the mean check score, the mean judge grade, and the number of old-pattern hits (16 runs).

| Model | baseline | current | new |
|-------|----------|---------|-----|
| Haiku | 0.58, 4.6, 19 | 0.56, 4.3, 18 | 1.00, 8.4, 0 |
| Sonnet | 0.78, 6.2, 10 | 0.78, 6.5, 11 | 1.00, 8.7, 0 |
| Opus | 0.78, 6.5, 11 | 0.78, 6.8, 11 | 1.00, 8.9, 0 |

### For each scenario

Each cell gives the mean check score, the mean judge grade, and the number of old-pattern hits (6 runs).

| Scenario | Skill | baseline | current | new |
|----------|-------|----------|---------|-----|
| S1 Upgrade to .NET 11 | `dotnet-11` | 0.90, 6.5, 2 | 0.79, 6.3, 4 | 1.00, 9.0, 0 |
| S2 Grain with a flush timer and a test | `orleans` | 0.82, 6.7, 6 | 0.82, 7.3, 6 | 1.00, 8.3, 0 |
| S3 AppHost | `aspire` | 0.42, 3.0, 11 | 0.40, 3.0, 11 | 1.00, 8.8, 0 |
| S4 Page that loads one time | `blazor` | 0.83, 7.5, 2 | 0.83, 7.8, 2 | 1.00, 8.8, 0 |
| S5 Static form with nested validation | `blazor` | 0.83, 6.2, 2 | 0.83, 6.7, 2 | 1.00, 8.2, 0 |
| S6 Stdio MCP server | `dotnet-mcp` | 0.72, 4.8, 8 | 0.76, 4.7, 6 | 1.00, 8.0, 0 |
| S7 Orders API contract | `api-design` | 0.92, 8.7, 0 | 0.92, 8.3, 0 | 1.00, 9.0, 0 |
| S8 WebMCP tools | `webmcp` | 0.27, 2.8, 9 | 0.27, 2.8, 9 | 1.00, 9.0, 0 |

The success rule of the brief is met: on each model, the new arm passes more checks than the baseline and writes no old pattern. The text of dev-stack 1.3.0 gives no gain over the baseline.

### Fixes during the test

- `dotnet-11`: one Haiku run stopped at the version gate NET-01 and changed no file, although the task asked for the move. The skill now says that a request of the human to move is the answer to NET-01. The 6 runs of S1 ran again with the fix: each did the move (checks 1.00, grade 9).
- `dotnet-mcp`: 3 runs used the example name `Example.Orders.McpServer`. A namespace with that name and a parameter of type `McpServer` gives CS0118. The skill now uses `Example.Orders.Mcp` and has a "Never write" row. The 6 runs of S6 ran again: no CS0118, and the mean grade went from 6.3 to 8.0.
- `dotnet-mcp`, second run: one Haiku run left out `ImplicitUsings` (grade 4). The project example now sets it.
- `dotnet-mcp`, third run (2026-11-20, only the new arm): all 6 project files set `ImplicitUsings`. The check score is 1.00 in each run, and the mean grade is 8.5 (9, 7, 9, 9, 8, 9). We built each result with the .NET 11 SDK: 5 of 6 built. The Opus result that failed has `--yes` in an XML comment of the project file (MSB4025). Without that comment it built. The grade 7 is a Haiku run that did not register an `IOrderStore`.
- The S6 judge key of the second run names the MRTR API that the compile check proved. The first judge could not confirm it.
- Six check patterns were too strict, and the scorer read comment lines and skipped `.txt` files. The fixes apply to the 3 arms the same way. All numbers above use the fixed checks.

## Limits

- One trigger run for each query, on one model.
- No run built or ran code. The judge grades from the text, and the compile check covers only the key traps.
- The judge is one Sonnet agent with reference facts that this research wrote.
- One run broke the tool rule (it used Bash `cat`), and said so.
- The second `aspire` description is not in the behaviour test. The behaviour test reads the skill by path, so the description has no effect there.
- Tokens: about 20.3 million for all agents of the test and the reruns.
