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

## Phase 3: test-stack 1.1.0

Phase 3 adds `playwright` and `reqnroll` to test-stack, and an xUnit v3 part to `dotnet-testing`. The plan is in `concept-phase3.md`. The facts come from `research-playwright.md`, `research-reqnroll-xunit-mstest.md`, and `compile-check-phase3.md`.

### Trigger check, phase 3

Set: `evals/stack-split/triggers-p3.json`. Each query ran one time on Sonnet, with test-stack, dev-stack, frontend-stack, and product-flow loaded. The runner is `evals/ux-design/trigger.py`.

| Skill | Starts (of 5) | Quiet (of 5) |
|-------|---------------|--------------|
| `playwright` | 4 | 5 |
| `reqnroll` | 4 | 5 |
| `dotnet-testing` | 4 | 5 |

In each miss, the agent called no skill and answered directly. The misses are a flaky test with `waitForTimeout`, a trace for each Reqnroll scenario, and a `decimal` `DataRow` that fails. No skip query started a skill.

### Behaviour test, phase 3

The same workflow as phase 2: 8 scenarios (`evals/stack-split/scenarios-p3.json`), 6 agents (2 Haiku 5.5, 2 Sonnet 5.5, 2 Opus 5.5), and 3 arms: 144 runs. Each run gave its model ID, and each ID was the expected one.

Arms: "baseline" has no skill. "current" reads the nearest skill of dev-stack 1.3.0 (`dotnet-testing`, `angular`, or `secure-coding`). "new" reads the new skill.

Each cell gives the mean check score, the mean judge grade, and the number of old-pattern hits (16 runs).

| Model | baseline | current | new |
|-------|----------|---------|-----|
| Haiku | 0.71, 4.6, 16 | 0.73, 4.8, 16 | 0.99, 7.9, 0 |
| Sonnet | 0.77, 5.6, 15 | 0.80, 6.1, 13 | 1.00, 8.9, 0 |
| Opus | 0.85, 6.4, 8 | 0.87, 6.4, 7 | 1.00, 9.1, 0 |

Each cell gives the mean check score, the mean judge grade, and the number of old-pattern hits (6 runs).

| Scenario | Skill | baseline | current | new |
|----------|-------|----------|---------|-----|
| S1 TypeScript suite with a sign-in setup | `playwright` | 1.00, 8.7, 0 | 1.00, 8.8, 0 | 1.00, 9.0, 0 |
| S2 Playwright for .NET with MSTest 4 | `playwright` | 0.71, 4.5, 5 | 0.68, 4.3, 5 | 1.00, 9.0, 0 |
| S3 Playwright MCP for the team | `playwright` | 0.77, 4.8, 1 | 0.75, 4.7, 1 | 0.98, 7.5, 0 |
| S4 Reqnroll feature with Playwright steps | `reqnroll` | 0.80, 5.0, 7 | 0.86, 5.0, 4 | 1.00, 8.3, 0 |
| S5 Imperative feature file to declarative | `reqnroll` | 0.96, 8.3, 2 | 0.94, 7.5, 3 | 1.00, 8.0, 0 |
| S6 New xUnit v3 project with a database container | `dotnet-testing` | 0.64, 2.8, 12 | 0.69, 3.5, 11 | 1.00, 9.0, 0 |
| S7 Move from xUnit v2 to v3 | `dotnet-testing` | 0.47, 2.8, 12 | 0.47, 3.2, 12 | 1.00, 9.0, 0 |
| S8 MSTest 4 data rows with `decimal` | `dotnet-testing` | 0.85, 7.3, 0 | 1.00, 9.0, 0 | 1.00, 9.3, 0 |

The success rule of the brief is met: on each model, the new arm passes more checks than the baseline and writes no old pattern.

- The largest gains are in xUnit v3 (S6, S7) and in Playwright for .NET (S2). Without the skill, each S6 and S7 run used `xunit.v3` 3.x and `CollectionBehavior(DisableTestParallelization = true)`, which does not compile on 4.0. No S2 run without the skill used `Microsoft.Playwright.MSTest.v4`.
- S1 (TypeScript) and S5 (Gherkin) show no clear gain. The models know these well without a skill. In S5 the judge grade of the new arm (8.0) is lower than the baseline (8.3), with the same check score.
- The text of dev-stack 1.3.0 gives a gain only in S8. Its `dotnet-testing` already had the MSTest 4 rules.

### Fixes during the test, phase 3

- Five check patterns were wrong. The fixes apply to the 3 arms the same way, and all numbers above use them:
  - The package ID is `Reqnroll.MsTest`, and the S4 check was case-sensitive.
  - The S2 checks for the deprecated tool and the `<Playwright>` section of `.runsettings` matched README lines that warn against them.
  - The S3 check for "not a security boundary" did not accept "Do not treat the settings as a security boundary".
  - The S3 check for `@latest` matched `deny` rules in `.claude/settings.json`.
- The S4 judge key did not have two facts of the compile check. These facts are that `public partial class Program` is not necessary on .NET 10, and that `ClientOptions.BaseAddress` is the real address after `StartServer()`. The first judge called each new-arm S4 result a build error (CS0122). The 18 S4 runs had a second judge with the full key. The new arm went from 5.3 to 8.3. The baseline and current arms stayed at 5.0.
- `playwright`: rule 16 now also names the permission rules of `.claude/settings.json`, and rule 17 asks for the "not a security boundary" sentence in the setup docs. The 6 new-arm S3 runs ran again with this text. Each run wrote the sentence and pinned each version: check score 0.98, grade 7.8 (first run 7.5).

### Build check, phase 3

After the test, the main session built each new-arm result of S2, S4, S7, and S8 with SDK 10.0.404, one build at a time. S6 cannot build, because its task names a data project that the seed does not have.

- 23 of 24 results built.
- The one failure (S7, Haiku) has a stray `</content>` line at the end of the project file (MSB4025). The tool call of the run wrote it. The skill text has no such line.
- The build check did not run the tests. S2 and S4 need a browser, and the runs wrote no test data.

### Limits, phase 3

- One trigger run for each query, on one model.
- The judge is one Sonnet agent with reference facts that this research wrote. In S4, a missing fact changed the grades, and a second judge graded the 18 runs again.
- The S3 rerun used the changed `playwright` text. The other scenarios used the text of the first run. Rules 16 and 17 have no effect on them.
- Tokens: about 21 million for all agents of phase 3: research, compile check, build, test, and reruns.
