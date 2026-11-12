# maikelsoft-skills

maikelsoft-skills is a Claude Code plugin marketplace with skills and agents for software developers. Each skill writes clear output: English in ASD-STE100 Simplified Technical English, and Dutch in Begrijpelijk Nederlands (taalniveau B1).

## Install

Add the marketplace once:

```sh
claude plugin marketplace add Charmedme/maikelsoft-skills
```

Then install a plugin from the list below:

```sh
claude plugin install <plugin>@maikelsoft-skills
```

## Plugins

| Plugin | Version | What it does |
|--------|---------|--------------|
| [dev-docs](plugins/dev-docs/README.md) | 1.4.0 | Writes, rewrites, and reviews developer documentation: README, tutorial, how-to, reference, explanation, ADR, changelog, code comments, and UI text. |
| [dev-flow](plugins/dev-flow/README.md) | 3.0.0 | Builds one work item (story, bug, task, or chore) from its ticket to tested commits, spec first. Copies the Behaviour spec of the ticket and locks one test file for each rule. Builds one scenario at a time with TDD. Proves the result with held-back tests, a mutation check, and fresh reviews. Sets the size from the change and the risk. Also handles the review comments on a pull request. |
| [dev-build](plugins/dev-build/README.md) | 1.1.1 | Builds agent skills and agents: research, concept, test, and build. Two skills, `build-skill` and `build-agent`, for Claude Code, the Agent SDK, Managed Agents, and OpenClaw. |
| [product-flow](plugins/product-flow/README.md) | 2.0.0 | Takes an idea, a request, or a bug to approved backlog items in the ticket system, for product owners and business analysts. One command for each phase: `/to-backlog`, `/epic`, `/feature`, `/user-story`, `/bug-item`, `/refine`, `/prototype`, and `/product-goal`. Each story and bug gets a Gherkin Behaviour spec. |
| [dev-stack](plugins/dev-stack/README.md) | 1.3.0 | Stack skills for a .NET 10 team with an Angular front end. They cover C#, ASP.NET Core, OpenAPI, validation, EF Core, SQL Server, PostgreSQL, MSTest 4, and analyzers. Also observability, Redis, Elasticsearch, RabbitMQ, Wolverine, MailKit, PuppeteerSharp, Angular 22 and 21, secure coding, and the move to AwesomeAssertions. Each skill has version-specific rules, a check step with a tool, and one report format. |
| [dev-team](plugins/dev-team/README.md) | 0.6.0 | Preview. Delivers one work item with a team of agents, when you ask for the team. A senior designs and builds the core, mediors build work packages, and a security reviewer checks the diff. Two human gates. Needs dev-stack. |
| [ux-design](plugins/ux-design/README.md) | 0.4.0 | Preview. A UX designer agent that frames the problem and designs the flow, the patterns, the screens, and the states, with a source for each decision. It audits the design system or makes a foundation, and makes mockups and prototypes from it, also in Claude Design, Figma, or OpenPencil. It reviews the built feature against the design and WCAG 2.2 with Playwright. |
| [dev-setup](plugins/dev-setup/README.md) | 1.0.0 | Sets up the delivery flow one time. `/setup` keeps your role for all projects or for one repository. It asks only the facts that the repository does not show, and writes the review and CI files. `/setup starter` makes the Claude Code basics of a project. |

### Which plugin to use

| You have | Use | Skip it when |
|----------|-----|--------------|
| An idea, not yet items | `product-flow` | You build it yourself and know what to build |
| An approved item with rules and limits | `dev-flow` | The change is small and clear: plain Claude Code is faster and cheaper |
| A risky item, and you need a full record | `dev-team` | You want a fast or cheap result: one session with `dev-flow` gets the same result |
| .NET code, or an Angular front end on a .NET API | `dev-stack` | The stack is not .NET or Angular |
| Docs to write or review | `dev-docs` | |
| A skill or an agent to build | `dev-build` | |
| Screens or a flow to design | `ux-design` | The story has no user interface, or you only want one quick visual idea |
| A new person or a new project for the delivery flow | `dev-setup` | You use one plugin alone and its own questions are enough |

Each plugin README has a section "When it helps".

## Language rules

All skills share one set of language rules. The source is [`shared/language.md`](shared/language.md). A skill can have different rules only when its own instructions say so.

## Contributing

Read [CONTRIBUTING.md](CONTRIBUTING.md). It gives the steps to add or change a skill.

## License

MIT. Refer to the [LICENSE](LICENSE) file.
