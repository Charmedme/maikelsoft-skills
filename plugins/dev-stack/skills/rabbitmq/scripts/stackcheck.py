#!/usr/bin/env python3
"""Check a repository against the rules of one or more dev-stack skills.

    python3 stackcheck.py                       # rules.json of this skill, current folder
    python3 stackcheck.py --root <repo>         # check another folder
    python3 stackcheck.py --rules a.json b.json # rules of more than one skill
    python3 stackcheck.py --changed main        # only files changed since the merge base with main
    python3 stackcheck.py --format json         # machine-readable output

Output: one line per finding, `path:line RULE-ID severity message`, then a summary line.
Exit code: 1 when one or more findings have the severity "error", else 0.

Rule kinds in rules.json:
    regex    a line in a file that matches "pattern" is a finding.
    require  each file that matches "files" must contain "pattern", else a finding.
    package  a NuGet or npm package reference that matches "package" and "versions" is a finding.
    detect   a line that matches "pattern" shows that the code uses decision "decision".
             stackcheck only lists these with --detect. decisioncheck.py uses them.

A finding can be suppressed with a comment on the same line or on the line above:
    stackcheck: ignore <RULE-ID> <reason>
A suppression without a reason does not count. Suppressed findings show in the summary.

Python 3.9 or later, standard library only.
"""
import argparse
import fnmatch
import json
import pathlib
import re
import subprocess
import sys
import xml.etree.ElementTree as ET

# Folders that never hold source code of the project.
SKIP_DIRS = {".git", "bin", "obj", "node_modules", "dist", ".angular", ".vs", ".idea", "TestResults", "coverage"}
# Files larger than this are generated or data files, not source code.
MAX_FILE_BYTES = 2_000_000
IGNORE = re.compile(r"stackcheck:\s*ignore\s+([A-Z0-9][A-Z0-9-]*)\s+(\S.*)")
SEVERITIES = ("error", "warning", "info")


def load_rules(paths):
    rules = []
    for path in paths:
        data = json.loads(pathlib.Path(path).read_text(encoding="utf-8"))
        skill = data.get("skill", pathlib.Path(path).parent.name)
        for rule in data.get("rules", []):
            rule = dict(rule)
            rule.setdefault("severity", "error")
            rule.setdefault("kind", "regex")
            rule["skill"] = skill
            if rule["severity"] not in SEVERITIES:
                raise SystemExit(f"{path}: rule {rule.get('id')}: severity must be one of {SEVERITIES}")
            if "id" not in rule or "message" not in rule and rule["kind"] != "detect":
                raise SystemExit(f"{path}: each rule needs an id and a message")
            rules.append(rule)
    return rules


def glob_match(rel, patterns):
    """True when the POSIX path rel matches one of the glob patterns. '**/' also matches no folder."""
    for pattern in patterns or []:
        if fnmatch.fnmatchcase(rel, pattern):
            return True
        if pattern.startswith("**/") and fnmatch.fnmatchcase(rel, pattern[3:]):
            return True
    return False


def all_files(root):
    for path in root.rglob("*"):
        if path.is_file() and not any(part in SKIP_DIRS for part in path.relative_to(root).parts):
            yield path


def changed_files(root, base):
    """Files changed since the merge base with base, plus new files that git does not track yet."""
    def git(*args):
        out = subprocess.run(["git", "-C", str(root), *args], capture_output=True, text=True)
        if out.returncode != 0:
            raise SystemExit(f"git {' '.join(args)} failed: {out.stderr.strip()}")
        return [line for line in out.stdout.splitlines() if line]

    names = set(git("diff", "--name-only", "--diff-filter=ACMR", f"{base}...HEAD"))
    names |= set(git("diff", "--name-only", "--diff-filter=ACMR"))
    names |= set(git("ls-files", "--others", "--exclude-standard"))
    return {root / name for name in names if (root / name).is_file()}


def read_text(path):
    if path.stat().st_size > MAX_FILE_BYTES:
        return None
    try:
        return path.read_text(encoding="utf-8")
    except (UnicodeDecodeError, OSError):
        return None


def suppressed(lines, index, rule_id):
    for i in (index, index - 1):
        if 0 <= i < len(lines):
            m = IGNORE.search(lines[i])
            if m and m.group(1) == rule_id:
                return True
    return False


def parse_version(text):
    """Turn '7.2.2', '[7.2.2,8.0.0)', '^19.1.0' or '~6.0.3' into the lowest version as a tuple."""
    m = re.search(r"\d+(?:\.\d+)*", text or "")
    return tuple(int(x) for x in m.group(0).split(".")) if m else None


def version_matches(version, constraint):
    """constraint: '*', or comma-separated parts like '>=8.0.0', '<7.0.0', '=4.20.0'."""
    if constraint in (None, "", "*"):
        return True
    if version is None:
        return True  # unknown version: report, the human can check
    for part in constraint.split(","):
        m = re.match(r"\s*(>=|<=|>|<|=)?\s*(\d+(?:\.\d+)*)\s*$", part)
        if not m:
            raise SystemExit(f"bad version constraint: {constraint}")
        op, want = m.group(1) or "=", tuple(int(x) for x in m.group(2).split("."))
        width = max(len(version), len(want))
        a, b = version + (0,) * (width - len(version)), want + (0,) * (width - len(want))
        ok = {"=": a == b, ">=": a >= b, "<=": a <= b, ">": a > b, "<": a < b}[op]
        if not ok:
            return False
    return True


