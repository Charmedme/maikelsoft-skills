#!/usr/bin/env python3
"""Scores for batch 2 of the ux-design evals.

    python3 evals/ux-design/score2.py <runs> --suite prototype
    python3 evals/ux-design/score2.py <runs> --suite review [--model sonnet] [--parallel 4]

prototype: for each run, find <work>/prototype/prototype.html. Run protocheck.py of the shared
shell on it, render it with Playwright at 390, 820, and 1440 px (overflow, script errors), and
count the variants and states. A shell prototype also gets one screenshot for each variant,
screen, and state (max 24). The screenshots go to <run>/shots2/ for the graders.

review: a reader (read-only claude) maps the findings of <work>/ux-review.md to the planted
defects in DEFECTS.md of the app fixture, and judges each other finding as valid or false. The
clean app runs count the P0 and P1 findings (each is a false alarm).
"""
import argparse
import concurrent.futures as cf
import json
import re
import subprocess
import sys
from pathlib import Path

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
from run import child_env  # noqa: E402

EV = json.loads((HERE / "evals2.json").read_text(encoding="utf-8"))
REPO = HERE.parents[1]
# The same protocheck.py for each variant: the one of this repository.
SHARED = REPO / "shared" / "proto-shell" / "protocheck.py"
WIDTHS = [390, 820, 1440]


def score_prototype(run, slug):
    work = run / "repo" / "docs" / "work" / slug / "prototype"
    files = sorted(work.glob("prototype*.html")) if work.exists() else []
    rec = {"run": str(run), "file": None}
    if not files:
        rec["missing"] = True
        return rec
    f = next((x for x in files if x.name == "prototype.html"), files[0])
    rec["file"] = f.name
    text = f.read_text(encoding="utf-8", errors="replace")
    rec["kb"] = len(text.encode()) // 1024
    rec["shell"] = 'content="proto-shell/1"' in text
    out = subprocess.run([sys.executable, str(SHARED), str(f), "--json"], capture_output=True, text=True)
    try:
        pc = json.loads(out.stdout)
        items = pc if isinstance(pc, list) else pc.get("items", [])
        rec["protocheck_errors"] = len([i for i in items if i.get("level") == "error"])
    except json.JSONDecodeError:
        m = re.search(r"(\d+) errors", out.stdout)
        rec["protocheck_errors"] = int(m.group(1)) if m else None
    rec["protocheck_out"] = out.stdout[-400:]
    m = re.search(r'<script[^>]*id="proto-manifest"[^>]*>(.*?)</script>', text, re.S)
    man = None
    if m:
        try:
            man = json.loads(m.group(1))
        except json.JSONDecodeError:
            pass
    if man:
        rec["variants"] = len(man.get("variants", []))
        rec["states"] = sum(len(s.get("states", [])) for s in man.get("screens", []))
        rec["tasks"] = len(man.get("tasks", []))
        rec["question"] = man.get("question")
    else:
        rec["states"] = len(set(re.findall(r'data-state="([^"]+)"', text)))
    plan = work / "test-plan.md"
    rec["test_plan"] = plan.exists()
    shots = run / "shots2"
    shots.mkdir(exist_ok=True)
    from playwright.sync_api import sync_playwright
    errs, over = [], {}
    with sync_playwright() as p:
        b = p.chromium.launch()
        for w in WIDTHS:
            page = b.new_page(viewport={"width": w, "height": 900})
            page.on("pageerror", lambda e: errs.append(str(e)[:120]))
            page.goto(f.resolve().as_uri())
            page.wait_for_timeout(700)
            over[w] = page.evaluate("() => document.documentElement.scrollWidth - document.documentElement.clientWidth")
            page.screenshot(path=str(shots / f"00-page-{w}.png"), full_page=w != 1440)
            page.close()
        if man and rec["shell"]:
            n = 0
            page = b.new_page(viewport={"width": 1440, "height": 960})
            for v in man.get("variants", []):
                for s in man.get("screens", []):
                    for st in s.get("states", []):
                        if n >= 24:
                            break
                        n += 1
                        page.goto(f.resolve().as_uri() + f"#mode=explore&screen={s['id']}&state={st}&v={v['id']}&size=desktop")
                        page.wait_for_timeout(500)
                        page.screenshot(path=str(shots / f"{n:02d}-{v['id']}-{s['id']}-{st}.png"))
            page.close()
        b.close()
    rec["overflow"] = over
    rec["js_errors"] = len(errs)
    return rec


