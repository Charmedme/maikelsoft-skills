#!/usr/bin/env python3
"""Trigger test: does each skill start for its own requests, and stay quiet for close requests?

    python3 evals/ux-design/trigger.py <out folder> --plugin <plugin folder> [--model sonnet] [--parallel 4]

Each query runs headless one time, with only this plugin loaded (--plugin-dir) and a clean
environment. A query counts as a start of skill X when the run calls the Skill tool with X in its
first turns. Result: triggers-result.json in <out folder>, and a table per skill.
"""
import argparse
import concurrent.futures as cf
import json
import subprocess
import sys
from pathlib import Path

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
from run import child_env  # noqa: E402


def run_query(skill, kind, query, args, out):
    work = out / "work"
    work.mkdir(parents=True, exist_ok=True)
    cmd = ["claude", "-p", query, "--model", args.model, "--plugin-dir", str(Path(args.plugin).resolve()),
           "--permission-mode", "default", "--allowedTools", "Skill", "Read", "Glob", "Grep",
           "--disallowedTools", "Write", "Edit", "Bash", "Agent", "WebFetch", "WebSearch",
           "--max-turns", "3", "--output-format", "stream-json", "--verbose", "--max-budget-usd", "1"]
    r = subprocess.run(cmd, cwd=work, env=child_env(), capture_output=True, text=True, timeout=600)
    called = []
    for line in r.stdout.splitlines():
        try:
            ev = json.loads(line)
        except json.JSONDecodeError:
            continue
        for block in (ev.get("message") or {}).get("content", []) if isinstance(ev.get("message"), dict) else []:
            if isinstance(block, dict) and block.get("type") == "tool_use" and block.get("name") == "Skill":
                called.append(str(block.get("input", {}).get("skill", "")))
    hit = any(c.split(":")[-1] == skill for c in called)
    ok = hit if kind == "start" else not hit
    return {"skill": skill, "kind": kind, "query": query, "called": called, "ok": ok}


def main():
    p = argparse.ArgumentParser()
    p.add_argument("out")
    p.add_argument("--plugin", required=True)
    p.add_argument("--model", default="sonnet")
    p.add_argument("--parallel", type=int, default=4)
    p.add_argument("--skill", nargs="*", help="test only these skills")
    args = p.parse_args()
    out = Path(args.out)
    out.mkdir(parents=True, exist_ok=True)
    spec = json.loads((HERE / "triggers.json").read_text(encoding="utf-8"))
    if args.skill:
        spec = {k: v for k, v in spec.items() if k in args.skill}
    jobs = [(s, k, q) for s, kinds in spec.items() for k, qs in kinds.items() for q in qs]
    with cf.ThreadPoolExecutor(max_workers=args.parallel) as ex:
        results = list(ex.map(lambda j: run_query(*j, args, out), jobs))
    (out / "triggers-result.json").write_text(json.dumps(results, indent=2), encoding="utf-8")
    print("| skill | starts (of 5) | quiet (of 5) |\n|---|---|---|")
    for s in spec:
        st = sum(1 for r in results if r["skill"] == s and r["kind"] == "start" and r["ok"])
        sk = sum(1 for r in results if r["skill"] == s and r["kind"] == "skip" and r["ok"])
        print(f"| {s} | {st} | {sk} |")
    for r in results:
        if not r["ok"]:
            print(f'MISS {r["skill"]} {r["kind"]}: "{r["query"]}" called {r["called"]}')


if __name__ == "__main__":
    main()
