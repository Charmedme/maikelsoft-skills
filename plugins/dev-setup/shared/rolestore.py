#!/usr/bin/env python3
"""rolestore: keep the delivery role and route of a person, for all projects or for one repository.

Usage:
    python3 rolestore.py [--data DIR] set --role ROLE --scope project|global [--cwd ROOT] [STEP=VALUE ...]
    python3 rolestore.py [--data DIR] route [--scope project|global] [--cwd PATH] STEP=VALUE [STEP=VALUE ...]
    python3 rolestore.py [--data DIR] clear --scope project|global [--cwd ROOT]
    python3 rolestore.py [--data DIR] show [--cwd PATH] [--format text|json]
    python3 rolestore.py key [--cwd PATH]
    python3 rolestore.py hook

The store is roles.json in the data folder of the dev-setup plugin: --data, else the
environment variable CLAUDE_PLUGIN_DATA. It has a "global" entry and a "projects" map.
The key of a project is its git remote (origin, else the first remote) as host/path in
lower case, without the user, the port, and ".git". An Azure DevOps SSH remote gets the
form of its HTTPS remote. Without a remote, the key is the path of the git common folder,
so each worktree of a repository gets the same key. "set" and "clear" with --scope project
need --cwd: the root of the repository of the person, so a "cd" cannot pick another repository.

The role comes from the first of these:
    1. the environment variable DELIVERY_ROLE (a cloud session): "developer" or "developer; team=on"
    2. the entry of the project
    3. the global entry

Roles: product owner, designer, developer, tech lead, all. Steps of the route and their
values: refine, docs, team, approve-at-ready, designer-review; each on, off, or ask. "set"
gives the route the defaults of the role, and then the steps that you give.

"hook" is the SessionStart hook. It reads the folder from the JSON on stdin and prints the
role line as additional context, or nothing when no role is set. It never blocks a session.
Python 3.9 or later, standard library only.
"""
from __future__ import annotations

import argparse
import json
import os
import pathlib
import re
import subprocess
import sys

ROLES = ("product owner", "designer", "developer", "tech lead", "all")
VALUES = ("on", "off", "ask")
BASE = {"refine": "on", "docs": "on", "team": "ask", "approve-at-ready": "off", "designer-review": "off"}
DEFAULTS = {
    "product owner": dict(BASE),
    "designer": dict(BASE, **{"designer-review": "on"}),
    "developer": dict(BASE),
    "tech lead": dict(BASE, team="on"),
    "all": dict(BASE, **{"approve-at-ready": "on"}),
}
ADO_SSH = re.compile(r"^ssh\.dev\.azure\.com/v3/([^/]+)/([^/]+)/(.+)$")


def git(cwd: pathlib.Path, *args) -> str:
    try:
        p = subprocess.run(["git", *args], cwd=cwd, capture_output=True, text=True, timeout=5)
    except (OSError, subprocess.SubprocessError):
        return ""
    return p.stdout.strip() if p.returncode == 0 else ""


def normalize(url: str) -> str:
    """Make one key from each form of a remote URL."""
    u = url.strip()
    u = re.sub(r"^[a-z+]+://", "", u)          # scheme
    u = re.sub(r"^[^@/]+@", "", u)             # user (and password)
    if re.match(r"^[^/:]+:(?!\d+/)", u):       # scp form host:path
        u = u.replace(":", "/", 1)
    u = re.sub(r"^([^/:]+):\d+/", r"\1/", u)   # port
    u = u.rstrip("/")
    if u.endswith(".git"):
        u = u[:-4]
    u = u.lower()
    m = ADO_SSH.match(u)
    if m:
        u = f"dev.azure.com/{m.group(1)}/{m.group(2)}/_git/{m.group(3)}"
    return u


def project_key(cwd: pathlib.Path):
    remotes = git(cwd, "remote").split()
    if remotes:
        name = "origin" if "origin" in remotes else remotes[0]
        url = git(cwd, "remote", "get-url", name)
        if url:
            return normalize(url)
    common = git(cwd, "rev-parse", "--path-format=absolute", "--git-common-dir")
    return str(pathlib.Path(common).resolve()) if common else None


def store_path(data) -> pathlib.Path:
    folder = data or os.environ.get("CLAUDE_PLUGIN_DATA")
    if not folder:
        raise SystemExit("rolestore: give --data or set CLAUDE_PLUGIN_DATA")
    return pathlib.Path(folder) / "roles.json"


def load(path: pathlib.Path) -> dict:
    try:
        data = json.loads(path.read_text(encoding="utf-8"))
    except (OSError, ValueError):
        data = {}
    data.setdefault("global", None)
    data.setdefault("projects", {})
    return data


def save(path: pathlib.Path, data: dict) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(data, indent=2, sort_keys=True) + "\n", encoding="utf-8")


def parse_steps(pairs) -> dict:
    steps = {}
    for pair in pairs:
        step, _, value = pair.partition("=")
        step, value = step.strip(), value.strip()
        if step not in BASE or value not in VALUES:
            raise SystemExit(f"rolestore: '{pair}' is not a step: use {', '.join(BASE)} = {', '.join(VALUES)}")
        steps[step] = value
    return steps


