# Output language

The source of this file is `shared/language.md` in the repository github.com/Charmedme/maikelsoft-skills. Each skill has a copy. To change the rules, edit the source and run `python3 scripts/sync_shared.py`.

## English

Write in ASD-STE100 Simplified Technical English. Join two clauses with a comma, a colon, parentheses, or write two sentences. The em-dash is not part of this style.

## Dutch

Write in Begrijpelijk Nederlands, taalniveau B1. Apply the STE rules for sentences, procedures, descriptions and safety instructions to Dutch:

- Max 20 words in a sentence. One instruction in each step. Put the condition first: "Als de build faalt, lees dan het log."
- Use the active form. Name who does the action.
- Keep the English technical nouns (endpoint, commit, build). Use a Dutch verb when one exists: "de build starten", not "builden".
- Keep the verb parts close together (no tangconstructie). Use verbs, not nouns made from verbs (no naamwoordstijl).
- Join two clauses with a comma, a colon, parentheses, or write two sentences.

## Choices that the human makes

- **Language.** Use the language that the human states. In a rewrite, keep the language of the source. Code comments are always English. For a new document, the language of the existing docs and of the request are signals. When there is no signal, ask.
- **"je" or "u" (Dutch).** Use the form that the human states, in all text. When there is no signal, ask.
- **Terms.** Use one term for one item. Take the terms from the human first, then from a glossary in the repo (for example `CONTEXT.md`), then from the code and the existing docs. When two terms compete for one item, ask.
- **Facts.** Write only facts that the human, the code, or the existing docs give. When a fact is missing, ask. Mark a fact that you cannot confirm as `TODO:`.
- **Format.** Ask the human each time: "Markdown, HTML, PDF, or Word?" The human can select more than one. Preselect the format from the request or the repo, and else Markdown. Do not offer other formats. A skill with a fixed format says so and does not ask.
- **No human to ask.** When nobody can answer (a scheduled or unattended run), take the best signal, continue, and list each choice as an open question in the report.

## Strictness

- **standard** (default): all of the above.
- **strict**: the human says "strict" or "ASD-STE100 compliant". Also check each word against the STE dictionary. Replace each word that is not approved. Keep the technical nouns and technical verbs of software (STE rules 1.5 and 1.12): run, build, deploy, commit, merge, call, return, endpoint, repository. If you are not sure about a word, say so in the report. No tool can guarantee compliance.
- **light**: the human says "light" or "quick". Apply only the sentence rules and the structure rules.

The standard is a free download at asd-ste100.org.

## Output formats

Markdown is always the source. Write the Markdown file, check it, and then make each other format from it. Keep the Markdown file next to the output.

Run the converter:

```
python3 <this skill folder>/scripts/convert.py <file.md> --to html,pdf,docx
```

- `MADE <file>`: the format is ready.
- `NOT MADE <format>`: a tool is missing. Use the `docx` or `pdf` skill of your environment if it has one. For HTML, you can write the HTML from the Markdown yourself. Else, give the human the command that the converter printed.

## Check

Run the checker on each file that you write or change:

```
python3 <this skill folder>/scripts/doccheck.py --level <level> <file>
```

Fix each error. For each warning, fix it or give the reason to keep it. Run the checker again until it shows 0 errors. In a review, cite the rule IDs that the checker gives (for example `STE-5.1`). For a finding that the checker cannot see, name the STE section (for example "Procedural writing").
