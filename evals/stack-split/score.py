#!/usr/bin/env python3
"""Score the runs of the stack-split behaviour test with the regex checks of scenarios.json.

    python3 evals/stack-split/score.py <runs> [--judge <runs>/judge.json] [--scenarios scenarios-p3.json]

A "must" check passes when its pattern matches a line in one of the files of the work folder.
A "never" check fails when its pattern matches a line that is not a comment line. The score of a run is
(passed must checks + clean never checks) / (all checks). A run with no written file scores 0.
The script writes <runs>/scores.json and prints a table: scenario x arm x model.
"""
import argparse
import json
import pathlib
import re
import sys
from collections import defaultdict

HERE = pathlib.Path(__file__).resolve().parent
# A comment line can name an old pattern to explain its removal. The "never" checks skip it.
COMMENT = re.compile(r"^\s*(//|#|@\*|\*|<!--|/\*)")
TEXT = {".cs", ".csproj", ".json", ".razor", ".props", ".yaml", ".yml", ".js", ".ts", ".html", ".md", ".cshtml", ".xml", ".txt", ".feature", ".config", ".runsettings"}


def files_text(work: pathlib.Path, seed: dict) -> str:
    """All text of the work folder. A seed file that the run did not change still counts."""
    parts = []
    for f in sorted(work.rglob("*")):
        if f.is_file() and f.suffix in TEXT and "obj" not in f.parts and "bin" not in f.parts:
            parts.append(f.read_text(encoding="utf-8", errors="replace"))
    return "\n".join(parts)


def changed(work: pathlib.Path, seed: dict) -> bool:
    """True when the run wrote a new file or changed a seed file."""
    for f in work.rglob("*"):
        if not f.is_file():
            continue
        rel = f.relative_to(work).as_posix()
        if rel not in seed or f.read_text(encoding="utf-8", errors="replace") != seed[rel]:
            return True
    return False


def score_run(work: pathlib.Path, sc: dict) -> dict:
    if not changed(work, sc["seed"]):
        return {"score": 0.0, "written": False, "must": {}, "never": {}}
    text = files_text(work, sc["seed"])
    must = {name: bool(re.search(p, text, re.M)) for name, p in sc["must"].items()}
    code = "\n".join(line for line in text.splitlines() if not COMMENT.match(line))
    never = {name: bool(re.search(p, code, re.M)) for name, p in sc["never"].items()}
    total = len(must) + len(never)
    ok = sum(must.values()) + sum(not v for v in never.values())
    return {"score": round(ok / total, 3), "written": True, "must": must, "never": never}


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("runs")
    parser.add_argument("--judge", help="judge.json: {run id: {grade: 0-10}}")
    parser.add_argument("--scenarios", default="scenarios.json", help="file in evals/stack-split")
    args = parser.parse_args()
    runs = pathlib.Path(args.runs).resolve()
    scenarios = json.loads((HERE / args.scenarios).read_text(encoding="utf-8"))
    plan = json.loads((runs / "plan.json").read_text(encoding="utf-8"))
    judge = json.loads(pathlib.Path(args.judge).read_text(encoding="utf-8")) if args.judge else {}

    results = {}
    for run in plan:
        r = score_run(pathlib.Path(run["work"]), scenarios[run["scenario"]])
        r.update({k: run[k] for k in ("scenario", "arm", "model")})
        if run["id"] in judge:
            r["grade"] = judge[run["id"]].get("grade")
        results[run["id"]] = r
    (runs / "scores.json").write_text(json.dumps(results, indent=1), encoding="utf-8")

    table = defaultdict(list)
    old = defaultdict(int)
    grades = defaultdict(list)
    for r in results.values():
        table[(r["scenario"], r["arm"], r["model"])].append(r["score"])
        old[(r["arm"], r["model"])] += sum(r["never"].values())
        if r.get("grade") is not None:
            grades[(r["arm"], r["model"])].append(r["grade"])
    models = sorted({r["model"] for r in results.values()}, key=["haiku", "sonnet", "opus"].index)
    arms = ["baseline", "current", "new"]
    print("| Scenario | " + " | ".join(f"{a} {m}" for m in models for a in arms) + " |")
    print("|---" * (1 + len(models) * len(arms)) + "|")
    for sid in sorted({k[0] for k in table}):
        cells = []
        for m in models:
            for a in arms:
                v = table.get((sid, a, m))
                cells.append(f"{sum(v) / len(v):.2f}" if v else "-")
        print(f"| {sid} | " + " | ".join(cells) + " |")
    print()
    print("| Model | Arm | Mean check score | Old-pattern hits | Mean judge grade |")
    print("|---|---|---|---|---|")
    for m in models:
        for a in arms:
            vals = [s for (sid, aa, mm), v in table.items() if aa == a and mm == m for s in v]
            if not vals:
                continue
            g = grades.get((a, m))
            print(f"| {m} | {a} | {sum(vals) / len(vals):.2f} | {old[(a, m)]} | "
                  f"{(sum(g) / len(g)):.1f} |" if g else
                  f"| {m} | {a} | {sum(vals) / len(vals):.2f} | {old[(a, m)]} | - |")
    return 0


if __name__ == "__main__":
    sys.exit(main())
