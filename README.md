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
| [dev-flow](plugins/dev-flow/README.md) | 1.0.0 | Plans and builds a change from an idea to working code: research, questions, plan, specs, tickets, a prototype to validate, and then the build. |
| [dev-build](plugins/dev-build/README.md) | 1.1.0 | Builds agent skills and agents: research, concept, test, and build. Two skills, `build-skill` and `build-agent`, for Claude Code, the Agent SDK, Managed Agents, and OpenClaw. |
| [product-flow](plugins/product-flow/README.md) | 1.2.0 | Takes a product idea to an approved backlog in the ticket system, for product owners and business analysts. Seven skills: research, questions, prototype, spec, items, and push. |
| [dev-stack](plugins/dev-stack/README.md) | 1.1.0 | Stack skills for a .NET 10 team. They cover C#, ASP.NET Core, OpenAPI, validation, EF Core, SQL Server, PostgreSQL, MSTest 4, and analyzers. Also observability, Redis, Elasticsearch, RabbitMQ, Wolverine, MailKit, PuppeteerSharp, secure coding, and the move to AwesomeAssertions. Each skill has version-specific rules, a check step with a tool, and one report format. |
| [dev-team](plugins/dev-team/README.md) | 0.2.1 | Preview. Delivers one user story with a team of agents. A senior designs and builds the core, mediors build work packages, and a security reviewer checks the diff. Two human gates. Needs dev-stack. |

## Language rules

All skills share one set of language rules. The source is [`shared/language.md`](shared/language.md). A skill can have different rules only when its own instructions say so.

## Contributing

Read [CONTRIBUTING.md](CONTRIBUTING.md). It gives the steps to add or change a skill.

## License

MIT. Refer to the [LICENSE](LICENSE) file.
