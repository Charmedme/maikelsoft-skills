#!/usr/bin/env python3
"""runheader: read, write, and check the shared header of run.md.

Usage:
    python3 runheader.py read <run.md> [--format text|json]
    python3 runheader.py write <run.md> --flow solo|team --item KEY --size SIZE --step STEP [--baseline VALUE]
    python3 runheader.py adopt <work folder> [--format text|json]

dev-flow and dev-team both write run.md in docs/work/<item>/. The file starts with one
header in YAML front matter. Each flow keeps its own body below the header.

    ---
    flow: solo
    item: SHOP-12
    size: medium
    step: plan gate
    baseline: set
    ---

Fields:
    flow      solo (dev-flow) or team (dev-team).
    item      the ticket ID, or the slug of a patch without a ticket.
    size      patch, small, medium, or large.
    step      the step of the run, in free text.
    baseline  none (no spec copy yet), set (spec-baseline.json holds the fingerprints),
              or legacy (a run from before the header).

"read" exits 1 when the header is missing or has an error.
"write" makes the header or changes its fields. It keeps the body.
"adopt" gives a header to an old run: a run.md without a header, or a folder with
plan.md, examples.md, or story.md and no run.md. The run keeps its locks.
The script uses the Python standard library only.
"""
from __future__ import annotations

import argparse
import json
import pathlib
import re
import sys

FIELDS = ("flow", "item", "size", "step", "baseline")
ALLOWED = {
    "flow": ("solo", "team"),
    "size": ("patch", "small", "medium", "large"),
    "baseline": ("none", "set", "legacy"),
}
FRONT = re.compile(r"\A---\n(.*?)\n---\n?", re.DOTALL)
TIER = re.compile(r"^\s*Tier:\s*(patch|small|medium|large)\b", re.I | re.M)
OLD_FILES = ("plan.md", "examples.md", "story.md")


def split(text: str):
    """Return the header fields (or None) and the body."""
    m = FRONT.match(text)
    if not m:
        return None, text
    fields = {}
    for line in m.group(1).splitlines():
        if ":" in line:
            key, value = line.split(":", 1)
            fields[key.strip()] = value.strip()
    return fields, text[m.end():]


def errors(fields) -> list:
    if fields is None:
        return ["run.md has no header"]
    out = []
    for key in FIELDS:
        value = fields.get(key, "")
        if not value:
            out.append(f"field '{key}' is missing")
        elif key in ALLOWED and value not in ALLOWED[key]:
            out.append(f"field '{key}' is '{value}'; use {', '.join(ALLOWED[key])}")
    for key in fields:
        if key not in FIELDS:
            out.append(f"field '{key}' is not a header field")
    return out


def render(fields: dict, body: str) -> str:
    head = "\n".join(f"{k}: {fields[k]}" for k in FIELDS)
    return f"---\n{head}\n---\n{body}"


def guess(folder: pathlib.Path, run_text: str) -> dict:
    """Make header fields for an old run from what is in the folder."""
    team = (folder / "story.md").is_file() or "Mode:" in run_text
    size = "medium"
    plan = folder / "plan.md"
    if plan.is_file():
        m = TIER.search(plan.read_text(encoding="utf-8"))
        if m:
            size = m.group(1).lower()
    if team:
        size = "large"
    return {"flow": "team" if team else "solo", "item": folder.name, "size": size,
            "step": "adopted from an old run", "baseline": "legacy"}


def adopt(folder: pathlib.Path) -> dict:
    run = folder / "run.md"
    if run.is_file():
        text = run.read_text(encoding="utf-8")
        fields, body = split(text)
        if fields is not None:
            return {"action": "none", "reason": "run.md has a header", "fields": fields}
        fields = guess(folder, text)
        run.write_text(render(fields, text), encoding="utf-8")
        return {"action": "header added", "fields": fields}
    if any((folder / name).is_file() for name in OLD_FILES):
        fields = guess(folder, "")
        run.write_text(render(fields, "\n# Run\n\nAdopted from an old run. The locks in .dev-flow/spec-lock.json stay.\n"),
                       encoding="utf-8")
        return {"action": "run.md made", "fields": fields}
    return {"action": "none", "reason": "no old run in this folder", "fields": {}}


def main(argv=None) -> int:
    ap = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    sub = ap.add_subparsers(dest="cmd", required=True)
    r = sub.add_parser("read")
    r.add_argument("file")
    r.add_argument("--format", choices=("text", "json"), default="text")
    w = sub.add_parser("write")
    w.add_argument("file")
    for key in FIELDS:
        w.add_argument(f"--{key}", choices=ALLOWED.get(key))
    a = sub.add_parser("adopt")
    a.add_argument("folder")
    a.add_argument("--format", choices=("text", "json"), default="text")
    args = ap.parse_args(argv)

    if args.cmd == "adopt":
        folder = pathlib.Path(args.folder)
        if not folder.is_dir():
            print(f"runheader: {folder} is not a folder", file=sys.stderr)
            return 2
        result = adopt(folder)
        if args.format == "json":
            print(json.dumps(result, indent=2))
        else:
            print(f"runheader: {result['action']}" + (f" ({result['reason']})" if "reason" in result else ""))
        return 0

    path = pathlib.Path(args.file)
    if args.cmd == "read":
        if not path.is_file():
            print(f"runheader: {path} does not exist", file=sys.stderr)
            return 2
        fields, _ = split(path.read_text(encoding="utf-8"))
        found = errors(fields)
        if args.format == "json":
            print(json.dumps({"fields": fields or {}, "errors": found}, indent=2))
        else:
            for key in FIELDS:
                if fields and key in fields:
                    print(f"{key}: {fields[key]}")
            for e in found:
                print(f"ERROR {path}: {e}")
        return 1 if found else 0

    text = path.read_text(encoding="utf-8") if path.is_file() else "\n# Run\n"
    fields, body = split(text)
    fields = dict(fields or {})
    fields.setdefault("baseline", "none")
    for key in FIELDS:
        value = getattr(args, key)
        if value:
            fields[key] = value
    found = errors(fields)
    if found:
        for e in found:
            print(f"ERROR {path}: {e}", file=sys.stderr)
        return 1
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(render(fields, body), encoding="utf-8")
    print(f"runheader: {path} has the header")
    return 0


if __name__ == "__main__":
    sys.exit(main())
