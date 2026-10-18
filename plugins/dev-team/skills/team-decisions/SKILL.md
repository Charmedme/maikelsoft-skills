---
name: team-decisions
description: The flow of team decisions in a dev-team run - the pre-filled decision sheet at G1, the decision index docs/dev-team/decisions.md, one ADR per decision, the user profile of earlier answers, the approval of a new package, a reopened decision, and decisioncheck.py. Use when a dev-team agent meets a decision that the story needs, writes the index or an ADR, or when the user asks to record or check team decisions.
---

# Team decisions

A team decision is a choice that the code follows from then on: minimal APIs or controllers, the error contract, the test framework. The human makes each decision one time. The agents follow it. Write all text in the language rules of `references/language.md`.

## Where the decisions are

| What | Where |
|------|-------|
| The questions, options, and recommendations | `references/decisions.md` of each dev-stack skill (IDs such as `API-01`) |
| Detect rules (code that uses a decision) | `rules.json` of each dev-stack skill, rules of the kind `detect` |
| The answers of this repository | `docs/dev-team/decisions.md` (the index) and one ADR per decision |
| The earlier answers of this human | `~/.claude/dev-team/profile.md` |
| The open decisions of one story | `decision-sheet.md` in the run folder |

## Flow

1. **Design (senior).** List each decision that the story needs. Skip a decision that the index has as `accepted` or `assumed`. Existing code that already follows one option counts as that option: write it as `assumed`, with the file as evidence.
2. **Pre-fill.** For each open decision, use the profile answer as the recommendation. Without a profile answer, use the recommendation of the stack skill. Write the sheet with the template in `references/formats.md`. Only the decisions that this story needs.
3. **New package.** Each package that the design adds is a row in the sheet: name, version, license, reason, and "exists: yes". Check that it exists with `dotnet package search <name> --exact-match` or `npm view <name> version` before you write the row. A package that does not exist is a design error, not a row. A paid license needs the same approval. MediatR and AutoMapper are not allowed: use direct handlers, manual mapping, Mapperly, or an open source mediator.
4. **Gate G1 (main session).** The human approves or changes each row. The main session writes the answers into the sheet (column Answer, column By).
5. **Record (senior, in `build-core`).** For each answered row, write one ADR, add the row to the index, and update the profile. Use `dev-docs:doc-adr` for the ADR when you have it, else the template in `references/formats.md`.
6. **Follow.** Each agent reads the index before it designs, plans, or builds. Code follows the accepted and assumed rows.
7. **Check (G2).** `decisioncheck.py --changed <base> --rules <dev-stack>/skills/*/rules.json`. Each finding is an open finding of G2.

## Reopen a decision

A later need can reopen a decision (for example, a story that needs controllers for OData). The senior writes it in the sheet with the reason, the old answer, and the new recommendation. After the approval: write a new ADR that supersedes the old ADR, set the old row to `superseded`, and add a new `accepted` row. Never edit an accepted ADR, except its status and the link to the new ADR.

## Rules

- An agent never decides a team decision itself. No answer and no human: stop with `NEEDS_HUMAN` (senior) or `NEEDS_CONTEXT` (medior).
- `assumed` is only for a choice that existing code already makes. It needs no ADR. Write the file that shows it in the column Check.
- One ID has at most one `accepted` or `assumed` row.
- The profile keeps the answers, not the reasons of one project. A new answer replaces the old one.

## References

- `references/formats.md`: read before you write a file. The formats of the sheet, the index, the ADR, and the profile.
- `references/language.md`: the language rules.
- `scripts/decisioncheck.py`: checks the index, the ADRs, and the code that uses a decision. `python3 scripts/decisioncheck.py --root . [--changed main] [--rules ...]`.
