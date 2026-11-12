#!/usr/bin/env python3
"""Compare the flows on one small and three complex stories, with hidden tests that the runs never see.

    python3 evals/review-flow/hard.py run <runs> [--story ...] [--arm ...] [--repeat 1] [--parallel 3] [--budget 10]
    python3 evals/review-flow/hard.py score <runs>

Each arm has dev-stack, so that only the flow differs:
    baseline  no flow plugin
    flow      dev-flow alone
    team      dev-team alone
    both      dev-flow and dev-team, with the neutral prompt of the baseline, so the run chooses the route

The hidden tests are in hard/probes. E tests check the criteria of the story. H tests check docs/domain.md of the
fixture and traps of the existing code. Each hidden test passes on the reference solution in hard/references.
"""
import argparse
import concurrent.futures
import importlib.util
import json
import os
import pathlib
import shutil
import subprocess
import sys

HERE = pathlib.Path(__file__).resolve().parent
ROOT = HERE.parents[1]
HARD = HERE / "hard"
FIXTURE = HERE / "fixtures" / "shop-api"
sys.path.insert(0, str(HERE.parent))
import runwatch  # noqa: E402


def _load(name, path):
    spec = importlib.util.spec_from_file_location(name, path)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


runner = _load("reviewflow_run", HERE / "run.py")
STORIES = ["order-summary", "partial-cancel", "discount-limit", "returns"]
ARMS = {"baseline": ["dev-stack"], "flow": ["dev-flow", "dev-stack"], "team": ["dev-team", "dev-stack"],
        "both": ["dev-flow", "dev-team", "dev-stack"]}
NEUTRAL = "Deliver the story in {story}. The answer sheet of the human is {answers}. Take each answer of the human from that sheet. Work on a new branch and commit."
PROMPTS = {
    "baseline": NEUTRAL,
    "both": NEUTRAL,
    "flow": "Use the dev-flow:plan-and-build skill to build the approved item in {story}. The answer sheet of the human is {answers}. The human is also the product owner. Take each answer of the human from that sheet.",
    "team": "Use the dev-team:deliver-story skill to deliver the story in {story}. The answer sheet of the human is {answers}. Take each answer of the human from that sheet.",
}
SUFFIX = " Nobody can answer questions in this run. Do not push. Do not call any tool whose name starts with mcp__."
GIT = ["git", "-c", "user.name=Dev", "-c", "user.email=dev@example.com", "-c", "commit.gpgsign=false", "-C"]


def make_repo(repo):
    shutil.copytree(FIXTURE, repo, ignore=shutil.ignore_patterns("bin", "obj", "TestResults"))
    subprocess.run(["git", "init", "-q", "-b", "main", str(repo)], check=True)
    for args in (["add", "-A"], ["commit", "-qm", "Shop API"], ["tag", "eval-start"]):
        subprocess.run([*GIT, str(repo), *args], check=True)


def one(run, story, arm, args):
    repo = run / "repo"
    if not repo.exists():
        make_repo(repo)
    inputs = run / "input"
    inputs.mkdir(exist_ok=True)
    for name, target in ((f"{story}.md", "story.md"), (f"{story}.answers.md", "answers.md")):
        shutil.copy(HARD / "stories" / name, inputs / target)
    env = runner.environment()
    profile = run / "profile.md"
    profile.write_text("# Dev-team profile\n\n| ID | Answer | Date |\n|----|--------|------|\n", encoding="utf-8")
    env["DEV_TEAM_PROFILE"] = str(profile)
    before = runner.fingerprint(runner.REAL_PROFILE)
    prompt = PROMPTS[arm].format(story=inputs / "story.md", answers=inputs / "answers.md") + SUFFIX
    cmd = ["claude", "-p", prompt, "--model", args.model, "--permission-mode", "bypassPermissions",
           "--setting-sources", "project,local", "--output-format", "stream-json", "--verbose", "--max-budget-usd", str(args.budget)]
    for name in ARMS[arm]:
        cmd += ["--plugin-dir", runner.pinned_copy(ROOT / "plugins" / name, args.runs / "_plugins" / name, args.model)]
    code = runwatch.run_watched(cmd, repo, env, run, {"max_minutes": args.max_minutes, "stall_minutes": 12})
    (run / "final.md").write_text(runner.final_message(run / "stream.jsonl"), encoding="utf-8")
    if runner.fingerprint(runner.REAL_PROFILE) != before:
        raise runner.ProfileChanged(f"{run} changed {runner.REAL_PROFILE}. Restore the file before the next run.")
    return f"{run.relative_to(args.runs)} exit {code}"


