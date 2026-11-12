# Questions, route, and names

## Contents

- The questions of /setup
- Facts that the repository shows
- Route
- Next command of each role
- Setup steps of other plugins
- Name check

## The questions of /setup

Ask the role and the scope each time. Ask a fact only when it is missing. Give each question the recommended answer, and say why in a few words.

| No | Question | Recommended answer |
|----|----------|--------------------|
| 1 | Your role: product owner or analyst, designer, developer, tech lead, or all of them? | All of them, when one person works on the repository. Else the role that the person names. |
| 2 | For all your projects, or for this project only? | This project, when the person has a different role in other projects. Else all projects. |
| 3 | The tracker, and the state that means "ready to build"? | The tracker of the git remote (Azure DevOps or GitHub), with its usual ready state. No tracker: item files in `docs/backlog/`. |
| 4 | The backlog profile: Agile, Scrum, SAFe, or a team template? | Agile for Azure DevOps. Else Scrum. |
| 5 | The language of the backlog and the docs, and Dutch "je" or "u"? | The language of the README. |
| 6 | The design tool and the command that runs the app? | Only when the project has UI code. The run command from the project files. |
| 7 | The review limits: the problems that stop a merge, and the paths of high risk? | The defaults of `review-template.md`, with the paths of authentication, migrations, and CI. |

Only a person who knows the steps can choose them, so do not ask about the route. The route takes the defaults of the role. `/setup route` changes it later.

## Facts that the repository shows

Do not ask for these. Read them and say what you found.

| Fact | Where |
|------|-------|
| Tracker | The git remote: `dev.azure.com` or `visualstudio.com` is Azure DevOps, `github.com` is GitHub, `gitlab` is GitLab. A tracker MCP server in `.mcp.json`. |
| Organization and project | The path of the remote. `dev.azure.com/<organization>/<project>/_git/<repository>` gives both for Azure DevOps. `github.com/<owner>/<repository>` gives the owner. |
| Stack, build and test commands | The project files (`*.sln`, `*.csproj`, `package.json`, `pyproject.toml`) and the README. |
| Language | The README and the existing docs. |
| CI system | `.github/workflows/`, `azure-pipelines.yml`, `.gitlab-ci.yml`. |
| Earlier answers | `.claude/rules/delivery.md`, `REVIEW.md`, `CLAUDE.md`. |

## Route

| Step | Values | What it does | Default |
|------|--------|--------------|---------|
| `refine` | on, off | The refine pass at the end of each item command. | on |
| `docs` | on, off | `/build` updates the docs of the change before the final gate. | on |
| `team` | on, off, ask | `/build` gives large work with high risk to the dev-team agents. On: always. Off: never. Ask: after a question. | Ask. For a tech lead: on. |
| `approve-at-ready` | on, off | The approval of a ready spec also approves its low-risk scenarios. | Off. For all: on. |
| `designer-review` | on, off | The final gate of `/build` waits for the `/ux-review` of the designer. | Off. For a designer: on. |

## Next command of each role

| Role | Next |
|------|------|
| product owner | `Next: /to-backlog <your idea, feature, or bug>` |
| designer | `Next: /ux-design <item>` |
| developer | `Next: /build <item>` |
| tech lead | `Next: /build <item>` |
| all | `Next: /to-backlog <your idea>, or /build <item> when the item exists` |

When a command of this table is not installed, give its plugin: "Install product-flow for `/to-backlog`."

## Setup steps of other plugins

| Plugin | Step | Section of `delivery.md` |
|--------|------|--------------------------|
| ux-design | The skill `ux-design:ux-setup` | `## Design` |
| product-flow | The questions 3 to 5 | `## Tracker`, `## Backlog` |
| dev-flow, dev-team | The questions 3 and 7 | `## Tracker`, `## Review` |

Ask each question only one time. When a plugin is not installed, skip its section.

## Name check

1. List the skills that the person can start with `/`. Look in the plugins of `~/.claude/plugins/installed_plugins.json`, in `~/.claude/skills/`, and in `.claude/skills/` of the project. The short name is the `name` in the frontmatter of each `SKILL.md`. Skip a skill with `user-invocable: false`.
2. A clash is a short name that two of them use. A short name of a built-in command is also a clash. The built-in commands:

   `/bug`, `/goal`, `/review`, `/plan`, `/init`, `/code-review`, `/security-review`, `/pr-comments`, `/design`, `/update`, `/run`, `/help`, `/clear`, `/compact`, `/config`, `/memory`, `/model`, `/agents`, `/hooks`, `/mcp`, `/plugin`, `/resume`, `/status`.
3. For each clash, give the full name to use: `/<plugin>:<name>`. A built-in command always wins. When two plugins share a name, the plugin that loads first wins, and Claude Code gives no warning.
