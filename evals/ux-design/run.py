#!/usr/bin/env python3
"""Make the run folders of the ux-design evals and start one headless claude run in each.

    python3 evals/ux-design/run.py <runs> --plugin <plugin folder> [--case <name> ...]
                                   [--variant baseline skill] [--runs-per-variant 3]
                                   [--parallel 3] [--budget 12] [--model opus] [--tag <label>]

Each run gets a fresh copy of the fixture in <runs>/<case>/<variant>/<n>/repo, with the story in
docs/work/<slug>/story.md. The output (stream-json) goes to transcript.jsonl, the final message to
final.md. A run folder that has a final.md is skipped.

The child runs get a clean environment: no session, project, or account variables of the parent
session. So they cannot write into the parent session, and no account skill reaches either variant.
The runs have no permission bypass. They can edit only inside the run folder, and use only
Read, Write, Edit, Glob, Grep, python3, and ls. They cannot use the Agent, Skill, or web tools. The skill variant reads the plugin files by path.
"""
import argparse
import concurrent.futures as cf
import json
import os
import re
import shutil
import subprocess
import sys
import time
from pathlib import Path

HERE = Path(__file__).resolve().parent
# Parent-session variables that a child run must not inherit.
DROP_PREFIX = ("CLAUDE_CODE_", "CLAUDE_PROJECT", "CCR_SESSION", "SESSION_INGRESS")
DROP = {"CLAUDECODE", "CLAUDE_PID", "CLAUDE_ADDITIONAL_DIRECTORIES", "TRACEPARENT", "CLAUDE_AFTER_LAST_COMPACT"}
# Variables that the child needs to reach the model through the host proxy.
KEEP = {"CLAUDE_CODE_PROVIDER_MANAGED_BY_HOST", "CLAUDE_CODE_PROXY_RESOLVES_HOSTS", "CLAUDE_CODE_USE_CCR_V2",
        "CLAUDE_CODE_ORGANIZATION_UUID", "CLAUDE_CODE_ACCOUNT_UUID", "CLAUDE_CODE_USER_EMAIL"}


def child_env():
    env = {}
    for k, v in os.environ.items():
        if k in KEEP or (not k.startswith(DROP_PREFIX) and k not in DROP):
            env[k] = v
    return env


def prompt_for(case, variant, repo, plugin):
    text = (HERE / "runner.md").read_text(encoding="utf-8")
    common = re.search(r"## Common part\n\n(.*?)\n## Variant: skill", text, re.S).group(1)
    work = f"docs/work/{case['slug']}"
    out = common.replace("<repo>", str(repo)).replace("<work>", work).replace("<css-line>", EV["css_line"][case.get("fixture", "strong")])
    if variant.startswith("skill"):
        extra = re.search(r"## Variant: skill\n\nAdd this text after the common part:\n\n(.*)", text, re.S).group(1)
        out += "\n" + extra.replace("<plugin>", str(plugin)).replace("<work>", work)
    return out.strip() + "\n"


def prepare(run_dir, case, fixture):
    repo = run_dir / "repo"
    if repo.exists():
        shutil.rmtree(repo)
    shutil.copytree(fixture, repo, ignore=shutil.ignore_patterns("stories"))
    work = repo / "docs" / "work" / case["slug"]
    work.mkdir(parents=True)
    shutil.copy(fixture / case["story"], work / "story.md")
    return repo


def run_one(job, args):
    run_dir, case, variant = job
    if (run_dir / "final.md").exists():
        return run_dir, "skipped", 0.0
    run_dir.mkdir(parents=True, exist_ok=True)
    repo = prepare(run_dir, case, HERE / EV["fixtures"][case.get("fixture", "strong")])
    plugin = Path(args.plugin_v2 if variant == "skill-v2" else args.plugin).resolve()
    prompt = prompt_for(case, variant, repo, plugin)
    (run_dir / "prompt.md").write_text(prompt, encoding="utf-8")
    # No permission bypass: edits only inside the run folder (acceptEdits), and only the tools
    # that the task needs. python3 runs the checkers and Playwright renders.
    cmd = ["claude", "-p", prompt, "--model", args.model, "--permission-mode", "acceptEdits",
           "--allowedTools", "Read", "Write", "Edit", "Glob", "Grep", "Bash(python3:*)", "Bash(ls:*)",
           "--disallowedTools", "Agent", "Skill", "WebFetch", "WebSearch",
           "--output-format", "stream-json", "--verbose", "--max-budget-usd", str(args.budget)]
    if variant.startswith("skill"):
        cmd += ["--add-dir", str(plugin)]
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
            final = ev.get("result") or ""
            cost = ev.get("total_cost_usd") or 0.0
    meta = {"case": case["name"], "variant": variant, "exit": code, "cost_usd": cost,
            "seconds": round(time.time() - t0), "model": args.model, "tag": args.tag}
    (run_dir / "meta.json").write_text(json.dumps(meta, indent=2), encoding="utf-8")
    (run_dir / "final.md").write_text(final or f"(no final message, exit {code})", encoding="utf-8")
    return run_dir, code, cost


def main():
    p = argparse.ArgumentParser()
    p.add_argument("runs")
    p.add_argument("--plugin", required=True)
    p.add_argument("--plugin-v2", help="plugin folder for the variant skill-v2")
    p.add_argument("--case", nargs="*")
    p.add_argument("--variant", nargs="*", default=["baseline", "skill"])
    p.add_argument("--runs-per-variant", type=int, default=3)
    p.add_argument("--parallel", type=int, default=3)
    p.add_argument("--budget", type=float, default=12)
    p.add_argument("--model", default="opus")
    p.add_argument("--timeout", type=int, default=3600)
    p.add_argument("--tag", default="")
    args = p.parse_args()
    jobs = []
    for case in EV["cases"]:
        if args.case and case["name"] not in args.case:
            continue
        for variant in args.variant:
            for n in range(1, args.runs_per_variant + 1):
                jobs.append((Path(args.runs) / case["name"] / variant / str(n), case, variant))
    total = 0.0
    with cf.ThreadPoolExecutor(max_workers=args.parallel) as ex:
        for run_dir, code, cost in ex.map(lambda j: run_one(j, args), jobs):
            total += cost
            print(f"{run_dir}: exit {code}, ${cost:.2f}", flush=True)
    print(f"total ${total:.2f}")


EV = json.loads((HERE / "evals.json").read_text(encoding="utf-8"))

if __name__ == "__main__":
    sys.exit(main())
