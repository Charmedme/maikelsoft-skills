# product-flow

product-flow is a Claude Code plugin for product owners (PO) and business analysts (BA). It takes a product idea to an approved backlog. You make the decisions. The skills find the facts, ask the questions, build a prototype, and write the items. The skills do not build the product.

English output follows ASD-STE100 Simplified Technical English. Dutch output follows Begrijpelijk Nederlands (taalniveau B1). When your templates use another language, the skills use the language of the templates.

## Install

You need Claude Code and Python 3.9 or later.

```sh
claude plugin marketplace add Charmedme/maikelsoft-skills
claude plugin install product-flow@maikelsoft-skills
```

## Commands

Each phase of the work has one command. Claude Code starts a command by itself when your request matches its trigger. You can also type it. Text after the command goes to the skill, for example `/product-flow:to-backlog Customers want to save a search`. When no other plugin has a command with the same name, the short name also works: `/to-backlog`.

| Command | What it does | Starts by itself when you ask for |
|---------|--------------|-----------------------------------|
| `/product-flow:to-backlog` | Takes an idea, a feature request, or a bug report to the items that it needs. Sets the depth by size. | An idea, a request, or a bug turned into backlog items, or such a run continued |
| `/product-flow:epic` | Writes an epic with its outcome, scope, and features. | An epic |
| `/product-flow:feature` | Writes a feature with its stories. | A feature |
| `/product-flow:user-story` | Writes, reviews, or splits user stories, each with acceptance criteria and a Behaviour spec. | A user story, a PBI, or a requirement |
| `/product-flow:bug-item` | Writes one bug item with the current and the expected behaviour as a Behaviour spec. | A bug item or a bug report |
| `/product-flow:refine` | Makes a draft item ready to build: one review, the open questions to the PO in one message, and the approval. | An item refined, made ready, or checked for what is not clear |
| `/product-flow:prototype` | Makes a throwaway clickable prototype for one product question. | A feature, screen, or flow to see, or layout options to compare |
| `/product-flow:product-goal` | Writes a product goal or a sprint goal. | A product goal or sprint goal |

Each item command (`epic`, `feature`, `user-story`, `bug-item`) ends with the refine pass and the push, after your approval.

### Skills that are not in the menu

These skills do not show in the `/` menu. The commands start them.

| Skill | Use |
|-------|-----|
| `backlog-push` | Puts approved items into Jira, Azure DevOps, GitHub, GitLab, or Linear after a duplicate check, or into files. It sets the label `spec:draft` or `spec:ready`. |
| `example-mapping` | Writes `examples.md` for `dev-flow:plan-and-build` 2.x. It stays until dev-flow reads the Behaviour spec. |
| `idea-to-backlog`, `backlog-epic`, `backlog-feature`, `backlog-story`, `backlog-goal`, `product-prototype` | The old names of version 1. Each one starts the skill with the new name. They stop in version 3.0.0. |

## Quick start

1. Open Claude Code in your product repository, or in a claude.ai Project.
2. Give the request in your own words:

   ```
   /product-flow:to-backlog Customers can find their items via the search box.
   ```

3. Answer the questions. Each question has a recommended answer. Answer "ok" to accept all, or "Q3: no" to change one.
4. Answer at each gate. Nothing goes into your ticket system before you approve the exact list.

The skills read your role and the facts of the project from the `dev-setup` plugin, when you use it. Without it, they ask the facts that they need.

## Usage

### Size and depth

`to-backlog` sets the size first, says it and the reason, and asks when it is not sure.

| Size | Signal | What runs |
|------|--------|-----------|
| Small | One result for one user, or one bug | `user-story` or `bug-item`: a few questions, the item, the refine pass, the push. No research, no story map, no prototype. |
| Medium | One capability with more than one story: a feature | All phases. A prototype only after you say yes. |
| Large | More than one capability, or a new product area: an epic | All phases. A prototype only after you say yes. |

### Phases and gates

| Phase | The skill makes | Gate (you decide) |
|-------|-----------------|-------------------|
| 1. Scan and research | Sources: your tools, documents, existing tickets, and the web | |
| 2. Grill | Problem (round 1: the need), decisions, open questions | **G1** Is this what you mean? |
| 3. Prototype (after a yes) | A prototype file. With the `ux-design` plugin: the UX design, the mockups, and the prototype of the agent. | **G2** Is this what you mean? |
| 4. Spec | Goal, users, scope, story map | |
| 5. Backlog | Goal, features, stories, each story with its Behaviour spec | **G3** Is this backlog correct? |
| 6. Refine and push | Ready items in the ticket system, or files | **G4** Approval of the exact list |

### Levels and profiles

- The default is 2 levels: Feature, then User Story. The goal goes in the feature.
- Ask for 3 levels (Epic, Feature, User Story) when your team uses epics.
- Profiles: Agile (default), Scrum, and SAFe. SAFe features get a benefit hypothesis.
- A bug gets a bug item (`bug-item`). Tasks are not part of this flow: they are for the team.

### The intent file

A medium or large run keeps its facts in one file: `docs/backlog/<slug>/intent.md`. It has the problem, the outcome, the users, the scope, the sources, the decisions, the prototype verdict, the story map, the items, and the status. In claude.ai, the file stays in the Project, or you get it as a download. To continue later, start `to-backlog` again in the same repository, or give the file to the skill. `to-backlog` also reads an older `backlog-<slug>.md` of version 1.

### The Behaviour spec

Each user story and each bug item has its acceptance criteria (AC1, AC2) and, below them, a section "Behaviour spec" in Gherkin. Each rule names its criterion, and each limit has a scenario below, at, and above it:

