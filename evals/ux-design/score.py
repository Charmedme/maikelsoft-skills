#!/usr/bin/env python3
"""Score the ux-design runs with the deterministic checks.

    python3 evals/ux-design/score.py <runs> --plugin <plugin folder>

For each run folder with a final.md: runs `uxcheck.py mockup` (tells, frames, screenshots) and
`uxcheck.py design` (the spec format), and writes score.json next to final.md.
"""
import argparse
import json
import subprocess
import sys
from collections import Counter
from pathlib import Path

HERE = Path(__file__).resolve().parent
EV = json.loads((HERE / "evals.json").read_text(encoding="utf-8"))
SLUG = {c["name"]: c["slug"] for c in EV["cases"]}
CASES = {c["name"]: c for c in EV["cases"]}


def fault_scores(case, work, tells):
    """Planted faults of the faulty fixture: named in the design files, and still in the mockups."""
    faults = json.loads((HERE / case["faults"]).read_text(encoding="utf-8"))
    lines = []
    for md in work.rglob("*.md"):
        if md.name != "story.md":
            lines += md.read_text(encoding="utf-8", errors="replace").lower().splitlines()
    html = " ".join(f.read_text(encoding="utf-8", errors="replace") for f in work.rglob("*.html"))
    css = " ".join(f.read_text(encoding="utf-8", errors="replace") for f in (work / "mockups").rglob("*.css")) if (work / "mockups").exists() else ""
    detected, propagated = [], []
    for f in faults:
        a, b = f["doc"]
        if any(any(x in ln for x in a) and any(y in ln for y in b) for ln in lines):
            detected.append(f["id"])
        if f["id"] == "F6":
            if ":focus-visible" not in css and ":focus-visible" not in html:
                propagated.append("F6")
        elif f["id"] == "F7":
            if "fl-dot" in html:
                propagated.append("F7")
        elif f["id"] == "F9":
            if tells.get("TELL-04") or "fl-card" in html:
                propagated.append("F9")
        elif f["id"] == "F4":
            if tells.get("TELL-02"):
                propagated.append("F4")
        elif f["tell"] and tells.get(f["tell"]):
            propagated.append(f["id"])
    return {"faults_total": len(faults), "faults_detected": detected, "faults_propagated": propagated}


def uxcheck(plugin, args):
    script = Path(plugin) / "skills" / "ux-practice" / "scripts" / "uxcheck.py"
    r = subprocess.run([sys.executable, str(script)] + args + ["--json"], capture_output=True, text=True, timeout=600)
    try:
        return json.loads(r.stdout)
    except json.JSONDecodeError:
        return {"items": [], "errors": None, "warnings": None, "raw": r.stdout[-2000:] + r.stderr[-2000:]}


def score_run(run, plugin):
    case, variant = run.parts[-3], run.parts[-2]
    work = run / "repo" / "docs" / "work" / SLUG[case]
    design = work / "ux-design.md"
    mockups = sorted((work / "mockups").glob("*.html")) if (work / "mockups").exists() else []
    out = {"case": case, "variant": variant, "run": run.name, "design_exists": design.exists(),
           "mockup_files": len(mockups)}
    meta = run / "meta.json"
    if meta.exists():
        m = json.loads(meta.read_text())
        out.update({"cost_usd": m.get("cost_usd"), "seconds": m.get("seconds"), "exit": m.get("exit")})
    if mockups:
        args = ["mockup"] + [str(f) for f in mockups] + ["--shots", str(run / "shots")]
        if design.exists():
            args += ["--design", str(design)]
        mk = uxcheck(plugin, args)
        rules = Counter(i["rule"] for i in mk.get("items", []) if i["level"] == "error")
        out["tells"] = sum(v for k, v in rules.items() if k.startswith("TELL"))
        out["tells_by_rule"] = dict(sorted((k, v) for k, v in rules.items() if k.startswith("TELL")))
        out["missing_state_frames"] = rules.get("MK-STATE", 0)
        out["frames"] = mk.get("frames", 0)
    case_def = CASES[case]
    if case_def.get("faults"):
        out.update(fault_scores(case_def, work, out.get("tells_by_rule", {})))
    if case_def.get("fixture") == "empty":
        out["foundation_files"] = sorted(str(p.relative_to(run / "repo")) for p in (run / "repo").rglob("*.css"))
    if design.exists():
        ds = uxcheck(plugin, ["design", str(design), "--patterns",
                              str(Path(plugin) / "skills" / "ux-patterns" / "references" / "patterns.md"),
                              "--decisions", str(run / "repo" / "docs" / "ux" / "decisions.md")])
        out["design_errors"] = ds.get("errors")
        out["design_rules"] = dict(Counter(i["rule"] for i in ds.get("items", []) if i["level"] == "error"))
        out["decisions_total"] = ds.get("decisions", 0)
        out["decisions_sourced"] = ds.get("sourced", 0)
    (run / "score.json").write_text(json.dumps(out, indent=2), encoding="utf-8")
    return out


def main():
    p = argparse.ArgumentParser()
    p.add_argument("runs")
    p.add_argument("--plugin", required=True)
    p.add_argument("--variant", nargs="*", help="score only these variants (others keep their score.json)")
    args = p.parse_args()
    for final in sorted(Path(args.runs).glob("*/*/*/final.md")):
        if args.variant and final.parent.parts[-2] not in args.variant:
            continue
        s = score_run(final.parent, args.plugin)
        print(json.dumps({k: s.get(k) for k in ("case", "variant", "run", "frames", "tells", "missing_state_frames",
                                                 "design_errors", "decisions_sourced", "cost_usd")}))


if __name__ == "__main__":
    main()
