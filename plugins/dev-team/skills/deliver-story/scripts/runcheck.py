#!/usr/bin/env python3
"""Check the run folder of one dev-team story before gate G2.

    python3 runcheck.py docs/work/<slug>            # check the run folder
    python3 runcheck.py docs/work/<slug> --stage g1 # check only what must exist at G1
    python3 runcheck.py docs/work/<slug> --stage report # check only g2.md and risk-2.txt, after you write g2.md
    python3 runcheck.py docs/work/<slug> --format json

What it checks:
    RUN-HEADER    run.md starts with the shared header of dev-flow and dev-team (YAML front matter with
                  flow, item, size, step, and baseline), and the flow is team. runheader.py writes it.
    RUN-FILE      run.md, spec.md, design.md, decision-sheet.md, and log.md exist. A run with the header
                  "baseline: legacy" (a run from before dev-team 1.0.0) can have story.md in place of spec.md.
    RUN-SPEC      spec.md has a section "## Checks" with a table: Part | Check. Each rule R<n> of the
                  behaviour spec block has a row, and each row names a test file or "review only".
                  spec-baseline.json exists when spec.md has a spec block. At G2, each test file exists.
    RUN-STATUS    each step file has one "Status:" (or "Verdict:") line with a valid word, or the word
                  on the first line under a "## Verdict" heading.
    RUN-COUNTER   each counter in run.md is at or below its limit. At G2, security-review is 1.
                  A light run (run.md field Mode: light) has no reflection and no security review.
                  A raised run (Mode: raised) has a security review and no reflection.
                  When the run folder has ux-design.md, or spec.md (story.md in a legacy run) lists a mock-up
                  (a line "Mock-up: ..."), ux-review is 1 at G2 and ux-review.md exists.
    RUN-ROUNDS    each package has at most one plan feedback, one review, and one fix.
    RUN-STEPS     the files of each step exist (plan, feedback, report, review per package).
    RUN-FIX       a review with CHANGES_REQUIRED has a fix row in log.md.
    RUN-LOG       log.md has the columns of the template, and each row has a valid status.
    RUN-G2        stage report: risk-2.txt exists, and g2.md has the line Result, the table of the rules
                  (first column "Rule (verbatim)", or "Criterion (verbatim)" in an old run) with one or more rows,
                  and the text for the pull request.
    RUN-EVIDENCE  before G2, a log row that says a check passed has a matching command in commands.jsonl.
                  After G2, each follow-up window has a checks.py command. A window starts at a FOLLOWUP_STARTED
                  row whose Reason holds the full UTC time, and it ends at the next such row. A follow-up that
                  waits for the human (the last log row is NEEDS_HUMAN) has no finding yet.
    RUN-HOOK      commands.jsonl exists and has commands when a log row says a check passed, and a follow-up
                  window has commands.
                  One finding for the cause, not one for each row.

Output: one line per finding, `<file>: <RULE> <message>`, then a summary line.
Exit code: 1 when there is a finding, else 0.

Python 3.9 or later, standard library only.
"""
import argparse
import json
import pathlib
import re
import subprocess
import sys

STATUS = {
    "design.md": {"DESIGN_READY", "NEEDS_HUMAN", "BLOCKED"},
    "reflection.md": {"REFLECTION_READY"},
    "core-report.md": {"CORE_DONE", "CORE_DONE_WITH_CONCERNS", "NEEDS_HUMAN", "BLOCKED"},
    "core-review.md": {"APPROVED", "APPROVED_WITH_COMMENTS", "CHANGES_REQUIRED"},
    "security-review.md": {"PASS", "PASS_WITH_FINDINGS", "BLOCKED"},
    "ux-review.md": {"DONE", "DONE_WITH_CONCERNS", "CHANGES_REQUIRED"},
}
PACKAGE_STATUS = {
    "plan": {"PLAN_READY", "NEEDS_CONTEXT", "BLOCKED"},
    "plan-feedback": {"PLAN_APPROVED", "PLAN_APPROVED_WITH_CHANGES"},
    "report": {"DONE", "DONE_WITH_CONCERNS", "NEEDS_CONTEXT", "BLOCKED"},
    "review": {"APPROVED", "APPROVED_WITH_COMMENTS", "CHANGES_REQUIRED"},
}
ALL_STATUS = set().union(*STATUS.values(), *PACKAGE_STATUS.values(), {
    "STARTED", "PACKAGES_DONE", "PACKAGES_DONE_WITH_CONCERNS", "FIXED", "FIX_DONE", "NEEDS_CONTEXT",
    "G1_APPROVED", "G1_CHANGED", "RISK_CHECKED", "G2_SHOWN", "FOLLOWUP_STARTED", "FOLLOWUP_DONE", "DONE", "DONE_WITH_CONCERNS", "NEEDS_HUMAN", "BLOCKED"})
