#!/usr/bin/env python3
"""Check the team decisions of a repository: the index, the ADRs, and the code that uses a decision.

    python3 decisioncheck.py                                  # current folder
    python3 decisioncheck.py --root <repo>
    python3 decisioncheck.py --rules <dev-stack>/skills/*/rules.json --changed main
    python3 decisioncheck.py --format json

Index: docs/dev-team/decisions.md, a table with the columns
    ID | Question | Answer | Status | By | ADR | Check | Date
Status is one of: accepted, assumed, superseded, rejected.

What it checks:
    DEC-INDEX       the index exists and has the columns above.
    DEC-STATUS      each row has a valid status.
    DEC-DUPLICATE   an ID has at most one row with the status accepted or assumed. IDs compare
                    without case, spaces, and dash differences (SHP-15, shp 15, SHP\u201115 are one ID).
    DEC-ADR-DUPLICATE  two accepted ADR files carry the same 'Decision ID:' line.
    DEC-ADR         each accepted or superseded row links an ADR file that exists.
    DEC-ADR-STATUS  the status in the ADR front matter matches the row.
    DEC-SUPERSEDE   a superseded ADR links the new ADR, and the new ADR links back.
    DEC-USED        code that a detect rule ties to a decision has an accepted or assumed row.
                    It skips test code, the run files in docs/work, and the 'exclude' paths of the rule.
                    With --changed, only the lines that the branch added are read. Old code in a
                    touched file is not a finding.

Output: one line per finding, `<file>: <RULE> <message>`, sorted by rule (the rules about the
index first, DEC-USED last), then a summary line with the count for each rule.
Exit code: 1 when there is a finding, else 0.

Python 3.9 or later, standard library only.
"""
import argparse
import fnmatch
import json
import pathlib
import re
import subprocess
import sys

INDEX = pathlib.Path("docs/dev-team/decisions.md")
COLUMNS = ["id", "question", "answer", "status", "by", "adr", "check", "date"]
VALID = {"accepted", "assumed", "superseded", "rejected"}
LINK = re.compile(r"\]\(([^)]+)\)|`([^`]+\.md)`|^([\w./-]+\.md)$")
FRONT = re.compile(r"\A---\n(.*?)\n---", re.DOTALL)
ORDER = ["DEC-INDEX", "DEC-STATUS", "DEC-DUPLICATE", "DEC-ADR-DUPLICATE", "DEC-ADR", "DEC-ADR-STATUS",
         "DEC-SUPERSEDE", "DEC-USED"]
DASHES = re.compile(r"[\u2010-\u2015\u2212_\s]+")
DECISION_ID_LINE = re.compile(r"^Decision ID:\s*([A-Za-z0-9][\w\u2010-\u2015\u2212 -]*?)\s*[.,;]?(?:\s|$)", re.MULTILINE)
SKIP_DIRS = {".git", "bin", "obj", "node_modules", "dist", ".angular", "TestResults"}
# DEC-USED reads only product code. The run files of dev-team (docs/work) and test code do not
# carry a team decision, so a detect rule that matches them gives a false finding.
NOT_PRODUCT = ["docs/work/**", "tests/**", "test/**", "**/*Tests/**", "**/*.Tests/**", "**/*Test/**",
               "**/*.spec.ts", "**/*.test.ts", "**/__tests__/**"]


def norm_id(value):
    """One spelling of a decision ID: no case, space, underscore, or dash differences."""
    return DASHES.sub("-", value.strip()).upper()


def table(text):
    rows, header = [], None
    for line in text.splitlines():
        s = line.strip()
        if not s.startswith("|"):
            if header:
                break
            continue
        cells = [c.strip() for c in s.strip("|").split("|")]
        if header is None:
            header = [c.lower() for c in cells]
            continue
        if set("".join(cells)) <= set("-: "):
            continue
        rows.append(dict(zip(header, cells)))
    return header, rows


def link_of(cell, index_dir):
    m = LINK.search(cell.strip())
    if not m:
        return None
    target = next(g for g in m.groups() if g)
    return (index_dir / target).resolve()


def adr_status(path):
    m = FRONT.search(path.read_text(encoding="utf-8"))
    if not m:
        return None
    for line in m.group(1).splitlines():
        if line.lower().startswith("status:"):
            return line.split(":", 1)[1].strip().strip("'\"").lower()
    return None


def merge_base_of(root, base):
    try:
        return subprocess.run(["git", "merge-base", base, "HEAD"], cwd=root, capture_output=True,
                              text=True, check=True).stdout.strip()
    except (subprocess.CalledProcessError, FileNotFoundError):
        return None


def changed_files(root, base):
    merge_base = merge_base_of(root, base)
    if merge_base is None:
        return None
    try:
        out = subprocess.run(["git", "diff", "--name-only", "--diff-filter=AMR", merge_base], cwd=root,
                             capture_output=True, text=True, check=True).stdout
    except (subprocess.CalledProcessError, FileNotFoundError):
        return None
    return {line.strip() for line in out.splitlines() if line.strip()}


def added_text(root, merge_base, rel):
    """The lines that the branch added to one file, or None when git cannot tell."""
    try:
        out = subprocess.run(["git", "diff", "-U0", "--diff-filter=AMR", merge_base, "--", rel], cwd=root,
                             capture_output=True, text=True, check=True).stdout
    except (subprocess.CalledProcessError, FileNotFoundError):
        return None
    return "\n".join(l[1:] for l in out.splitlines() if l.startswith("+") and not l.startswith("+++"))


def all_files(root):
    for path in root.rglob("*"):
        if path.is_file() and not (set(path.relative_to(root).parts) & SKIP_DIRS):
            yield path.relative_to(root).as_posix()


