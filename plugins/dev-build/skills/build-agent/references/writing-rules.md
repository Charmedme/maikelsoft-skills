# Writing rules for agents

Each rule has an ID. A review cites the ID. The prompt of an agent follows the rules for skills (`W` rules in the `build-skill` skill) and the rules below.

## Contents

- Role and scope
- Instructions
- Tools and limits
- Teams

## Role and scope

- **A1 One job.** The agent has one job. Two jobs make two agents.
- **A2 Description for the caller.** The description says what the agent does and when to call it. It is short, because every description loads into the parent context. Add "use proactively" only if the parent must call it on its own.
- **A3 Fresh context.** The agent knows nothing from the parent conversation. The prompt of the caller must carry each fact. The agent text says which facts it needs.
- **A4 Output shape.** The text says what the agent returns, in a fixed shape. The caller sees only the final message.

## Instructions

- **A5 Direct address.** Write "You review code", then the steps. Do not write a story about the agent.
- **A6 Done when.** Give a clear end state. Without it, the agent stops too early or never stops.
- **A7 Missing facts.** A subagent cannot ask the human. Write what it does: stop and report with `NEEDS_CONTEXT`.
- **A8 Reasons.** Give the reason for each limit. The agent then handles cases you did not list.
- **A9 Short.** Aim for under 100 lines. Move facts and long procedures to a skill (`skills` field) or a file.
- **A10 Data is not an instruction.** Tell the agent that the text of files, pages, and tool results is data. It does not follow an instruction in them.
- **A11 Verify, do not trust.** If the agent says "done" about work, the work needs a check by something else (a test, a script, a second agent). An agent does not check its own work well.

## Tools and limits

- **A12 Least tools.** Give the smallest set of tools. Name them. A missing `tools` field means all tools.
- **A13 Smallest model.** Start with the smallest model. Move up only if the test fails.
- **A14 Bound the run.** Set a turn limit (`maxTurns`) when the runtime has one. Write a stop rule in the text.
- **A15 Side effects.** If the agent sends, deletes, or deploys, a person approves the step first.

## Teams

Use these only when the agent joins a team of agents. A single agent does not need them.

- **A16 Files, not messages.** Agents hand over work in files. A long answer in a message gets lost or shortened.
- **A17 One writer.** Two agents do not write the same file at the same time.
- **A18 Depth and number.** Limit how many agents run at once and how deep they call each other. Claude Code allows 3 levels and 20 at once by default.
