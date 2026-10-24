# Cost analysis: where the cost of a run goes (2026-10-24)

Input: the stream logs of the flow, team, and baseline runs of 2026-10-23 (`results.md`). Script: one pass over each `stream.jsonl`, with the usage of each API call, split into the main agent and the sub-agents.

## Findings from the runs

| Arm and case | Cost | Main calls | Sub-agent calls | Sub-agent share | Command output |
|--------------|------|------------|-----------------|-----------------|----------------|
| Baseline, bug | $0.36 | 7 | 0 | 0% | 16k chars |
| Flow, bug (round 2) | $0.70 | 13 | 2 | 18% | 40k chars |
| Flow, get-order (round 3, no ADRs) | $1.03 | 16 | 3 | 28% | 63k chars |
| Flow, get-order (round 4, with ADRs) | $1.31 | 20 | 3 | 23% | 82k chars |
| Flow, cancel-shop-day | $2.58 | 27 | 15 | 40% | 143k chars |
| Flow, ship-history | $6.19 | 30 | 70 | 72% | 321k chars |
| Team, bug | $3.21 | 16 | 47 | 78% | 186k chars |

1. **The skill text is not the cost.** Each call starts with about 51,000 tokens of context, also in the baseline. A test call without skills gives 43,000 tokens: that is the system prompt and the tools of Claude Code. The listing of about 140 account skills adds about 7,500 tokens. The plugins of this repository add about 500 tokens. MCP tools add nothing, because Claude Code loads them on demand.
2. **Cost is the number of calls times the context size.** Almost all input tokens are cache reads of the same context. Each extra call costs the full context again. The baseline makes 7 calls. The flow makes 15 to 23 calls on a small item. The team makes 63.
3. **The process steps are the extra calls.** A small flow run has these extra calls. It uses 4 to 6 calls to read references and templates. It uses 5 calls for the team decision skill, the ADR skill, and the profile. Also 2 checker calls, 3 edits of `plan.md`, and a review sub-agent. The ADR rule of round 4 added 4 calls and 27% cost on get-order, for no change in the code.
4. **Each sub-agent runs on Opus.** The review sub-agent is `general-purpose`, and it inherits the model of the session. A new sub-agent also pays a new cache write of about 30,000 to 45,000 tokens. In the team, the sub-agents are 78% of the cost.
5. **Build and test output fills the context.** The command output grows from 16k characters (baseline) to 321k characters (ship-history). Each later call reads it again.
6. **The large case.** The tier rule says "more than one feature" for large. ship-history has three behaviours in one story. The agent reads that as one feature in 2 runs, and as three features in 1 run. The rule is not deterministic. The large run cost $9.50, and the two medium runs cost about $4.50. All three pass each probe.
7. **The baseline is not clean.** The baseline arm loads the account plugins and skills of this machine. Examples: `dev-flow` 1.0.0 from claude.ai, `dev-docs`, and `anthropic-skills:csharp`. The 3 baseline runs of the bug did not call a skill. But the arm is Claude Code with the account skills, not plain Claude Code.

## What the sources say

- Anthropic, Building effective agents: "find the simplest solution possible, and only increase complexity when needed", and add complexity "only when it demonstrably improves outcomes". Agentic systems "trade latency and cost for better task performance".
- Anthropic, Harness design for long-running apps: each component of a harness encodes an assumption about what the model cannot do alone. These assumptions "can quickly go stale as models improve". The evaluator is worth its cost only "when the task sits beyond what the current model does reliably solo".
- Anthropic, Multi-agent research system: agents use about 4 times the tokens of chat, multi-agent systems about 15 times. Token usage explains 80% of the variance in quality there. Coding is a poor fit for many agents, because the agents share much context.
- Anthropic, Effective context engineering: use "the smallest possible set of high-signal tokens". A sub-agent gives back a short summary (1,000 to 2,000 tokens).
- Anthropic, Agent skills: progressive disclosure, and a script instead of token generation for deterministic work.
- ETH Zurich, Evaluating AGENTS.md (2026): context files raise the cost by about 20%. They add 2 to 4 steps. Written by an LLM, they lower the success rate a little. Advice: keep only the minimal requirements, such as the tools of the repository.
- Jain, Are harnesses worth the cost? (2026-10-13): with a frontier model, the harness used 10.5 times the tokens for the same 10 of 10 result. Advice for frontier models: run the tests once as the final gate.
- Artificial Analysis, read by Beri (2026): at the same score, Sonnet 5.5 costs more per task than Opus 5.5, because it uses more tokens. Sonnet is cheaper only at lower effort and a lower score. A switch to Sonnet is a hypothesis to test, not a sure saving.

## Conclusions

- On these stories, Opus with Claude Code gets the code right without process. Baseline, flow, and team pass the same hidden tests. The process gives value in two places only. It stops when a fact of the human is missing (trap). It leaves a record (decisions, ADRs).
- The measured value of this repository is in the stack skills. In dev-stack batch 2, the baseline failed 29 trap checks. The skill variant failed 1. The checks test facts and tool rules that the model does not have.
- The eval stories are too easy. Both arms reach the ceiling, so they cannot show a better result. The same is true for the batch 2 cases of dev-stack.

## Experiments to run

| No | Change | Expected effect | Measure |
|----|--------|-----------------|---------|
| X1 | Lean patch and small path: no `plan.md`, no checker, no ADR. The report holds the plan and the result | Cost near the baseline plus 30% | Bug, get-order, cancel-order, 3 runs each |
| X2 | Review sub-agent with `model: sonnet` (or haiku), the diff as its only input, no review for a patch | Sub-agent share down by half | The same runs as X1 |
| X3 | One quiet checks command that prints a summary of 10 lines | Less command output in the context | All cases |
| X4 | Deterministic large rule: a count of acceptance criteria, layers, and schema changes | The same tier in 3 of 3 runs | ship-history |
| X5 | Model matrix: baseline and flow on Sonnet 5.5, and flow on Opus with Sonnet sub-agents | Shows if a cheaper model needs the skills more | Trap and hard cases |
| X6 | Hard cases as the yardstick, not the easy stories | A baseline that fails, so a skill can show a gain | The hard cases of dev-stack |
| X7 | A clean baseline: no account plugins, no account skills | A fair comparison | All baseline runs |
