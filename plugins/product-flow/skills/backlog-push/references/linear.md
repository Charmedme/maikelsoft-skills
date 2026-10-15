# Linear

## Contents

- Tools
- Levels and types
- Fields and format
- Parent link
- Duplicates
- Goals

## Tools

The Linear MCP server (`https://mcp.linear.app/mcp`) can find, create, and update issues, projects, and comments. The tool names are not in the public docs. Read them from the tool list of the session. `/mcp/readonly` is a read-only variant: it cannot push.

## Levels and types

- Linear levels: Initiative, Project (with milestones), Issue, sub-issue. A cycle is a time box, not a level.
- With 2 levels: draft Feature becomes a Project, and the story becomes an Issue in that project.
- With 3 levels: draft Epic becomes a Project, Feature becomes a project milestone, and the story becomes an Issue with that milestone. Or, when the human prefers: Epic becomes an Initiative, and Feature becomes a Project.
- Linear has no story type. Use a label `story`, `spike`, or `enabler` when the team uses labels.
- Each issue belongs to a team. Ask for the team when there is more than one.

## Fields and format

- Title and description. The description is Markdown. The acceptance criteria stay a checklist.
- Issue templates of the team can set fields. Use their headings when the team has a template.
- Do not fill the estimate, the priority, or the cycle.

## Parent link

- A story under a feature: set the project of the issue.
- A sub-issue: set the parent issue. Sub-issues get the team, the priority, and the project of the parent.

## Duplicates

- Search issues by words of the title, in the team.
- Triage can mark an issue as a duplicate. This skill never marks or closes items: it proposes the action to the human.

## Goals

- Product goal: an Initiative, with the projects of the features linked to it.
- Feature goal: the first section "Goal" of the description of the project.
- Sprint goal: the description of the cycle. Write it only when the human asks.
