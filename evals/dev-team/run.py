#!/usr/bin/env python3
"""Make the run folders of the dev-team evals and start one headless claude run in each.

    python3 evals/dev-team/run.py <runs> [--eval get-order ...] [--arm team baseline] [--runs-per-arm 3]
                                  [--parallel 3] [--budget 15] [--model opus]

Each run gets <runs>/<eval>/<arm>/<n>/repo (a git repository with the orders-api fixture on main)
and writes stream.jsonl and stderr.log next to it. The arm team loads dev-flow, dev-team, and dev-stack,
and reads the ticket of the story (field "ticket" of evals.json) in place of the story. A run folder that has a stream.jsonl is skipped.
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

sys.path.insert(0, str(pathlib.Path(__file__).resolve().parents[1]))
import runwatch  # noqa: E402

HERE = pathlib.Path(__file__).resolve().parent
ROOT = HERE.parents[1]
sys.path.insert(0, str(HERE.parent / "dev-stack"))
import prepare  # noqa: E402

# dev-team 1.0.0 is the team mode of dev-flow:build, so the team arm loads dev-flow too.
PLUGINS = {"team": ["dev-flow", "dev-team", "dev-stack"], "baseline": ["dev-stack"], "flow": ["dev-flow", "dev-team", "dev-stack"],
           "flowspec": ["dev-flow", "dev-team", "dev-stack"]}
# dev-flow 2.0 (tiers, no spec first) from a worktree of an older commit, for a comparison arm.
if os.environ.get("DEV_FLOW20_ROOT"):
    _old = pathlib.Path(os.environ["DEV_FLOW20_ROOT"]) / "plugins"
    PLUGINS["flow20"] = [str(_old / "dev-flow"), str(_old / "dev-team"), "dev-stack"]
# Variables of a parent Claude Code session. A child run that inherits them shares the session id
# of the parent and sends foreground agents to the background. A real user session has none of them.
PARENT_VARS = ["CLAUDE_CODE_SESSION_ID", "CLAUDE_CODE_REMOTE_SESSION_ID", "CLAUDE_AUTO_BACKGROUND_TASKS",
               "CLAUDE_CODE_BG_TASKS_REPORT_RUNNING", "CLAUDE_CODE_MESSAGING_SOCKET", "CLAUDE_CODE_MESSAGING_TOKEN",
               "CLAUDECODE", "CLAUDE_CODE_CHILD_SESSION", "CLAUDE_CODE_POST_TURN_MEMORY",
               "CLAUDE_CODE_POST_TURN_MEMORY_CONFIG", "CLAUDE_CODE_TEE_SDK_STDOUT", "CLAUDE_CODE_INCLUDE_PARTIAL_MESSAGES",
               "CLAUDE_AFTER_LAST_COMPACT"]


TICKET_ARMS = ("team",)  # these arms get the ticket (the story with a Behaviour spec) in place of the story


def item_file(ev, arm):
    """The item that the run reads: the ticket for an arm of TICKET_ARMS, else the plain story."""
    if arm not in TICKET_ARMS:
        return HERE / ev["story"]
    ticket = HERE / ev.get("ticket", "")
    if not ev.get("ticket") or not ticket.is_file():
        raise FileNotFoundError(f"{ticket if ev.get('ticket') else ev['name']}: the arm {arm} needs the ticket of the story")
    return ticket


def prompt_for(ev, arm, cfg):
    return cfg["prompt"][arm].format(story=item_file(ev, arm), answers=HERE / ev["answers"]) + cfg["prompt"]["suffix"]


def plugin_dirs(arm):
    return [p if os.path.isabs(p) else str(ROOT / "plugins" / p) for p in PLUGINS[arm]]


def start(run, ev, arm, cfg, args):
    repo = run / "repo"
    if not repo.exists():
        prepare.make_repo({"kind": "write", "fixture": cfg["fixture"]}, repo)
    prompt = prompt_for(ev, arm, cfg)
    cmd = ["claude", "-p", prompt, "--model", args.model, "--permission-mode", "bypassPermissions",
           "--output-format", "stream-json", "--verbose", "--max-budget-usd", str(args.budget)]
    for folder in plugin_dirs(arm):
        cmd += ["--plugin-dir", folder]
    env = {k: v for k, v in os.environ.items() if k not in PARENT_VARS}
    # A headless run ends when the main agent ends its turn, and that stops an agent in the background.
    env["CLAUDE_CODE_DISABLE_BACKGROUND_TASKS"] = "1"
    env["CLAUDE_CODE_MAX_SUBAGENT_SPAWN_DEPTH"] = "3"
    # Each run has its own profile, so that runs at the same time do not share earlier answers.
    env["DEV_TEAM_PROFILE"] = str(run.resolve() / "profile.md")
    env["PATH"] = env.get("PATH", "") + os.pathsep + str(pathlib.Path.home() / ".dotnet")
    code = runwatch.run_watched(cmd, repo, env, run, {"max_minutes": args.max_minutes, "stall_minutes": args.stall_minutes})
    # Free disk space: the scorer builds again, and Docker keeps the volumes of stopped containers.
    for name in ("bin", "obj", "TestResults"):
        for folder in list(repo.rglob(name)):
            if folder.is_dir():
                subprocess.run(["rm", "-rf", str(folder)])
    subprocess.run(["docker", "volume", "prune", "-f"], capture_output=True)
    return f"{run.relative_to(args.runs)} exit {code}"


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("runs", type=pathlib.Path)
    parser.add_argument("--eval", nargs="*")
    parser.add_argument("--arm", nargs="*", default=["team", "baseline"])
    parser.add_argument("--runs-per-arm", type=int, default=3)
    parser.add_argument("--parallel", type=int, default=3)
    parser.add_argument("--budget", type=float, default=15)
    parser.add_argument("--model", default="opus")
    parser.add_argument("--max-minutes", type=float, default=25, help="stop a run after this wall time")
    parser.add_argument("--stall-minutes", type=float, default=6, help="stop a run without output for this time")
    args = parser.parse_args(argv)
    cfg = json.loads((HERE / "evals.json").read_text(encoding="utf-8"))
    jobs = []
    for ev in cfg["evals"]:
        if args.eval and ev["name"] not in args.eval:
            continue
        for arm in args.arm:
            item_file(ev, arm)  # stop before the first run when a ticket is missing
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