REVIEW_PROMPT = """You score a UX review of a built app. The planted defects of the app are in `DEFECTS.md`.
The review is in `ux-review.md`. Read both.

1. For each defect D1 to D6, answer: did the review find it (a finding that names this fault, even in other words)? Give the finding title or "no".
2. For each finding of the review that matches no defect, answer: "valid" (a real problem of the app for the user, with evidence) or "false" (wrong, not visible, or only a taste).
3. Give the severity that the review gives to each defect it found.

Reply with only one JSON object:
{"defects": {"D1": {"found": true, "finding": "...", "severity": "P1"}, ...}, "other": [{"finding": "...", "judgment": "valid|false"}], "verdict": "<the verdict of the review>"}"""


def score_review(run, slug, model, defects_file):
    review = run / "repo" / "docs" / "work" / slug / "ux-review.md"
    out_file = run / "review-score.json"
    if out_file.exists():
        return json.loads(out_file.read_text())
    if not review.exists():
        rec = {"run": str(run), "missing": True}
        out_file.write_text(json.dumps(rec), encoding="utf-8")
        return rec
    work = run / "score-work"
    work.mkdir(exist_ok=True)
    (work / "DEFECTS.md").write_text(Path(defects_file).read_text(encoding="utf-8"), encoding="utf-8")
    (work / "ux-review.md").write_text(review.read_text(encoding="utf-8"), encoding="utf-8")
    res = {"_error": "no answer"}
    for _ in range(2):
        cmd = ["claude", "-p", REVIEW_PROMPT, "--model", model, "--permission-mode", "default",
               "--allowedTools", "Read", "--disallowedTools", "Write", "Edit", "Bash", "Agent", "Skill",
               "--output-format", "json"]
        r = subprocess.run(cmd, cwd=work, env=child_env(), capture_output=True, text=True, timeout=900)
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
    text = review.read_text(encoding="utf-8")
    rec = {"run": str(run), "result": res,
           "found": sum(1 for d in res.get("defects", {}).values() if isinstance(d, dict) and d.get("found")),
           "valid_other": sum(1 for o in res.get("other", []) if o.get("judgment") == "valid"),
           "false_other": sum(1 for o in res.get("other", []) if o.get("judgment") == "false"),
           "p0p1": len(re.findall(r"\bP[01]\b", text)),
           "verdict": "CHANGES_REQUIRED" if "CHANGES_REQUIRED" in text else ("DONE" if re.search(r"\bDONE\b", text) else "?")}
    meta = json.loads((run / "meta.json").read_text())
    rec["cost_usd"] = meta.get("cost_usd")
    out_file.write_text(json.dumps(rec, indent=2), encoding="utf-8")
    return rec


def score_handoff(run, slug):
    """State coverage of examples.md: the share of the design states (not n/a) that have an example."""
    import importlib.util
    work = run / "repo" / "docs" / "work" / slug
    rec = {"run": str(run)}
    ex = work / "examples.md"
    if not ex.exists():
        rec["missing"] = True
        return rec
    spec = importlib.util.spec_from_file_location("ec", REPO / "shared" / "examplecheck.py")
    ec = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(ec)
    design = (work / "ux-design.md").read_text(encoding="utf-8")
    text = ex.read_text(encoding="utf-8")
    errors, waiting, rules, questions = ec.check(text, design)
    states = ec.ux_states(design)
    # States that the design marks n/a are not required.
    na = set()
    for line in design.splitlines():
        m = ec.ROW.match(line)
        if m:
            cells = [c.strip().strip("`").lower() for c in m.group(1).split("|")]
            if len(cells) > 1 and cells[1].startswith("n/a"):
                na.add(cells[0])
    need = {(scr, st) for scr, sts in states.items() for st in sts if st not in na and st != "state"}
    rows = [ln for ln in text.splitlines() if ec.ROW.match(ln) and not set(ln) <= set("|-: ")]
    covered = set()
    for scr, st in need:
        pat = re.compile(rf"ux:\s*{re.escape(scr)}/{re.escape(st)}\b|\|\s*`?{re.escape(st)}`?\s*\|", re.I)
        if any(pat.search(r) for r in rows):
            covered.add((scr, st))
    judged = judge_handoff(run, slug, "sonnet", need, design)
    rec["judged_coverage"] = round(sum(1 for v in judged.values() if v is True) / len(need), 2) if need and judged else None
    rec.update({"errors": len(errors), "rules": rules, "examples": max(0, len(rows) - 1),
                "ux_rows": sum(1 for r in rows if re.search(r"\bux:", r)),
                "states_needed": len(need), "states_covered": len(covered),
                "coverage": round(len(covered) / len(need), 2) if need else None,
                "open_questions": len(questions)})
    return rec


