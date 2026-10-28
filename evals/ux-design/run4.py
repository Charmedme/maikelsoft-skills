#!/usr/bin/env python3
"""Runs for the ux-design 0.4.0 cases (party game): 0.3.1 (old) against 0.4.0 (new).

    python3 evals/ux-design/run4.py <runs> --plugin-old <ux-design folder> [--plugin-new <ux-design folder>]
                                    [--case party-new-direction party-review-faults] [--variant old new new2]
                                    [--runs-per-variant 3] [--parallel 3] [--model sonnet]

Each run gets a fresh copy of fixtures/<case>/ in repo/, with the story in docs/work/<slug>/story.md.
Both variants get the same prompt (prompts4/<case>.md) with the path of their plugin. The child runs
have no permission bypass and a clean environment (see run.py). A run folder with a final.md is skipped.
"""
import argparse
import concurrent.futures as cf
import json
import shutil
import subprocess
import sys
import time
from pathlib import Path

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
from run import child_env  # noqa: E402

CASES = {
    "party-new-direction": "ndk-12-new-direction",
    "party-review-faults": "ndk-14-lobby-stand",
}


def run_one(job, args):
    run_dir, case, variant = job
    if (run_dir / "final.md").exists():
        return run_dir, "skipped", 0.0
    run_dir.mkdir(parents=True, exist_ok=True)
    repo = run_dir / "repo"
    if repo.exists():
        shutil.rmtree(repo)
    fixture = HERE / "fixtures" / case
    shutil.copytree(fixture, repo, ignore=shutil.ignore_patterns("stories"))
    work = f"docs/work/{CASES[case]}"
    (repo / work).mkdir(parents=True, exist_ok=True)
    shutil.copy(fixture / "stories" / f"{case}.md", repo / work / "story.md")
    plugin = Path(args.plugin_new if variant.startswith("new") else args.plugin_old).resolve()
    prompt = (HERE / "prompts4" / f"{case}.md").read_text(encoding="utf-8")
    prompt = prompt.replace("<repo>", str(repo)).replace("<work>", work).replace("<plugin>", str(plugin))
    (run_dir / "prompt.md").write_text(prompt, encoding="utf-8")
    cmd = ["claude", "-p", prompt, "--model", args.model, "--permission-mode", "acceptEdits",
           "--allowedTools", "Read", "Write", "Edit", "Glob", "Grep", "Bash(python3:*)", "Bash(ls:*)",
           "--disallowedTools", "Agent", "Skill", "WebFetch", "WebSearch",
           "--output-format", "stream-json", "--verbose", "--max-budget-usd", str(args.budget),
           "--add-dir", str(plugin)]
    t0 = time.time()
    with open(run_dir / "transcript.jsonl", "w") as out, open(run_dir / "stderr.log", "w") as err:
        try:
            code = subprocess.run(cmd, cwd=repo, env=child_env(), stdout=out, stderr=err, timeout=args.timeout).returncode
        except subprocess.TimeoutExpired:
            code = "timeout"
    final, cost, turns = "", 0.0, 0
    for line in (run_dir / "transcript.jsonl").read_text(encoding="utf-8", errors="replace").splitlines():
        try:
            ev = json.loads(line)
        except json.JSONDecodeError:
            continue
        if ev.get("type") == "result":
            final, cost, turns = ev.get("result") or "", ev.get("total_cost_usd") or 0.0, ev.get("num_turns") or 0
    meta = {"case": case, "variant": variant, "exit": code, "cost_usd": cost, "turns": turns,
            "seconds": round(time.time() - t0), "model": args.model, "plugin": str(plugin)}
    (run_dir / "meta.json").write_text(json.dumps(meta, indent=2), encoding="utf-8")
    (run_dir / "final.md").write_text(final or f"(no final message, exit {code})", encoding="utf-8")
    return run_dir, code, cost


def main():
    p = argparse.ArgumentParser()
    p.add_argument("runs")
    p.add_argument("--plugin-old", required=True)
    p.add_argument("--plugin-new", default=str(HERE.parents[1] / "plugins" / "ux-design"))
    p.add_argument("--case", nargs="*", default=list(CASES))
    p.add_argument("--variant", nargs="*", default=["old", "new"])
    p.add_argument("--runs-per-variant", type=int, default=3)
    p.add_argument("--parallel", type=int, default=3)
    p.add_argument("--model", default="claude-sonnet-5-5")
    p.add_argument("--budget", type=float, default=15)
    p.add_argument("--timeout", type=int, default=3600)
    args = p.parse_args()
    jobs = [(Path(args.runs) / case / v / str(n), case, v)
            for case in args.case for v in args.variant for n in range(1, args.runs_per_variant + 1)]
    total = 0.0
    with cf.ThreadPoolExecutor(max_workers=args.parallel) as ex:
        for run_dir, code, cost in ex.map(lambda j: run_one(j, args), jobs):
            total += cost
            print(f"{run_dir}: exit {code}, ${cost:.2f}", flush=True)
    print(f"total ${total:.2f}")


if __name__ == "__main__":
    main()
