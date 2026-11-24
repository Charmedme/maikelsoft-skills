#!/usr/bin/env python3
"""Score the codefix choice runs against answers.json.

    python3 evals/security/codefix/score.py <runs>

Reads <runs>/results.json: a list of {"id": <run id>, "answers": [{"id": "CF01", "choice": "A"}, ...]}.
A run with no answer for a challenge counts that challenge as wrong. A challenge with "disputed" in
answers.json does not count. Writes <runs>/scores.json and prints
the correct share for each skill, arm, and model, and the challenges where the two arms differ.
"""
import json
import pathlib
import sys
from collections import defaultdict

HERE = pathlib.Path(__file__).resolve().parent


def main() -> int:
    runs = pathlib.Path(sys.argv[1]).resolve()
    key = json.loads((HERE / "answers.json").read_text(encoding="utf-8"))
    plan = {r["id"]: r for r in json.loads((runs / "plan.json").read_text(encoding="utf-8"))}
    results = {r["id"]: r for r in json.loads((runs / "results.json").read_text(encoding="utf-8"))}

    rows = []
    for rid, run in plan.items():
        got = {a["id"]: str(a.get("choice", "")).strip().upper()[:1] for a in (results.get(rid) or {}).get("answers") or []}
        for cid in [i for i in run["ids"] if not key[i].get("disputed")]:
            skill = run["set"].rsplit("-", 1)[0]
            rows.append({"run": rid, "challenge": cid, "skill": skill, "arm": run["arm"], "model": run["model"],
                         "choice": got.get(cid, ""), "correct": got.get(cid) == key[cid]["correct"]})
    (runs / "scores.json").write_text(json.dumps(rows, indent=1), encoding="utf-8")

    total = defaultdict(lambda: [0, 0])
    per = defaultdict(lambda: [0, 0])
    for r in rows:
        for k in ((r["skill"], r["arm"], r["model"]), ("all", r["arm"], r["model"])):
            total[k][0] += r["correct"]
            total[k][1] += 1
        per[(r["challenge"], r["arm"])][0] += r["correct"]
        per[(r["challenge"], r["arm"])][1] += 1

    print("| Skill | Model | Baseline | Skill |")
    print("|---|---|---|---|")
    for skill in ("secure-coding", "web-security", "all"):
        for model in ("haiku", "sonnet", "opus"):
            b, s = total.get((skill, "baseline", model)), total.get((skill, "skill", model))
            if b and s:
                print(f"| {skill} | {model} | {b[0]}/{b[1]} | {s[0]}/{s[1]} |")
    print()
    print("Challenges where the arms differ (correct runs, baseline -> skill):")
    for cid in key:
        b, s = per.get((cid, "baseline")), per.get((cid, "skill"))
        if b and s and b[0] != s[0]:
            print(f"  {cid} {key[cid]['source']}: {b[0]}/{b[1]} -> {s[0]}/{s[1]}")
    always = [cid for cid in key if per.get((cid, "baseline")) and per[(cid, "baseline")][0] == per[(cid, "baseline")][1]]
    print(f"Baseline correct in each run: {len(always)} of {len(key)} challenges")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
