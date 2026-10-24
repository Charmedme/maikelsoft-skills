#!/usr/bin/env python3
"""Extract the main design decisions of each run into fixed answers, and score the agreement.

    python3 evals/ux-design/extract.py <runs> [--model sonnet] [--parallel 4]

For each run, a headless reader (read-only tools) answers the fixed questions of the case in
evals.json from ux-design.md and the mockups. It writes extract.json. Then the script prints, per
case and variant, the share of run pairs that give the same answer (the determinism score).
"""
import argparse
import concurrent.futures as cf
import itertools
import json
import re
import subprocess
import sys
from pathlib import Path

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
from run import child_env  # noqa: E402

EV = json.loads((HERE / "evals.json").read_text(encoding="utf-8"))
CASES = {c["name"]: c for c in EV["cases"]}

PROMPT = """You read a UX design and its HTML mockups, and you answer fixed questions about the design.
The design is in `{work}/ux-design.md`. The mockups are in `{work}/mockups/`. Read them all.

Answer each question with exactly one of its options. Take the answer from what the design and the
mockups show for the main screen of the story. When the design does not decide it, use the option
for "not designed" or "not stated" when it exists, else "other".

Questions (name: options):
{questions}

Reply with only one JSON object: {{"<name>": "<option>", ...}}. No other text."""


def extract(run, model, tries=2):
    """Ask the fixed questions. Try again once when the reply is not valid JSON."""
    for _ in range(tries):
        run, ans = extract_once(run, model)
        if "_error" not in ans:
            break
    (run / "extract.json").write_text(json.dumps(ans, indent=2), encoding="utf-8")
    return run, ans


def extract_once(run, model):
    case = CASES[run.parts[-3]]
    work = f"docs/work/{case['slug']}"
    qs = "\n".join(f"- {k}: {' | '.join(v)}" for k, v in case["questions"].items())
    cmd = ["claude", "-p", PROMPT.format(work=work, questions=qs), "--model", model, "--permission-mode", "default",
           "--allowedTools", "Read", "Glob", "Grep", "--disallowedTools", "Write", "Edit", "Bash", "Agent", "Skill",
           "--output-format", "json"]
    r = subprocess.run(cmd, cwd=run / "repo", env=child_env(), capture_output=True, text=True, timeout=900)
    try:
        # The CLI can print more than one line: take the last line that is a result.
        text = ""
        for line in r.stdout.strip().splitlines():
            try:
                ev = json.loads(line)
            except json.JSONDecodeError:
                continue
            if isinstance(ev, dict) and "result" in ev:
                text = ev.get("result") or ""
        ans = json.loads(re.search(r"\{.*\}", text, re.S).group(0))
    except (json.JSONDecodeError, AttributeError):
        ans = {"_error": (r.stdout + r.stderr)[-500:]}
    return run, ans


def agreement(answers, questions):
    """Share of run pairs that give the same answer, over all questions."""
    same = total = 0
    per_q = {}
    for q in questions:
        vals = [a.get(q) for a in answers]
        pairs = list(itertools.combinations(vals, 2))
        s = sum(1 for x, y in pairs if x is not None and x == y)
        per_q[q] = (s, len(pairs))
        same += s
        total += len(pairs)
    return (same / total if total else 0.0), per_q


def main():
    p = argparse.ArgumentParser()
    p.add_argument("runs")
    p.add_argument("--model", default="sonnet")
    p.add_argument("--parallel", type=int, default=4)
    p.add_argument("--again", action="store_true", help="extract again when extract.json exists")
    args = p.parse_args()
    runs = [f.parent for f in sorted(Path(args.runs).glob("*/*/*/final.md"))]
    todo = [r for r in runs if args.again or not (r / "extract.json").exists()]
    with cf.ThreadPoolExecutor(max_workers=args.parallel) as ex:
        for run, ans in ex.map(lambda r: extract(r, args.model), todo):
            print(f"{run}: {ans}", flush=True)
    groups = {}
    for r in runs:
        groups.setdefault((r.parts[-3], r.parts[-2]), []).append(json.loads((r / "extract.json").read_text()))
    summary = {}
    for (case, variant), answers in sorted(groups.items()):
        score, per_q = agreement(answers, CASES[case]["questions"])
        summary[f"{case}/{variant}"] = {"agreement": round(score, 3), "runs": len(answers),
                                        "per_question": {q: f"{s}/{n}" for q, (s, n) in per_q.items()},
                                        "answers": answers}
        print(f"{case:18} {variant:8} agreement {score:.2f} ({len(answers)} runs)")
    (Path(args.runs) / "determinism.json").write_text(json.dumps(summary, indent=2), encoding="utf-8")


if __name__ == "__main__":
    main()
