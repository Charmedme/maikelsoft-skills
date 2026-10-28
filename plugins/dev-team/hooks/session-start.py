#!/usr/bin/env python3
"""SessionStart hook of the dev-team plugin: name the entry skill for each kind of development work.

The hook writes only in a project that uses dev-team: the project has docs/dev-team/conventions.md or
docs/dev-team/decisions.md. In any other project it does nothing. It reads the working folder from the
JSON on stdin (field "cwd"), else from CLAUDE_PROJECT_DIR, else from the current folder.

The text goes to the session as additional context. The hook never blocks the session: on any error it
exits 0 without output.
Python 3.9 or later, standard library only.
"""
import json
import os
import pathlib
import sys

MARKERS = ("docs/dev-team/conventions.md", "docs/dev-team/decisions.md")

TEXT = """This project uses the dev-team plugin. Do development work with its skills. Do not build inline.

- A large or risky story: skill dev-team:deliver-story.
- A small or medium change: skill dev-flow:plan-and-build, when it is installed.
- The comments of a pull request review: skill dev-team:process-review.
- Before you start, read docs/dev-team/conventions.md and docs/dev-team/decisions.md.
- Use the build and test commands of conventions.md, also the steps that get past an SDK pin."""


def project_dir():
    try:
        data = json.loads(sys.stdin.read() or "{}")
    except ValueError:
        data = {}
    folder = data.get("cwd") if isinstance(data, dict) else None
    return pathlib.Path(folder or os.environ.get("CLAUDE_PROJECT_DIR") or os.getcwd())


def main():
    root = project_dir()
    if not any((root / marker).is_file() for marker in MARKERS):
        return 0
    print(json.dumps({"hookSpecificOutput": {"hookEventName": "SessionStart", "additionalContext": TEXT}}))
    return 0


if __name__ == "__main__":
    try:
        sys.exit(main())
    except Exception:
        sys.exit(0)
