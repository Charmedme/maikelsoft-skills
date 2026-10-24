#!/usr/bin/env python3
"""Summarize a round: checks, tells, determinism, grades, and cost per case and variant.

    python3 evals/ux-design/compare.py <runs> [<runs of a second version> ...]

Reads score.json of each run, determinism.json, and grades.json. Prints a Markdown table.
With more than one runs folder, the skill variant of each later folder is shown as v2, v3.
"""
import json
import statistics
import sys
from pathlib import Path


def mean(xs):
    xs = [x for x in xs if isinstance(x, (int, float))]
    return round(statistics.mean(xs), 2) if xs else None


def rows_for(root, label_skill):
    root = Path(root)
    det = json.loads((root / "determinism.json").read_text()) if (root / "determinism.json").exists() else {}
    out = {}
    for s in sorted(root.glob("*/*/*/score.json")):
        d = json.loads(s.read_text())
        # Variant names stay as they are (baseline, skill-v2, skill), so a round shows all of them.
        label = d["variant"] if (d["variant"] != "skill" or label_skill == "skill") else label_skill
        key = (d["case"], label)
        out.setdefault(key, []).append(d)
    table = []
    for (case, label), runs in sorted(out.items()):
        dkey = f"{case}/{runs[0]['variant']}"
        table.append({
            "case": case, "variant": label, "runs": len(runs),
            "frames": mean([r.get("frames") for r in runs]),
            "tells": mean([r.get("tells") for r in runs]),
            "missing_states": mean([r.get("missing_state_frames") for r in runs]),
            "spec_sourced": mean([(r.get("decisions_sourced") or 0) / r["decisions_total"] for r in runs if r.get("decisions_total")]),
            "agreement": det.get(dkey, {}).get("agreement"),
            "cost": mean([r.get("cost_usd") for r in runs]),
            "minutes": mean([(r.get("seconds") or 0) / 60 for r in runs]),
        })
    return table


def main():
    roots = sys.argv[1:]
    table = []
    for i, root in enumerate(roots):
        rows = rows_for(root, "skill" if i == 0 else f"skill v{i + 1}")
        table += [r for r in rows if i == 0 or r["variant"] != "baseline"]
    cols = ["case", "variant", "runs", "frames", "tells", "missing_states", "spec_sourced", "agreement", "cost", "minutes"]
    print("| " + " | ".join(cols) + " |")
    print("|" + "|".join("---" for _ in cols) + "|")
    for r in sorted(table, key=lambda r: (r["case"], r["variant"])):
        print("| " + " | ".join("" if r[c] is None else str(r[c]) for c in cols) + " |")
    for root in roots:
        g = Path(root) / "grades.json"
        if g.exists():
            print(f"\nBlind grades ({root}):\n")
            print("| pair and criterion | votes |\n|---|---|")
            for k, t in sorted(json.loads(g.read_text())["tally"].items()):
                print(f"| {k} | " + ", ".join(f"{w} {c}" for w, c in sorted(t.items())) + " |")


if __name__ == "__main__":
    main()
