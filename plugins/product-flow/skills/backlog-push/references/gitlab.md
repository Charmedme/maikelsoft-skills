# GitLab

## Contents

- Tools
- Levels and types
- Fields and format
- Parent link
- Duplicates
- Goals
- Limits

## Tools

The GitLab MCP server (endpoint `/api/v4/mcp`) gives these tools. Names can have a prefix in the session.

| Task | Tool |
|------|------|
| Create or update a work item | `save_work_item` (also `create_work_item`) |
| Read | `get_work_item` |
| Search and list | `list_work_items` with `search` |
| Link two items | `link_work_items` |

## Levels and types

- Work item types: Epic, Issue, Task, Objective, Key Result, Incident, Test case.
- Epics need Premium or Ultimate. Child epics need Ultimate. Read the plan of the group from the tools or ask.
- With 2 levels: draft Feature becomes Epic (Premium), and the story becomes Issue.
- With 3 levels on Ultimate: draft Epic becomes Epic, Feature becomes a child Epic, the story becomes Issue.
- Free tier: no epics. Recommendation: draft Feature becomes a label `feature::<slug>` (scoped label) on the issues, or a milestone when the human prefers.
- A Task is the child of an issue and is work of the developers. This skill does not write tasks.

## Fields and format

- The description is GitLab Flavored Markdown. The draft Markdown stays as it is. The acceptance criteria stay a task list.
- Description templates are in `.gitlab/issue_templates/` (project) or in the group (Premium, for epics). Use their headings.
- Weight is Premium. Do not fill it.

## Parent link

Set `parent_id` in `save_work_item` when you create the child. An issue has one parent epic.

## Duplicates

- `list_work_items` with `search: "<words>"` in the project or group.
- `link_work_items` with `relates_to` for an overlap.

## Goals

- Product goal: an Objective (Ultimate) when the feature flag `okrs_mvc` is on. GitLab says that OKRs are not ready for production, so use them only when the team already does. Else the goal stays in the feature (the epic).
- Sprint goal: the description of the iteration cadence. Write it only when the human asks.

## Limits

- Epics nest 7 levels deep. A parent has at most 5,000 direct children.
