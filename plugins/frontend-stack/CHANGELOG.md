# Changelog

This file records all notable changes to the frontend-stack plugin in the maikelsoft-skills marketplace.

The format follows [Keep a Changelog 1.1.0](https://keepachangelog.com/en/1.1.0/). The plugin uses [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [1.0.0] - 2026-11-18

The first version. The skills come from dev-stack 1.3.0.

### Added

- `angular`: moved from dev-stack 1.3.0 without a change to its rules.
- `web-security`: the browser part of `dev-stack:secure-coding`, with the reference `frontend.md` and the tool rules `SC-TS-*` and `SC-NG1-TRUST-HTML`.
- `modern-web`: sends HTML, CSS, and client-side JavaScript work to Modern Web Guidance of Google Chrome. It starts that skill when it is installed, and else gives the install command and uses its `npx` search. It holds no web guidance of its own.

[Unreleased]: https://github.com/Charmedme/maikelsoft-skills/compare/frontend-stack-v1.0.0...HEAD
[1.0.0]: https://github.com/Charmedme/maikelsoft-skills/releases/tag/frontend-stack-v1.0.0
