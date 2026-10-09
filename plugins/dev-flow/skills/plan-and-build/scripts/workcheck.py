#!/usr/bin/env python3
"""workcheck: check the work folder of the plan-and-build skill.

Usage:
    python3 workcheck.py <work folder> [--next] [--format text|json]

The script checks:
  - plan.md exists, and its local links point to files that exist.
  - Each ticket file has a number, a spec link, "Blocked by", a status, and
    acceptance criteria. A done ticket has all criteria checked.
  - Each blocker exists, and the blockers have no cycle.
  - The ticket table in plan.md agrees with the ticket files.
  - The gates are in order: G1 before G2, G2 before G3, G3 before the build.

With --next, the script prints the tickets that can start now: status todo,
and each blocker done. The script only reports. It does not change a file.
It uses the Python standard library only.

Exit code 1 means one or more errors.
"""
from __future__ import annotations

import argparse
import json
import pathlib
import re
import sys
from dataclasses import asdict, dataclass

STATUSES = ("draft", "todo", "doing", "blocked", "done")
BUILD_STATUSES = ("todo", "doing", "blocked", "done")
GATE_STATES = ("open", "waiting", "approved", "assumed")
GATES = ("G1", "G2", "G3", "G4")

TICKET_NAME = re.compile(r"^(\d{2,3})-[a-z0-9][a-z0-9-]*\.md$")
TITLE = re.compile(r"^#\s+(\d{2,3}):\s+\S")
FIELD = re.compile(r"^\*\*(Spec|Blocked by|Status):\*\*\s*(.*?)\s*$", re.MULTILINE)
CRITERION = re.compile(r"^\s*[-*]\s+\[([ xX])\]\s+\S", re.MULTILINE)
LINK = re.compile(r"\[[^\]]*\]\(([^)\s#]+)(?:#[^)]*)?\)")
GATE_LINE = re.compile(r"^\s*[-*]\s+(G[1-4])\b[^:\n]*:\s*([A-Za-z]+)", re.MULTILINE)
ROW = re.compile(r"^\|\s*(\d{2,3})\s*\|(.*)\|\s*$", re.MULTILINE)


@dataclass
class Finding:
    severity: str
    file: str
    message: str


@dataclass
class Ticket:
    number: str
    path: str
    blocked_by: list
    status: str
    criteria: int
    checked: int


def numbers(text: str) -> list:
    """The ticket numbers in a "Blocked by" value. "None" or "-" gives []."""
    if re.fullmatch(r"\s*(none|-|n/a)?\s*(\(.*\))?\s*", text, re.IGNORECASE):
        return []
    return re.findall(r"\b(\d{2,3})\b", text)


def local_links(text: str) -> list:
    return [t for t in LINK.findall(text) if not re.match(r"^[a-z]+:", t, re.IGNORECASE)]


def read_ticket(path: pathlib.Path, root: pathlib.Path, out: list) -> Ticket | None:
    rel = str(path.relative_to(root))
    name = TICKET_NAME.match(path.name)
    if not name:
        out.append(Finding("error", rel, "File name must be NN-slug.md (for example 01-add-due-date.md)"))
        return None
    text = path.read_text(encoding="utf-8")
    first = next((line for line in text.splitlines() if line.strip()), "")
    title = TITLE.match(first)
    if not title:
        out.append(Finding("error", rel, 'First line must be "# NN: Title"'))
    elif title.group(1) != name.group(1):
        out.append(Finding("error", rel, f"Title number {title.group(1)} differs from file number {name.group(1)}"))

    fields = {k: v for k, v in FIELD.findall(text)}
    for key in ("Spec", "Blocked by", "Status"):
        if key not in fields:
            out.append(Finding("error", rel, f'Missing line "**{key}:**"'))
    spec_links = local_links(fields.get("Spec", ""))
    if "Spec" in fields and not spec_links:
        out.append(Finding("error", rel, "The Spec line has no link to a spec file"))
    for target in spec_links:
        if not (path.parent / target).resolve().is_file():
            out.append(Finding("error", rel, f"Spec link target does not exist: {target}"))

    status = fields.get("Status", "").strip().lower().strip("`")
    if "Status" in fields and status not in STATUSES:
        out.append(Finding("error", rel, f'Status "{status}" is not one of: {", ".join(STATUSES)}'))

    marks = CRITERION.findall(text)
    checked = sum(m.lower() == "x" for m in marks)
    if not marks:
        out.append(Finding("error", rel, "No acceptance criteria (lines like '- [ ] ...')"))
    if status == "done" and checked < len(marks):
        out.append(Finding("error", rel, f"Status is done, but {len(marks) - checked} criteria are not checked"))
    if status == "blocked" and not re.search(r"(?im)^#+\s*blocked|reason", text):
        out.append(Finding("warning", rel, "Status is blocked, but the ticket gives no reason"))

    return Ticket(name.group(1), rel, numbers(fields.get("Blocked by", "")), status, len(marks), checked)


