#!/usr/bin/env bash
# Make a small plugin marketplace repository for the release-plugin cases.
#   make.sh <target folder> <variant>
# variant "entries": [Unreleased] has an Added entry.
# variant "empty":   [Unreleased] is empty; the commits since 1.0.0 hold a feature and a test change.
# The target gets repo/ (the work clone, on branch feat/greeting) and origin.git (a bare remote).
set -euo pipefail
target="$1"; variant="$2"
git_c() { git -c user.name=Dev -c user.email=dev@example.com -c init.defaultBranch=main "$@"; }
mkdir -p "$target"; cd "$target"
git_c init -q --bare origin.git
git_c init -q repo; cd repo
mkdir -p .claude-plugin plugins/hello/.claude-plugin plugins/hello/skills/hello .github/workflows scripts
cat > .claude-plugin/marketplace.json <<'J'
{
  "name": "demo-market",
  "owner": {"name": "Dev"},
  "plugins": [{"name": "hello", "source": "./plugins/hello", "description": "Greets the user."}]
}
J
cat > plugins/hello/.claude-plugin/plugin.json <<'J'
{
  "name": "hello",
  "version": "1.0.0",
  "description": "Greets the user."
}
J
cat > plugins/hello/skills/hello/SKILL.md <<'J'
---
name: hello
description: Greeting. Use when the user wants a greeting.
---
Say hello to the user in English.
J
cat > plugins/hello/README.md <<'J'
# hello

A plugin that greets the user.
J
cat > README.md <<'J'
# demo-market

| Plugin | Version | What it does |
|--------|---------|--------------|
| hello | 1.0.0 | Greets the user. |
J
cat > plugins/hello/CHANGELOG.md <<'J'
# Changelog

## [Unreleased]

## [1.0.0] - 2026-09-01

### Added

- The skill `hello`.

[Unreleased]: https://github.com/dev/demo-market/compare/hello-v1.0.0...HEAD
[1.0.0]: https://github.com/dev/demo-market/releases/tag/hello-v1.0.0
J
cat > scripts/check.py <<'J'
#!/usr/bin/env python3
"""Fail when a Markdown file has a line with TODO."""
import pathlib, sys
bad = [f"{p}:{i}" for p in pathlib.Path(".").rglob("*.md") if ".git" not in p.parts
       for i, line in enumerate(p.read_text().splitlines(), 1) if "TODO" in line]
print("\n".join(bad) or "ok")
sys.exit(1 if bad else 0)
J
cat > .github/workflows/ci.yml <<'J'
name: CI
on:
  pull_request:
jobs:
  checks:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - name: Docs have no TODO
        run: python3 scripts/check.py
J
git_c add -A && git_c commit -qm "hello 1.0.0"
git_c remote add origin ../origin.git && git_c push -q origin main
git_c checkout -q -b feat/greeting
sed -i 's/in English./in English or in Dutch, in the language of the user./' plugins/hello/skills/hello/SKILL.md
echo "TODO: add an example." >> plugins/hello/README.md
git_c add -A && git_c commit -qm "feat(hello): greet in Dutch too"
mkdir -p plugins/hello/evals/dutch
echo "Say hello. Ik spreek Nederlands." > plugins/hello/evals/dutch/prompt.md
git_c add -A && git_c commit -qm "test(hello): add an eval for Dutch"
if [ "$variant" = "entries" ]; then
  python3 - <<'P'
import pathlib
p = pathlib.Path("plugins/hello/CHANGELOG.md")
p.write_text(p.read_text().replace("## [Unreleased]\n", "## [Unreleased]\n\n### Added\n\n- `hello` greets in Dutch when the user writes Dutch.\n", 1))
P
  git_c add -A && git_c commit -qm "docs(hello): changelog entry"
fi
