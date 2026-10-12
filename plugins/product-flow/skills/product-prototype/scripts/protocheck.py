#!/usr/bin/env python3
"""Check a prototype HTML file against references/build.md.

    python3 protocheck.py <prototype.html> [--json]

Output: one line for each finding, then a summary line:
    <file>: <n> errors, <n> warnings
Exit code: 1 when there is an error, else 0.
"""
import argparse
import json
import pathlib
import re
import sys

MAX_BYTES = 500_000   # a prototype is small; a bigger file has embedded images or a framework
STATES = ("normal", "empty", "error")   # the minimum states of build.md

CHECKS = [
    # (rule, severity, message, test on the text)
    ("P1", "error", 'No banner. Add an element with id="prototype-banner" and the word PROTOTYPE.',
     lambda t: re.search(r'id=["\']prototype-banner["\']', t) and re.search(r"prototype", t, re.I)),
    ("P2", "error", 'No question. Add an element with id="question".',
     lambda t: re.search(r'id=["\']question["\']', t)),
    ("P3", "error", "Network call found (fetch, XMLHttpRequest, or WebSocket). The prototype must work offline.",
     lambda t: not re.search(r"\bfetch\s*\(|XMLHttpRequest|new\s+WebSocket", t)),
    ("P4", "error", "External script, stylesheet, or image found. Put everything in the file.",
     lambda t: not re.search(r'<(script|link|img)[^>]+(src|href)=["\']https?://', t, re.I)),
    ("P5", "warning", "Lorem ipsum found. Use real words of the users.",
     lambda t: not re.search(r"lorem ipsum", t, re.I)),
    ("P6", "warning", "No viewport meta tag. The layout must work on a phone.",
     lambda t: re.search(r'<meta[^>]+name=["\']viewport["\']', t, re.I)),
]


def check(path):
    text = path.read_text(encoding="utf-8", errors="replace")
    findings = []
    for rule, severity, message, test in CHECKS:
        if not test(text):
            findings.append((rule, severity, message))
    missing = [s for s in STATES if not re.search(r'data-state=["\']' + s + r'["\']', text)]
    if missing:
        findings.append(("P7", "error", f"States without a data-state attribute: {', '.join(missing)}."))
    if path.stat().st_size > MAX_BYTES:
        findings.append(("P8", "warning", f"The file is larger than {MAX_BYTES // 1000} KB. Remove embedded images."))
    return findings


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("file")
    parser.add_argument("--json", action="store_true")
    args = parser.parse_args()
    path = pathlib.Path(args.file)
    if not path.is_file():
        print(f"{args.file}: file not found")
        return 1
    findings = check(path)
    errors = sum(1 for f in findings if f[1] == "error")
    if args.json:
        print(json.dumps({"file": args.file, "errors": errors, "warnings": len(findings) - errors,
                          "findings": [dict(zip(("rule", "severity", "message"), f)) for f in findings]}))
    else:
        for rule, severity, message in findings:
            print(f"{args.file}: {severity.upper()} {rule} {message}")
        print(f"{args.file}: {errors} errors, {len(findings) - errors} warnings")
    return 1 if errors else 0


if __name__ == "__main__":
    sys.exit(main())
