#!/usr/bin/env python3
"""Check the run folder of one dev-team story before gate G2.

    python3 runcheck.py docs/work/<slug>            # check the run folder
    python3 runcheck.py docs/work/<slug> --stage g1 # check only what must exist at G1
    python3 runcheck.py docs/work/<slug> --format json

What it checks:
    RUN-FILE      run.md, story.md, design.md, decision-sheet.md, and log.md exist.
    RUN-STATUS    each step file has one "Status:" (or "Verdict:") line with a valid word, or the word
                  on the first line under a "## Verdict" heading.
    RUN-COUNTER   each counter in run.md is at or below its limit. At G2, security-review is 1.
                  A light run (run.md field Mode: light) has no reflection and no security review.
    RUN-ROUNDS    each package has at most one plan feedback, one review, and one fix.
    RUN-STEPS     the files of each step exist (plan, feedback, report, review per package).
    RUN-FIX       a review with CHANGES_REQUIRED has a fix row in log.md.
    RUN-LOG       log.md has the columns of the template, and each row has a valid status.
    RUN-EVIDENCE  a log row that says a check passed has a matching command in commands.jsonl.

Output: one line per finding, `<file>: <RULE> <message>`, then a summary line.
Exit code: 1 when there is a finding, else 0.

Python 3.9 or later, standard library only.
"""
import argparse
import json
import pathlib
import re
import sys

STATUS = {
    "design.md": {"DESIGN_READY", "NEEDS_HUMAN", "BLOCKED"},
    "reflection.md": {"REFLECTION_READY"},
    "core-report.md": {"CORE_DONE", "CORE_DONE_WITH_CONCERNS", "NEEDS_HUMAN", "BLOCKED"},
    "core-review.md": {"APPROVED", "APPROVED_WITH_COMMENTS", "CHANGES_REQUIRED"},
    "security-review.md": {"PASS", "PASS_WITH_FINDINGS", "BLOCKED"},
}
PACKAGE_STATUS = {
    "plan": {"PLAN_READY", "NEEDS_CONTEXT", "BLOCKED"},
    "plan-feedback": {"PLAN_APPROVED", "PLAN_APPROVED_WITH_CHANGES"},
    "report": {"DONE", "DONE_WITH_CONCERNS", "NEEDS_CONTEXT", "BLOCKED"},
    "review": {"APPROVED", "APPROVED_WITH_COMMENTS", "CHANGES_REQUIRED"},
}
ALL_STATUS = set().union(*STATUS.values(), *PACKAGE_STATUS.values(), {
    "STARTED", "PACKAGES_DONE", "PACKAGES_DONE_WITH_CONCERNS", "FIXED", "FIX_DONE", "NEEDS_CONTEXT",
    "G1_APPROVED", "G1_CHANGED", "G2_SHOWN", "DONE", "DONE_WITH_CONCERNS", "NEEDS_HUMAN", "BLOCKED"})
LOG_COLUMNS = ["time", "agent", "step", "status", "files", "commit", "checks", "decisions", "assumptions", "reason"]
STATUS_LINE = re.compile(r"^\s*(?:\*\*)?(?:Status|Verdict)(?:\*\*)?\s*:\s*(?:\*\*)?`?([A-Z_]+)`?", re.MULTILINE)
PACKAGE_FILE = re.compile(r"^(\d{2})-(?!plan|plan-feedback|report|review|checks|fix)[a-z0-9-]+\.md$")
CHECK_WORDS = re.compile(r"\b(build|test|tests|format|checks?)\b[^|,;]*\bpass", re.IGNORECASE)
CHECK_COMMAND = re.compile(r"checks\.py|dotnet\s+(build|test|format)|npm\s+(run|test)|npx\s+tsc")
# A log name that is a mode of another agent: the hook logs the agent type, not the mode.
AGENT_TYPES = {"reflector": "senior"}


def table_rows(text, first_header):
    """Return the rows (lists of cells) of the first Markdown table whose first header cell matches."""
    rows, inside = [], False
    for line in text.splitlines():
        if line.strip().startswith("|"):
            cells = [c.strip() for c in line.strip().strip("|").split("|")]
            if not inside:
                if cells and cells[0].lower() == first_header:
                    inside = True
                    rows.append(cells)
                continue
            if set("".join(cells)) <= set("-: "):
                continue
            rows.append(cells)
        elif inside:
            break
    return rows


# The review template of dev-stack:secure-coding puts the word on the first line under "## Verdict".
VERDICT_HEADING = re.compile(r"^#+\s*Verdict\s*\n+\s*(?:\*\*)?`?([A-Z_]+)\b", re.MULTILINE)


def run_mode(run):
    """The Mode field of run.md: 'light' or 'full' (the default)."""
    path = run / "run.md"
    if not path.is_file():
        return "full"
    for cells in table_rows(path.read_text(encoding="utf-8"), "field")[1:]:
        if len(cells) >= 2 and cells[0].lower() == "mode":
            return "light" if cells[1].strip("` ").lower().startswith("light") else "full"
    return "full"


def status_of(path):
    text = path.read_text(encoding="utf-8")
    match = STATUS_LINE.search(text) or VERDICT_HEADING.search(text)
    return match.group(1) if match else None


