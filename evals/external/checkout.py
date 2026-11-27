#!/usr/bin/env python3
"""Clone an external project of projects.json at its pinned commit, outside this repository.

    python3 evals/external/checkout.py --list [--skill wolverine]
    python3 evals/external/checkout.py <owner/repo> <target folder>

The clone gets only the pinned commit (git fetch --depth 1). The target folder must not be inside this
repository, so that no file of an external project gets into a commit here. A project keeps its own
license: check it before you copy a file from it.
"""
import argparse
import json
import pathlib
import subprocess
import sys

HERE = pathlib.Path(__file__).resolve().parent
ROOT = HERE.parents[1]


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("repo", nargs="?")
    parser.add_argument("target", nargs="?")
    parser.add_argument("--list", action="store_true")
    parser.add_argument("--skill")
    args = parser.parse_args()
    projects = json.loads((HERE / "projects.json").read_text(encoding="utf-8"))["projects"]

    if args.list:
        for p in projects:
            if args.skill and args.skill not in p["skills"]:
                continue
            print(f"{p['repo'].removeprefix('https://github.com/'):55} {p['commit'][:10]} {p['license']:13} {', '.join(p['skills'])}")
        return 0
    if not (args.repo and args.target):
        parser.error("give <owner/repo> and <target folder>, or --list")

    match = [p for p in projects if p["repo"].endswith("/" + args.repo)]
    if not match:
        print(f"{args.repo} is not in projects.json", file=sys.stderr)
        return 1
    project = match[0]
    target = pathlib.Path(args.target).resolve()
    if target == ROOT or ROOT in target.parents:
        print("The target folder must be outside this repository.", file=sys.stderr)
        return 1
    if target.exists() and any(target.iterdir()):
        print(f"{target} is not empty.", file=sys.stderr)
        return 1
    target.mkdir(parents=True, exist_ok=True)
    for cmd in (["git", "init", "-q"],
                ["git", "remote", "add", "origin", project["repo"] + ".git"],
                ["git", "fetch", "-q", "--depth", "1", "origin", project["commit"]],
                ["git", "checkout", "-q", "FETCH_HEAD"]):
        subprocess.run(cmd, cwd=target, check=True)
    print(f"{args.repo} at {project['commit'][:10]} in {target}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
