# product-flow

product-flow is a Claude Code plugin for product owners (PO) and business analysts (BA). It takes a product idea to an approved backlog. You make the decisions. The skills find the facts, ask the questions, build a prototype, and write the items. The skills do not build the product.

English output follows ASD-STE100 Simplified Technical English. Dutch output follows Begrijpelijk Nederlands (taalniveau B1). When your templates use another language, the skills use the language of the templates.

## Install

You need Claude Code and Python 3.9 or later.

```sh
claude plugin marketplace add Charmedme/maikelsoft-skills
claude plugin install product-flow@maikelsoft-skills
```

## Skills

| Skill | Use it to |
|-------|-----------|
| `idea-to-backlog` | Take a request from the first idea to approved items in the ticket system. This skill calls the other skills. |
| `backlog-story` | Write, rewrite, review, or split user stories. |
| `backlog-feature` | Write or review a feature with its goal, scope, criteria, and stories. |
| `backlog-epic` | Write or review an epic, when the team uses 3 levels. |
| `backlog-goal` | Write or review a product goal or a sprint goal. |
| `product-prototype` | Build a throwaway clickable prototype with your design system or branding, to test one question. |
| `backlog-push` | Put a drafted backlog into Jira, Azure DevOps, GitHub, GitLab, or Linear, or into files. |
| `example-mapping` | Turn one approved item into rules, examples, and questions for the PO before the build (`examples.md`). |

## Quick start

1. Open Claude Code in your product repository, or in a claude.ai Project.
2. Give the request in your own words:

   ```
   Customers can find their items via the search box. Take this to backlog items.
   ```

3. Answer the questions. Each question has a recommended answer. Answer "ok" to accept all, or "Q3: no" to change one.
4. Answer at each gate. Nothing goes into your ticket system before you approve the exact list.

## Usage

### Phases and gates

| Phase | The skill makes | Gate (you decide) |
|-------|-----------------|-------------------|
| 1. Scan and research | Sources: your tools, documents, existing tickets, and the web | |
| 2. Grill | Problem (round 1: the need), decisions, open questions | **G1** Is this what you mean? |
| 3. Prototype | A prototype file | **G2** Is this what you mean? |
| 4. Spec | Goal, users, scope, story map | |
| 5. Backlog | Goal, features, stories | **G3** Is this backlog correct? |
| 6. Push | Items in the ticket system, or files | **G4** Approval of the exact list |

### Levels and profiles

- The default is 2 levels: Feature, then User Story. The goal goes in the feature.
- Ask for 3 levels (Epic, Feature, User Story) when your team uses epics.
- Profiles: Agile (default), Scrum, and SAFe. SAFe features get a benefit hypothesis.
- Bugs and tasks are not part of this flow.

### The context file

One file holds the full run: `backlog-<slug>.md`. It has the request, the sources, the decisions, the prototype verdict, the spec, and all items in full detail. The skill keeps it in the claude.ai Project of the session, else in your repository (`docs/work/<slug>/`), else as a download. To continue later, give the file to the skill.

### Templates and team profile

At the start, the skills ask which template applies. A team profile is one Markdown file with the standard of the team, for example an MVP framework. It wins over the rules of the skills where they conflict. Its "Checker" section can allow Given/When/Then, criteria on an epic, and a title format. Without a profile, the skills use the template of the ticket system or their own templates. Acceptance criteria are then a short rule list, not Given/When/Then.

Each criterion has one or more concrete examples in an "Examples" section (rule A10). A profile turns that rule off with `examples: no`.

### Open questions

The context file keeps each open question one time, with its owner and status. Items refer to it by number (O1, O2). The G3 summary gives the open questions for each owner, so the list can go to a refinement or a customer meeting.

### Example mapping

`example-mapping` works as a BA on one approved item, before the build. It lists the rules of each criterion and gives an example below, at, and above each limit. It lists the words with more than one meaning. Then it searches the existing decisions, for example the Decisions table of the context file. Only the rules without an answer go to the PO, each with a recommended answer. `dev-flow:plan-and-build` starts it for each small, medium, or large item.

### Duplicates

Before the push, `backlog-push` searches your ticket system for items with a similar title. It shows each possible duplicate and asks what to do. Without a ticket system, it writes files: one file for each item, or one combined file, in Markdown, HTML, or text.

### The checkers

The skills use these scripts. They use only the Python standard library.

| Script | Checks |
|--------|--------|
| `backlogcheck.py` | Stories, features, epics, and goals: role, value, criteria, edge cases, scope, measures, size. |
| `backlogtree.py` | The tree of the draft, possible duplicates, the push log (to continue a push that stopped), and the file export. |
| `examplecheck.py` | `examples.md`: each rule has an example, each limit has the cases below, at, and above, and each open example has a question. |
| `protocheck.py` | The prototype: the banner, the question, the states, no external calls. |
| `doccheck.py` | The language rules of this repository. |

### No human to answer

In a scheduled or unattended run, the skill takes its recommended answers and marks them "assumed". It stops after the prototype, because only a human can validate it. `backlog-push` never creates items without your approval.

## When it helps

Use it when:

- An idea must become items that other people or agents build. The items get one standard: role, value, criteria, examples, and scope.
- The idea is not clear yet. The questions and the prototype find what the requester means before the backlog exists.
- Your team has a standard, for example an MVP framework. A team profile makes the skills follow it.
- An item goes to a build. `example-mapping` finds the limits and the words with more than one meaning, and searches the existing decisions first.

Do not use it when:

- You know what to build and you build it yourself. Write one item, or go to `dev-flow:plan-and-build`.
- The work is a bug or a chore. One clear item is enough.
- You need a decision of the business. The skills prepare each question with a recommended answer. The PO decides.

## Limits

- The push needs a connector for your ticket system in Claude Code or claude.ai. Without one, you get files.
- Jira has no Feature level by default. GitLab epics need GitLab Premium. The reference file of each system gives the details.
- The prototype is throwaway. It is not production code.
- The checkers check the structure and the wording. They do not check if an item is the right thing to build.

## Documentation

- [Changelog](CHANGELOG.md)
- Grill test: [before](../../evals/product-flow/grill-test/results-before.md) and [after](../../evals/product-flow/grill-test/results-after.md) the change of 1.2.0.
- Test results: [idea-to-backlog](../../evals/product-flow/results-2026-10-12-idea-to-backlog.md), [backlog-story](../../evals/product-flow/results-2026-10-12-backlog-story.md), [backlog-feature](../../evals/product-flow/results-2026-10-12-backlog-feature.md), [backlog-epic and backlog-goal](../../evals/product-flow/results-2026-10-12-backlog-epic-goal.md), [product-prototype](../../evals/product-flow/results-2026-10-12-product-prototype.md), [backlog-push](../../evals/product-flow/results-2026-10-12-backlog-push.md).

## License

MIT. Refer to the [LICENSE](../../LICENSE) file.