def check(run, stage):
    findings = []

    def add(file, rule, message):
        findings.append((str(file), rule, message))

    must = ["run.md", "story.md", "design.md", "decision-sheet.md", "log.md"]
    for name in must:
        if not (run / name).is_file():
            add(run / name, "RUN-FILE", "file is missing")
    light = run_mode(run) == "light"
    if stage == "g1" or light:
        must_after = []
    else:
        must_after = ["reflection.md", "security-review.md"]
        for name in must_after:
            if not (run / name).is_file():
                add(run / name, "RUN-FILE", "file is missing (required before G2)")

    for name, allowed in STATUS.items():
        path = run / name
        if path.is_file():
            word = status_of(path)
            if word not in allowed:
                add(path, "RUN-STATUS", f"status {word!r} is not one of {sorted(allowed)}")

    counters = {}
    if (run / "run.md").is_file():
        rows = table_rows((run / "run.md").read_text(encoding="utf-8"), "counter")
        for cells in rows[1:]:
            if len(cells) >= 3:
                try:
                    counters[cells[0]] = (int(cells[1]), int(cells[2]))
                except ValueError:
                    add(run / "run.md", "RUN-COUNTER", f"counter {cells[0]!r} has no number")
        for name, (limit, value) in counters.items():
            if value > limit:
                add(run / "run.md", "RUN-COUNTER", f"{name} is {value}, the limit is {limit}")
        if stage != "g1" and not light and counters.get("security-review", (1, 0))[1] != 1:
            add(run / "run.md", "RUN-COUNTER", "security-review must be 1 before G2")

    log_rows = []
    if (run / "log.md").is_file():
        rows = table_rows((run / "log.md").read_text(encoding="utf-8"), "time")
        if rows:
            header = [c.lower() for c in rows[0]]
            if header != LOG_COLUMNS:
                add(run / "log.md", "RUN-LOG", f"columns must be {LOG_COLUMNS}")
            for cells in rows[1:]:
                row = dict(zip(header, cells))
                log_rows.append(row)
                word = row.get("status", "").strip("` ")
                if word not in ALL_STATUS:
                    add(run / "log.md", "RUN-LOG", f"row {row.get('step', '?')!r}: status {word!r} is not valid")
        else:
            add(run / "log.md", "RUN-LOG", "no log table found")

    packages = run / "packages"
    if packages.is_dir() and stage != "g1":
        ids = sorted({m.group(1) for p in packages.iterdir() if (m := PACKAGE_FILE.match(p.name))})
        for nn in ids:
            for kind, allowed in PACKAGE_STATUS.items():
                files = sorted(packages.glob(f"{nn}-{kind}*.md"))
                files = [f for f in files if re.fullmatch(rf"{nn}-{kind}(-\d+)?\.md", f.name)]
                if not files:
                    add(packages / f"{nn}-{kind}.md", "RUN-STEPS", "file is missing")
                    continue
                if kind in ("plan-feedback", "review") and len(files) > 1:
                    add(files[1], "RUN-ROUNDS", f"package {nn} has {len(files)} {kind} files, the limit is 1")
                for f in files:
                    word = status_of(f)
                    if word not in allowed:
                        add(f, "RUN-STATUS", f"status {word!r} is not one of {sorted(allowed)}")
            review = packages / f"{nn}-review.md"
            if review.is_file() and status_of(review) == "CHANGES_REQUIRED":
                fixed = any(nn in r.get("step", "") and "fix" in r.get("step", "").lower() for r in log_rows)
                if not fixed:
                    add(review, "RUN-FIX", f"package {nn} needs one fix pass after CHANGES_REQUIRED (no fix row in log.md)")
            for counter in (f"{nn} plan-feedback", f"{nn} review", f"{nn} fix"):
                if counter in counters and counters[counter][1] > 1:
                    add(run / "run.md", "RUN-ROUNDS", f"{counter} is {counters[counter][1]}, the limit is 1")

    commands_file = run / "commands.jsonl"
    commands = []
    if commands_file.is_file():
        for line in commands_file.read_text(encoding="utf-8").splitlines():
            try:
                commands.append(json.loads(line))
            except ValueError:
                continue
    for row in log_rows:
        if CHECK_WORDS.search(row.get("checks", "")):
            agent = row.get("agent", "").strip("` ").lower()
            agent_type = AGENT_TYPES.get(agent, agent)
            ran = [c for c in commands if CHECK_COMMAND.search(c.get("command", ""))
                   and (not agent_type or agent_type in (c.get("agent_type") or "main").lower())]
            if not ran:
                add(run / "log.md", "RUN-EVIDENCE",
                    f"row {row.get('step', '?')!r} says a check passed, but commands.jsonl has no check command by {agent or 'any agent'}")
    return findings


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("run", type=pathlib.Path)
    parser.add_argument("--stage", choices=("g1", "g2"), default="g2")
    parser.add_argument("--format", choices=("text", "json"), default="text")
    args = parser.parse_args(argv)
    if not args.run.is_dir():
        print(f"{args.run}: not a folder", file=sys.stderr)
        return 2
    findings = check(args.run, args.stage)
    if args.format == "json":
        print(json.dumps([{"file": f, "rule": r, "message": m} for f, r, m in findings], indent=1))
    else:
        for f, r, m in findings:
            print(f"{f}: {r} {m}")
        print(f"{len(findings)} findings")
    return 1 if findings else 0


if __name__ == "__main__":
    sys.exit(main())
