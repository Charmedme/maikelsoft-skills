# dev-docs

dev-docs is a Claude Code plugin for developers who write documentation. It writes, rewrites, and reviews each common type of developer document in clear, controlled language.

English output follows ASD-STE100 Simplified Technical English. Dutch output follows Begrijpelijk Nederlands (taalniveau B1), with the STE rules for sentences and procedures. Each skill includes a checker that counts the rules that a script can count.

## Install

You need Claude Code and Python 3.9 or later.

```sh
claude plugin marketplace add Charmedme/maikelsoft-skills
claude plugin install dev-docs@maikelsoft-skills
```

Do you also use the skills `engineering:documentation`, `engineering:architecture`, or `design:ux-copy`? Disable them. They compete with dev-docs for the same requests, and their language rules are different.

## Quick start

1. Open Claude Code in a repository.
2. Ask for a document in your own words:

   ```
   Write a README for this project.
   ```

3. Claude selects `doc-readme`, collects the facts from the code, and writes the file. Then it runs the checker and gives a report:

   ```
   README.md: 0 errors, 0 warnings | 44 sentences, avg 9.5 words, max 24 | lang=en level=standard
   ```

   The report also lists the open questions, the `TODO:` items, and the errors that it found in the source.

## Usage

### Skills

| Skill | Document type | Example request |
|-------|---------------|-----------------|
| `doc-plan` | Documentation plan or audit | Document this project. Which docs are missing? |
| `doc-readme` | README | Write a README for this repo. |
| `doc-tutorial` | Tutorial (a lesson for a new user) | Write a getting-started tutorial. |
| `doc-howto` | How-to guide or runbook | Write the steps to rotate the API key. |
| `doc-reference` | Reference (API, CLI, configuration) | Document all CLI options and exit codes. |
| `doc-explanation` | Explanation (how and why) | Explain how the cache works. |
| `doc-adr` | Architecture decision record (MADR 4.0.0) | Record the decision to use Postgres. |
| `doc-changelog` | Changelog entry and release notes | Write the release notes for 2.3.0. |
| `doc-code-comments` | Code comments and doc comments | Review the comments in `Parser.cs`. |
| `doc-ui-text` | UI text (buttons, errors, dialogs) | Schrijf de foutmelding voor een volle schijf. |

`doc-plan` makes a plan and waits for your approval. Then it uses the other skills, and it writes the README last.

### Modes

Each skill has three modes:

- **Write**: make a new document.
- **Rewrite**: improve a document. The skill keeps each fact, code line, link, and heading anchor.
- **Review**: report the findings. The skill changes nothing. When you disagree with a finding, the skill checks the rule again and tells you if the finding is still true. You decide what to change.

### Language and strictness

The skill uses the language that you state. In a rewrite, it keeps the language of the source. Code comments are always English. For Dutch text, the skill uses "je" or "u" as you say. When there is no signal, it asks.

| Level | Request word | What the skill applies |
|-------|--------------|------------------------|
| standard | (default) | All sentence, structure, and word rules |
| strict | "strict", "ASD-STE100 compliant" | Also a check of each word against the STE dictionary |
| light | "light", "quick" | Only the sentence rules and the structure rules |

### Checker

Each skill has the checker `scripts/doccheck.py`. It uses only the Python standard library. You can also run it yourself:

```sh
python3 doccheck.py --level standard docs/how-to/deploy.md
python3 doccheck.py --source old/deploy.md docs/how-to/deploy.md   # rewrite: what did it lose?
python3 doccheck.py --comments src/Parser.cs                        # comments in code
python3 doccheck.py --format json README.md
```

The exit code is 1 when there is one or more errors.

## Reference

### Checker rules

The STE numbers refer to ASD-STE100 Issue 9. The labels are our own words.

| Rule ID | Severity | Finding |
|---------|----------|---------|
| `STE-5.1` | error | A procedural sentence (a numbered step) has more than 20 words. |
| `STE-6.3` | error | A descriptive sentence has more than 25 words. |
| `B1-LEN` | error | A Dutch sentence has more than 20 words. |
| `STE-6.6` | error | A paragraph has more than 6 sentences. |
| `STE-8.1` | error | The text has a semicolon. |
| `HOUSE-DASH` | error | The text has an em-dash, or a dash that makes a pause. |
| `STE-4.2` | error | The text has a contraction ("don't"). |
| `STE-1.1` | warning (error in strict) | The text has a modal verb that STE does not approve ("should", "may"). |
| `STE-3.6` | warning | The sentence is possibly passive. |
| `STE-3.5` | warning | An -ing word follows a comma. |
| `B1-PASSIVE` | warning | The Dutch sentence is possibly passive. |
| `HOUSE-WORDS` | warning | The text has a filler word or a formal word ("just", "utilize", "teneinde"). |
| `KEEP-CODE` | error | A rewrite lost or changed a code line of the source. |
| `KEEP-LINK` | error | A rewrite lost a link target of the source. |
| `KEEP-ANCHOR` | warning | A rewrite changed a heading anchor. |

Inline code, a link, a URL, and a short quoted label (4 words or fewer) count as one word.

## Limits

- No tool can guarantee compliance with ASD-STE100. In strict mode, the word check uses the knowledge of the model.
- The passive checks are heuristics. They can give false warnings.
- The checker does not read a line that starts with `<` (HTML), or text in code blocks and Mermaid diagrams.
- `--comments` reads the comments that start a line, block comments, and Python docstrings. It does not read a comment at the end of a line of code.

## Documentation

- [Changelog](CHANGELOG.md)
- [ADR 0001: a Python checker instead of Vale](../../docs/decisions/0001-python-checker-instead-of-vale.md)
- The ASD-STE100 standard is a free download at [asd-ste100.org](https://www.asd-ste100.org/).

## License

MIT. Refer to the [LICENSE](../../LICENSE) file.
