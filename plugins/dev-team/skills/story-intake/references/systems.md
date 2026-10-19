# Read tools per system

Tool names can have a prefix in the session (for example `mcp__github__issue_read`). Look at the tool list first. These are the read tools only. The write tools are in the system references of `product-flow:backlog-push`.

## Contents

- Jira
- Azure DevOps
- GitHub
- GitLab
- Linear
- No tool

## Jira

Atlassian Rovo MCP server. Read with `getJiraIssue` (key `PROJ-123`). The acceptance criteria are often in a custom field: read the fields of the item, and look for a field named "Acceptance criteria". Search with `searchJiraIssuesUsingJql` only to find the parent or the linked items.

## Azure DevOps

Azure DevOps MCP server of Microsoft. Read with `wit_get_work_item` (local server) or `wit_work_item` with the action `get` (remote server). The field `Microsoft.VSTS.Common.AcceptanceCriteria` holds the criteria as HTML: convert it to text, keep the list items. Read the parent with the link of type `System.LinkTypes.Hierarchy-Reverse`.

## GitHub

GitHub MCP server. Read with `issue_read` (method `get`, owner, repo, number). The criteria are in the body, often as a task list (`- [ ]`). Read the comments with `issue_read` (method `get_comments`) only when the body points to them. Read the parent with `issue_read` (method `get_sub_issues` on the parent) when the issue is a sub-issue.

## GitLab

GitLab MCP server (`/api/v4/mcp`). Read with `get_work_item`. The criteria are in the description, often as a task list.

## Linear

Linear MCP server (`https://mcp.linear.app/mcp`, or `/mcp/readonly`). The tool names are not in the public docs: read them from the tool list (a tool to get one issue by its identifier, such as `ENG-42`). The criteria are in the description.

## No tool

When the session has no tool for the system, ask the human to paste the story and its criteria. Write "pasted by the human" as the source. Do not fetch the web page of the tracker: it needs a sign-in, and the content can differ from the API.
