#!/usr/bin/env python3
"""SessionStart hook of the dev-team plugin: give the rules of the team, and the route when dev-setup does not.

The hook writes only in a project that uses dev-team: the project has docs/dev-team/conventions.md or
docs/dev-team/decisions.md. In any other project it does nothing. It reads the working folder from the
JSON on stdin (field "cwd"), else from CLAUDE_PROJECT_DIR, else from the current folder.

The rules always go to the session. The route (which skill starts which kind of work) goes to the
session unless the hook of dev-setup surely gives the route line. That is true only when all of these
are true:
    - plugins/installed_plugins.json has dev-setup with the scope "user", or with a projectPath that is
      the working folder or a folder above it,
    - the settings enable it: "enabledPlugins" has true for it in the project settings
      (.claude/settings.local.json, then .claude/settings.json) or else in the user settings,
    - a role is set: the environment variable DELIVERY_ROLE, or a "global" entry in roles.json of the
      data folder of dev-setup. A role only for some projects counts as not sure.
The config folder is CLAUDE_CONFIG_DIR, else ~/.claude. When the hook is not sure, it gives the route.

The text goes to the session as additional context. The hook never blocks the session: on any error it
exits 0 without output.
Python 3.9 or later, standard library only.
"""
import json
import os
import pathlib
import sys

MARKERS = ("docs/dev-team/conventions.md", "docs/dev-team/decisions.md")

RULES = """This project uses the dev-team plugin. Do development work with the skills of dev-flow and dev-team. Do not build inline.

- Before you start, read docs/dev-team/conventions.md and docs/dev-team/decisions.md.
- Use the build and test commands of conventions.md, also the steps that get past an SDK pin."""

ROUTE = """
- An item to build: skill dev-flow:build. It sets the size, and it gives a large item with a risk to the team (dev-team:deliver-story).
- A team run to continue (the header of docs/work/<item>/run.md says flow: team): skill dev-team:deliver-story.
- The comments of a pull request review: skill dev-flow:review-comments. It gives a team run to dev-team:process-review."""


def project_dir():
    try:
        data = json.loads(sys.stdin.read() or "{}")
    except ValueError:
        data = {}
    folder = data.get("cwd") if isinstance(data, dict) else None
    return pathlib.Path(folder or os.environ.get("CLAUDE_PROJECT_DIR") or os.getcwd())


def read_json(path):
    """The JSON object in the file, or None when the file is missing or bad."""
    try:
        data = json.loads(pathlib.Path(path).read_text(encoding="utf-8"))
    except (OSError, ValueError):
        return None
    return data if isinstance(data, dict) else None


def config_dir():
    return pathlib.Path(os.environ.get("CLAUDE_CONFIG_DIR") or pathlib.Path.home() / ".claude")


def same_or_above(path, root):
    try:
        root.resolve().relative_to(pathlib.Path(path).resolve())
        return True
    except (ValueError, OSError):
        return False


def installed_names(root):
    """The full names (name@marketplace) of the dev-setup installs that apply to this folder."""
    data = read_json(config_dir() / "plugins" / "installed_plugins.json") or {}
    plugins = data.get("plugins", {})
    names = []
    for name, entries in (plugins.items() if isinstance(plugins, dict) else []):
        if str(name).split("@")[0] != "dev-setup":
            continue
        for entry in entries if isinstance(entries, list) else []:
            if not isinstance(entry, dict):
                continue
            if entry.get("scope") == "user" or (entry.get("projectPath") and same_or_above(entry["projectPath"], root)):
                names.append(str(name))
                break
    return names


def enabled(name, root):
    """The value of enabledPlugins for the plugin: the project settings first, then the user settings."""
    for path in (root / ".claude" / "settings.local.json", root / ".claude" / "settings.json",
                 config_dir() / "settings.json"):
        settings = read_json(path) or {}
        value = (settings.get("enabledPlugins") or {}).get(name) if isinstance(settings.get("enabledPlugins"), dict) else None
        if value is not None:
            return value is True
    return False


def role_set(name):
    """True when dev-setup surely prints a role line in each project."""
    if os.environ.get("DELIVERY_ROLE"):
        return True
    folder = name.replace("@", "-")
    store = read_json(config_dir() / "plugins" / "data" / folder / "roles.json") or {}
    return bool(store.get("global"))


def dev_setup_gives_route(root):
    return any(enabled(name, root) and role_set(name) for name in installed_names(root))


def main():
    root = project_dir()
    if not any((root / marker).is_file() for marker in MARKERS):
        return 0
    text = RULES if dev_setup_gives_route(root) else RULES + ROUTE
    print(json.dumps({"hookSpecificOutput": {"hookEventName": "SessionStart", "additionalContext": text}}))
    return 0


if __name__ == "__main__":
    try:
        sys.exit(main())
    except Exception:
        sys.exit(0)