def matches(rel, patterns):
    return any(fnmatch.fnmatch(rel, p) or fnmatch.fnmatch(rel, p.replace("**/", "")) for p in patterns)


def check(root, rule_files, changed, base=None):
    findings = []

    def add(file, rule, message):
        findings.append((str(file), rule, message))

    index = root / INDEX
    rows = []
    if not index.is_file():
        add(INDEX, "DEC-INDEX", "the decision index does not exist")
    else:
        header, rows = table(index.read_text(encoding="utf-8"))
        if header != COLUMNS:
            add(INDEX, "DEC-INDEX", f"columns must be {COLUMNS}, found {header}")
            rows = []
    active = {}
    for row in rows:
        did, status = norm_id(row.get("id", "")), row.get("status", "").lower()
        if status not in VALID:
            add(INDEX, "DEC-STATUS", f"{did}: status {status!r} is not one of {sorted(VALID)}")
            continue
        if status in ("accepted", "assumed"):
            if did in active:
                add(INDEX, "DEC-DUPLICATE", f"{did} has more than one accepted or assumed row")
            active[did] = row
        if status in ("accepted", "superseded"):
            adr = link_of(row.get("adr", ""), index.parent)
            if adr is None or not adr.is_file():
                add(INDEX, "DEC-ADR", f"{did}: the ADR link {row.get('adr', '')!r} does not lead to a file")
                continue
            got = adr_status(adr) or ""
            if not got.startswith(status):
                add(adr.relative_to(root) if adr.is_relative_to(root) else adr, "DEC-ADR-STATUS",
                    f"{did}: the ADR status is {got!r}, the index says {status!r}")
            if status == "superseded":
                text = adr.read_text(encoding="utf-8")
                newer = re.search(r"superseded by[^\n]*?([\w./-]+\.md)", text, re.IGNORECASE)
                if not newer:
                    add(adr.relative_to(root), "DEC-SUPERSEDE", f"{did}: no link to the ADR that supersedes it")
                else:
                    new_path = (adr.parent / newer.group(1)).resolve()
                    if not new_path.is_file() or adr.name not in new_path.read_text(encoding="utf-8"):
                        add(adr.relative_to(root), "DEC-SUPERSEDE", f"{did}: the new ADR does not link back to {adr.name}")

    adr_dirs = set()
    for row in rows:
        adr = link_of(row.get("adr", ""), index.parent) if index.is_file() else None
        if adr is not None:
            adr_dirs.add(adr.parent)
    accepted_adrs = {}
    for adr_dir in adr_dirs:
        for adr in sorted(adr_dir.glob("*.md")):
            found = DECISION_ID_LINE.search(adr.read_text(encoding="utf-8", errors="ignore"))
            if found and (adr_status(adr) or "").startswith("accepted"):
                accepted_adrs.setdefault(norm_id(found.group(1)), set()).add(adr)
    for did, adrs in sorted(accepted_adrs.items()):
        if len(adrs) > 1:
            names = ", ".join(sorted(a.name for a in adrs))
            add(INDEX, "DEC-ADR-DUPLICATE", f"{did} is in more than one accepted ADR: {names}")

    detect = []
    for rf in rule_files:
        try:
            data = json.loads(pathlib.Path(rf).read_text(encoding="utf-8"))
        except (OSError, ValueError):
            add(rf, "DEC-USED", "cannot read the rules file")
            continue
        for rule in data.get("rules", data if isinstance(data, list) else []):
            if rule.get("kind") == "detect":
                detect.append((rule["id"], rule["decision"], rule.get("files", ["**/*"]), rule.get("exclude", []),
                               re.compile(rule["pattern"], re.MULTILINE)))
    if detect:
        files = sorted(changed) if changed is not None else list(all_files(root))
        merge_base = merge_base_of(root, base) if base else None
        seen = set()
        for rel in files:
            path = root / rel
            if not path.is_file() or matches(rel, NOT_PRODUCT):
                continue
            text = None
            for rid, decision, patterns, exclude, rx in detect:
                if norm_id(decision) in active or (decision, rel) in seen or not matches(rel, patterns) or matches(rel, exclude):
                    continue
                if text is None:
                    text = added_text(root, merge_base, rel) if merge_base and changed is not None else None
                    if text is None:
                        text = path.read_text(encoding="utf-8", errors="ignore")
                if rx.search(text):
                    seen.add((decision, rel))
                    add(rel, "DEC-USED", f"uses decision {decision} (rule {rid}), but the index has no accepted or assumed row for it")
    return findings


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("--root", type=pathlib.Path, default=pathlib.Path.cwd())
    parser.add_argument("--rules", nargs="*", default=[], help="rules.json files of dev-stack skills")
    parser.add_argument("--changed", default=None, help="only check files changed since the merge base with this branch")
    parser.add_argument("--format", choices=("text", "json"), default="text")
    args = parser.parse_args(argv)
    root = args.root.resolve()
    changed = changed_files(root, args.changed) if args.changed else None
    findings = check(root, args.rules, changed, args.changed)
    findings.sort(key=lambda f: ORDER.index(f[1]) if f[1] in ORDER else len(ORDER))
    if args.format == "json":
        print(json.dumps([{"file": f, "rule": r, "message": m} for f, r, m in findings], indent=1))
    else:
        for f, r, m in findings:
            print(f"{f}: {r} {m}")
        counts = {}
        for _, rule, _ in findings:
            counts[rule] = counts.get(rule, 0) + 1
        detail = ", ".join(f"{rule} {n}" for rule, n in sorted(counts.items(), key=lambda c: ORDER.index(c[0]) if c[0] in ORDER else len(ORDER)))
        print(f"{len(findings)} findings" + (f" ({detail})" if detail else ""))
    return 1 if findings else 0


if __name__ == "__main__":
    sys.exit(main())
