# Agent SDK agent

An SDK agent is an `AgentDefinition` that you pass in the `agents` option of `query()`. The main agent calls it with the `Agent` tool.

## Contents

- Fields
- Python example
- TypeScript example
- Notes

## Fields

| Field | Required | Meaning |
|-------|----------|---------|
| `description` | Yes | When to use the agent. |
| `prompt` | Yes | The system prompt. |
| `tools` | No | Allowed tools. Missing means all tools that a subagent can use. |
| `disallowedTools` | No | Tools to remove. |
| `model` | No | Alias, full ID, or `inherit`. |
| `skills` | No | Skills to preload. |
| `memory` | No | `user`, `project`, or `local`. |
| `mcpServers` | No | Names or inline definitions. |
| `maxTurns` | No | Turn limit. |
| `background` | No | Run without blocking. |
| `effort` | No | Reasoning effort. |
| `permissionMode` | No | Permission mode inside the agent. |

The Python field names `disallowedTools` and `mcpServers` keep camelCase. `Agent` must be in `allowed_tools` of the parent, or the parent cannot call the agent.

## Python example

```python
from claude_agent_sdk import query, ClaudeAgentOptions, AgentDefinition

options = ClaudeAgentOptions(
    allowed_tools=["Read", "Grep", "Glob", "Agent"],
    agents={
        "code-reviewer": AgentDefinition(
            description="Reviews code for security issues. Use when code changed.",
            prompt="You review code. Return findings as a list. End with a status line.",
            tools=["Read", "Grep", "Glob"],
            model="sonnet",
        )
    },
)
```

## TypeScript example

```typescript
const options = {
  allowedTools: ["Read", "Grep", "Glob", "Agent"],
  agents: {
    "code-reviewer": {
      description: "Reviews code for security issues. Use when code changed.",
      prompt: "You review code. Return findings as a list. End with a status line.",
      tools: ["Read", "Grep", "Glob"],
      model: "sonnet",
    },
  },
};
```

## Notes

- The agent starts with a fresh context. It gets its own prompt and the prompt string of the `Agent` call. Put each needed fact in that string.
- Set limits for a run: `CLAUDE_CODE_MAX_SUBAGENT_SPAWN_DEPTH` (default 3), `CLAUDE_CODE_MAX_CONCURRENT_SUBAGENTS` (default 20), and `max_budget_usd` or `maxBudgetUsd`.
- An agent in `agents` wins over a file agent with the same name.

Source: `code.claude.com/docs/en/agent-sdk/subagents`.
