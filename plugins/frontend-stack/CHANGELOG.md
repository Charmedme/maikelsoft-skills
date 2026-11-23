# Changelog

This file records all notable changes to the frontend-stack plugin in the maikelsoft-skills marketplace.

The format follows [Keep a Changelog 1.1.0](https://keepachangelog.com/en/1.1.0/). The plugin uses [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [1.1.1] - 2026-11-21

### Changed

- The description of `angular` sends end-to-end tests with Playwright to `test-stack:playwright`.

## [1.1.0] - 2026-11-19

### Added

- `webmcp` (preview): tools for browser agents with WebMCP. Covers the feature check, `document.modelContext.registerTool` with an `AbortSignal`, and tool design and hints. Also a confirmation that the agent cannot complete itself, the built-in API of Angular 22, and the W3C polyfill on request. The API is a Community Group draft and can change.

### Changed

- `angular` sends browser security to `frontend-stack:web-security`, and tools for browser agents to `webmcp`.

## [1.0.0] - 2026-11-18

The first version. The skills come from dev-stack 1.3.0.

### Added

- `angular`: moved from dev-stack 1.3.0 without a change to its rules.
- `web-security`: the browser part of `dev-stack:secure-coding`, with the reference `frontend.md` and the tool rules `SC-TS-*` and `SC-NG1-TRUST-HTML`.
- `modern-web`: sends HTML, CSS, and client-side JavaScript work to Modern Web Guidance of Google Chrome. It starts that skill when it is installed, and else gives the install command and uses its `npx` search. It holds no web guidance of its own.

[Unreleased]: https://github.com/Charmedme/maikelsoft-skills/compare/frontend-stack-v1.1.1...HEAD
[1.1.1]: https://github.com/Charmedme/maikelsoft-skills/compare/frontend-stack-v1.1.0...frontend-stack-v1.1.1
[1.1.0]: https://github.com/Charmedme/maikelsoft-skills/compare/frontend-stack-v1.0.0...frontend-stack-v1.1.0
[1.0.0]: https://github.com/Charmedme/maikelsoft-skills/releases/tag/frontend-stack-v1.0.0
