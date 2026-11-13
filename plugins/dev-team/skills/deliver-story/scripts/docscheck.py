#!/usr/bin/env python3
"""docscheck: name the docs of a change before the final gate.

Usage:
    python3 docscheck.py --base <commit> [--ok <doc> ...] [--format text|json]

The script compares the work tree, with new files, against <base>. It takes these names from the change:
  - the name of each changed code file, without its folder and extension (4 or more characters),
  - each command line option in a changed line (--dry-run),
  - each setting name in a changed line (ORDER_LIMIT, Orders:MaxItems),
  - each endpoint path in a changed line ("/api/orders/{id}").

Then it reports:
  DOC-NAMES      a doc that names one of these names, and that the change did not update.
                 Update the doc, or give it with --ok when it needs no change.
  DOC-CHANGELOG  the project has CHANGELOG.md, and the change did not add a line to it.
  DOC-README     the project has no README. Propose one.

A doc is a .md, .rst, .adoc, or .txt file. The script skips docs/work/, the folders of
tools and packages, and the references/ copies of skills.
Exit code 1 means one or more findings. The script needs git. It uses the Python standard library only.
"""
from __future__ import annotations

import argparse
import json
import pathlib
import re
import subprocess
import sys

DOC_EXT = {".md", ".rst", ".adoc", ".txt"}
SKIP_DIRS = {".git", "node_modules", "bin", "obj", "dist", "build", ".venv", "venv", "__pycache__",
             ".claude", ".dev-flow", ".dev-team", "vendor", "references"}
SKIP_PREFIX = ("docs/work/",)
OPTION = re.compile(r"(?<![\w-])(--[a-z][a-z0-9]+(?:-[a-z0-9]+)*)")
SETTING = re.compile(r"\b([A-Z][A-Z0-9]*_[A-Z0-9_]{2,}|[A-Z][A-Za-z0-9]+:[A-Z][A-Za-z0-9]+)\b")
ENDPOINT = re.compile(r"[\"'`](/[a-z][\w\-./{}:]*)[\"'`]")


def git(root: pathlib.Path, *args) -> str:
    p = subprocess.run(["git", *args], cwd=root, capture_output=True, text=True)
    if p.returncode != 0:
        raise SystemExit(f"docscheck: git {' '.join(args)} failed: {p.stderr.strip()}")
    return p.stdout


def is_doc(rel: str) -> bool:
    parts = rel.split("/")
    if any(part in SKIP_DIRS for part in parts[:-1]) or rel.startswith(SKIP_PREFIX):
        return False
    return pathlib.PurePosixPath(rel).suffix.lower() in DOC_EXT


def names_of_change(root: pathlib.Path, base: str, changed: list) -> set:
    found = set()
    for rel in changed:
        if is_doc(rel) or rel.endswith((".json", ".lock")):
            continue
        stem = pathlib.PurePosixPath(rel).stem
        if len(stem) >= 4:
            found.add(stem)
    lines = [line[1:] for line in git(root, "diff", "--unified=0", base, "--").splitlines()
             if line.startswith(("+", "-")) and not line.startswith(("+++", "---"))]
    for rel in git(root, "ls-files", "--others", "--exclude-standard").splitlines():
        if not is_doc(rel):
            lines += (root / rel).read_text(encoding="utf-8", errors="replace").splitlines()
    for line in lines:
        for pattern in (OPTION, SETTING, ENDPOINT):
            found.update(m.group(1) for m in pattern.finditer(line))
    return found


def all_docs(root: pathlib.Path) -> list:
    tracked = git(root, "ls-files", "--cached", "--others", "--exclude-standard").splitlines()
    return sorted(rel for rel in set(tracked) if is_doc(rel) and (root / rel).is_file())


def check(root: pathlib.Path, base: str, ok: set) -> list:
    changed = [line for line in git(root, "diff", "--name-only", base, "--").splitlines() if line]
    changed += git(root, "ls-files", "--others", "--exclude-standard").splitlines()
    changed_set = set(changed)
    docs = all_docs(root)
    findings = []

    names = names_of_change(root, base, changed)
    for rel in docs:
        if rel in changed_set or rel in ok or pathlib.PurePosixPath(rel).name.upper() == "CHANGELOG.MD":
            continue
        text = (root / rel).read_text(encoding="utf-8", errors="replace")
        hits = sorted(n for n in names if re.search(r"(?<![\w-])" + re.escape(n) + r"(?![\w-])", text))
        if hits:
            findings.append({"id": "DOC-NAMES", "doc": rel, "names": hits,
                             "message": f"names {', '.join(hits[:5])}; update it, or give it with --ok"})

    logs = [rel for rel in docs if pathlib.PurePosixPath(rel).name.upper() == "CHANGELOG.MD"]
    for rel in logs:
        if rel not in changed_set and rel not in ok:
            findings.append({"id": "DOC-CHANGELOG", "doc": rel, "names": [],
                             "message": "the change has no line in the changelog"})

    if not any(pathlib.PurePosixPath(rel).name.upper().startswith("README") for rel in docs):
        findings.append({"id": "DOC-README", "doc": "README.md", "names": [],
                         "message": "the project has no README; propose one"})
    return findings


def main(argv=None) -> int:
    ap = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    ap.add_argument("--base", required=True, help="the commit before the change")
    ap.add_argument("--ok", nargs="*", default=[], help="docs that you checked and that need no change")
    ap.add_argument("--format", choices=("text", "json"), default="text")
    args = ap.parse_args(argv)

    root = pathlib.Path(git(pathlib.Path.cwd(), "rev-parse", "--show-toplevel").strip())
    ok = set()
    for name in args.ok:
        p = pathlib.Path(name)
        p = p if p.is_absolute() else pathlib.Path.cwd() / p
        ok.add(p.resolve().relative_to(root.resolve()).as_posix())
    findings = check(root, args.base, ok)
    if args.format == "json":
        print(json.dumps({"findings": findings}, indent=2))
    else:
        for f in findings:
            print(f"{f['id']} {f['doc']}: {f['message']}")
        print(f"docscheck: {len(findings)} findings")
    return 1 if findings else 0


if __name__ == "__main__":
    sys.exit(main())
