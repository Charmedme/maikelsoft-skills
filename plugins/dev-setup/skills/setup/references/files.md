# Files of /setup

## Contents

- The list
- .claude/rules/delivery.md
- REVIEW.md
- Pull request template
- CI check
- Block for claude.ai

## The list

| File | When | Content |
|------|------|---------|
| `.claude/rules/delivery.md` | Always | The facts of the project, one section for each plugin. |
| `REVIEW.md` | Always | `references/review-template.md`, with the answers to question 7. |
| Pull request template | Always | The sections Testing, Risk, and How to undo. |
| CI check | GitHub Actions or Azure Pipelines | `python3 .dev-flow/speclock.py check` on each pull request. |
| `.dev-flow/speclock.py` | With the CI check | The copy of `${CLAUDE_SKILL_DIR}/scripts/speclock.py`. A new `/setup` updates it. |

For each file that exists, show only the lines to add. Do not change a line that exists.

## .claude/rules/delivery.md

Claude Code loads each file in `.claude/rules/` at the start of a session, so each command sees these facts. Write only the sections with facts.

```markdown
# Delivery facts

The facts of this project for the delivery flow. /setup writes this file. Change it in a pull request.

## Tracker
- Tracker: <Azure DevOps project Shop | GitHub Issues | item files in docs/backlog/>
- Ready state: <Approved | the label spec:ready>
- Spec field: <Acceptance Criteria | Description>

## Backlog
- Profile: <Agile | Scrum | SAFe | team template at ...>
- Language: <English | Dutch, je>

## Build
- Build: <dotnet build>
- Test: <dotnet test>

## Review
- Limits: REVIEW.md
- CI check: <.github/workflows/speclock.yml | azure-pipelines-speclock.yml | none>
```

ux-design writes `## Design` in the same file.

## REVIEW.md

Copy `references/review-template.md` to `REVIEW.md` at the root of the repository. Fill in each `<...>` line from the answers and the project files. Remove each section that has no content.

## Pull request template

GitHub: `.github/pull_request_template.md`. Azure DevOps: `.azuredevops/pull_request_template.md`. Other: `docs/pull_request_template.md`.

```markdown
## What changed and why
<The item and one or two sentences.>

## Testing
<The commands that ran, and their result.>

## Risk
<What can break, and who sees it.>

## How to undo
<Revert this pull request, or the steps for data and settings.>
```

## CI check

The check fails a pull request when a locked test changed after its approval.

GitHub Actions, `.github/workflows/speclock.yml`:

```yaml
name: speclock
on: pull_request
jobs:
  speclock:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - run: python3 .dev-flow/speclock.py check
```

Azure Pipelines, `azure-pipelines-speclock.yml`:

```yaml
trigger: none
pr:
  branches:
    include: ['*']
pool:
  vmImage: ubuntu-latest
steps:
  - checkout: self
  - script: python3 .dev-flow/speclock.py check
    displayName: speclock check
```

Azure DevOps runs a new pipeline file only after a person adds it. Tell the person: "In Azure DevOps, select Pipelines, New pipeline, and select the file `azure-pipelines-speclock.yml`. Then add it as a build validation of the main branch." Azure Repos ignores the `pr` trigger. The branch policy starts the check.

Other CI: give the command `python3 .dev-flow/speclock.py check` for the pull request job.

## Block for claude.ai

The person pastes this block into the instructions of the claude.ai Project.

```text
Delivery role: <role>. Scope: project. Route: refine=on, docs=on, team=ask, approve-at-ready=off, designer-review=off.
Tracker: <...>. Ready state: <...>. Backlog profile: <...>. Language: <...>.
```
