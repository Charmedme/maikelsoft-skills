#!/usr/bin/env python3
"""mutate: find rules without an example, with boundary mutations of the changed code.

Usage:
    python3 mutate.py --base <commit> --test "<test command>" [--max 30] [--timeout 300] [--exclude <glob> ...]

For each comparison in the lines that changed since <base> (test files excluded), the script makes
one change at a time: > to >=, >= to >, < to <=, <= to <, == to !=, != to ==, === to !==, !== to ===.
It runs the test command, and then puts the file back. A mutation that survives (the tests still
pass) shows a rule that no test checks: often a limit without an example on each side.

Only operators with a space on each side count, so generics (List<int>) and arrows (=>) do not.
The script needs git. It uses the Python standard library only. Exit code 1 means one or more
mutations survived.
"""
from __future__ import annotations

import argparse
import fnmatch
import json
import pathlib
import re
import subprocess
import sys

SWAP = {">": ">=", ">=": ">", "<": "<=", "<=": "<", "==": "!=", "!=": "==", "===": "!==", "!==": "==="}
OPERATOR = re.compile(r"(?<= )(===|!==|==|!=|>=|<=|>|<)(?= )")
TEST_FILES = ["*test*", "*Test*", "*spec*", "*Spec*", "tests/*", "test/*", "*/tests/*", "*/Tests/*", "*.md", "*.json"]


def git(*args):
    return subprocess.run(["git", *args], capture_output=True, text=True, check=True).stdout


def changed_lines(base, exclude):
    """{file: [line numbers]} of the lines that changed since base."""
    out = {}
    current = None
    for line in git("diff", "-U0", base, "--").splitlines():
        if line.startswith("+++ "):
            name = line[6:] if line.startswith("+++ b/") else None
            current = None if not name or any(fnmatch.fnmatch(name, g) for g in exclude) else name
        elif current and line.startswith("@@"):
            m = re.search(r"\+(\d+)(?:,(\d+))?", line)
            start, count = int(m.group(1)), int(m.group(2) or 1)
            out.setdefault(current, []).extend(range(start, start + count))
    return out


def candidates(changes):
    for name, numbers in changes.items():
        path = pathlib.Path(name)
        if not path.is_file():
            continue
        lines = path.read_text(encoding="utf-8", errors="ignore").splitlines(keepends=True)
        for n in numbers:
            if n - 1 >= len(lines):
                continue
            text = lines[n - 1]
            if text.lstrip().startswith(("//", "#", "*", "/*")):
                continue
            for m in OPERATOR.finditer(text):
                yield name, n, m.start(), m.group(1)


def run_tests(command, timeout):
    try:
        return subprocess.run(command, shell=True, capture_output=True, text=True, timeout=timeout).returncode == 0
    except subprocess.TimeoutExpired:
        return False


def main(argv=None) -> int:
    ap = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    ap.add_argument("--base", required=True)
    ap.add_argument("--test", required=True, help="the command that runs the acceptance tests")
    ap.add_argument("--max", type=int, default=30)
    ap.add_argument("--timeout", type=int, default=300)
    ap.add_argument("--exclude", nargs="*", default=[])
    ap.add_argument("--format", choices=("text", "json"), default="text")
    args = ap.parse_args(argv)

    if not run_tests(args.test, args.timeout):
        print("mutate: the tests fail before any mutation. Make them pass first.", file=sys.stderr)
        return 2
    found = list(candidates(changed_lines(args.base, TEST_FILES + args.exclude)))[: args.max]
    survivors, killed = [], 0
    for name, n, col, op in found:
        path = pathlib.Path(name)
        original = path.read_text(encoding="utf-8")
        lines = original.splitlines(keepends=True)
        line = lines[n - 1]
        lines[n - 1] = line[:col] + SWAP[op] + line[col + len(op):]
        path.write_text("".join(lines), encoding="utf-8")
        try:
            passed = run_tests(args.test, args.timeout)
        finally:
            path.write_text(original, encoding="utf-8")
        if passed:
            survivors.append({"file": name, "line": n, "from": op, "to": SWAP[op], "code": line.strip()[:160]})
        else:
            killed += 1

    if args.format == "json":
        print(json.dumps({"mutations": len(found), "killed": killed, "survived": survivors}, indent=2))
    else:
        for s in survivors:
            print(f"SURVIVED {s['file']}:{s['line']}: {s['from']} to {s['to']} | {s['code']}")
        print(f"mutate: {len(found)} mutations, {killed} killed, {len(survivors)} survived")
    return 1 if survivors else 0


if __name__ == "__main__":
    sys.exit(main())