LOG_COLUMNS = ["time", "agent", "step", "status", "files", "commit", "checks", "decisions", "assumptions", "reason"]
MOCKUP_LINE = re.compile(r"^[ \t]*(?:[-*][ \t]+)?Mock-up[ \t]*:", re.MULTILINE | re.IGNORECASE)
STATUS_LINE = re.compile(r"^\s*(?:\*\*)?(?:Status|Verdict)(?:\*\*)?\s*:\s*(?:\*\*)?`?([A-Z_]+)`?", re.MULTILINE)
PACKAGE_FILE = re.compile(r"^(\d{2})-(?!plan|plan-feedback|report|review|checks|fix)[a-z0-9-]+\.md$")
CHECK_WORDS = re.compile(r"\b(build|test|tests|format|checks?)\b[^|,;]*\bpass", re.IGNORECASE)
CHECK_COMMAND = re.compile(r"checks\.py|dotnet\s+(build|test|format)|npm\s+(run|test)|npx\s+tsc")
# A log name that is a mode of another agent: the hook logs the agent type, not the mode.
AGENT_TYPES = {"reflector": "senior"}
FULL_TIME = re.compile(r"\b\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}Z\b")
# The shared header of run.md. Keep it the same as FIELDS and ALLOWED in runheader.py.
HEADER_FIELDS = ("flow", "item", "size", "step", "baseline")
HEADER_ALLOWED = {"flow": ("solo", "team"), "size": ("patch", "small", "medium", "large"),
                  "baseline": ("none", "set", "legacy")}
FRONT = re.compile(r"\A---\n(.*?)\n---\n?", re.DOTALL)
SPEC_BLOCK = re.compile(r"<!-- BEGIN behaviour spec: [^>]*-->(.*?)<!-- END behaviour spec -->", re.DOTALL)
SPEC_RULE = re.compile(r"^\s*(?:Rule|Regel):\s*(R\d+)\b", re.MULTILINE)
REVIEW_ONLY = re.compile(r"^review only$", re.IGNORECASE)


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


def run_header(run):
    """The fields of the shared header of run.md, or None when run.md has no header."""
    path = run / "run.md"
    if not path.is_file():
        return None
    match = FRONT.match(path.read_text(encoding="utf-8"))
    if not match:
        return None
    fields = {}
    for line in match.group(1).splitlines():
        key, sep, value = line.partition(":")
        if sep:
            fields[key.strip()] = value.strip()
    return fields


def header_errors(fields):
    """The problems of the header, as messages."""
    if fields is None:
        return ["run.md has no shared header. Run runheader.py write --flow team (or runheader.py adopt for an old run)"]
    out = []
    for key in HEADER_FIELDS:
        value = fields.get(key, "")
        if not value:
            out.append(f"header field '{key}' is missing")
        elif key in HEADER_ALLOWED and value not in HEADER_ALLOWED[key]:
            out.append(f"header field '{key}' is '{value}'; use {', '.join(HEADER_ALLOWED[key])}")
    for key in fields:
        if key not in HEADER_FIELDS:
            out.append(f"field '{key}' is not a header field")
    if fields.get("flow") == "solo":
        out.append("the header says flow: solo. A team run has flow: team")
    return out


def section(text, title):
    """The text under '## <title>' up to the next heading of level 1 or 2."""
    match = re.search(rf"^##\s+{re.escape(title)}\s*$(.*?)(?=^#{{1,2}}\s|\Z)", text, re.MULTILINE | re.DOTALL)
    return match.group(1) if match else ""


