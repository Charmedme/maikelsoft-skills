#!/usr/bin/env python3
"""Blind pairwise grading of baseline and skill runs.

    python3 evals/ux-design/grade.py <runs> [--model opus] [--parallel 3]

For each case and run number n, the grader compares baseline n with skill n. It sees the story,
PRODUCT.md, DESIGN.md, and the decision log, and for each design "A" and "B": the screenshots of the
mockups (visual grade) or ux-design.md (design grade). The order is random and each pair is graded
two times with A and B swapped, so a position bias cancels. The names of the variants are hidden.
Result: grades.json in <runs>.
"""
import argparse
import concurrent.futures as cf
import json
import random
import re
import shutil
import subprocess
import sys
from pathlib import Path

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
from run import child_env  # noqa: E402

EV = json.loads((HERE / "evals.json").read_text(encoding="utf-8"))
SLUG = {c["name"]: c["slug"] for c in EV["cases"]}
CASES = {c["name"]: c for c in EV["cases"]}

CRITERIA = {
    "visual": {
        "specific": "Which design is more specific to Fieldline, its planners, and this story (domain words, realistic data, fits the work), and less generic?",
        "system": "Which design follows the Fieldline design system more closely (its components, tokens, density, one primary action, no decoration)?",
        "clarity": "Which design makes the main task of the story clearer (what the eye sees first, order, hierarchy)?",
        "states": "Which design covers the states that the story needs (empty, loading, errors, partial failure, waiting, and so on) better?",
    },
    "design": {
        "thought_through": "Which design covers the acceptance criteria, the edge cases, and the failure paths more completely?",
        "grounded": "Which design invents fewer facts about users, and marks its assumptions and reasons more clearly?",
        "buildable": "With which design can a developer build the screens with fewer guesses?",
    },
}

PROMPT = """You are a senior UX design lead. You compare two designs, A and B, for the same story. You do not
know who made them. Judge the content, not the length or the document format.

Context (read these first): the files in `context/` (the story, the product, and the design system of the project when it has one).
Design A: the files in `A/`. Design B: the files in `B/`. {what}

For each criterion, answer "A", "B", or "tie", with one short reason.

Criteria:
{criteria}

Reply with only one JSON object: {{"<criterion>": {{"winner": "A|B|tie", "reason": "..."}}, ...}}. No other text."""


def stage(dst, run, kind, slug):
    dst.mkdir(parents=True)
    work = run / "repo" / "docs" / "work" / slug
    if kind == "visual":
        for png in sorted((run / "shots").glob("*.png")):
            shutil.copy(png, dst / png.name)
    else:
        # All design documents of the run: the design, an issues file, a foundation DESIGN.md.
        for md in sorted(work.rglob("*.md")):
            if md.name == "story.md":
                continue
            shutil.copy(md, dst / str(md.relative_to(work)).replace("/", "__"))


def grade_pair(job, model):
    case, n, kind, swap, root, (vx, vy) = job
    tag = f"{case}-{n}-{kind}-{'swap' if swap else 'plain'}" + ("" if (vx, vy) == ("baseline", "skill") else f"-{vx}-vs-{vy}")
    out_file = root / "grading" / f"{tag}.json"
    if out_file.exists():
        return json.loads(out_file.read_text())
    base, skill = root / case / vx / str(n), root / case / vy / str(n)
    a, b = (skill, base) if swap else (base, skill)
    work = root / "grading" / tag
    if work.exists():
        shutil.rmtree(work)
    ctx = work / "context"
    ctx.mkdir(parents=True)
    repo = base / "repo"
    shutil.copy(repo / "docs" / "work" / SLUG[case] / "story.md", ctx / "story.md")
    for f in ("PRODUCT.md", "DESIGN.md"):
        if (repo / f).exists():
            shutil.copy(repo / f, ctx / f)
    if (repo / "docs" / "ux" / "decisions.md").exists():
        shutil.copy(repo / "docs" / "ux" / "decisions.md", ctx / "decisions.md")
    if (repo / "design-system").exists():
        shutil.copytree(repo / "design-system", ctx / "design-system")
    stage(work / "A", a, kind, SLUG[case])
    stage(work / "B", b, kind, SLUG[case])
    what = ("Each folder has PNG screenshots of the HTML mockups. Look at each image." if kind == "visual"
            else "Each folder has the design documents of that design (Markdown files).")
    crits = dict(CRITERIA[kind])
    crits.update(CASES[case].get("extra_criteria", {}).get(kind, {}))
    crit = "\n".join(f"- {k}: {v}" for k, v in crits.items())
    cmd = ["claude", "-p", PROMPT.format(what=what, criteria=crit), "--model", model, "--permission-mode", "default",
           "--allowedTools", "Read", "Glob", "--disallowedTools", "Write", "Edit", "Bash", "Agent", "Skill",
           "--output-format", "json"]
    r = subprocess.run(cmd, cwd=work, env=child_env(), capture_output=True, text=True, timeout=1200)
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
        res = json.loads(re.search(r"\{.*\}", text, re.S).group(0))
    except (json.JSONDecodeError, AttributeError):
        res = {"_error": (r.stdout + r.stderr)[-500:]}
    # Map A and B back to the variants.
    mapped = {}
    for crit_name, v in res.items():
        if isinstance(v, dict) and v.get("winner") in ("A", "B", "tie"):
            w = v["winner"]
            variant = "tie" if w == "tie" else ((vy if w == "A" else vx) if swap else (vx if w == "A" else vy))
            mapped[crit_name] = {"winner": variant, "reason": v.get("reason", "")}
        else:
            mapped[crit_name] = v
    rec = {"case": case, "n": n, "kind": kind, "swap": swap, "pair": f"{vx}:{vy}", "result": mapped}
    out_file.write_text(json.dumps(rec, indent=2), encoding="utf-8")
    return rec


def main():
    p = argparse.ArgumentParser()
    p.add_argument("runs")
    p.add_argument("--model", default="opus")
    p.add_argument("--parallel", type=int, default=3)
    p.add_argument("--pairs", default="baseline:skill", help="comma list of x:y variant pairs")
    args = p.parse_args()
    pairs = [tuple(x.split(":")) for x in args.pairs.split(",")]
    root = Path(args.runs).resolve()
    (root / "grading").mkdir(exist_ok=True)
    jobs = []
    for case_dir in sorted(d for d in root.iterdir() if d.name in SLUG):
        for vx, vy in pairs:
            for n_dir in sorted((case_dir / vx).glob("*")):
                n = n_dir.name
                if not (case_dir / vy / n / "final.md").exists() or not (n_dir / "final.md").exists():
                    continue
                for kind in CRITERIA:
                    for swap in (False, True):
                        jobs.append((case_dir.name, n, kind, swap, root, (vx, vy)))
    random.seed(7)
    random.shuffle(jobs)
    with cf.ThreadPoolExecutor(max_workers=args.parallel) as ex:
        results = list(ex.map(lambda j: grade_pair(j, args.model), jobs))
    tally = {}
    for rec in results:
        for crit, v in rec["result"].items():
            if isinstance(v, dict):
                t = tally.setdefault(f'{rec.get("pair", "baseline:skill")} {rec["kind"]}/{crit}', {})
                t[v["winner"]] = t.get(v["winner"], 0) + 1
    for k, t in sorted(tally.items()):
        print(f"{k:44} " + "  ".join(f"{w} {c}" for w, c in sorted(t.items())))
    (root / "grades.json").write_text(json.dumps({"tally": tally, "results": results}, indent=2), encoding="utf-8")


if __name__ == "__main__":
    main()
