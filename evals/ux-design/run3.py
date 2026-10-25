#!/usr/bin/env python3
"""Runs for batch 3 of the ux-design evals: ux-setup (suite setup) and brand-rules (suite brand).

    python3 evals/ux-design/run3.py <runs> --suite setup|brand [--variant baseline skill] [--runs-per-variant 2]
                                    [--parallel 4] [--model sonnet] [--plugin <ux-design folder>]

Each run gets a fresh copy of the fixture of the case in repo/. The baseline gets the common prompt.
The skill variant also gets the skill prompt, with the path of the plugin. The child runs have no
permission bypass and a clean environment.
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

SUITES = {
    "setup": {"cases": ["setup-a", "setup-b", "setup-c"], "prompt": "setup-common.md", "skill": "setup-skill.md"},
    "brand": {"cases": ["brand-raw"], "prompt": "brand-common.md", "skill": "brand-skill.md"},
    "figma": {"cases": ["figma-dev", "figma-view"], "prompt": "figma-common.md", "skill": "figma-skill.md", "fakefigma": True},
}


def run_one(job, args):
    run_dir, case, variant, suite = job
    if (run_dir / "final.md").exists():
        return run_dir, "skipped", 0.0
    run_dir.mkdir(parents=True, exist_ok=True)
    repo = run_dir / "repo"
    if repo.exists():
        shutil.rmtree(repo)
    shutil.copytree(HERE / "fixtures" / case, repo)
    plugin = Path(args.plugin).resolve()
    prompt = (HERE / "prompts3" / suite["prompt"]).read_text(encoding="utf-8")
    if variant == "skill":
        prompt += "\n" + (HERE / "prompts3" / suite["skill"]).read_text(encoding="utf-8")
    prompt = prompt.replace("<repo>", str(repo)).replace("<plugin>", str(plugin))
    (run_dir / "prompt.md").write_text(prompt, encoding="utf-8")
    cmd = ["claude", "-p", prompt, "--model", args.model, "--permission-mode", "acceptEdits",
           "--allowedTools", "Read", "Write", "Edit", "Glob", "Grep", "Bash(python3:*)", "Bash(ls:*)",
           "--disallowedTools", "Agent", "Skill", "WebFetch", "WebSearch",
           "--output-format", "stream-json", "--verbose", "--max-budget-usd", str(args.budget)]
    if variant == "skill":
        cmd += ["--add-dir", str(plugin)]
    if suite.get("fakefigma"):
        cfg = {"mcpServers": {"figma": {"command": sys.executable, "args": [str(HERE / "tools" / "fakefigma" / "server.py")],
                                        "env": {"FAKEFIGMA_LOG": str(run_dir / "figma-calls.jsonl")}}}}
        (run_dir / "mcp.json").write_text(json.dumps(cfg), encoding="utf-8")
        cmd += ["--mcp-config", str(run_dir / "mcp.json"), "--strict-mcp-config"]
        cmd[cmd.index("--allowedTools") + 1:cmd.index("--allowedTools") + 1] = ["mcp__figma__*"]
    t0 = time.time()
    with open(run_dir / "transcript.jsonl", "w") as out, open(run_dir / "stderr.log", "w") as err:
        try:
            code = subprocess.run(cmd, cwd=repo, env=child_env(), stdout=out, stderr=err, timeout=args.timeout).returncode
        except subprocess.TimeoutExpired:
            code = "timeout"
    final, cost = "", 0.0
    for line in (run_dir / "transcript.jsonl").read_text(encoding="utf-8", errors="replace").splitlines():
        try:
            ev = json.loads(line)
        except json.JSONDecodeError:
            continue
        if ev.get("type") == "result":
            final, cost = ev.get("result") or "", ev.get("total_cost_usd") or 0.0
    meta = {"case": case, "variant": variant, "exit": code, "cost_usd": cost, "seconds": round(time.time() - t0), "model": args.model}
    (run_dir / "meta.json").write_text(json.dumps(meta, indent=2), encoding="utf-8")
    (run_dir / "final.md").write_text(final or f"(no final message, exit {code})", encoding="utf-8")
    return run_dir, code, cost


def main():
    p = argparse.ArgumentParser()
    p.add_argument("runs")
    p.add_argument("--suite", required=True, choices=sorted(SUITES))
    p.add_argument("--variant", nargs="*", default=["baseline", "skill"])
    p.add_argument("--runs-per-variant", type=int, default=2)
    p.add_argument("--parallel", type=int, default=4)
    p.add_argument("--model", default="sonnet")
    p.add_argument("--plugin", default=str(HERE.parents[1] / "plugins" / "ux-design"))
    p.add_argument("--budget", type=float, default=5)
    p.add_argument("--timeout", type=int, default=2400)
    args = p.parse_args()
    suite = SUITES[args.suite]
    jobs = [(Path(args.runs) / args.suite / case / v / str(n), case, v, suite)
            for case in suite["cases"] for v in args.variant for n in range(1, args.runs_per_variant + 1)]
    total = 0.0
    with cf.ThreadPoolExecutor(max_workers=args.parallel) as ex:
        for run_dir, code, cost in ex.map(lambda j: run_one(j, args), jobs):
            total += cost
            print(f"{run_dir}: exit {code}, ${cost:.2f}", flush=True)
    print(f"total ${total:.2f}")


if __name__ == "__main__":
    main()
