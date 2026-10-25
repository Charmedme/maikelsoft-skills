#!/usr/bin/env python3
"""examplecheck: check the examples.md of example mapping.

Usage:
    python3 examplecheck.py <examples.md> [--design <ux-design.md>] [--format text|json]

The script checks:
  - The five sections exist: Rules, Examples, Words with more than one meaning,
    Answers from decisions, Open questions.
  - Each rule has an ID (AC1 R1) and one or more example rows.
  - Each rule with "(limit: ...)" has the cases below, at, and above.
  - Each example row names a rule that exists, and has a source.
  - Each "open: P<n>" source has a question P<n> under Open questions.
  - Each "ux: <screen>/<state>" source names a state of that screen in ux-design.md
    (the file next to examples.md, or --design), when that file exists.
  - No word with more than one meaning in a criterion has a technical or an assumed
    answer. A word of a technical contract, marked "(contract)", can have one.

It prints the rules that wait for an answer. Exit code 1 means one or more errors.
It uses the Python standard library only.
"""
from __future__ import annotations

import argparse
import json
import pathlib
import re
import sys

SECTIONS = ["Rules", "Examples", "Words with more than one meaning", "Answers from decisions", "Open questions"]
RULE = re.compile(r"^\s*[-*]\s+(AC\d+\s+R\d+)\b(.*)$")
LIMIT = re.compile(r"\(limit:", re.I)
ROW = re.compile(r"^\|(.+)\|\s*$")
QUESTION = re.compile(r"^\s*[-*]\s+(P\d+)\b")
GUESS = re.compile(r"\b(technical|technisch|assumed|aangenomen|recommendation of the agent|aanbeveling van de agent)\b", re.I)


def sections(text):
    out, current = {}, None
    for line in text.splitlines():
        m = re.match(r"^##\s+(.+?)\s*$", line)
        if m:
            current = m.group(1).strip()
            out[current] = []
        elif current:
            out[current].append(line)
    return out


def ux_states(design: str | None) -> dict | None:
    """The states of each screen in ux-design.md: {screen: {state, ...}}. None when there is no design."""
    if not design:
        return None
    states, screen = {}, None
    for line in design.splitlines():
        h = re.match(r"^#{2,4}[ \t]+States:[ \t]*(\S+)", line)
        if h:
            screen = h.group(1).strip().lower()
            states.setdefault(screen, set())
            continue
        if line.startswith("#"):
            screen = None
            continue
        m = ROW.match(line)
        if screen and m:
            first = m.group(1).split("|")[0].strip().strip("`").lower()
            if first and first != "state" and not set(first) <= set("-: "):
                states[screen].add(first)
    return states


def check(text, design=None):
    errors = []
    sec = sections(text)
    for name in SECTIONS:
        if name not in sec:
            errors.append(f'Missing section "## {name}"')
    rules = {}
    for line in sec.get("Rules", []):
        m = RULE.match(line)
        if m:
            rules[re.sub(r"\s+", " ", m.group(1))] = bool(LIMIT.search(m.group(2)))
        elif re.match(r"^\s*[-*]\s+\S", line) and "none" not in line.lower():
            errors.append(f"A rule needs an ID like AC1 R1: {line.strip()[:80]}")
    if not rules:
        errors.append("No rules")
    cases, waiting = {r: set() for r in rules}, set()
    header = None
    for line in sec.get("Examples", []):
        m = ROW.match(line)
        if not m:
            continue
        cells = [c.strip() for c in m.group(1).split("|")]
        if header is None:
            header = [c.lower() for c in cells]
            continue
        if set("".join(cells)) <= set("-: "):
            continue
        row = dict(zip(header, cells))
        rule = re.sub(r"\s+", " ", row.get("rule", ""))
        if rule not in rules:
            errors.append(f"An example names a rule that does not exist: {rule or '(empty)'}")
            continue
        cases[rule].add(row.get("case", "").lower())
        source = row.get("source", "")
        if not source:
            errors.append(f"An example of {rule} has no source")
        u = re.search(r"\bux:\s*([\w-]+)/([\w-]+)", source, re.I)
        known = ux_states(design)
        if u and known is not None:
            scr, st = u.group(1).lower(), u.group(2).lower()
            if scr not in known:
                errors.append(f"An example of {rule} names a screen that is not in ux-design.md: {scr}")
            elif st not in known[scr]:
                errors.append(f"An example of {rule} names a state that is not in the states of {scr}: {st}")
        m = re.search(r"open:\s*(P\d+)", source, re.I)
        if m:
            waiting.add((rule, m.group(1).upper()))
    for rule, is_limit in rules.items():
        if not cases[rule]:
            errors.append(f"{rule} has no example")
        elif is_limit:
            missing = {"below", "at", "above"} - cases[rule]
            if missing:
                errors.append(f"{rule} has a limit, but no example for: {', '.join(sorted(missing))}")
    for line in sec.get("Words with more than one meaning", []):
        word, _, answer = line.partition("Answer:")
        if GUESS.search(answer) and "(contract" not in word.lower():
            errors.append(f"A word with more than one meaning is a product question, not a technical or assumed answer: {line.strip()[:80]}")
    questions = {m.group(1) for line in sec.get("Open questions", []) if (m := QUESTION.match(line))}
    for rule, q in sorted(waiting):
        if q not in questions:
            errors.append(f"{rule} waits for {q}, but {q} is not under Open questions")
    return errors, sorted(waiting), len(rules), sorted(questions)


def main(argv=None) -> int:
    ap = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    ap.add_argument("file")
    ap.add_argument("--design", help="ux-design.md (default: the file next to examples.md)")
    ap.add_argument("--format", choices=("text", "json"), default="text")
    args = ap.parse_args(argv)
    path = pathlib.Path(args.file)
    design_path = pathlib.Path(args.design) if args.design else path.parent / "ux-design.md"
    design = design_path.read_text(encoding="utf-8") if design_path.exists() else None
    errors, waiting, count, questions = check(path.read_text(encoding="utf-8"), design)
    if args.format == "json":
        print(json.dumps({"errors": errors, "rules": count, "waiting": waiting, "questions": questions}, indent=2))
    else:
        for e in errors:
            print(f"ERROR {e}")
        for rule, q in waiting:
            print(f"WAIT {rule}: {q}")
        print(f"examplecheck: {count} rules, {len(errors)} errors, {len(questions)} open questions")
    return 1 if errors else 0


if __name__ == "__main__":
    sys.exit(main())
