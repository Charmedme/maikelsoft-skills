# Changelog

This file records all notable changes to the product-flow plugin in the maikelsoft-skills marketplace.

The format follows [Keep a Changelog 1.1.0](https://keepachangelog.com/en/1.1.0/). The plugin uses [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [1.0.0] - 2026-10-12

### Added

- First release of the product-flow plugin for Claude Code, for product owners and business analysts.
- The skill `idea-to-backlog`. It takes a request to approved backlog items in six phases: scan and research, grill, prototype, spec, backlog, and push. Four gates where the human decides.
- One context file for each run (`backlog-<slug>.md`), so that a later run can continue.
- The skills `backlog-story`, `backlog-feature`, `backlog-epic`, and `backlog-goal`. Each writes, rewrites, and reviews its item type. `backlog-story` also splits stories.
- Levels: 2 by default (Feature, User Story), 3 on request (Epic, Feature, User Story). Profiles: Agile, Scrum, and SAFe.
- The skill `product-prototype`. It builds a throwaway clickable prototype with the design tokens, design system, or branding that it finds.
- The skill `backlog-push`. It puts the items into Jira, Azure DevOps, GitHub, GitLab, or Linear, or writes them to files. It first does a duplicate check and gets the approval of the human. A push log lets a stopped push continue.
- The checkers `backlogcheck.py`, `backlogtree.py`, and `protocheck.py`.
- The shared language rules: English in ASD-STE100 Simplified Technical English, and Dutch in Begrijpelijk Nederlands (B1).
- Test results for each skill against a baseline without the skill.

[Unreleased]: https://github.com/Charmedme/maikelsoft-skills/compare/product-flow-v1.0.0...HEAD
[1.0.0]: https://github.com/Charmedme/maikelsoft-skills/releases/tag/product-flow-v1.0.0
