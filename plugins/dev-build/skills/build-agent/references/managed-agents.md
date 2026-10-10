# Claude Managed Agent

A Managed Agent runs on the Claude platform in a sandbox. You create the agent once, then start sessions with it. The feature is in beta, so check the docs before you build.

## Contents

- What you define
- Agent file
- Environment and session
- Open points

## What you define

- **Agent**: model, system prompt, tools, MCP servers, skills. Reference it by ID.
- **Environment**: where sessions run. An Anthropic cloud sandbox or your own sandbox.
- **Session**: one running agent in one environment. It keeps its history on the server.

All endpoints need the beta header `managed-agents-2026-04-01`. The SDK sets it.

## Agent file

The `ant` command line tool takes a Markdown file. The frontmatter holds the settings. The body is the system prompt.

```markdown
---
name: Dependency Auditor
model: claude-opus-5-5
tools:
  - type: agent_toolset_20260401
---

You audit the dependencies of one repository. Return a table and a status line.
```

Run `ant apply dependency-auditor.md`. The tool prints the agent ID and records it in `claude-lock.json`. The tool type `agent_toolset_20260401` turns on the pre-built tools: bash, file operations, web search and fetch.

## Environment and session

```yaml
name: audit-env
config:
  type: cloud
  networking:
    type: limited
    allow_package_managers: true
```

Run `ant apply environment.yaml`. Start a session with `agent`, `environment_id`, and `title`. Send work as a `user.message` event. Read the stream until `session.status_idle`.

## Open points

The overview page does not show the fields for `mcp_servers` and `skills`. It does not show the raw HTTP request. Read `platform.claude.com/docs/en/managed-agents/agent-setup` before you add them. Restrict the network of the environment to what the job needs. Managed Agents is not covered by Zero Data Retention.

Source: `platform.claude.com/docs/en/managed-agents/overview` and `.../quickstart`.