def repo_root(folder):
    try:
        done = subprocess.run(["git", "-C", str(folder), "rev-parse", "--show-toplevel"],
                              capture_output=True, text=True, timeout=10)
    except (OSError, subprocess.SubprocessError):
        return folder
    return pathlib.Path(done.stdout.strip()) if done.returncode == 0 else folder


def check_spec(run, stage):
    """The rule RUN-SPEC: spec.md has a check for each rule of the behaviour spec."""
    path = run / "spec.md"
    text = path.read_text(encoding="utf-8")
    block = SPEC_BLOCK.search(text)
    rules = SPEC_RULE.findall(block.group(1)) if block else []
    findings = []
    if block and not (run / "spec-baseline.json").is_file():
        findings.append((str(run / "spec-baseline.json"), "RUN-SPEC",
                         "file is missing. Copy the spec with specsync.py copy, so each rule has a fingerprint"))
    rows = [cells for cells in table_rows(section(text, "Checks"), "part")[1:] if cells and cells[0]]
    if not rows:
        return findings + [(str(path), "RUN-SPEC", "spec.md needs a section '## Checks' with a table: Part | Check")]
    parts = {}
    root = repo_root(run) if stage == "g2" else None
    for cells in rows:
        value = cells[1].strip().strip("`").strip() if len(cells) > 1 else ""
        rid = re.match(r"(R\d+)\b", cells[0])
        if rid:
            parts.setdefault(rid.group(1), []).append(value)
        if not value:
            findings.append((str(path), "RUN-SPEC", f"the part {cells[0]!r} has no check. Name a test file or 'review only'"))
        elif root is not None and not REVIEW_ONLY.match(value) and not (root / value).exists():
            findings.append((str(path), "RUN-SPEC", f"the test file {value} of {cells[0]!r} does not exist"))
    for rid in rules:
        if rid not in parts:
            findings.append((str(path), "RUN-SPEC", f"the rule {rid} has no row in '## Checks'"))
    return findings


# The review template of dev-stack:secure-coding puts the word on the first line under "## Verdict".
VERDICT_HEADING = re.compile(r"^#+\s*Verdict\s*\n+\s*(?:\*\*)?`?([A-Z_]+)\b", re.MULTILINE)


def run_mode(run):
    """The Mode field of run.md: 'light', 'raised', or 'full' (the default)."""
    path = run / "run.md"
    if not path.is_file():
        return "full"
    for cells in table_rows(path.read_text(encoding="utf-8"), "field")[1:]:
        if len(cells) >= 2 and cells[0].lower() == "mode":
            word = cells[1].strip("` ").lower()
            return "light" if word.startswith("light") else "raised" if word.startswith("raised") else "full"
    return "full"


def status_of(path):
    text = path.read_text(encoding="utf-8")
    match = STATUS_LINE.search(text) or VERDICT_HEADING.search(text)
    return match.group(1) if match else None


def check_report(run):
    """The rule RUN-G2: the report that the human reads is complete."""
    findings = []
    if not (run / "risk-2.txt").is_file():
        findings.append((str(run / "risk-2.txt"), "RUN-G2", "file is missing, so G2 has no second risk check"))
    path = run / "g2.md"
    if not path.is_file():
        return findings + [(str(path), "RUN-G2", "file is missing")]
    text = path.read_text(encoding="utf-8")
    if not re.search(r"^Result:", text, re.MULTILINE):
        findings.append((str(path), "RUN-G2", "the line 'Result:' is missing"))
    if len(table_rows(text, "rule (verbatim)")) < 2 and len(table_rows(text, "criterion (verbatim)")) < 2:
        findings.append((str(path), "RUN-G2", "the table of the rules (Rule (verbatim) | Locked test) has no rows"))
    if not re.search(r"^#+\s*Text for the pull request", text, re.MULTILINE):
        findings.append((str(path), "RUN-G2", "the section 'Text for the pull request' is missing"))
    return findings