def find_cycle(tickets: dict) -> list:
    """One cycle in the blocker graph as a list of numbers, or []."""
    state = {}

    def visit(n, trail):
        state[n] = "open"
        for b in tickets[n].blocked_by:
            if b not in tickets:
                continue
            if state.get(b) == "open":
                return trail[trail.index(b):] + [b] if b in trail else [n, b]
            if b not in state:
                found = visit(b, trail + [b])
                if found:
                    return found
        state[n] = "closed"
        return []

    for n in sorted(tickets):
        if n not in state:
            found = visit(n, [n])
            if found:
                return found
    return []


def read_gates(text: str) -> dict:
    gates = {}
    for gate, state in GATE_LINE.findall(text):
        gates[gate] = state.lower()
    return gates


def check(folder: pathlib.Path) -> tuple:
    out = []
    plan_path = folder / "plan.md"
    if not plan_path.is_file():
        out.append(Finding("error", "plan.md", "plan.md does not exist"))
        return out, {}, {}
    plan = plan_path.read_text(encoding="utf-8")

    for target in local_links(plan):
        if not (folder / target).resolve().exists():
            out.append(Finding("error", "plan.md", f"Link target does not exist: {target}"))

    gates = read_gates(plan)
    for gate in GATES:
        state = gates.get(gate)
        if state is None:
            out.append(Finding("error", "plan.md", f'No line for gate {gate} in the "Gates" section'))
        elif state not in GATE_STATES:
            out.append(Finding("error", "plan.md", f'Gate {gate} state "{state}" is not one of: {", ".join(GATE_STATES)}'))

    def passed(gate):
        return gates.get(gate) in ("approved", "assumed")

    if passed("G2") and not passed("G1"):
        out.append(Finding("error", "plan.md", "G2 is passed, but G1 is not"))
    if gates.get("G2") == "assumed":
        out.append(Finding("error", "plan.md", "G2 cannot be assumed: only a human can validate the prototype"))
    if passed("G3") and gates.get("G2") != "approved":
        out.append(Finding("error", "plan.md", "G3 is passed, but G2 is not approved"))
    if passed("G4") and not passed("G3"):
        out.append(Finding("error", "plan.md", "G4 is passed, but G3 is not"))

    tickets = {}
    ticket_dir = folder / "tickets"
    for path in sorted(ticket_dir.glob("*.md")) if ticket_dir.is_dir() else []:
        t = read_ticket(path, folder, out)
        if not t:
            continue
        if t.number in tickets:
            out.append(Finding("error", t.path, f"Ticket number {t.number} is used two times"))
            continue
        tickets[t.number] = t

    for t in tickets.values():
        for b in t.blocked_by:
            if b not in tickets:
                out.append(Finding("error", t.path, f"Blocker {b} does not exist"))
            elif b == t.number:
                out.append(Finding("error", t.path, "The ticket blocks itself"))
            elif t.status in ("doing", "done") and tickets[b].status != "done":
                out.append(Finding("error", t.path, f"Status is {t.status}, but blocker {b} is {tickets[b].status}"))
        if t.status in BUILD_STATUSES and not passed("G3"):
            out.append(Finding("error", t.path, f"Status is {t.status}, but gate G3 is not passed"))
    cycle = find_cycle(tickets)
    if cycle:
        out.append(Finding("error", "tickets/", "Blocker cycle: " + " -> ".join(cycle)))

    specs = sorted((folder / "specs").glob("*.md")) if (folder / "specs").is_dir() else []
    if tickets and not specs:
        out.append(Finding("error", "specs/", "There are tickets, but no spec in specs/"))
    linked = {str((folder / t).resolve()) for t in local_links(plan)}
    for spec in specs:
        if str(spec.resolve()) not in linked:
            out.append(Finding("warning", "plan.md", f"No link to {spec.relative_to(folder)}"))

    rows = {}
    for number, rest in ROW.findall(plan):
        cells = [c.strip() for c in rest.split("|")]
        rows[number] = cells
    for n, t in tickets.items():
        if n not in rows:
            out.append(Finding("error", "plan.md", f"Ticket {n} is not in the ticket table"))
            continue
        cells = rows[n]
        if len(cells) >= 3:
            if sorted(numbers(cells[1])) != sorted(t.blocked_by):
                out.append(Finding("error", "plan.md", f"Ticket {n}: 'Blocked by' differs from the ticket file"))
            if cells[2].lower().strip("`* ") != t.status:
                out.append(Finding("error", "plan.md", f"Ticket {n}: status '{cells[2]}' differs from the ticket file ({t.status})"))
        if not any(pathlib.Path(x).name == pathlib.Path(t.path).name for x in local_links(cells[0] if cells else "")):
            out.append(Finding("warning", "plan.md", f"Ticket {n}: the table has no link to {t.path}"))
    for n in rows:
        if n not in tickets:
            out.append(Finding("error", "plan.md", f"Ticket {n} is in the table, but tickets/ has no file for it"))

    return out, tickets, gates