def stream_facts(stream):
    """Cost, time, the skills that the main session loaded, and the agents that it started."""
    facts = {"cost": None, "minutes": None, "skills": [], "agents": []}
    if not stream.is_file():
        return facts
    for line in stream.read_text(encoding="utf-8", errors="ignore").splitlines():
        try:
            event = json.loads(line)
        except ValueError:
            continue
        if event.get("type") == "result":
            facts["cost"] = event.get("total_cost_usd")
            facts["minutes"] = round((event.get("duration_ms") or 0) / 60000, 1)
        elif event.get("type") == "assistant":
            for block in (event.get("message") or {}).get("content") or []:
                if isinstance(block, dict) and block.get("type") == "tool_use":
                    tool_input = block.get("input") or {}
                    if block.get("name") == "Skill":
                        facts["skills"].append(tool_input.get("skill", "?"))
                    elif block.get("name") in ("Agent", "Task"):
                        facts["agents"].append(tool_input.get("subagent_type", "?"))
    return facts


def score_run(run, story, ds, passes):
    """A test counts as passed only when it passes in each pass, because a race does not show each time."""
    facts = stream_facts(run / "stream.jsonl")
    repo = run / "repo"
    facts["commits"] = len(subprocess.run([*GIT, str(repo), "log", "--oneline", "--all", "--not", "eval-start"], capture_output=True, text=True).stdout.splitlines())
    results, note, misses = {}, "", {}
    for _ in range(passes):
        ds._CACHE.clear()
        one_pass, note = ds.probe_results(repo, story)
        if not one_pass:
            return {**facts, "built": False, "note": note, "E": "0/0", "H": "0/0", "failed": []}
        for name, (outcome, message) in one_pass.items():
            if outcome != "Passed":
                misses[name] = misses.get(name, 0) + 1
            if name not in results or outcome != "Passed":
                results[name] = (outcome, message)
    failed = sorted(f"{name} ({misses[name]}/{passes})" for name in misses)
    count = lambda kind: (sum(1 for n, (o, _) in results.items() if n.startswith(kind) and o == "Passed"), sum(1 for n in results if n.startswith(kind)))
    facts.update({"built": True, "note": "", "E": "%d/%d" % count("E"), "H": "%d/%d" % count("H"), "failed": failed})
    return facts


def score(runs, passes):
    ds = _load("devstack_score", ROOT / "evals" / "dev-stack" / "score.py")
    ds.PROBES = HARD / "probes"
    table = {}
    for stream in sorted(runs.glob("*/*/*/stream.jsonl")):
        run = stream.parent
        story, arm, number = run.relative_to(runs).parts
        table[f"{story}/{arm}/{number}"] = score_run(run, story, ds, passes)
        print(f"{story}/{arm}/{number}", json.dumps(table[f"{story}/{arm}/{number}"]), flush=True)
    (runs / "scores.json").write_text(json.dumps(table, indent=1), encoding="utf-8")


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    sub = parser.add_subparsers(dest="command", required=True)
    run_cmd = sub.add_parser("run")
    run_cmd.add_argument("runs", type=pathlib.Path)
    run_cmd.add_argument("--story", nargs="*", default=STORIES)
    run_cmd.add_argument("--arm", nargs="*", default=list(ARMS))
    run_cmd.add_argument("--repeat", type=int, default=1)
    run_cmd.add_argument("--first", type=int, default=1)
    run_cmd.add_argument("--parallel", type=int, default=3)
    run_cmd.add_argument("--budget", type=float, default=10)
    run_cmd.add_argument("--max-minutes", type=float, default=45)
    run_cmd.add_argument("--model", default="sonnet")
    score_cmd = sub.add_parser("score")
    score_cmd.add_argument("runs", type=pathlib.Path)
    score_cmd.add_argument("--passes", type=int, default=3)
    args = parser.parse_args(argv)
    args.runs = args.runs.resolve()
    if args.command == "score":
        os.environ.setdefault("DOTNET_ROOT", str(pathlib.Path.home() / ".dotnet"))
        score(args.runs, args.passes)
        return 0
    jobs = []
    for story in args.story:
        for arm in args.arm:
            for number in range(args.first, args.first + args.repeat):
                run = args.runs / story / arm / str(number)
                if not (run / "final.md").exists():
                    run.mkdir(parents=True, exist_ok=True)
                    jobs.append((run, story, arm))
    with concurrent.futures.ThreadPoolExecutor(args.parallel) as pool:
        for line in pool.map(lambda job: one(*job, args), jobs):
            print(line, flush=True)
    return 0


if __name__ == "__main__":
    sys.exit(main())
