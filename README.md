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
| [dev-flow](plugins/dev-flow/README.md) | 2.0.0 | Builds one approved work item from the tracker, spec first. Turns the examples into acceptance tests, locks them after your approval, and builds one scenario at a time. Sets a tier from the size and the risk. Product questions go to the product owner on the item. |
| [dev-build](plugins/dev-build/README.md) | 1.1.0 | Builds agent skills and agents: research, concept, test, and build. Two skills, `build-skill` and `build-agent`, for Claude Code, the Agent SDK, Managed Agents, and OpenClaw. |
| [product-flow](plugins/product-flow/README.md) | 1.3.0 | Takes a product idea to an approved backlog in the ticket system, for product owners and business analysts. Eight skills: research, questions, prototype, spec, items, push, and example mapping. |
| [dev-stack](plugins/dev-stack/README.md) | 1.1.0 | Stack skills for a .NET 10 team. They cover C#, ASP.NET Core, OpenAPI, validation, EF Core, SQL Server, PostgreSQL, MSTest 4, and analyzers. Also observability, Redis, Elasticsearch, RabbitMQ, Wolverine, MailKit, PuppeteerSharp, secure coding, and the move to AwesomeAssertions. Each skill has version-specific rules, a check step with a tool, and one report format. |
| [dev-team](plugins/dev-team/README.md) | 0.3.0 | Preview. Delivers one work item with a team of agents, when you ask for the team. A senior designs and builds the core, mediors build work packages, and a security reviewer checks the diff. Two human gates. Needs dev-stack. |
| [ux-design](plugins/ux-design/README.md) | 0.1.0 | Preview. A UX designer agent that frames the problem and designs the flow, the patterns, the screens, and the states, with a source for each decision. It audits the design system or makes a foundation, and makes mockups from it. |

### Which plugin to use

| You have | Use | Skip it when |
|----------|-----|--------------|
| An idea, not yet items | `product-flow` | You build it yourself and know what to build |
| An approved item with rules and limits | `dev-flow` | The change is small and clear: plain Claude Code is faster and cheaper |
| A risky item, and you need a full record | `dev-team` | You want a fast or cheap result: one session with `dev-flow` gets the same result |
| .NET code | `dev-stack` | The stack is not .NET |
| Docs to write or review | `dev-docs` | |
| A skill or an agent to build | `dev-build` | |
| Screens or a flow to design | `ux-design` | The story has no user interface, or you only want one quick visual idea |

Each plugin README has a section "When it helps".

## Language rules

All skills share one set of language rules. The source is [`shared/language.md`](shared/language.md). A skill can have different rules only when its own instructions say so.

## Contributing

Read [CONTRIBUTING.md](CONTRIBUTING.md). It gives the steps to add or change a skill.

## License

MIT. Refer to the [LICENSE](LICENSE) file.
