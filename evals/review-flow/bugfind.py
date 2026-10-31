#!/usr/bin/env python3
"""Bug-finding test of the core review: 6 cases, 2 versions, 2 runs for each pair, one blind grader.

    python3 evals/review-flow/bugfind.py run <runs> --bundle-root <worktree with the held rules> [--parallel 2]
    python3 evals/review-flow/bugfind.py collect <runs> <out>

The version "trimmed" is this worktree. The version "bundle" is a worktree of the commit with Origin, the nit cap,
and the REVIEW.md rules. Each run does only one review, and it fixes nothing. The command collect writes each
review under a random name, with no Origin column, for a grader that sees no version. The file map.json holds the names.
"""
import argparse
import concurrent.futures
import importlib.util
import json
import pathlib
import random
import re
import sys

HERE = pathlib.Path(__file__).resolve().parent
ROOT = HERE.parents[1]
sys.path.insert(0, str(HERE.parent))
sys.path.insert(0, str(HERE))
sys.path.insert(0, str(ROOT / "evals" / "dev-stack"))
import bugcases  # noqa: E402
import runwatch  # noqa: E402

# evals/dev-stack also has a module named run, so load this one by its path.
_spec = importlib.util.spec_from_file_location("reviewflow_run", HERE / "run.py")
runner = importlib.util.module_from_spec(_spec)
_spec.loader.exec_module(runner)

PROMPT = ("Start the agent dev-team:senior one time with this prompt: \"mode: review-core, run folder docs/work/{slug}. "
          "Review the change of the story from Base. Write core-review.md and your log row.\" "
          "Do no other step, change no code, and start no fix. Do not push. Do not call any tool whose name starts with mcp__.")


def one(run, case, version, args):
    repo = run / "repo"
    if not repo.exists():
        bugcases.prepare_case(case, repo)
    root = ROOT if version == "trimmed" else args.bundle_root
    env = runner.environment()
    profile = run / "profile.md"
    profile.write_text("# Dev-team profile\n\n| ID | Answer | Date |\n|----|--------|------|\n", encoding="utf-8")
    env["DEV_TEAM_PROFILE"] = str(profile)
    before = runner.fingerprint(runner.REAL_PROFILE)
    cmd = ["claude", "-p", PROMPT.format(slug=bugcases.SLUG), "--model", args.model, "--permission-mode", "bypassPermissions",
           "--setting-sources", "project,local", "--output-format", "stream-json", "--verbose", "--max-budget-usd", str(args.budget)]
    plugins = args.runs / "_plugins" / version
    for name in ("dev-team", "dev-stack"):
        cmd += ["--plugin-dir", runner.pinned_copy(pathlib.Path(root) / "plugins" / name, plugins / name, args.model)]
    code = runwatch.run_watched(cmd, repo, env, run, {"max_minutes": 25, "stall_minutes": 10})
    (run / "final.md").write_text(runner.final_message(run / "stream.jsonl"), encoding="utf-8")
    if runner.fingerprint(runner.REAL_PROFILE) != before:
        raise runner.ProfileChanged(f"{run} changed {runner.REAL_PROFILE}. Restore the file before the next run.")
    return f"{run.relative_to(args.runs)} exit {code}"


def strip_origin(text):
    """Remove the column Origin from each Markdown table that has it."""
    out, index = [], None
    for line in text.splitlines():
        if line.strip().startswith("|"):
            cells = line.strip().strip("|").split("|")
            names = [c.strip().lower() for c in cells]
            if "origin" in names:
                index = names.index("origin")
            if index is not None and len(cells) > index:
                del cells[index]
                line = "|" + "|".join(cells) + "|"
        else:
            index = None
        out.append(line)
    return "\n".join(out) + "\n"


def collect(runs, out, seed=20261031):
    out.mkdir(parents=True, exist_ok=True)
    found = sorted(runs.glob(f"*/*/*/repo/docs/work/{bugcases.SLUG}/core-review.md"))
    names = [f"R{i:02d}" for i in range(1, len(found) + 1)]
    random.Random(seed).shuffle(names)
    mapping = {}
    for name, path in zip(names, found):
        case, version, number = path.relative_to(runs).parts[:3]
        mapping[name] = {"case": case, "version": version, "run": number}
        text = strip_origin(path.read_text(encoding="utf-8", errors="replace"))
        text = re.sub(r"(?i)\borigin\b:?", "", text)
        (out / f"{name}.md").write_text(f"Case: {case}\n\n{text}", encoding="utf-8")
    (out / "map.json").write_text(json.dumps(mapping, indent=1), encoding="utf-8")
    (out / "key.json").write_text(json.dumps(bugcases.KEY, indent=1), encoding="utf-8")
    return len(found)


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    sub = parser.add_subparsers(dest="command", required=True)
    run_cmd = sub.add_parser("run")
    run_cmd.add_argument("runs", type=pathlib.Path)
    run_cmd.add_argument("--bundle-root", required=True, type=pathlib.Path)
    run_cmd.add_argument("--parallel", type=int, default=2)
    run_cmd.add_argument("--repeat", type=int, default=2)
    run_cmd.add_argument("--budget", type=float, default=4)
    run_cmd.add_argument("--model", default="sonnet")
    col = sub.add_parser("collect")
    col.add_argument("runs", type=pathlib.Path)
    col.add_argument("out", type=pathlib.Path)
    args = parser.parse_args(argv)
    if args.command == "collect":
        print(collect(args.runs.resolve(), args.out.resolve()), "reviews")
        return 0
    args.runs = args.runs.resolve()
    jobs = []
    for case in bugcases.CASES:
        for version in ("trimmed", "bundle"):
            for number in range(1, args.repeat + 1):
                run = args.runs / case / version / str(number)
                if not (run / "final.md").exists():
                    run.mkdir(parents=True, exist_ok=True)
                    jobs.append((run, case, version))
    with concurrent.futures.ThreadPoolExecutor(args.parallel) as pool:
        for line in pool.map(lambda job: one(*job, args), jobs):
            print(line, flush=True)
    return 0


if __name__ == "__main__":
    sys.exit(main())