```gherkin
Rule: R1 A return within 30 days is free (AC1) (limit: 30 days)

  Scenario: return at the limit
    Given an order that the customer got 30 days ago
    When the customer returns the order
    Then the return is free
```

A bug item has a `Now:` line with the current behaviour, and a rule "Unchanged" for the behaviour that must stay. The scenarios become the acceptance tests of the team. In Azure DevOps, the criteria and the spec go in the field Acceptance Criteria. In the other trackers, they go in the description.

### Templates and team profile

At the start, the skills ask which template applies. A team profile is one Markdown file with the standard of the team, for example an MVP framework. It wins over the rules of the skills where they conflict. Its "Checker" section can allow Given/When/Then, criteria on an epic, and a title format. Without a profile, the skills use the template of the ticket system or their own templates. Acceptance criteria are then a short rule list, not Given/When/Then.

Each criterion has one or more rules in the Behaviour spec, and each rule names a criterion that exists (rule A11). An older item with only an "Examples" section gets rule A10. A profile turns both rules off with `examples: no`.

### Open questions

The intent file keeps each open question one time, with its owner and status. Items refer to it by number (O1, O2). The G3 summary gives the open questions for each owner, so the list can go to a refinement or a customer meeting.

### The refine pass

`refine` works as a BA on one draft item. A new reviewer checks the item against INVEST, checks that each criterion has a rule, and checks the scenarios at each limit. It lists the words with more than one meaning and the values that are missing. Then the skill searches the existing decisions, for example the Decisions section of the intent file. Only the questions without an answer go to the PO, in one message, each with a recommended answer. The pass stops after 3 rounds.

An item is `spec:ready` when no question is open, the checkers show 0 errors, and the PO approved the exact text. Only the tool sets this label. When a person sets it by hand, the next command checks the item again. Each item command ends with this pass. Use `/product-flow:refine` for an item that is still `spec:draft`.

### Duplicates

Before the push, `backlog-push` searches your ticket system for items with a similar title. It shows each possible duplicate and asks what to do. Without a ticket system, it writes files: one file for each item, or one combined file, in Markdown, HTML, or text.

### The checkers

The skills use these scripts. They use only the Python standard library.

| Script | Checks |
|--------|--------|
| `backlogcheck.py` | Stories, bugs, features, epics, and goals: role, value, criteria, the link from each criterion to a rule, edge cases, scope, measures, size. |
| `backlogtree.py` | The tree of the draft, and the Behaviour spec of each story and bug (`specs`). Also possible duplicates, the push log (to continue a push that stopped), and the file export. |
| `specsync.py` | The Behaviour spec: the IDs, the number of rules and scenarios, the steps, the limit cases, and the parts of a bug. |
| `examplecheck.py` | `examples.md` of the hidden `example-mapping` skill. |
| `protocheck.py` | The prototype: the manifest, the header text, the states, the variants, the tasks, and no external calls. |
| `doccheck.py` | The language rules of this repository. |

### No human to answer

In a scheduled or unattended run, the skill takes its recommended answers and marks them "assumed". It stops after the prototype, because only a human can validate it. `backlog-push` never creates items without your approval.

## When it helps

Use it when:

- An idea must become items that other people or agents build. The items get one standard: role, value, criteria, examples, and scope.
- The idea is not clear yet. The questions and the prototype find what the requester means before the backlog exists.
- Your team has a standard, for example an MVP framework. A team profile makes the skills follow it.
- An item goes to a build. The Behaviour spec gives the acceptance tests, and the refine pass finds the limits and the words with more than one meaning first.

Do not use it when:

- You know what to build and you build it yourself. Write one item, or go to `dev-flow:plan-and-build`.
- The work is a chore. One clear task is enough.
- You need a decision of the business. The skills prepare each question with a recommended answer. The PO decides.

## Use in claude.ai

`python3 scripts/build_claude_ai.py --plugin product-flow` in this repository makes the plugin zip `dist/claude-ai/product-flow.zip`. Upload this zip in claude.ai as a plugin. Then add this text to the instructions of your Project:

```
Delivery role: product owner. Scope: project. Route: refine=on, approve-at-ready=off.
Tracker: <Jira, Azure DevOps, GitHub, GitLab, Linear, or files>.
Item files: docs/backlog.
Use the product-flow skills for each backlog item. Push an item only after my approval.
```

Without a connector for your ticket system, the skills give the items as files.

## Limits

- The push needs a connector for your ticket system in Claude Code or claude.ai. Without one, you get files.
- Jira has no Feature level by default. GitLab epics need GitLab Premium. The reference file of each system gives the details.
- The prototype is throwaway. It is not production code.
- The checkers check the structure and the wording. They do not check if an item is the right thing to build.

## Documentation

- [Changelog](CHANGELOG.md)
- Grill test: [before](../../evals/product-flow/grill-test/results-before.md) and [after](../../evals/product-flow/grill-test/results-after.md) the change of 1.2.0.
- Test results of 2.0.0: [results](../../docs/work/product-flow/2.0.0/results.md).
- Test results of 1.x: [idea-to-backlog](../../evals/product-flow/results-2026-10-12-idea-to-backlog.md), [backlog-story](../../evals/product-flow/results-2026-10-12-backlog-story.md), [backlog-feature](../../evals/product-flow/results-2026-10-12-backlog-feature.md), [backlog-epic and backlog-goal](../../evals/product-flow/results-2026-10-12-backlog-epic-goal.md), [product-prototype](../../evals/product-flow/results-2026-10-12-product-prototype.md), [backlog-push](../../evals/product-flow/results-2026-10-12-backlog-push.md).

## License

MIT. Refer to the [LICENSE](../../LICENSE) file.
