#!/usr/bin/env python3
"""workcheck: check the work folder of the build skill (docs/work/<item>/).

Usage:
    python3 workcheck.py <work folder> [--final] [--format text|json]

The script checks:
  - RUN-HEADER   run.md starts with the shared header, with an item and a size
                 (patch, small, medium, or large).
  - SPEC-FILE    spec.md exists.
  - CHECKS       spec.md has a section "## Checks" with a table: Part | Check.
  - CHECK-RULE   each rule R<n> of the behaviour spec block in spec.md has a row in the table.
  - CHECK-EMPTY  each row names a check: a test file, or "review only".
  - PLAN         medium and large: plan.md exists and has a section "## Scenarios".
  - PLAN-RULE    medium and large: each rule with a test file is in the section "## Scenarios".
  - With --final, CHECK-FILE: each test file of the table exists in the repository.

A chore or a task has no behaviour spec block. Its table names the parts of the change.
A check is "review only" when no test can check the part, for example a text in a document.
A test file is a path relative to the root of the repository, in backticks or plain.
The script only reports. It does not change a file. Exit code 1 means one or more errors.
It uses the Python standard library only.
"""
import argparse
import json
import pathlib
import re
import subprocess
import sys

SIZES = {"patch", "small", "medium", "large"}
BLOCK = re.compile(r"<!-- BEGIN behaviour spec: [^>]*-->(.*?)<!-- END behaviour spec -->", re.S)
RULE = re.compile(r"^\s*(?:Rule|Regel):\s*(R\d+)\b", re.M)
REVIEW_ONLY = re.compile(r"^review only$", re.I)


def header(run_md: pathlib.Path) -> dict:
    """Read the front matter of run.md. Return {} when it is missing."""
    if not run_md.is_file():
        return {}
    lines = run_md.read_text(encoding="utf-8").splitlines()
    if not lines or lines[0].strip() != "---":
        return {}
    fields = {}
    for line in lines[1:]:
        if line.strip() == "---":
            return fields
        key, _, value = line.partition(":")
        fields[key.strip()] = value.strip()
    return {}


def section(text: str, title: str) -> str:
    """Return the text under '## <title>' up to the next heading of level 1 or 2."""
    m = re.search(rf"^##\s+{re.escape(title)}\s*$(.*?)(?=^#{{1,2}}\s|\Z)", text, re.M | re.S)
    return m.group(1) if m else ""


def table_rows(text: str) -> list:
    """Return (part, check) for each data row of the Markdown table in the text."""
    rows = []
    for line in text.splitlines():
        line = line.strip()
        if not line.startswith("|"):
            continue
        cells = [c.strip() for c in line.strip("|").split("|")]
        if len(cells) < 2 or set(cells[0]) <= set("-: ") or cells[0].lower() == "part":
            continue
        rows.append((cells[0], cells[1]))
    return rows


def test_path(check: str) -> str:
    """Return the file path of a check, or an empty text for 'review only' and an empty check."""
    value = check.strip().strip("`").strip()
    if not value or REVIEW_ONLY.match(value):
        return ""
    return value


def repo_root(folder: pathlib.Path) -> pathlib.Path:
    p = subprocess.run(["git", "-C", str(folder), "rev-parse", "--show-toplevel"], capture_output=True, text=True)
    return pathlib.Path(p.stdout.strip()) if p.returncode == 0 else folder


def check(folder: pathlib.Path, final: bool = False) -> list:
    """Return the errors of the work folder as (code, message)."""
    errors = []
    head = header(folder / "run.md")
    size = head.get("size", "")
    if not head.get("item") or size not in SIZES:
        errors.append(("RUN-HEADER", "run.md needs the header with an item and a size (patch, small, medium, large)"))
    spec_md = folder / "spec.md"
    if not spec_md.is_file():
        return errors + [("SPEC-FILE", "spec.md does not exist")]
    text = spec_md.read_text(encoding="utf-8")
    block = BLOCK.search(text)
    rules = RULE.findall(block.group(1)) if block else []
    rows = table_rows(section(text, "Checks"))
    if not rows:
        errors.append(("CHECKS", "spec.md needs a section '## Checks' with a table: Part | Check, and one row or more"))
    parts = {}
    for part, value in rows:
        rid = re.match(r"(R\d+)\b", part)
        if rid:
            parts.setdefault(rid.group(1), []).append(value)
        if not value.strip().strip("`").strip():
            errors.append(("CHECK-EMPTY", f"the part {part!r} has no check. Name a test file or 'review only'"))
    for rid in rules:
        if rid not in parts:
            errors.append(("CHECK-RULE", f"the rule {rid} has no row in '## Checks'"))
    tested = [rid for rid in rules if any(test_path(v) for v in parts.get(rid, []))]
    if size in ("medium", "large"):
        plan = folder / "plan.md"
        scenarios = section(plan.read_text(encoding="utf-8"), "Scenarios") if plan.is_file() else ""
        if not scenarios.strip():
            errors.append(("PLAN", "plan.md needs a section '## Scenarios' with the order of the rules"))
        else:
            for rid in tested:
                if not re.search(rf"\b{rid}\b", scenarios):
                    errors.append(("PLAN-RULE", f"the rule {rid} is not in '## Scenarios' of plan.md"))
    if final:
        root = repo_root(folder)
        for part, value in rows:
            path = test_path(value)
            if path and not (root / path).exists():
                errors.append(("CHECK-FILE", f"the test file {path} of {part!r} does not exist"))
    return errors


def main(argv=None) -> int:
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("folder", type=pathlib.Path)
    parser.add_argument("--final", action="store_true", help="also check that each test file exists")
    parser.add_argument("--format", choices=("text", "json"), default="text")
    args = parser.parse_args(argv)
    errors = check(args.folder, args.final)
    if args.format == "json":
        print(json.dumps([{"code": c, "message": m} for c, m in errors], indent=2))
    else:
        for code, message in errors:
            print(f"ERROR {code}: {message}")
        print(f"{args.folder}: {len(errors)} error(s)")
    return 1 if errors else 0


if __name__ == "__main__":
    sys.exit(main())
