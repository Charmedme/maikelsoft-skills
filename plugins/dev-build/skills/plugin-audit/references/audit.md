# Audit sources and report form

Read this file before the research (step 4) and before you write the gap tables (step 5).

## Contents

- Sources
- Checks for each skill
- Priority
- Report form

## Sources

Use at least two sources for each skill:

1. **A standard or an official guide** for the subject of the skill. Examples: Keep a Changelog for changelogs, WCAG for accessibility, the Claude Code docs for plugins.
2. **A practice source**. For example a style guide of a large organization, a known template project, or a similar tool.
3. **The own evidence**: checker output, eval results, and the limits in the results report.

Prefer the primary source. Give the link in the table. When you could not open a source, say so. Do not cite from memory.

## Checks for each skill

Apply these checks to each skill. Each "no" is a gap. They come from the skill authoring guidance of Anthropic and from the four-step workflow.

- The description says what the skill does and when to use it, in the third person.
- `SKILL.md` has steps with a "done when" line, and it is short. Detail is in `references/`.
- References are one level deep. Each link says when to read the file.
- No text depends on a date or a version that will change.
- A script handles its own errors. Each constant has a comment.
- The skill has an eval with a baseline, 3 runs for each variant, and at least 3 cases.
- The eval ran on the current model.
- The skill tells the output language rules: ASD-STE100 for English, B1 for Dutch.
- The always-on token cost is in proportion to the use. A skill with a long description costs tokens in each session.

## Priority

| Priority | Use when |
|----------|----------|
| High | The skill gives wrong or incomplete output today, and a source shows it. A failed assertion, a checker error, or a clear gap with the standard. |
| Medium | A clear improvement with a small change. |
| Low | Useful, but only when there is time. |
| Question for you | The human must decide, or the finding has no source yet. |

In each priority, put the item with the most effect for the least work first.

## Report form

```markdown
# <plugin> audit (release <version>)

This audit compares the <n> skills of <plugin> <version> with current practice, the checkers, and the eval results.

## Facts

<The output of auditfacts.py, short: checker counts, validate, token cost, eval status for each skill.>

## For all skills

| # | Gap | Change | Priority | Source |
|---|-----|--------|----------|--------|
| A1 | <What the plugin does not do yet.> | <The change.> | High | <link or "Own test, <date>"> |

## <skill>

| # | Gap | Change | Priority | Source |
|---|-----|--------|----------|--------|

## What not to add

- <Idea>. <Why the cost is higher than the value.>

## Proposed release

- **<next version>**: all High items (<IDs>). Then a test round.
- **<version after>**: the Medium items, after you select them.
```

Give each row an ID with a letter for the skill and a number (A1, P1, R1). The human answers with the IDs.