def check(run, stage):
    if stage == "report":
        return check_report(run)
    findings = []

    def add(file, rule, message):
        findings.append((str(file), rule, message))

    fields = run_header(run)
    if (run / "run.md").is_file():
        for message in header_errors(fields):
            add(run / "run.md", "RUN-HEADER", message)
    legacy = bool(fields) and fields.get("baseline") == "legacy"
    item = "story.md" if legacy and not (run / "spec.md").is_file() else "spec.md"
    must = ["run.md", item, "design.md", "decision-sheet.md", "log.md"]
    for name in must:
        if not (run / name).is_file():
            add(run / name, "RUN-FILE", "file is missing")
    if item == "spec.md" and (run / "spec.md").is_file():
        findings.extend(check_spec(run, stage))
    mode = run_mode(run)
    light = mode == "light"
    if stage == "g1" or light:
        must_after = []
    else:
        must_after = ["security-review.md"] if mode == "raised" else ["reflection.md", "security-review.md"]
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
        story = run / item
        has_mockup = story.is_file() and MOCKUP_LINE.search(story.read_text(encoding="utf-8", errors="ignore"))
        if stage != "g1" and ((run / "ux-design.md").is_file() or has_mockup):
            source = "ux-design.md" if (run / "ux-design.md").is_file() else f"a mock-up in {item}"
            if counters.get("ux-review", (1, 0))[1] != 1:
                add(run / "run.md", "RUN-COUNTER", f"ux-review must be 1 before G2 (the run has {source})")
            if not (run / "ux-review.md").is_file():
                add(run / "ux-review.md", "RUN-FILE", f"file is missing (the run has {source})")

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
                fixed = any(re.match(rf"{nn}\s+fix\b", r.get("step", "").strip(), re.IGNORECASE) for r in log_rows)
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
    # A row counts for the claims only before G2. After G2, the text of a row says nothing, because the
    # cells are free text in the language of the run. So each follow-up needs one checks.py command.
    before_g2, windows = [], []
    after_g2 = False
    for row in log_rows:
        word = row.get("status", "").strip("` ")
        if word == "FOLLOWUP_STARTED":
            after_g2 = True
            windows.append(FULL_TIME.search(row.get("reason", "")))
        elif not after_g2:
            before_g2.append(row)
        if word == "G2_SHOWN":
            after_g2 = True
    claims = [r for r in before_g2 if CHECK_WORDS.search(r.get("checks", ""))]
    hook_found = False
    if claims and not commands:
        add(commands_file if commands_file.is_file() else run / "log.md", "RUN-HOOK",
            f"commands.jsonl is {'empty' if commands_file.is_file() else 'missing'}, so the logging hook did not run. "
            f"{len(claims)} log rows say a check passed, and nothing proves it. Find out why the hook did not run "
            f"(for example no working python command) before you show G2.")
        claims = []
        hook_found = True
    for row in claims:
        agent = row.get("agent", "").strip("` ").lower()
        agent_type = AGENT_TYPES.get(agent, agent)
        ran = [c for c in commands if CHECK_COMMAND.search(c.get("command", ""))
               and (not agent_type or agent_type in (c.get("agent_type") or "main").lower())]
        if not ran:
            add(run / "log.md", "RUN-EVIDENCE",
                f"row {row.get('step', '?')!r} says a check passed, but commands.jsonl has no check command by {agent or 'any agent'}")
    starts = [m.group(0) for m in windows if m]  # A window with no time comes from an old run folder.
    if log_rows and log_rows[-1].get("status", "").strip("` ") == "NEEDS_HUMAN":
        starts = starts[:-1]
    for start, end in zip(starts, starts[1:] + [None]):
        inside = [c for c in commands if c.get("time", "") >= start and (end is None or c.get("time", "") < end)]
        if not inside:
            if not hook_found:
                add(run / "log.md", "RUN-HOOK", f"the follow-up from {start} has no commands, so the marker .dev-team/active-run or the logging hook did not work")
                hook_found = True
        elif not any("checks.py" in c.get("command", "") for c in inside):
            add(run / "log.md", "RUN-EVIDENCE", f"the follow-up from {start} has no checks.py command in commands.jsonl")
    return findings


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("run", type=pathlib.Path)
    parser.add_argument("--stage", choices=("g1", "g2", "report"), default="g2")
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