def frontier(tickets: dict) -> list:
    return [t for n, t in sorted(tickets.items())
            if t.status == "todo" and all(tickets.get(b) and tickets[b].status == "done" for b in t.blocked_by)]


def main(argv=None) -> int:
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("folder", help="the work folder, for example docs/work/due-dates")
    parser.add_argument("--next", action="store_true", help="print the tickets that can start now")
    parser.add_argument("--format", choices=("text", "json"), default="text")
    args = parser.parse_args(argv)

    folder = pathlib.Path(args.folder)
    if not folder.is_dir():
        print(f"workcheck: {folder} is not a folder", file=sys.stderr)
        return 2
    findings, tickets, gates = check(folder)
    errors = sum(f.severity == "error" for f in findings)
    warnings = len(findings) - errors
    done = sum(t.status == "done" for t in tickets.values())
    ready = frontier(tickets)

    if args.format == "json":
        print(json.dumps({
            "errors": errors, "warnings": warnings,
            "findings": [asdict(f) for f in findings],
            "tickets": {n: asdict(t) for n, t in tickets.items()},
            "gates": gates, "next": [t.number for t in ready],
        }, indent=2))
        return 1 if errors else 0

    for f in findings:
        print(f"{f.severity.upper()} {f.file}: {f.message}")
    if args.next:
        if ready:
            for t in ready:
                print(f"NEXT {t.number} {t.path}")
        else:
            print("NEXT none")
    gate_text = " ".join(f"{g}={gates.get(g, '?')}" for g in GATES)
    print(f"workcheck: {errors} errors, {warnings} warnings | {len(tickets)} tickets, {done} done | {gate_text}")
    return 1 if errors else 0


if __name__ == "__main__":
    sys.exit(main())
