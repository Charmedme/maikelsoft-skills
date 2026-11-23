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
| [dev-docs](plugins/dev-docs/README.md) | 2.0.0 | Writes, rewrites, and reviews developer documentation: README, tutorial, how-to, reference, explanation, ADR, changelog, code comments, and UI text. `/update-docs` checks and updates the whole doc set of a project. |
| [dev-flow](plugins/dev-flow/README.md) | 3.2.0 | Builds one work item (story, bug, task, or chore) from its ticket to tested commits, spec first. Copies the Behaviour spec of the ticket and locks one test file for each rule. Builds one scenario at a time with TDD. Proves the result with held-back tests, a mutation check, and fresh reviews. Sets the size from the change and the risk. Also handles the review comments on a pull request. |
| [dev-build](plugins/dev-build/README.md) | 1.1.1 | Builds agent skills and agents: research, concept, test, and build. Two skills, `build-skill` and `build-agent`, for Claude Code, the Agent SDK, Managed Agents, and OpenClaw. |
| [product-flow](plugins/product-flow/README.md) | 2.0.0 | Takes an idea, a request, or a bug to approved backlog items in the ticket system, for product owners and business analysts. One command for each phase: `/to-backlog`, `/epic`, `/feature`, `/user-story`, `/bug-item`, `/refine`, `/prototype`, and `/product-goal`. Each story and bug gets a Gherkin Behaviour spec. |
| [dev-stack](plugins/dev-stack/README.md) | 2.1.0 | Stack skills for the .NET 10 and 11 back end. They cover C#, the move to .NET 11, ASP.NET Core, API design, OpenAPI, validation, EF Core, SQL Server, PostgreSQL, and analyzers. Also Orleans, Aspire, Blazor, and MCP servers. Also observability, Redis, Elasticsearch, RabbitMQ, Wolverine, MailKit, PuppeteerSharp, and secure coding. Each skill has version-specific rules, a check step with a tool, and one report format. |
| [frontend-stack](plugins/frontend-stack/README.md) | 1.1.1 | Front-end stack skills: Angular 22 and 21 next to the official Angular skill, security rules for browser code, and WebMCP tools (preview). A third skill sends HTML, CSS, and client-side JavaScript work to Modern Web Guidance of Google Chrome. |
| [test-stack](plugins/test-stack/README.md) | 1.1.0 | Test stack skills: MSTest 4 and xUnit v3, assertions, fakes, `WebApplicationFactory`, and Testcontainers. Playwright (TypeScript, .NET, and Playwright MCP), Reqnroll with Gherkin, and the move from FluentAssertions to AwesomeAssertions. |
| [dev-team](plugins/dev-team/README.md) | 1.1.2 | The team mode of dev-flow for one large or risky work item, on the same spec.md, locked tests, and run.md header. A senior designs and builds the core, mediors build work packages, and a security reviewer checks the diff. The plan gate and the final gate. Needs dev-flow 3.1.0 or later and dev-stack. |
| [ux-design](plugins/ux-design/README.md) | 1.0.0 | `/ux-design` designs one item with UI before the build, and `/ux-review` checks the built screens against the design. A UX designer agent frames the problem and designs the flow, the patterns, the screens, and the states, with a source for each decision. It audits the design system or makes a foundation, and makes mockups and prototypes from it, also in Claude Design, Figma, or OpenPencil. It reviews the built feature against the design and WCAG 2.2 with Playwright. |
| [dev-setup](plugins/dev-setup/README.md) | 1.0.0 | Sets up the delivery flow one time. `/setup` keeps your role for all projects or for one repository. It asks only the facts that the repository does not show, and writes the review and CI files. `/setup starter` makes the Claude Code basics of a project. |

### Which plugin to use

| You have | Use | Skip it when |
|----------|-----|--------------|
| An idea, not yet items | `product-flow` | You build it yourself and know what to build |
| An approved item with rules and limits | `dev-flow` | The change is small and clear: plain Claude Code is faster and cheaper |
| A large or risky item, and you need a full record | `dev-team` | You want a fast or cheap result: one session with `dev-flow` costs about a third, and its code had more defects in a blind grading |
| .NET back-end code | `dev-stack` | The stack is not .NET |
| An Angular front end, or HTML, CSS, and browser code | `frontend-stack` | The change has no browser code |
| .NET tests, browser tests with Playwright, or Gherkin scenarios with Reqnroll | `test-stack` | The project has no tests of these types |
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
