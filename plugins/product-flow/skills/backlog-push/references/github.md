# GitHub

## Contents

- Tools
- Levels and types
- Fields and format
- Parent link
- Duplicates
- Goals
- Limits

## Tools

The GitHub MCP server gives these tools. Names can have a prefix in the session.

| Task | Tool |
|------|------|
| Issue types of the organization | `list_issue_types` |
| Issue fields of the organization | `list_issue_fields` |
| Create | `issue_write` with `method: create` |
| Link a sub-issue | `sub_issue_write` with `method: add` |
| Search | `search_issues` |
| Projects (fields, iterations) | `projects_list`, `projects_write` |

## Levels and types

- Issue types are set for the organization. The defaults are task, bug, and feature. There is no default "Epic" type. A personal repository has no issue types.
- Levels come from sub-issues: a parent issue holds its sub-issues.
- With 2 levels: draft Feature becomes an issue of type "feature" (when it exists). Each story is a sub-issue of it, of type "task" or a "story" type when the organization has one.
- With 3 levels: draft Epic becomes an issue of type "Epic" when the organization has it. Else an issue with the label `epic`.
- No issue types: use labels (`feature`, `story`, `epic`).

## Fields and format

- Title and body. The body is GitHub Flavored Markdown, so the draft Markdown stays as it is. The acceptance criteria stay a task list (`- [ ]`).
- Issue forms in `.github/ISSUE_TEMPLATE/` are the templates of the repository. Use the headings of the form in the body. The API does not apply the form.
- Do not fill Priority, Effort, or dates of the issue fields.

## Parent link

- `issue_write` can set the parent at create time with `parent_issue_number`. You cannot use it together with `issue_fields`.
- Else, create the child, then `sub_issue_write` with `method: add`. It takes the sub-issue **ID**, not its number. Read the ID from the result of the create.

## Duplicates

- `search_issues` with `repo:<owner>/<repo> is:issue <words> in:title`.
- Close a confirmed duplicate only when the human asks. This skill never closes items.

## Goals

- Product goal: GitHub has no goal object. Use an issue type "Goal" when the organization has one, as the parent of the features. Else it stays in the "Supports:" line of each feature.
- Feature goal: a text issue field of the organization for the goal, when `list_issue_fields` shows one. Then set the parent after the create, with `sub_issue_write`. Else the first section "Goal" of the body.
- Sprint goal: a Projects iteration has no text field. It stays in the feature or the project README.

## Limits

- Up to 100 sub-issues for each parent, 8 levels deep. 25 issue types for each organization.
