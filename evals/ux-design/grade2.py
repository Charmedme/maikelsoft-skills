#!/usr/bin/env python3
"""Blind pairwise grading of the prototype suite.

    python3 evals/ux-design/grade2.py <runs> --pairs baseline:ux,pp-prev:pp-new,pp-new:ux [--model sonnet]

For each pair and run number n, the grader sees the story, the approved design and mockups, and for
each prototype "A" and "B": its screenshots (shots2/ from score2.py) and its test plan. Each pair
is graded two times with A and B swapped. The names of the variants are hidden.
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

EV = json.loads((HERE / "evals2.json").read_text(encoding="utf-8"))
SLUG = EV["prototype"]["slug"]

CRITERIA = {
    "question": "Which prototype targets a more important open question of the approved design, and answers it more directly?",
    "test_ready": "With which prototype and test plan can a moderator run a user test today with fewer gaps (tasks as user goals, a success rule, the order of variants, the participants, how results are recorded)?",
    "fidelity": "Which prototype matches the approved design and mockups more closely (structure, copy, the components and tokens of the design system)?",
    "states": "Which prototype shows the states that its question needs (errors, partial result, empty, loading) better?",
    "clarity": "In which prototype is the main task clearer for a participant (what the eye sees first, order, hierarchy)?",
}

PROMPT = """You are a senior UX researcher. You compare two clickable prototypes, A and B, made for a test with
real users of the same approved design. You do not know who made them. Judge the content, not the length.

Context (read these first): `context/story.md`, `context/ux-design.md` (the approved design), and the
screenshots of the approved mockups in `context/mockups/`.
Prototype A: the files in `A/`. Prototype B: the files in `B/`. Each folder has PNG screenshots of the
prototype (look at each image) and its test plan `test-plan.md` when it has one.

For each criterion, answer "A", "B", or "tie", with one short reason.

Criteria:
{criteria}

Reply with only one JSON object: {{"<criterion>": {{"winner": "A|B|tie", "reason": "..."}}, ...}}. No other text."""


def stage(dst, run):
    dst.mkdir(parents=True)
    for png in sorted((run / "shots2").glob("*.png")):
        shutil.copy(png, dst / png.name)
    plan = run / "repo" / "docs" / "work" / SLUG / "prototype" / "test-plan.md"
    if plan.exists():
        shutil.copy(plan, dst / "test-plan.md")


def grade_pair(job, model, root, ctx_src):
    n, swap, (vx, vy) = job
    tag = f"{n}-{vx}-vs-{vy}-{'swap' if swap else 'plain'}"
    out_file = root / "grading" / f"{tag}.json"
    if out_file.exists():
        return json.loads(out_file.read_text())
    rx, ry = root / vx / n, root / vy / n
    a, b = (ry, rx) if swap else (rx, ry)
    work = root / "grading" / tag
    if work.exists():
        shutil.rmtree(work)
    shutil.copytree(ctx_src, work / "context")
    stage(work / "A", a)
    stage(work / "B", b)
    crit = "\n".join(f"- {k}: {v}" for k, v in CRITERIA.items())
    res = {"_error": "no answer"}
    for _ in range(2):
        cmd = ["claude", "-p", PROMPT.format(criteria=crit), "--model", model, "--permission-mode", "default",
               "--allowedTools", "Read", "Glob", "--disallowedTools", "Write", "Edit", "Bash", "Agent", "Skill",
               "--output-format", "json"]
        r = subprocess.run(cmd, cwd=work, env=child_env(), capture_output=True, text=True, timeout=1200)
        text = ""
        for line in r.stdout.strip().splitlines():
            try:
                ev = json.loads(line)
            except json.JSONDecodeError:
                continue
            if isinstance(ev, dict) and "result" in ev:
                text = ev.get("result") or ""
        try:
            res = json.loads(re.search(r"\{.*\}", text, re.S).group(0))
            break
        except (json.JSONDecodeError, AttributeError):
            res = {"_error": (r.stdout + r.stderr)[-300:]}
    mapped = {}
    for name, v in res.items():
        if isinstance(v, dict) and v.get("winner") in ("A", "B", "tie"):
            w = v["winner"]
            winner = "tie" if w == "tie" else ((vy if w == "A" else vx) if swap else (vx if w == "A" else vy))
            mapped[name] = {"winner": winner, "reason": v.get("reason", "")}
    rec = {"n": n, "swap": swap, "pair": f"{vx}:{vy}", "result": mapped}
    out_file.write_text(json.dumps(rec, indent=2), encoding="utf-8")
    return rec


def make_context(root):
    """The story, the approved design, and screenshots of the approved mockups (one per frame)."""
    ctx = root / "grading" / "_context"
    if ctx.exists():
        return ctx
    src = HERE / EV["prototype"]["design"]
    (ctx / "mockups").mkdir(parents=True)
    shutil.copy(src / "story.md", ctx / "story.md")
    shutil.copy(src / "ux-design.md", ctx / "ux-design.md")
    # Render the approved mockups, one image for each frame, with the checker of the plugin.
    # The mockups link the design system with ../../../../design-system/, so render them in place.
    stage_dir = root / "grading" / "_render"
    repo = stage_dir / "repo"
    if not repo.exists():
        shutil.copytree(HERE / "fixtures" / "fieldline", repo, ignore=shutil.ignore_patterns("stories"))
        shutil.copytree(src, repo / "docs" / "work" / SLUG)
    uxcheck = HERE.parents[1] / "plugins" / "ux-design" / "skills" / "ux-practice" / "scripts" / "uxcheck.py"
    subprocess.run([sys.executable, str(uxcheck), "mockup", *map(str, (repo / "docs" / "work" / SLUG / "mockups").glob("*.html")),
                    "--shots", str(ctx / "mockups")], capture_output=True, text=True)
    return ctx


def main():
    p = argparse.ArgumentParser()
    p.add_argument("runs")
    p.add_argument("--model", default="sonnet")
    p.add_argument("--parallel", type=int, default=4)
    p.add_argument("--pairs", default="baseline:ux,pp-prev:pp-new,pp-new:ux")
    args = p.parse_args()
    root = (Path(args.runs) / "prototype").resolve()
    (root / "grading").mkdir(exist_ok=True)
    ctx = make_context(root)
    pairs = [tuple(x.split(":")) for x in args.pairs.split(",")]
    jobs = []
    for vx, vy in pairs:
        for n_dir in sorted((root / vx).glob("*")):
            n = n_dir.name
            if (root / vy / n / "shots2").exists() and (n_dir / "shots2").exists():
                for swap in (False, True):
                    jobs.append((n, swap, (vx, vy)))
    random.seed(7)
    random.shuffle(jobs)
    with cf.ThreadPoolExecutor(max_workers=args.parallel) as ex:
        results = list(ex.map(lambda j: grade_pair(j, args.model, root, ctx), jobs))
    tally = {}
    for rec in results:
        for crit, v in rec["result"].items():
            t = tally.setdefault(f'{rec["pair"]} {crit}', {})
            t[v["winner"]] = t.get(v["winner"], 0) + 1
    for k, t in sorted(tally.items()):
        print(f"{k:36} " + "  ".join(f"{w} {c}" for w, c in sorted(t.items())))
    (root / "grades.json").write_text(json.dumps({"tally": tally, "results": results}, indent=2), encoding="utf-8")


if __name__ == "__main__":
    main()
