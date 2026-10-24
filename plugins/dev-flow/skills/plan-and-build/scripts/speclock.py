#!/usr/bin/env python3
"""speclock: lock the approved acceptance tests of the plan-and-build skill.

Usage:
    python3 speclock.py approve --item KEY --by NAME FILE [FILE ...] [--watch FILE ...]
    python3 speclock.py check [--format text|json]
    python3 speclock.py unlock --by NAME FILE [FILE ...]
    python3 speclock.py list

The lock file is .dev-flow/spec-lock.json at the root of the git repository. It holds
the SHA-256 of each approved file, the item, who approved it, and when. Commit it.

Locked files (the test cases) must not change. Watched files (the DSL) can grow: check lists
each change, and the review reads it. check exits 1 only when a locked file is changed or deleted. The PreToolUse hook of the
plugin (hooks/guard.py) reads the same file and blocks an edit of a locked file.
The script uses the Python standard library only.
"""
from __future__ import annotations

import argparse
import datetime
import hashlib
import json
import pathlib
import subprocess
import sys

LOCK = pathlib.Path(".dev-flow") / "spec-lock.json"


def repo_root() -> pathlib.Path:
    p = subprocess.run(["git", "rev-parse", "--show-toplevel"], capture_output=True, text=True)
    return pathlib.Path(p.stdout.strip()) if p.returncode == 0 else pathlib.Path.cwd()


def sha(path: pathlib.Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def load(root: pathlib.Path) -> dict:
    f = root / LOCK
    if f.is_file():
        return json.loads(f.read_text(encoding="utf-8"))
    return {"files": {}, "log": []}


def save(root: pathlib.Path, data: dict) -> None:
    f = root / LOCK
    f.parent.mkdir(parents=True, exist_ok=True)
    f.write_text(json.dumps(data, indent=2, sort_keys=True) + "\n", encoding="utf-8")


def rel(root: pathlib.Path, name: str) -> str:
    p = pathlib.Path(name)
    p = p if p.is_absolute() else (pathlib.Path.cwd() / p)
    return p.resolve().relative_to(root.resolve()).as_posix()


def now() -> str:
    return datetime.datetime.now(datetime.timezone.utc).strftime("%Y-%m-%dT%H:%MZ")


def changes(root: pathlib.Path, data: dict) -> list:
    out = []
    for name, entry in sorted(data["files"].items()):
        path = root / name
        if not path.is_file():
            out.append({"file": name, "change": "deleted"})
        elif sha(path) != entry["sha256"]:
            out.append({"file": name, "change": "changed"})
    return out


def main(argv=None) -> int:
    ap = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    sub = ap.add_subparsers(dest="cmd", required=True)
    a = sub.add_parser("approve")
    a.add_argument("--item", required=True)
    a.add_argument("--by", required=True)
    a.add_argument("files", nargs="+")
    a.add_argument("--watch", nargs="*", default=[])
    c = sub.add_parser("check")
    c.add_argument("--format", choices=("text", "json"), default="text")
    u = sub.add_parser("unlock")
    u.add_argument("--by", required=True)
    u.add_argument("files", nargs="+")
    sub.add_parser("list")
    args = ap.parse_args(argv)

    root = repo_root()
    data = load(root)

    if args.cmd == "approve":
        for name in args.files:
            r = rel(root, name)
            path = root / r
            if not path.is_file():
                print(f"speclock: {r} does not exist", file=sys.stderr)
                return 2
            data["files"][r] = {"sha256": sha(path), "item": args.item, "by": args.by, "date": now()}
            data["log"].append({"action": "approve", "file": r, "item": args.item, "by": args.by, "date": now()})
        for name in args.watch:
            r = rel(root, name)
            if (root / r).is_file():
                data.setdefault("watched", {})[r] = {"sha256": sha(root / r), "item": args.item, "date": now()}
        save(root, data)
        print(f"speclock: {len(args.files)} files locked, {len(args.watch)} watched, in {LOCK}")
        return 0

    if args.cmd == "unlock":
        for name in args.files:
            r = rel(root, name)
            if data["files"].pop(r, None) is None:
                print(f"speclock: {r} is not locked", file=sys.stderr)
                return 2
            data["log"].append({"action": "unlock", "file": r, "by": args.by, "date": now()})
        save(root, data)
        print(f"speclock: {len(args.files)} files unlocked")
        return 0

    if args.cmd == "list":
        for name, e in sorted(data["files"].items()):
            print(f"{name}  {e['item']}  {e['by']}  {e['date']}")
        return 0

    found = changes(root, data)
    watched = changes(root, {"files": data.get("watched", {})})
    if args.format == "json":
        print(json.dumps({"locked": len(data["files"]), "changes": found, "watched_changes": watched}, indent=2))
    else:
        for f in found:
            print(f"ERROR {f['file']}: {f['change']} after approval")
        for f in watched:
            print(f"WATCH {f['file']}: {f['change']} after approval. The review must check that no check got weaker")
        print(f"speclock: {len(data['files'])} locked files, {len(found)} changes")
    return 1 if found else 0


if __name__ == "__main__":
    sys.exit(main())
