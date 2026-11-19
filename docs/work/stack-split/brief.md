# Brief: the stack split

Status: approved (G1, 2026-11-18)

## Need

The owner builds .NET services with Orleans, Aspire, and Blazor, and Angular front ends. An agent without help uses the patterns of its training data. These patterns are often old. Newer techniques that the model does not know give better results. For web code, the Modern Web Guidance plugin of Google Chrome gives the best results.

## Plugins

- `dev-stack` 2.0.0: the .NET back end. New: `dotnet-11`, `orleans`, `aspire`, `blazor`, `dotnet-mcp` (MCP servers with the C# SDK), `api-design` (REST, independent of the framework).
- `frontend-stack` 1.0.0: `angular` (moved from dev-stack), a shim that sends web work to Modern Web Guidance, `webmcp`, and `web-security` (the front-end part of `secure-coding`).
- `test-stack` 1.0.0:
  - `dotnet-testing` (moved). It gets a part for MSTest and a part for xUnit, and it routes between them.
  - `migrate-fluentassertions` (moved).
  - `playwright`: one skill, with TypeScript, .NET, and Playwright MCP in references.
  - `reqnroll`, with the Gherkin rules as a part of the skill.

## Test

- Trigger check: each skill, 5 start and 5 skip queries.
- Behaviour test: one workflow for each phase. 8 scenarios over the skills of the phase, 6 agents (2 Haiku, 2 Sonnet, 2 Opus), 3 arms: 144 runs.
- A scenario is a small coding task in a temporary folder, without a build or Docker. Script checks look for the modern API and the absence of the old pattern. One blind judge grades each result.
- The agent reads the skill text from a path. Baseline: no skill. Current: the text of dev-stack 1.3.0 from its tag. New: the new text.
- Success: the new arm passes more checks than the baseline on each model, and gets no old pattern.

## Limits

- No name of a project of the owner in the repository, because the repository is public.
- No copy of Modern Web Guidance content, because the shim only routes to it.
- No push, merge, or release without the approval of the owner.

## Language and runtime

ASD-STE100 English. Claude Code.

## Decisions

| No | Decision | Who |
|----|----------|-----|
| Q1 | Skills in 3 plugins | human |
| Q2 | Orleans and MSTest are in use in a live service. Blazor is in use in a live portal. WebMCP is for future projects. Reqnroll goes on top of the Playwright tests | human |
| Q3 | Bad case: old techniques, and new techniques that the model does not use | human |
| Q4 | A moved skill keeps a forwarding stub under its old name for one major version | human |
| Q5 | Split `secure-coding` into a back-end part and a front-end part | human |
| Q6 | dev-team depends on dev-stack only. `stack-index` routes to the 3 plugins and gives the install command | human |
| Q7 | When the owner installed Modern Web Guidance, the shim hands over to it. Else it gives the install commands and the `npx` search. It holds no guidance of its own | human |
| Q8 | A `dotnet-11` skill. The other skills get a .NET 11 column | human |
| Q9 | `api-design` skill | human |
| Q10 | `dotnet-mcp` builds MCP servers in .NET | human |
| Q11 | One `playwright` skill with guides and references | human |
| Q12 | Gherkin is a part of the `reqnroll` skill | human |
| Q13 | MSTest and xUnit are parts of `dotnet-testing`, with routing | human |
| Q14 | 3 phases: the split, the new dev-stack skills, the new test-stack skills. One PR and one release for each | human |
| Q15 | Behaviour test as a workflow: 6 agents (2 Haiku, 2 Sonnet, 2 Opus) x 8 scenarios x 3 arms (baseline, current stack, new stack) | human |
| Q16 | `dev-stack:secure-coding` keeps the back end, the supply chain, containers, Kubernetes, and CI. `frontend-stack:web-security` gets XSS, token storage, CSP, client CSRF, and AngularJS | recommended, accepted |
| Q17 | `blazor` covers each render mode with a decision table | recommended, accepted |
| Q18 | `webmcp` has the label preview. It uses a feature check and progressive enhancement | recommended, accepted |
| Q19 | 8 scenarios for each phase, over the skills of the phase | recommended, accepted |
| Q20 | A scenario is a small task without a build. Script checks and one blind judge | recommended, accepted |
| Q21 | Arms: no skill, the 1.3.0 text, the new text, read from a path | recommended, accepted |
| Q22 | The eval agents have a fixed model. Fallback: `claude -p --model` | recommended, accepted |
| Q23 | 4 agents at the same time, a cap of $150 for each phase | recommended, accepted |
| Q24 | Phase 1 moves `angular` and `dotnet-testing` without a change, and adds only the routing | recommended, accepted |
