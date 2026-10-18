# Brief: dev-team and dev-stack

Status: approved (G1, 2026-10-16). Concept: `dev-team-concept-2026-10-16.md`.

## Need
Claude writes fair code, but the result changes per run. It has no role and no guidelines. It trusts old trained knowledge, does not reflect on a plan, and skips conventions, `dotnet format`, and analyzers. The team and the skills must make the result repeatable, current, and clean.

## Workflow (decided)
1. Orchestrator (main session, team lead) gets the story through `story-intake`, gives the context, writes `run.md`, and starts the senior. It writes no code and no design. It delegates work that costs many tokens.
2. Senior reads `decisions.md`, `conventions.md`, and the profile. It learns the system and checks the current facts. It writes the design, the open decisions, and the new packages. It splits the work: the critical core (senior) and the work packages (mediors).
3. A second senior reflects on the design one time. The senior applies the improvements.
4. Gate G1: the orchestrator shows the design and one pre-filled decision sheet. The human approves the design, the decisions, and each new package.
5. Senior writes the ADRs and the index rows, updates the profile, and builds the critical core. The reflector senior reviews the core code one time.
6. Senior starts a medior per package. The medior writes a plan, reflects on it, and sends it to the senior one time. The senior gives feedback one time.
7. Medior builds, runs the checks, and asks the senior for one code review. On `CHANGES_REQUIRED`: one fix pass, checks again, no second review.
8. The orchestrator starts `security-reviewer` one time on the full diff. The senior assigns one fix pass for blocking findings.
9. Gate G2: the orchestrator runs `runcheck.py` and `decisioncheck.py`, and shows the result, the open findings, and the commits.

Each agent adds its own rows to `log.md`. When an agent is lost, the orchestrator restarts from the last good step in the log.

## Decisions
| No | Decision | Who |
|----|----------|-----|
| Q1 | Agents + orchestrator skill + practice skills + stack skills. | human |
| Q2 | A skill finds stories in the common ticket systems. Without a system, it asks the human for the story. | human |
| Q4 | Main session is the team lead. Senior starts mediors (nested agents). | human |
| Q5 | Senior builds the critical core, after one reflection by another senior. | human |
| Q6 | One fix pass after `CHANGES_REQUIRED`, no second review, open items to G2. | recommended, accepted |
| Q7 | Senior: Opus 5.5, effort high. Medior: Sonnet 5.5, effort medium or high by size. | human |
| Q8 | Two plugins: `dev-team` and `dev-stack`. | recommended, accepted |
| Q9 | 24 stack skills as in the concept table, for v1. | recommended, accepted |
| Q10 | Scoped names `dev-stack:<name>`. Agents use `dev-stack:` skills first. | human |
| Q11 | Skills read the project version first. .NET 10 and .NET 11 can need different rules. | human |
| Q12 | FluentAssertions 7.2.2 and AwesomeAssertions both supported, plus a migration skill. | human |
| Q13 | Take all existing skills into dev-stack, improved. | human |
| Q14 | Third agent `security-reviewer`. `secure-coding` is known to all agents. The human gets what to do and what to check. | human |
| Q15 | The reflector senior also reviews the core code one time. | human |
| Q16 | The orchestrator starts `security-reviewer` one time per story, on the full diff. The senior assigns one fix pass. The report says what to do and what to check. | human |
| Q17 | Counters in `run.md`, `runcheck.py` at G2. A hook only when tests show breaks. | human |
| Q18 | Two agent files: `medior` (effort medium) and `medior-large` (effort high). | human |
| Q19 | Current facts rule (repo versions, latest for new projects, look-ups with date and source, skill "verified on" date of 6 months). Look-ups use MCP tools first, then web pages. | human |
| Q20 | Fixed self-check before the plan and the design go out. | human |
| Q21 | `docs/dev-team/conventions.md` in the repo. Each agent, also the medior, always writes a track record of what it did. | human |
| Q22 | `dev-team:story-intake`, read only, reuses the product-flow system references. | human |
| Q23 | One skill per technology, a reference per major version. | human |
| Q24 | Merge and add existing skills as proposed: 28 stack skills. | human |
| Q25 | Preload: practice skill, `stack-index`, `secure-coding`. | human |
| Q26 | Agents can create a branch and commit on it. No push, no PR. | human |
| Q27 | Use the dev-flow and dev-docs skills when they are installed, also for the files that the agents write. | human |
| Q28a | MediatR and AutoMapper are not allowed. Use direct handlers, manual mapping, Mapperly, or an open source mediator. | human |
| Q28 | For new code, ask the human for each default that the work needs, with a recommendation. Log each decision (ADR). A later need can reopen a decision. Research the defaults further. | human |
| Q29 | The orchestrator passes the answers. The senior writes the ADR (with `dev-docs:doc-adr` when installed) and the index row. | human |
| Q30 | Each agent adds its own rows to `log.md` (step, status, files, commit, checks, decisions, assumptions, reason). A hook writes `commands.jsonl`. `runcheck.py` compares both. The log lets the orchestrator restart from the last good step and check the reasoning of a lost agent. | human |
| Q31 | A user-level profile (`~/.claude/dev-team/profile.md`) keeps earlier answers. Each new answer updates it. In a new project, the profile answer is the recommendation. | human |
| Q32 | One pre-filled decision sheet per story at G1, only the decisions the story needs. | human |
| Q33 | Decisions in `references/decisions.md` of each stack skill, detect rules in `rules.json`. `dev-team:team-decisions` holds the flow and `decisioncheck.py`. | human |
| Q34 | The catalog defaults are the recommendations. Before an agent adds a package, it tells the human the name, version, license, and reason, and it checks that the package exists. The human approves or denies. A paid license needs the same approval. | human |

## Open
None.
