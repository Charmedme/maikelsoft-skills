#!/usr/bin/env python3
"""Find where the runs of a variant agree and where they disagree, item by item.

    python3 evals/dev-stack/divergence.py <runs> [--json out.json] [--case NAME ...]

An item is a check (C*), a judge assertion (J*), or a planted review mistake (P*). For each case
and item, the script counts the passing runs of each variant and gives the item one class:

    gap        the skill variant fails in every run: the skill does not cover it
    chance     the skill variant passes in some runs only: the skill leaves it to chance
    worse      the skill variant passes less often than the baseline
    skill-win  the skill variant passes in every run and the baseline does not: keep the rule
    no-effect  both variants pass in every run: the rule behind it adds nothing for this model
    base-fail  (not used: covered by skill-win)

Only "no-effect" items that test a rule of the skill are candidates to cut. Generic checks (build,
tests, format, files unchanged) are listed, but marked generic.
"""
import argparse
import json
import pathlib
import sys

HERE = pathlib.Path(__file__).resolve().parent
GENERIC = {"build_warnaserror", "build", "tests_pass", "format", "files_unchanged", "package_policy"}


def load(path):
    try:
        return json.loads(path.read_text(encoding="utf-8"))
    except (OSError, ValueError):
        return None


def describe(ev):
    """Item id -> (text, generic) for one case."""
    items = {}
    for a in ev.get("assertions", []):
        if "judge" in a:
            items[a["id"]] = (a["judge"], False)
        else:
            text = a.get("trap") or a["check"]
            items[a["id"]] = (text, a["check"] in GENERIC and not a.get("trap"))
    if ev.get("planted"):
        planted = load(HERE / ev["planted"]) or {}
        for p in planted.get("planted", planted if isinstance(planted, list) else []):
            items[p["id"]] = (p.get("title") or p.get("summary") or p.get("description", ""), False)
    return items


def run_items(run):
    """Item id -> passed, for one run. None when the run has no scores."""
    checks = load(run / "checks.json")
    grade = load(run / "grade.json")
    if checks is None and grade is None:
        return None
    out = {}
    for c in checks if isinstance(checks, list) else (checks or {}).get("results", []):
        out[c["id"]] = bool(c["ok"])
    for j in (grade or {}).get("results", []):
        out[j["id"]] = bool(j.get("pass"))
    return out, set((grade or {}).get("found", []))


def classify(base, skill, n_base, n_skill):
    if skill < base * n_skill / max(n_base, 1):
        return "worse"
    if skill == 0:
        return "gap"
    if skill < n_skill:
        return "chance"
    if base < n_base:
        return "skill-win"
    return "no-effect"


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("runs", type=pathlib.Path)
    ap.add_argument("--json", type=pathlib.Path)
    ap.add_argument("--case", nargs="*")
    args = ap.parse_args()

    evals = {e["name"]: e for e in json.loads((HERE / "evals.json").read_text(encoding="utf-8"))["evals"]}
    rows = []
    for case_dir in sorted(p for p in args.runs.iterdir() if p.is_dir()):
        name = case_dir.name
        if name not in evals or (args.case and name not in args.case):
            continue
        items = describe(evals[name])
        counts = {}
        for variant in ("baseline", "skill"):
            runs = [r for r in sorted((case_dir / variant).glob("*")) if r.is_dir()]
            scored = [x for x in (run_items(r) for r in runs) if x is not None]
            counts[variant] = {"n": len(scored), "pass": {}}
            for result, found in scored:
                for item in items:
                    ok = found.__contains__(item) if item.startswith("P") else result.get(item)
                    if ok is None:
                        continue
                    counts[variant]["pass"][item] = counts[variant]["pass"].get(item, 0) + int(ok)
        nb, ns = counts["baseline"]["n"], counts["skill"]["n"]
        for item, (text, generic) in items.items():
            if item not in counts["skill"]["pass"] and item not in counts["baseline"]["pass"]:
                continue
            b = counts["baseline"]["pass"].get(item, 0)
            s = counts["skill"]["pass"].get(item, 0)
            rows.append({"case": name, "skill": evals[name].get("skill"), "item": item, "base": b, "n_base": nb,
                         "with_skill": s, "n_skill": ns, "class": classify(b, s, nb, ns), "generic": generic,
                         "text": text})

    order = ["gap", "worse", "chance", "skill-win", "no-effect"]
    for cls in order:
        sel = [r for r in rows if r["class"] == cls]
        print(f"\n## {cls} ({len(sel)})\n")
        print("| Case | Item | Baseline | Skill | What it tests |")
        print("|------|------|----------|-------|---------------|")
        for r in sel:
            mark = " (generic)" if r["generic"] else ""
            text = r["text"].replace("|", "/")[:140]
            print(f"| {r['case']} | {r['item']} | {r['base']}/{r['n_base']} | {r['with_skill']}/{r['n_skill']} | {text}{mark} |")
    if args.json:
        args.json.write_text(json.dumps(rows, indent=2), encoding="utf-8")
    return 0


if __name__ == "__main__":
    sys.exit(main())
