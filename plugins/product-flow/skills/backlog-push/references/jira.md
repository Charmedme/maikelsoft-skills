# Jira

## Contents

- Tools
- Levels and types
- Fields and format
- Parent link
- Duplicates
- Goals
- Limits

## Tools

The Atlassian Rovo MCP server (remote) gives these tools. Names can have a prefix in the session.

| Task | Tool |
|------|------|
| Types of a project | `listJiraProjectIssueTypesMetadata` |
| Fields of a type | `getJiraIssueTypeMetaWithFields` |
| Create | `createJiraIssue` |
| Search (JQL) | `searchJiraIssuesUsingJql` |
| Search (meaning, also Confluence) | `search` (beta) |

Jira now says "work item" and "space" for "issue" and "project". The API still uses "issue".

## Levels and types

- Default: Epic (level 1), Story, Task, and Bug (level 0), Subtask (level -1). Jira has no Feature level.
- Premium and Enterprise can add levels above Epic, for example "Initiative". Read the types of the project. Do not assume them.
- With 2 levels (the default): draft Feature becomes Epic, and draft User Story becomes Story.
- With 3 levels and a level above Epic: draft Epic becomes that level, Feature becomes Epic.
- With 3 levels and no level above Epic: Feature is a missing level (see `mapping.md`). The recommendation is a label `feature:<slug>` on the stories.
- Spike and Enabler: Story with the label `spike` or `enabler`, unless the project has a type for them.
- Jira Product Discovery has "Ideas". This skill does not write ideas.

## Fields and format

- Read the fields of each type with `getJiraIssueTypeMetaWithFields` before you create. Required fields differ per project.
- `summary`: max 255 characters.
- `description`: Atlassian Document Format (ADF) in REST v3. The tools of the MCP server can take Markdown. Look at the tool description.
- Jira has no standard acceptance criteria field. When the project has a custom field for it, use that. Else use a heading "Acceptance criteria" in the description, with a checklist.
- Story points is a custom field with an ID per site. Do not fill it.

## Parent link

Set the `parent` field to the key of the parent (for example `SHOP-12`) when you create the child. The old "Epic Link" field is gone from the API.

## Duplicates

- JQL: `project = <KEY> AND text ~ "<words>" ORDER BY updated DESC`. Search the summary with `summary ~ "<words>"`.
- The Rovo `search` tool also finds similar items by meaning, and finds Confluence pages on the subject.
- Mark a confirmed duplicate with the link type "Duplicate", or with "Relates" for an overlap.

## Goals

- Product goal: Atlassian Goals. A "Goals" field links a work item to a goal, when an admin turned it on for the type.
- Feature goal: a custom text field for the goal when `getJiraIssueTypeMetaWithFields` shows one for the epic type. Else the first section "Goal" of the description (ADF).
- Sprint goal: the `goal` field of the sprint (Jira Software Agile API). Write it only when the human asks.

## Limits

- The human needs the permissions "Browse projects" and "Create issues".
- Changing the levels of Jira (hierarchy) is an admin task that cannot be undone. Never propose it as part of a push.
