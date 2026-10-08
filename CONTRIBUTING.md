# Contribute to maikelsoft-skills

This guide gives the steps to add or change a skill. You need Python 3.9 or later and Claude Code.

## How the repository works

- `shared/` holds the files that more than one skill uses: the language rules (`language.md`), the workflow of the dev-docs skills (`workflow.md`), and the checker (`doccheck.py`).
- `scripts/sync_shared.py` copies the shared files into each skill. A skill then works alone, without the rest of the repository.
- `plugins/<plugin>/` holds one plugin. Each plugin has `.claude-plugin/plugin.json`, a `README.md`, a `CHANGELOG.md`, and its skills in `skills/<skill>/SKILL.md`.
- `.claude-plugin/marketplace.json` lists the plugins.

Do not edit a copy in a skill folder (`references/language.md`, `scripts/doccheck.py`, or the workflow block in `SKILL.md`). The next sync overwrites it.

## Build a new skill

Each new skill follows these four steps:

1. **Research.** Study the subject from more than one view: the standards, the practice, the readers, and the existing tools.
2. **Concept.** Write the concept, and ask the human to decide each open point.
3. **Test.** Run the skill on realistic tasks, in separate agents, against a baseline without the skill. Compare more than one version. Show the results.
4. **Build.** After the human approves, add the skill to a plugin and update the files in "Release a change".

## Add a skill

1. Make the folder `plugins/<plugin>/skills/<skill>/` with a `SKILL.md`.
2. In the frontmatter, write a `description` that starts with the document type or the task. The model selects the skill from this text.
3. Add these two lines where the shared workflow must go:

   ```
   <!-- BEGIN shared/workflow.md -->
   <!-- END shared/workflow.md -->
   ```

4. Copy the shared files into the skill:

   ```sh
   python3 scripts/sync_shared.py
   ```

5. Check the skill text:

   ```sh
   python3 shared/doccheck.py --lang en plugins/<plugin>/skills/<skill>/SKILL.md
   ```

   The result must show 0 errors.

## Release a change

1. Run the tests and the checks:

   ```sh
   python3 -m unittest discover -s tests
   python3 scripts/sync_shared.py --check
   claude plugin validate --strict ./plugins/<plugin>
   claude plugin validate .
   ```

2. Increase `version` in `plugins/<plugin>/.claude-plugin/plugin.json`. Users get a new version only when this number changes.
3. Add an entry to `plugins/<plugin>/CHANGELOG.md` in the Keep a Changelog 1.1.0 format.
4. Update the version in the plugin table of `README.md`.
5. Merge to `main`, then add the tag `<plugin>-v<version>`.

## Language of the docs

Write all docs in this repository with the rules in `shared/language.md`. The CI runs the checker on each Markdown file, also on each `SKILL.md`.
