# Azure DevOps

## Contents

- Tools
- Levels and types
- Fields and format
- Parent link
- Duplicates
- Goals
- Templates
- Limits

## Tools

The Azure DevOps MCP server of Microsoft (remote or local) gives grouped tools with an `action` parameter. Names can have a prefix in the session.

| Task | Tool and action |
|------|-----------------|
| Backlog levels of a team | `wit_backlog` / `list` |
| One work item type | `wit_work_item` / `get_type` |
| Create | `wit_work_item_write` / `create` |
| Create children under a parent | `wit_work_item_write` / `add_child` |
| Link two existing items | `wit_work_item_link_write` / `link` |
| Text search | `search_workitem` |
| Query (WIQL) | `wit_query` (local server), `wit_query_by_wiql` (remote, Insiders only) |

There is no tool for a batch create, and no tool for templates.

## Levels and types

The process of the project sets the names:

| Process | Epic level | Feature level | Story level |
|---------|-----------|---------------|-------------|
| Agile | Epic | Feature | User Story |
| Scrum | Epic | Feature | Product Backlog Item |
| CMMI | Epic | Feature | Requirement |
| Basic | Epic | (none) | Issue |

- With 2 levels: draft Feature becomes Feature, and the story becomes the story level of the process.
- Basic has no Feature level. The recommendation: draft Feature becomes Epic, because that level is empty in a 2-level backlog.
- An inherited process can rename levels and add levels above Epic. Read the backlog levels with `wit_backlog` / `list`.
- Spike and Enabler: the story level, with the tag `spike` or `enabler`. Set Value Area to "Architectural" for an enabler when the field exists.

## Fields and format

- Title: max 255 characters. It is the only field that is always required.
- `System.Description` and `Microsoft.VSTS.Common.AcceptanceCriteria` take HTML. Convert the Markdown: `<h3>` for headings, `<ul><li>` for lists, `<p>` for text. Put the acceptance criteria in their own field, not in the description.
- Tags: the field `System.Tags`. Put a semicolon and a space between two tags.
- Area Path and Iteration Path: use the defaults of the team, unless the human gives them.
- Do not fill Story Points, Effort, Size, Priority, or dates.

## Parent link

Use `add_child` to create the children under the parent. Or create the child and add a relation of type `System.LinkTypes.Hierarchy-Reverse` to the parent. An item has one parent only.

## Duplicates

- `search_workitem` with 2 or 3 words of the title, in the project.
- WIQL: `SELECT [System.Id], [System.Title] FROM WorkItems WHERE [System.TeamProject] = @project AND [System.Title] CONTAINS WORDS '<words>'`.
- Mark a confirmed duplicate with the link type `System.LinkTypes.Duplicate-Forward`, or "Related" for an overlap.

## Goals

- Product goal: Azure DevOps has no goal type. The goal stays in the "Goal" section of the feature.
- Sprint goal: iterations have no goal field. The team keeps it in the team wiki. Write it there only when the human asks and a wiki tool is available.

## Templates

Work item templates belong to a team and to one type. The MCP server cannot read them. Ask the human for the template link, or use the fields of the type.

## Limits

- 1,000 links for each item. A remote server needs Microsoft Entra ID sign-in.