HANDOFF_PROMPT = """You check if a list of build examples covers the states of a UX design.
The states are in `states.txt` (one `<screen>/<state>: <what the user sees>` per line). The examples are in `examples.md`.
For each state, answer true when at least one example row tests that state (any words), else false.
Reply with only one JSON object: {"<screen>/<state>": true|false, ...}"""


def judge_handoff(run, slug, model, need, design):
    out = run / "handoff-judge.json"
    if out.exists():
        return json.loads(out.read_text())
    work = run / "score-work"
    work.mkdir(exist_ok=True)
    rows = {}
    for line in design.splitlines():
        m = re.match(r"^\|\s*`?([\w-]+)`?\s*\|\s*([^|]+)\|", line)
        if m:
            rows.setdefault(m.group(1).lower(), m.group(2).strip()[:120])
    (work / "states.txt").write_text("\n".join(f"{scr}/{st}: {rows.get(st, '')}" for scr, st in sorted(need)), encoding="utf-8")
    (work / "examples.md").write_text((run / "repo" / "docs" / "work" / slug / "examples.md").read_text(encoding="utf-8"), encoding="utf-8")
    res = {}
    for _ in range(2):
        cmd = ["claude", "-p", HANDOFF_PROMPT, "--model", model, "--permission-mode", "default",
               "--allowedTools", "Read", "--disallowedTools", "Write", "Edit", "Bash", "Agent", "Skill", "--output-format", "json"]
        r = subprocess.run(cmd, cwd=work, env=child_env(), capture_output=True, text=True, timeout=900)
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
            res = {}
    out.write_text(json.dumps(res, indent=2), encoding="utf-8")
    return res


def main():
    p = argparse.ArgumentParser()
    p.add_argument("runs")
    p.add_argument("--suite", required=True, choices=["prototype", "review", "handoff"])
    p.add_argument("--model", default="sonnet")
    p.add_argument("--parallel", type=int, default=4)
    args = p.parse_args()
    suite = EV[args.suite]
    root = Path(args.runs) / args.suite
    runs = sorted(r.parent for r in root.glob("*/*/final.md")) + sorted(r.parent for r in root.glob("*/*/*/final.md"))
    if args.suite == "handoff":
        slugs = {c["name"]: c["slug"] for c in suite["cases"]}
        recs = [score_handoff(r, slugs[r.parts[-3]]) for r in runs if r.parts[-3] in slugs]
    elif args.suite == "prototype":
        recs = [score_prototype(r, suite["slug"]) for r in runs]
    else:
        def defects_of(r):
            app = "hard" if r.parts[-2].endswith("-hard") else "defects"
            return HERE / suite["app"][app] / "DEFECTS.md"
        with cf.ThreadPoolExecutor(max_workers=args.parallel) as ex:
            recs = list(ex.map(lambda r: score_review(r, suite["slug"], args.model, defects_of(r)), runs))
    for rec in recs:
        rec["cost_usd"] = json.loads((Path(rec["run"]) / "meta.json").read_text()).get("cost_usd")
        short = {k: v for k, v in rec.items() if k not in ("protocheck_out", "result", "run")}
        print(Path(rec["run"]).relative_to(root), json.dumps(short))
    (root / "scores.json").write_text(json.dumps(recs, indent=2), encoding="utf-8")


if __name__ == "__main__":
    main()
