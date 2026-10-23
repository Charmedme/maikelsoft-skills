#!/usr/bin/env python3
"""Make the run folders of the dev-team evals and start one headless claude run in each.

    python3 evals/dev-team/run.py <runs> [--eval get-order ...] [--arm team baseline] [--runs-per-arm 3]
                                  [--parallel 3] [--budget 40] [--model opus]

Each run gets <runs>/<eval>/<arm>/<n>/repo (a git repository with the orders-api fixture on main)
and writes stream.jsonl and stderr.log next to it. A run folder that has a stream.jsonl is skipped.
Then score with: python3 evals/dev-team/score.py <runs>

Needs claude, git, the dotnet CLI, and Docker.
"""
import argparse
import concurrent.futures
import json
import os
import pathlib
import subprocess
import sys

HERE = pathlib.Path(__file__).resolve().parent
ROOT = HERE.parents[1]
sys.path.insert(0, str(HERE.parent / "dev-stack"))
import prepare  # noqa: E402

PLUGINS = {"team": ["dev-team", "dev-stack"], "baseline": ["dev-stack"], "flow": ["dev-flow", "dev-team", "dev-stack"]}
# Variables of a parent Claude Code session. A child run that inherits them shares the session id
# of the parent and sends foreground agents to the background. A real user session has none of them.
PARENT_VARS = ["CLAUDE_CODE_SESSION_ID", "CLAUDE_CODE_REMOTE_SESSION_ID", "CLAUDE_AUTO_BACKGROUND_TASKS",
               "CLAUDE_CODE_BG_TASKS_REPORT_RUNNING", "CLAUDE_CODE_MESSAGING_SOCKET", "CLAUDE_CODE_MESSAGING_TOKEN",
               "CLAUDECODE", "CLAUDE_CODE_CHILD_SESSION", "CLAUDE_CODE_POST_TURN_MEMORY",
               "CLAUDE_CODE_POST_TURN_MEMORY_CONFIG", "CLAUDE_CODE_TEE_SDK_STDOUT", "CLAUDE_CODE_INCLUDE_PARTIAL_MESSAGES",
               "CLAUDE_AFTER_LAST_COMPACT"]


def start(run, ev, arm, cfg, args):
    repo = run / "repo"
    if not repo.exists():
        prepare.make_repo({"kind": "write", "fixture": cfg["fixture"]}, repo)
    prompt = cfg["prompt"][arm].format(story=HERE / ev["story"], answers=HERE / ev["answers"]) + cfg["prompt"]["suffix"]
    cmd = ["claude", "-p", prompt, "--model", args.model, "--permission-mode", "bypassPermissions",
           "--output-format", "stream-json", "--verbose", "--max-budget-usd", str(args.budget)]
    for p in PLUGINS[arm]:
        cmd += ["--plugin-dir", str(ROOT / "plugins" / p)]
    env = {k: v for k, v in os.environ.items() if k not in PARENT_VARS}
    env["CLAUDE_CODE_MAX_SUBAGENT_SPAWN_DEPTH"] = "3"
    # Each run has its own profile, so that runs at the same time do not share earlier answers.
    env["DEV_TEAM_PROFILE"] = str(run.resolve() / "profile.md")
    env["PATH"] = env.get("PATH", "") + os.pathsep + str(pathlib.Path.home() / ".dotnet")
    with open(run / "stream.jsonl", "w") as out, open(run / "stderr.log", "w") as err:
        code = subprocess.run(cmd, cwd=repo, env=env, stdout=out, stderr=err, timeout=args.timeout).returncode
    return f"{run.relative_to(args.runs)} exit {code}"


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("runs", type=pathlib.Path)
    parser.add_argument("--eval", nargs="*")
    parser.add_argument("--arm", nargs="*", default=["team", "baseline"])
    parser.add_argument("--runs-per-arm", type=int, default=3)
    parser.add_argument("--parallel", type=int, default=3)
    parser.add_argument("--budget", type=float, default=40)
    parser.add_argument("--model", default="opus")
    parser.add_argument("--timeout", type=int, default=5400)
    args = parser.parse_args(argv)
    cfg = json.loads((HERE / "evals.json").read_text(encoding="utf-8"))
    jobs = []
    for ev in cfg["evals"]:
        if args.eval and ev["name"] not in args.eval:
            continue
        for arm in args.arm:
            for n in range(1, args.runs_per_arm + 1):
                run = args.runs / ev["name"] / arm / str(n)
                if (run / "stream.jsonl").exists():
                    continue
                run.mkdir(parents=True, exist_ok=True)
                jobs.append((run, ev, arm))
    with concurrent.futures.ThreadPoolExecutor(args.parallel) as pool:
        for line in pool.map(lambda j: start(*j, cfg, args), jobs):
            print(line, flush=True)
    return 0


if __name__ == "__main__":
    sys.exit(main())
