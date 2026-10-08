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
| [dev-docs](plugins/dev-docs/README.md) | 1.0.0 | Writes, rewrites, and reviews developer documentation: README, tutorial, how-to, reference, explanation, ADR, changelog, code comments, and UI text. |

## Language rules

All skills share one set of language rules. The source is [`shared/language.md`](shared/language.md). A skill can have different rules only when its own instructions say so.

## Contributing

Read [CONTRIBUTING.md](CONTRIBUTING.md). It gives the steps to add or change a skill.

## License

MIT. Refer to the [LICENSE](LICENSE) file.
