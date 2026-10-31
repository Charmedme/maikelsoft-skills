#!/usr/bin/env python3
"""Run the review-flow comparison: one scenario, one arm, one headless claude run in a new repository.

    python3 evals/review-flow/run.py <runs> --current-root <worktree of the old commit>
                                     [--scenario story review comments cap] [--arm baseline current new]
                                     [--parallel 2] [--budget 8] [--model sonnet]

Each run gets <runs>/<scenario>/<arm>/repo and writes stream.jsonl and final.md next to it. A run folder
that has a final.md is skipped. The arms:
    baseline  plain Claude Code with dev-stack, no dev-team
    current   dev-team and dev-stack from --current-root
    new       dev-team and dev-stack from this worktree

The runs read no user settings, so no installed plugin, hook, or router reaches them. Each arm runs from a copy of
its plugins with the model of each agent file set to --model. An agent file overrides the model of the session.
Then score with: python3 evals/review-flow/score.py <runs>

Needs claude, git, the dotnet CLI, and Docker.
"""
import argparse
import concurrent.futures
import json
import os
import pathlib
import re
import shutil
import sys

HERE = pathlib.Path(__file__).resolve().parent
ROOT = HERE.parents[1]
sys.path.insert(0, str(HERE.parent))
sys.path.insert(0, str(HERE))
import runwatch  # noqa: E402
import scenarios  # noqa: E402

SKIP_ARM = {"cap": {"baseline"}}  # A plain session has no round rule, and the comments scenario shows its handling.
PARENT_PREFIXES = ("CLAUDE_CODE_", "CLAUDE_AUTO_", "CLAUDE_AFTER_")


def pinned_copy(folder, target, model):
    """Copy one plugin, with each agent file on the given model."""
    if not target.exists():
        shutil.copytree(folder, target, ignore=shutil.ignore_patterns("__pycache__"))
        for agent in target.glob("agents/*.md"):
            text = agent.read_text(encoding="utf-8")
            agent.write_text(re.sub(r"(?m)^model: \S+$", f"model: {model}", text), encoding="utf-8")
    return str(target)


def plugin_dirs(arm, current_root):
    root = {"new": ROOT, "current": current_root, "baseline": current_root}[arm]
    names = ["dev-stack"] if arm == "baseline" else ["dev-team", "dev-stack"]
    return [str(pathlib.Path(root) / "plugins" / name) for name in names]


def pinned_dirs(arm, args):
    root = args.runs / "_plugins" / arm
    return [pinned_copy(pathlib.Path(folder), root / pathlib.Path(folder).name, args.model) for folder in plugin_dirs(arm, args.current_root)]


def command(prompt, arm, args):
    cmd = ["claude", "-p", prompt, "--model", args.model, "--permission-mode", "bypassPermissions",
           "--setting-sources", "project,local", "--output-format", "stream-json", "--verbose",
           "--max-budget-usd", str(args.budget)]
    for folder in pinned_dirs(arm, args):
        cmd += ["--plugin-dir", folder]
    return cmd


def environment():
    env = {k: v for k, v in os.environ.items() if k != "CLAUDECODE" and not k.startswith(PARENT_PREFIXES)}
    # A headless run ends when the main agent ends its turn, and that stops an agent in the background.
    env["CLAUDE_CODE_DISABLE_BACKGROUND_TASKS"] = "1"
    env["CLAUDE_CODE_MAX_SUBAGENT_SPAWN_DEPTH"] = "3"
    env["PATH"] = str(pathlib.Path.home() / ".dotnet") + os.pathsep + env.get("PATH", "")
    return env


def final_message(stream):
    final = ""
    for line in stream.read_text(encoding="utf-8", errors="ignore").splitlines():
        if '"type":"result"' in line.replace(" ", ""):
            try:
                final = json.loads(line).get("result", final)
            except ValueError:
                pass
    return final


def start(run, scenario, arm, args):
    repo = run / "repo"
    if not repo.exists():
        scenarios.prepare_scenario(scenario, repo)
    env = environment()
    env["DEV_TEAM_PROFILE"] = str(run.resolve() / "profile.md")
    limits = {"max_minutes": args.max_minutes, "stall_minutes": args.stall_minutes}
    code = runwatch.run_watched(command(scenarios.prompt_for(scenario, arm, run.resolve() / "input"), arm, args), repo, env, run, limits)
    (run / "final.md").write_text(final_message(run / "stream.jsonl"), encoding="utf-8")
    return f"{run.relative_to(args.runs)} exit {code}"


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("runs", type=pathlib.Path)
    parser.add_argument("--current-root", required=True, type=pathlib.Path)
    parser.add_argument("--scenario", nargs="*", default=["story", "review", "comments", "cap"])
    parser.add_argument("--arm", nargs="*", default=["baseline", "current", "new"])
    parser.add_argument("--parallel", type=int, default=2)
    parser.add_argument("--budget", type=float, default=8)
    parser.add_argument("--model", default="sonnet")
    parser.add_argument("--max-minutes", type=float, default=40)
    parser.add_argument("--stall-minutes", type=float, default=10)
    args = parser.parse_args(argv)
    args.runs = args.runs.resolve()
    args.current_root = args.current_root.resolve()
    jobs = []
    for scenario in args.scenario:
        for arm in args.arm:
            run = args.runs / scenario / arm
            if arm in SKIP_ARM.get(scenario, ()) or (run / "final.md").exists():
                continue
            run.mkdir(parents=True, exist_ok=True)
            jobs.append((run, scenario, arm))
    with concurrent.futures.ThreadPoolExecutor(args.parallel) as pool:
        for line in pool.map(lambda job: start(*job, args), jobs):
            print(line, flush=True)
    return 0


if __name__ == "__main__":
    sys.exit(main())
