# dev-flow

dev-flow is a Claude Code plugin for developers who want to plan a change before they build it. Its skill `plan-and-build` takes an idea to working code. You make the decisions. The skill finds the facts, writes the plan, builds a prototype, and writes the code.

English output follows ASD-STE100 Simplified Technical English. Dutch output follows Begrijpelijk Nederlands (taalniveau B1).

## Install

You need Claude Code and Python 3.9 or later.

```sh
claude plugin marketplace add Charmedme/maikelsoft-skills
claude plugin install dev-flow@maikelsoft-skills
```

Do you also use the mattpocock skills `grill-me`, `to-spec`, `to-tickets`, `to-questionnaire`, `prototype`, or `implement`? You can keep them. `plan-and-build` does not call them, and it does not need them.

## Quick start

1. Open Claude Code in a repository.
2. Ask for a change in your own words:

   ```
   Plan and build: I want due dates on my to-do tasks, and a way to see what is overdue.
   ```

3. The skill does the research and asks its first questions. Each question has a recommended answer. Answer "ok" to accept all, or "Q3: no" to change one:

   ```
   **Q4. When a task is overdue.** A task is overdue when it is not done and its due date is before today.
   Recommended: yes, this rule.
   ```

4. Answer the questions at each gate. After the last ticket, the skill gives a report with the test result.

## Usage

### Phases and gates

| Phase | The skill makes | Gate (you decide) |
|-------|-----------------|-------------------|
| 1. Research | `research.md` | |
| 2. Grill | `decisions.md`, and a questionnaire when needed | **G1** Is this the shared understanding? |
| 3. Plan | `plan.md`, one or more specs, ticket drafts | |
| 4. Prototype | `prototype/` | **G2** Is this what you want? **G3** Are the tickets correct? |
| 5. Build | code and tests, one commit for each ticket | |
| 6. Review | a review of the code against the specs | **G4** Is the work done? |

At G2 you answer "yes", "yes with changes", or "no". After a "no", the skill goes back to the questions for the part that is wrong. Then it updates the specs and the tickets, and builds the prototype again.

After G3, the skill builds all tickets without a stop. It stops only when a ticket needs a decision that is not in the spec, a test cannot pass, or a step cannot be undone.

### The work folder

Each item is a separate Markdown file:

```
docs/work/<slug>/
  plan.md                 the goal, the ticket table, the gates
  research.md
  decisions.md
  specs/<name>.md         one or more
  tickets/NN-<slug>.md    one file for each ticket
  questionnaires/<slug>.md
  prototype/
```

The skill uses the place for plans that your project guide (`CLAUDE.md`, `AGENTS.md`, `CONTRIBUTING.md`) gives. Else, it uses `docs/work/<slug>/`. In a chat without a repository, it gives each file to you as a download.

To continue later, ask the skill to continue the run. It reads `plan.md` and starts at the first open phase.

### Size

The skill sets the size at the start, and tells you. All phases and gates stay. Only the depth changes.

| Size | Signal | Questions | Tickets |
|------|--------|-----------|---------|
| small | One behaviour, one or two files | One round, max 6 | 1 to 3 |
| medium | One feature, more than one layer | Rounds until no question is open | 3 to 10 |
| large | More than one feature | The same, one branch at a time | One set for each spec |

### A question for a third person

When you say that another person knows an answer, the skill writes a questionnaire for that person. From then on, it does not ask you about that subject. When the answers come back, give them to the skill.

### The checker

`scripts/workcheck.py` checks the work folder. It uses only the Python standard library.

```sh
python3 workcheck.py docs/work/due-dates           # check the folder
python3 workcheck.py docs/work/due-dates --next    # which tickets can start now?
python3 workcheck.py docs/work/due-dates --format json
```

The exit code is 1 when there is one or more errors. The checker finds these errors:

- A ticket without a number, a spec link, "Blocked by", a status, or acceptance criteria.
- A blocker that does not exist, or a cycle of blockers.
- A ticket table in `plan.md` that does not agree with the ticket files.
- A gate out of order. For example, a ticket in the build before G3, or a G2 that is "assumed".

The skill also has `scripts/doccheck.py`, the shared language checker of this repository. It checks each document that the skill writes.

### No human to answer

In a scheduled or unattended run, the skill takes its recommended answers and marks them "assumed". It stops at G2, because only a human can validate the prototype.

## Limits

- The skill uses sub-agents for the research, for each ticket, and for the review when Claude Code has them. The tests ran without sub-agents. In that case, one agent writes and reviews the code, and the report says so.
- The checker checks the structure of the work folder. It does not check if a spec is good.
- A UI prototype needs a project with an interface. Without one, the skill makes one HTML file.

## Documentation

- [Changelog](CHANGELOG.md)
- [Credits](CREDITS.md): the six skills of Matt Pocock that this skill joins.
- [Test results](../../evals/dev-flow/results-2026-10-09.md): two rounds against the six source skills.

## License

MIT. Refer to the [LICENSE](../../LICENSE) file. The credits file has the license of the source skills.
