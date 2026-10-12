# Files instead of a ticket system

## Contents

- When
- Ask once
- Write
- Duplicates

## When

Use files when no tool for a ticket system is in the session, or when the human asks for files. Do not ask the human to connect a system. Tell them in one line which connector would let you push directly next time (Jira, Azure DevOps, GitHub, GitLab, or Linear).

## Ask once

Ask one question, with a recommended answer:

"How do you want the items: one file for each item, or one combined file? And in which format: Markdown, HTML, or TXT?"

Recommended: one file for each item, in Markdown. Most ticket systems accept Markdown when you paste it. One file for each item is easy to copy into one ticket.

## Write

Run:

```
python3 <this skill folder>/scripts/backlogtree.py export <draft.md> --format md|html|txt --split|--combined --out <folder>
```

- `--split` makes one file for each item. The number in the file name gives the order: parents first.
- `--combined` makes one file with all items, parents first.
- In claude.ai, give each file to the human as a download, or publish the combined file as a document artifact when the session can.
- In a repository, put the files in the work folder (for example `docs/work/<slug>/backlog/`).

## Duplicates

Without a ticket system, you cannot search for duplicates. Say so in the report. When the human gives an export of the existing backlog (CSV or Markdown), put its titles in a JSON file and run `backlogtree.py similar`.
