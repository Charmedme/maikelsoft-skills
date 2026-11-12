#!/usr/bin/env python3
"""Run the product-flow 2.0.0 evals: one headless claude session for each run, with the human replies of humans/<eval>.md.

    python3 evals/product-flow/v2/run.py <runs> [--eval NAME ...] [--variant baseline skill] [--runs-per-variant 3]
                                          [--parallel 2] [--model sonnet] [--budget 3] [--max-turns 4]

Each run gets <runs>/<eval>/<variant>/<n>/ with:
    repo/          a git repository made from the fixture, with the remote of the eval
    home/          the HOME of the run: a run cannot change the home folder of the person who runs
                   the evals. Only the login file of Claude Code is linked into it.
    turn-N.jsonl   the stream of each turn
    transcript.md  the messages of the agent and the replies of the human
A turn ends when the agent ends its message. run.py then sends the next reply with --resume.
After the last reply, the run ends. Needs claude and git.
"""
from __future__ import annotations

import argparse
import concurrent.futures
import json
import os
import pathlib
import shutil
import subprocess

HERE = pathlib.Path(__file__).resolve().parent
ROOT = HERE.parents[2]
PLUGIN = ROOT / "plugins" / "product-flow"

GIT = ["git", "-c", "user.name=Eval", "-c", "user.email=eval@example.com", "-c", "commit.gpgsign=false"]
PARENT_VARS = ["CLAUDE_CODE_SESSION_ID", "CLAUDE_CODE_REMOTE_SESSION_ID", "CLAUDE_AUTO_BACKGROUND_TASKS",
               "CLAUDE_CODE_BG_TASKS_REPORT_RUNNING", "CLAUDE_CODE_MESSAGING_SOCKET", "CLAUDE_CODE_MESSAGING_TOKEN",
               "CLAUDECODE", "CLAUDE_CODE_CHILD_SESSION", "CLAUDE_CODE_POST_TURN_MEMORY",
               "CLAUDE_CODE_POST_TURN_MEMORY_CONFIG", "CLAUDE_CODE_TEE_SDK_STDOUT", "CLAUDE_CODE_INCLUDE_PARTIAL_MESSAGES",
               "CLAUDE_PLUGIN_DATA", "CLAUDE_PLUGIN_ROOT", "DELIVERY_ROLE"]


def make_repo(ev: dict, repo: pathlib.Path) -> None:
    shutil.copytree(HERE / "fixtures" / ev["fixture"], repo)
    subprocess.run(GIT + ["init", "-q", "-b", "main"], cwd=repo, check=True)
    subprocess.run(GIT + ["add", "-A"], cwd=repo, check=True)
    subprocess.run(GIT + ["commit", "-q", "-m", "start"], cwd=repo, check=True)
    if ev.get("remote"):
        subprocess.run(GIT + ["remote", "add", "origin", ev["remote"]], cwd=repo, check=True)


def prompt_for(ev: dict, variant: str, repo: pathlib.Path, data: pathlib.Path) -> str:
    text = (HERE / "runner.md").read_text(encoding="utf-8")
    common = text.split("## Common part", 1)[1].split("## Variant: skill", 1)[0].strip()
    out = common.replace("<repo>", str(repo)).replace("<request>", ev["request"])
    if variant == "skill":
        part = text.split("## Variant: skill", 1)[1].strip()
        skill = PLUGIN / "skills" / ev["skill"]
        out += "\n\n" + (part.replace("<skill>", str(skill)).replace("<plugin>", str(PLUGIN))
                         .replace("<name>", ev["skill"]))
    return out


def replies(ev: dict) -> list:
    text = (HERE / ev["human"]).read_text(encoding="utf-8")
    return [part.strip() for part in text.split("\n---\n") if part.strip()]


def turn(cmd: list, repo: pathlib.Path, env: dict, stdin: str, out: pathlib.Path, timeout: int):
    with out.open("w", encoding="utf-8") as fh:
        try:
            subprocess.run(cmd, cwd=repo, env=env, input=stdin, stdout=fh, stderr=subprocess.STDOUT,
                           text=True, timeout=timeout)
        except subprocess.TimeoutExpired:
            pass
    session, result = None, ""
    for line in out.read_text(encoding="utf-8", errors="ignore").splitlines():
        try:
            item = json.loads(line)
        except ValueError:
            continue
        session = item.get("session_id", session)
        if item.get("type") == "result":
            result = item.get("result", "") or ""
    return session, result


def start(run: pathlib.Path, ev: dict, variant: str, args) -> str:
    repo, data = run / "repo", run / "data"
    if run.exists():
        shutil.rmtree(run)
    run.mkdir(parents=True)
    data.mkdir()
    make_repo(ev, repo)
    env = {k: v for k, v in os.environ.items() if k not in PARENT_VARS}
    home = run / "home"
    (home / ".claude").mkdir(parents=True)
    login = pathlib.Path.home() / ".claude" / ".credentials.json"
    if login.is_file():
        (home / ".claude" / ".credentials.json").symlink_to(login)
    env.update(HOME=str(home), GIT_AUTHOR_NAME="Eval", GIT_AUTHOR_EMAIL="eval@example.com",
               GIT_COMMITTER_NAME="Eval", GIT_COMMITTER_EMAIL="eval@example.com")
    # Only the settings of the run repository: no plugins, hooks, or skills of the person who runs the evals.
    base = ["claude", "-p", "--model", args.model, "--setting-sources", "project,local",
            "--permission-mode", "bypassPermissions",
            "--disallowedTools", "Agent", "Skill", "--output-format", "stream-json", "--verbose",
            "--max-budget-usd", str(args.budget)]
    log = [f"# Transcript: {ev['name']} / {variant} / {run.name}\n", f"## Human\n\n{ev['request']}\n"]
    session, message = turn(base, repo, env, prompt_for(ev, variant, repo, data), run / "turn-1.jsonl", args.timeout)
    log.append(f"## Agent\n\n{message}\n")
    for n, reply in enumerate(replies(ev)[: args.max_turns - 1], start=2):
        if not session:
            break
        log.append(f"## Human\n\n{reply}\n")
        session, message = turn(base + ["--resume", session], repo, env, reply, run / f"turn-{n}.jsonl", args.timeout)
        log.append(f"## Agent\n\n{message}\n")
    (run / "transcript.md").write_text("\n".join(log), encoding="utf-8")
    return f"{run.relative_to(args.runs)} turns {len(log) // 2}"


def main(argv=None) -> int:
    ap = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    ap.add_argument("runs", type=pathlib.Path)
    ap.add_argument("--eval", nargs="*")
    ap.add_argument("--variant", nargs="*", default=["baseline", "skill"])
    ap.add_argument("--runs-per-variant", type=int, default=3)
    ap.add_argument("--parallel", type=int, default=2)
    ap.add_argument("--model", default="sonnet")
    ap.add_argument("--budget", type=float, default=3.0)
    ap.add_argument("--max-turns", type=int, default=4)
    ap.add_argument("--timeout", type=int, default=900)
    args = ap.parse_args(argv)
    args.runs = args.runs.resolve()

    cases = json.loads((HERE / "evals.json").read_text(encoding="utf-8"))["evals"]
    jobs = [(args.runs / ev["name"] / variant / str(n), ev, variant)
            for ev in cases if not args.eval or ev["name"] in args.eval
            for variant in args.variant for n in range(1, args.runs_per_variant + 1)]
    with concurrent.futures.ThreadPoolExecutor(args.parallel) as pool:
        for line in pool.map(lambda job: start(*job, args), jobs):
            print(line, flush=True)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