def from_env(value: str):
    role, _, rest = value.partition(";")
    role = role.strip().lower()
    if role not in ROLES:
        return None
    pairs = [p for p in re.split(r"[,\s]+", rest) if "=" in p]
    try:
        route = dict(DEFAULTS[role], **parse_steps(pairs))
    except SystemExit:
        route = dict(DEFAULTS[role])
    return {"role": role, "route": route}


def resolve(data: dict, key):
    """Return (entry, scope) from the first source that has a role, or (None, None)."""
    env = os.environ.get("DELIVERY_ROLE")
    if env:
        entry = from_env(env)
        if entry:
            return entry, "cloud"
    if key and key in data["projects"]:
        return data["projects"][key], "project"
    if data.get("global"):
        return data["global"], "global"
    return None, None


def line(entry: dict, scope: str) -> str:
    route = dict(DEFAULTS[entry["role"]], **entry.get("route", {}))
    steps = ", ".join(f"{k}={route[k]}" for k in BASE)
    return f"Delivery role: {entry['role']}. Scope: {scope}. Route: {steps}."


def hook() -> int:
    try:
        event = json.loads(sys.stdin.read() or "{}")
    except ValueError:
        event = {}
    cwd = pathlib.Path(event.get("cwd") or os.environ.get("CLAUDE_PROJECT_DIR") or os.getcwd())
    data = load(store_path(None)) if os.environ.get("CLAUDE_PLUGIN_DATA") else {"global": None, "projects": {}}
    entry, scope = resolve(data, project_key(cwd))
    if entry:
        print(json.dumps({"hookSpecificOutput": {"hookEventName": "SessionStart", "additionalContext": line(entry, scope)}}))
    return 0


def main(argv=None) -> int:
    ap = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    ap.add_argument("--data", help="the data folder of the plugin (default: CLAUDE_PLUGIN_DATA)")
    sub = ap.add_subparsers(dest="cmd", required=True)
    s = sub.add_parser("set")
    s.add_argument("--role", required=True, choices=ROLES)
    s.add_argument("--scope", required=True, choices=("project", "global"))
    s.add_argument("--cwd")
    s.add_argument("steps", nargs="*")
    r = sub.add_parser("route")
    r.add_argument("--scope", choices=("project", "global"))
    r.add_argument("--cwd", default=".")
    r.add_argument("steps", nargs="+")
    c = sub.add_parser("clear")
    c.add_argument("--scope", required=True, choices=("project", "global"))
    c.add_argument("--cwd")
    sh = sub.add_parser("show")
    sh.add_argument("--cwd", default=".")
    sh.add_argument("--format", choices=("text", "json"), default="text")
    k = sub.add_parser("key")
    k.add_argument("--cwd", default=".")
    sub.add_parser("hook")
    args = ap.parse_args(argv)

    if args.cmd == "hook":
        return hook()
    if args.cmd in ("set", "clear") and args.scope == "project" and not args.cwd:
        print("rolestore: give --cwd <root of the repository> with --scope project", file=sys.stderr)
        return 2
    cwd = pathlib.Path(args.cwd or ".").resolve()
    key = project_key(cwd)
    if args.cmd == "key":
        print(key or "")
        return 0 if key else 1

    path = store_path(args.data)
    data = load(path)
    if args.cmd in ("set", "clear", "route") and getattr(args, "scope", None) == "project" and not key:
        print("rolestore: this folder is not in a git repository; use --scope global", file=sys.stderr)
        return 2

    if args.cmd == "set":
        entry = {"role": args.role, "route": dict(DEFAULTS[args.role], **parse_steps(args.steps))}
        if args.scope == "project":
            data["projects"][key] = entry
        else:
            data["global"] = entry
        save(path, data)
        print(line(entry, args.scope))
        if args.scope == "project":
            print(f"Project key: {key}")
        return 0

    if args.cmd == "route":
        scope = args.scope or ("project" if key in data["projects"] else "global")
        entry = data["projects"].get(key) if scope == "project" else data["global"]
        if not entry:
            print(f"rolestore: no {scope} role is set; run set first", file=sys.stderr)
            return 2
        route = dict(DEFAULTS[entry["role"]])
        route.update(entry.get("route", {}))
        route.update(parse_steps(args.steps))
        entry["route"] = route
        save(path, data)
        print(line(entry, scope))
        if scope == "project":
            print(f"Project key: {key}")
        return 0

    if args.cmd == "clear":
        if args.scope == "project":
            data["projects"].pop(key, None)
        else:
            data["global"] = None
        save(path, data)
        print(f"rolestore: the {args.scope} role is removed")
        return 0

    entry, scope = resolve(data, key)
    if args.format == "json":
        print(json.dumps({"key": key, "scope": scope, "entry": entry, "store": str(path)}, indent=2))
    else:
        print(line(entry, scope) if entry else "No delivery role is set.")
        print(f"Project key: {key or '-'}. Store: {path}")
    return 0


if __name__ == "__main__":
    if sys.argv[1:2] == ["hook"]:
        try:
            sys.exit(hook())
        except Exception:
            sys.exit(0)
    sys.exit(main())
