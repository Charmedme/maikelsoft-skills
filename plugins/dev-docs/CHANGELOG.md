# Changelog

This file records all notable changes to the dev-docs plugin in the maikelsoft-skills marketplace.

The format follows [Keep a Changelog 1.1.0](https://keepachangelog.com/en/1.1.0/). The plugin uses [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

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

[Unreleased]: https://github.com/Charmedme/maikelsoft-skills/compare/dev-docs-v1.0.0...HEAD
[1.0.0]: https://github.com/Charmedme/maikelsoft-skills/releases/tag/dev-docs-v1.0.0