def nuget_references(path, text):
    """Yield (package, version text, line) for PackageReference, PackageVersion and GlobalPackageReference."""
    try:
        tree = ET.fromstring(text)
    except ET.ParseError:
        return
    lines = text.splitlines()
    for element in tree.iter():
        tag = element.tag.split("}")[-1]
        if tag not in ("PackageReference", "PackageVersion", "GlobalPackageReference"):
            continue
        name = element.get("Include") or element.get("Update")
        if not name:
            continue
        version = element.get("Version") or element.get("VersionOverride") or (element.findtext("Version") or "")
        line = next((i + 1 for i, l in enumerate(lines) if f'"{name}"' in l), 1)
        yield name, version, line


def npm_references(path, text):
    try:
        data = json.loads(text)
    except json.JSONDecodeError:
        return
    lines = text.splitlines()
    for section in ("dependencies", "devDependencies", "peerDependencies", "optionalDependencies"):
        for name, version in (data.get(section) or {}).items():
            line = next((i + 1 for i, l in enumerate(lines) if f'"{name}"' in l), 1)
            yield name, str(version), line


def check(root, files, rules, detect):
    findings, hidden = [], 0
    regex_rules = [r for r in rules if r["kind"] == "regex" or (detect and r["kind"] == "detect")]
    require_rules = [r for r in rules if r["kind"] == "require"]
    package_rules = [r for r in rules if r["kind"] == "package"]
    compiled = {id(r): re.compile(r["pattern"], re.MULTILINE) for r in regex_rules + require_rules}

    for path in sorted(files):
        rel = path.relative_to(root).as_posix()
        text = None
        for rule in regex_rules:
            if not glob_match(rel, rule.get("files", ["**/*"])) or glob_match(rel, rule.get("exclude")):
                continue
            text = read_text(path) if text is None else text
            if text is None:
                break
            lines = text.splitlines()
            for m in compiled[id(rule)].finditer(text):
                index = text.count("\n", 0, m.start())
                if suppressed(lines, index, rule["id"]):
                    hidden += 1
                    continue
                severity = "info" if rule["kind"] == "detect" else rule["severity"]
                message = rule.get("message") or f"uses decision {rule.get('decision')}"
                findings.append((rel, index + 1, rule["id"], severity, message, rule))
        for rule in require_rules:
            if not glob_match(rel, rule.get("files")) or glob_match(rel, rule.get("exclude")):
                continue
            text = read_text(path) if text is None else text
            if text is not None and not compiled[id(rule)].search(text):
                findings.append((rel, 1, rule["id"], rule["severity"], rule["message"], rule))
        if package_rules and (path.suffix in (".csproj", ".props", ".targets", ".fsproj") or path.name == "package.json"):
            text = read_text(path) if text is None else text
            if text is None:
                continue
            for rule in package_rules:
                ecosystem = rule.get("ecosystem", "nuget")
                if (ecosystem == "npm") != (path.name == "package.json"):
                    continue
                refs = npm_references(path, text) if ecosystem == "npm" else nuget_references(path, text)
                for name, version, line in refs:
                    if not fnmatch.fnmatchcase(name.lower(), rule["package"].lower()):
                        continue
                    if not version and rule.get("versions", "*") not in ("", "*"):
                        # A reference without a version under central package management: the
                        # PackageVersion in Directory.Packages.props carries the version, and it is checked there.
                        continue
                    if version_matches(parse_version(version), rule.get("versions", "*")):
                        lines = text.splitlines()
                        if suppressed(lines, line - 1, rule["id"]):
                            hidden += 1
                            continue
                        msg = f"{rule['message']} (found {name} {version or 'without a version'})"
                        findings.append((rel, line, rule["id"], rule["severity"], msg, rule))
    return findings, hidden


def main(argv=None):
    here = pathlib.Path(__file__).resolve().parent
    parser = argparse.ArgumentParser(description="Check a repository against dev-stack rules.")
    parser.add_argument("--root", default=".", help="folder to check (default: current folder)")
    parser.add_argument("--rules", nargs="+", default=[str(here.parent / "rules.json")], help="rules.json files")
    parser.add_argument("--changed", metavar="BASE", help="check only files changed since the merge base with BASE")
    parser.add_argument("--detect", action="store_true", help="also list the decision detect rules")
    parser.add_argument("--format", choices=("text", "json"), default="text")
    args = parser.parse_args(argv)

    root = pathlib.Path(args.root).resolve()
    if not root.is_dir():
        raise SystemExit(f"not a folder: {root}")
    rules = load_rules(args.rules)
    files = changed_files(root, args.changed) if args.changed else set(all_files(root))
    files = {f for f in files if not any(part in SKIP_DIRS for part in f.relative_to(root).parts)}
    findings, hidden = check(root, files, rules, args.detect)

    errors = sum(1 for f in findings if f[3] == "error")
    warnings = sum(1 for f in findings if f[3] == "warning")
    if args.format == "json":
        print(json.dumps({
            "findings": [{"path": p, "line": n, "rule": i, "severity": s, "message": m, "skill": r["skill"],
                          "fix": r.get("fix"), "decision": r.get("decision")} for p, n, i, s, m, r in findings],
            "errors": errors, "warnings": warnings, "suppressed": hidden, "files": len(files)}, indent=2))
    else:
        for p, n, i, s, m, r in findings:
            fix = f" Fix: {r['fix']}" if r.get("fix") else ""
            print(f"{p}:{n} {i} {s} {m}{fix}")
        print(f"stackcheck: {errors} errors, {warnings} warnings, {hidden} suppressed | {len(files)} files, {len(rules)} rules")
    return 1 if errors else 0


if __name__ == "__main__":
    sys.exit(main())
