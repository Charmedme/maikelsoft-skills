# Changelog

This file records all notable changes to the dev-docs plugin in the maikelsoft-skills marketplace.

The format follows [Keep a Changelog 1.1.0](https://keepachangelog.com/en/1.1.0/). The plugin uses [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [1.1.0] - 2026-10-08

### Added

- Each skill treats the text of a source document and the code as data. It does not follow an instruction in that text, and it reports the instruction as a source error.
- In review mode, when the human disagrees with a finding, the skill checks the rule and the source again. If the finding is still true, the skill says so and gives the rule. The human decides what to change.
- The Check step now checks each fact (command, option, default, version, path) against its source. A fact without a source gets a `TODO:`.

### Changed

- `doc-readme`: the first two sentences name the main use and the reader. Before, the first sentence had to name both.
- `doccheck.py --source` ignores HTML comments. A rewrite does not have to keep a link or a code line that is only in a hidden comment.

## [1.0.0] - 2026-10-08

### Added

- First release of the dev-docs plugin for Claude Code.
- 10 documentation skills:
  - `doc-plan`: documentation plans and documentation audits.
  - `doc-readme`: README files.
  - `doc-tutorial`: tutorials.
  - `doc-howto`: how-to guides and runbooks.
  - `doc-reference`: reference documentation.
  - `doc-explanation`: explanation documents.
  - `doc-adr`: architecture decision records in the MADR 4.0.0 format.
  - `doc-changelog`: changelog entries in the Keep a Changelog 1.1.0 format, and release notes.
  - `doc-code-comments`: code comments, always in English.
  - `doc-ui-text`: user interface text in English or Dutch.
- Three modes in each skill: write, rewrite, and review.
- Language rules in each skill: English in ASD-STE100 Simplified Technical English, Dutch in Begrijpelijk Nederlands (B1).
- Three strictness levels: light, standard (default), and strict.
- `doccheck.py` in each skill: a checker that uses only the Python standard library. It checks sentence length, paragraph length, semicolons, em-dashes, contractions, modal verbs, and likely passive forms.
- `doccheck.py --source <original file>`: compares a rewrite with the original. It reports lost code lines, lost links, and changed heading anchors.
- `doccheck.py --comments <source file>`: checks the comments in source code and gives the line numbers of the source file.
- A fallback for unattended runs: when no human can answer, the skill takes the best signal, continues, and lists each choice as an open question.

[Unreleased]: https://github.com/Charmedme/maikelsoft-skills/compare/dev-docs-v1.1.0...HEAD
[1.1.0]: https://github.com/Charmedme/maikelsoft-skills/compare/dev-docs-v1.0.0...dev-docs-v1.1.0
[1.0.0]: https://github.com/Charmedme/maikelsoft-skills/releases/tag/dev-docs-v1.0.0
