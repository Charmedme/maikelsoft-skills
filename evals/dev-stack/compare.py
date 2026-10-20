#!/usr/bin/env python3
"""Compare the variants of the dev-stack eval runs: checks, judges, planted finds, cost and time.

    python3 evals/dev-stack/compare.py <runs> [--json out.json]

Reads per run: checks.json (score.py --repo output), grade.json (grade.py), stream.jsonl (claude -p).
Prints one row per case and variant with means over the runs, then a win/tie/loss count per case
(the variant with the higher mean of passed assertions wins) and a sign test over all cases.
"""
import argparse
import json
import math
import pathlib
import statistics
import sys


def load(path):
    try:
        return json.loads(path.read_text(encoding="utf-8"))
    except (OSError, ValueError):
        return None


def usage(run):
    cost, ms = 0.0, 0
    path = run / "stream.jsonl"
    if path.is_file():
        for line in path.read_text(encoding="utf-8", errors="ignore").splitlines():
            if '"type":"result"' in line.replace(" ", ""):
                try:
                    d = json.loads(line)
                except ValueError:
                    continue
                cost = max(cost, d.get("total_cost_usd", 0.0))
                ms += d.get("duration_ms", 0)
    return cost, ms / 60000


def run_row(run):
    checks = load(run / "checks.json")
    checks = checks if isinstance(checks, list) else (checks or {}).get("results", [])
    grade = load(run / "grade.json") or {}
    judges = grade.get("results", [])
    cost, minutes = usage(run)
    return {
        "checks": (sum(c["ok"] for c in checks), len(checks)),
        "judges": (sum(bool(j.get("pass")) for j in judges), len(judges)),
        "found": len(grade.get("found", [])),
        "fp": len([f for f in grade.get("false_positives", []) if f]),
        "cost": cost,
        "minutes": minutes,
        "graded": bool(judges),
        "failed": [c.get("id") for c in checks if not c["ok"]] + [j.get("id") for j in judges if not j.get("pass")],
    }


def sign_test(wins, losses):
    n = wins + losses
    if n == 0:
        return 1.0
    k = min(wins, losses)
    p = sum(math.comb(n, i) for i in range(k + 1)) / 2 ** n
    return min(1.0, 2 * p)


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("runs", type=pathlib.Path)
    parser.add_argument("--json", type=pathlib.Path)
    args = parser.parse_args(argv)
    table = {}
    for final in sorted(args.runs.glob("*/*/*/final.md")):
        run = final.parent
        table.setdefault(run.parts[-3], {}).setdefault(run.parts[-2], []).append(run_row(run))
    print("| Case | Variant | Runs | Checks | Judges | Planted found | False pos. | Cost | Min | Failed (all runs) |")
    print("|------|---------|------|--------|--------|---------------|------------|------|-----|-------------------|")
    wins = ties = losses = 0
    out = {}
    for case, variants in sorted(table.items()):
        score = {}
        for variant in ("baseline", "skill"):
            rows = variants.get(variant, [])
            if not rows:
                continue
            passed = [r["checks"][0] + r["judges"][0] for r in rows]
            total = rows[0]["checks"][1] + rows[0]["judges"][1]
            score[variant] = statistics.mean(passed) / total if total else 0
            failed = {}
            for r in rows:
                for f in r["failed"]:
                    failed[f] = failed.get(f, 0) + 1
            row = {
                "runs": len(rows),
                "checks": statistics.mean(r["checks"][0] for r in rows),
                "checks_total": rows[0]["checks"][1],
                "judges": statistics.mean(r["judges"][0] for r in rows),
                "judges_total": rows[0]["judges"][1],
                "found": statistics.mean(r["found"] for r in rows),
                "fp": statistics.mean(r["fp"] for r in rows),
                "cost": statistics.mean(r["cost"] for r in rows),
                "minutes": statistics.mean(r["minutes"] for r in rows),
                "failed": failed,
                "graded": sum(r["graded"] for r in rows),
            }
            out.setdefault(case, {})[variant] = row
            fails = ", ".join(f"{k}x{v}" for k, v in sorted(failed.items())) or "-"
            print(f"| {case} | {variant} | {row['runs']} | {row['checks']:.1f}/{row['checks_total']} | "
                  f"{row['judges']:.1f}/{row['judges_total']} | {row['found']:.1f} | {row['fp']:.1f} | "
                  f"${row['cost']:.2f} | {row['minutes']:.1f} | {fails} |")
        if len(score) == 2:
            if score["skill"] > score["baseline"] + 1e-9:
                wins += 1
            elif score["skill"] < score["baseline"] - 1e-9:
                losses += 1
            else:
                ties += 1
    print(f"\nSkill against baseline per case: {wins} wins, {ties} ties, {losses} losses. "
          f"Sign test (two-sided): p = {sign_test(wins, losses):.3f}")
    if args.json:
        args.json.write_text(json.dumps(out, indent=1), encoding="utf-8")
    return 0


if __name__ == "__main__":
    sys.exit(main())
