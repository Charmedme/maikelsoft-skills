# Changelog

This file records all notable changes to the dev-setup plugin in the maikelsoft-skills marketplace.

The format follows [Keep a Changelog 1.1.0](https://keepachangelog.com/en/1.1.0/). The plugin uses [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [1.0.0] - 2026-11-05

### Added

- `/setup` asks your role (product owner or analyst, designer, developer, tech lead, or all) and keeps it for all your projects or for one repository. Each worktree and each clone of the repository gets the same role.
- `/setup` asks only the facts of the project that the repository does not show. It writes `.claude/rules/delivery.md`, `REVIEW.md`, a pull request template, and a CI check for GitHub Actions or Azure Pipelines. It shows the list first and writes after your approval. It never overwrites a file.
- `/setup route` shows the steps of your route and changes only the steps that you name.
- `/setup starter` makes the Claude Code basics of a project. It makes a short `CLAUDE.md`, `.claude/settings.json`, `.claude/rules/`, and the folders `skills/`, `agents/`, and `hooks/`. It adds a `.claude/README.md` with a short instruction for each part, and two `.gitignore` lines.
- A session-start hook gives each session your role line. In a cloud session, the role comes from the environment variable `DELIVERY_ROLE`.
