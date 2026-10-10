# Changelog

This file records all notable changes to the dev-build plugin in the maikelsoft-skills marketplace.

The format follows [Keep a Changelog 1.1.0](https://keepachangelog.com/en/1.1.0/). The plugin uses [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [1.0.0] - 2026-10-10

### Added

- First release of the dev-build plugin for Claude Code.
- The skill `build-skill`. It creates, improves, or reviews an agent skill in four steps: research, concept, test, and build. It has a gate that checks if a skill is the right form, and a test method with a results table.
- The skill `build-agent`. It creates, improves, or reviews an agent. It starts from a role spec and renders it for Claude Code, the Agent SDK, Managed Agents, or OpenClaw.
- `skillcheck.py`: a checker for a skill folder. It checks the name, the description, the length, the links, the references, the frontmatter fields, and the style.
- `agentcheck.py`: a checker for an agent file or an OpenClaw workspace.
- Rules with IDs (W, P, and A rules) so that a review can cite them.
- The shared language rules: English in ASD-STE100 Simplified Technical English, and Dutch in Begrijpelijk Nederlands (B1).
- An eval set for both skills, and the results of the test rounds against a plain agent.

[Unreleased]: https://github.com/Charmedme/maikelsoft-skills/compare/dev-build-v1.0.0...HEAD
[1.0.0]: https://github.com/Charmedme/maikelsoft-skills/releases/tag/dev-build-v1.0.0
