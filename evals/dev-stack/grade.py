#!/usr/bin/env python3
"""Grade the 'judge' assertions of the dev-stack eval runs with blind grader agents.

    python3 evals/dev-stack/grade.py <runs> <graded> [--eval <name> ...] [--parallel 6] [--model sonnet]

For each run folder <runs>/<eval>/<variant>/<n> that has a final.md and no grade yet, the script copies
final.md and repo (without bin, obj and TestResults) to <graded>/G<nnn>, so that the grader does not see
the variant. It writes the prompt of grader.md, runs one headless claude per copy, and saves the
grader JSON as <run>/grade.json. The map from G<nnn> to the run is in <graded>/map.json.

Needs claude and git.
"""
import argparse
import concurrent.futures
import json
import os
import pathlib
import random
import re
import shutil
import subprocess
import sys

HERE = pathlib.Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
from run import PARENT_VARS  # noqa: E402

IGNORE = shutil.ignore_patterns("bin", "obj", "TestResults", "node_modules")


def grader_prompt(ev, copy):
    text = (HERE / "grader.md").read_text(encoding="utf-8")
    body = re.search(r"## Prompt\n\n(.*)", text, re.S).group(1).strip()
    assertions = "\n".join(f"- {a['id']}: {a['judge']}" for a in ev["assertions"] if "judge" in a)
    planted = HERE / "planted" / f"{ev['fixture']}.json"
    body = (body.replace("<run>", str(copy)).replace("<request>", ev["request"].replace("\n", "\n> "))
            .replace("<planted>", str(planted)).replace("<assertions>", assertions))
    return body + (f"\n\nWrite the JSON also to {copy}/grade.json. Do not change any other file. "
                   "Do not call any tool whose name starts with mcp__Agent_Mesh.")


def grade(job, args):
    gid, run, ev = job
    copy = args.graded / gid
    if not copy.exists():
        copy.mkdir(parents=True)
        shutil.copy(run / "final.md", copy / "final.md")
        shutil.copytree(run / "repo", copy / "repo", ignore=IGNORE, symlinks=True)
    env = {k: v for k, v in os.environ.items() if k not in PARENT_VARS}
    cmd = ["claude", "-p", grader_prompt(ev, copy), "--model", args.model, "--permission-mode", "bypassPermissions",
           "--disallowedTools", "Agent", "Skill", "--output-format", "json"]
    with open(copy / "grader.json", "w") as out, open(copy / "grader.err", "w") as err:
        subprocess.run(cmd, cwd=copy, env=env, stdout=out, stderr=err, timeout=args.timeout)
    result = copy / "grade.json"
    if result.is_file():
        try:
            data = json.loads(result.read_text(encoding="utf-8"))
            (run / "grade.json").write_text(json.dumps(data, indent=1), encoding="utf-8")
            return f"{gid} {run.relative_to(args.runs)} ok"
        except ValueError:
            pass
    return f"{gid} {run.relative_to(args.runs)} NO GRADE"


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("runs", type=pathlib.Path)
    parser.add_argument("graded", type=pathlib.Path)
    parser.add_argument("--eval", nargs="*")
    parser.add_argument("--parallel", type=int, default=6)
    parser.add_argument("--model", default="sonnet")
    parser.add_argument("--timeout", type=int, default=1800)
    args = parser.parse_args(argv)
    evals = {e["name"]: e for e in json.loads((HERE / "evals.json").read_text(encoding="utf-8"))["evals"]}
    args.graded.mkdir(parents=True, exist_ok=True)
    map_file = args.graded / "map.json"
    mapping = json.loads(map_file.read_text()) if map_file.exists() else {}
    known = {v: k for k, v in mapping.items()}
    runs = [p.parent for p in sorted(args.runs.glob("*/*/*/final.md"))
            if p.parent.parts[-3] in evals and (not args.eval or p.parent.parts[-3] in args.eval)
            and not (p.parent / "grade.json").exists() and p.read_text(encoding="utf-8").strip()]
    random.Random(len(mapping)).shuffle(runs)
    jobs = []
    for run in runs:
        rel = str(run.relative_to(args.runs))
        gid = known.get(rel)
        if gid is None:
            gid = f"G{len(mapping) + 1:03d}"
            mapping[gid] = rel
        jobs.append((gid, run, evals[run.parts[-3]]))
    map_file.write_text(json.dumps(mapping, indent=1), encoding="utf-8")
    with concurrent.futures.ThreadPoolExecutor(args.parallel) as pool:
        for line in pool.map(lambda j: grade(j, args), jobs):
            print(line, flush=True)
    return 0


if __name__ == "__main__":
    sys.exit(main())
