# Team profile

## Contents

- What it is
- Which template wins
- Format
- Checker settings

## What it is

A team profile is one Markdown file with the standard of the team for backlog items. Examples: an MVP framework, a definition of ready, or a company template. Look for it in the claude.ai Project, in the repository (for example `docs/team-profile.md`), or in the attachments.

## Which template wins

Ask one time, in the first round of questions: "Which template applies: <what you found>, or the default of this plugin?" Write the answer in the "Status" section.

1. **A team profile.** It wins over the rules of the skills where they conflict. Say in the report which rules you did not apply.
2. **A template in the ticket system.** Use its fields and headings. The rules of the skills apply where the template says nothing.
3. **The default of this plugin.** The templates and rules of the skills.

Do not mix two templates in one backlog.

## Format

A profile has these sections. All are optional.

```markdown
# Team profile: <name>

## Templates
<For each item type: the headings, in order, and what goes in each.>

## Titles
<The format of the title for each item type. Example: "[Module] - <title>".>

## Criteria
<Rule list or scenarios. Does an epic have acceptance criteria?>

## Extra parts
<For example: priority (MoSCoW), dependencies, technical approach, test notes, links to designs.>

## Checklist for a new module
<The points to check in the grill, for example: create, change, and delete in the user interface and the integration, search, roles, log.>

## Checker
criteria: rules | scenarios
epic-criteria: no | yes
title-pattern: <regular expression for all titles>
title-pattern-feature: <regular expression for the titles of one type>
```

## Checker settings

Give the profile to the checker: `backlogcheck.py <file> --profile <profile.md>`. The checker reads only the lines under "## Checker":

| Setting | Effect |
|---------|--------|
| `criteria: scenarios` | The checker accepts Given/When/Then (rule A5 is off). |
| `epic-criteria: yes` | An epic can have acceptance criteria (rule E7 is off). |
| `title-pattern: <regex>` | Each title must match. Else: warning TP. |
| `title-pattern-<type>: <regex>` | The same, for one item type. It wins over `title-pattern`. |
