#!/usr/bin/env python3
"""Make the run folders for the dev-stack evals.

For each eval, variant, and run number, this script makes <runs>/<eval>/<variant>/<n>/repo:
a git repository with the fixture. A write case has its fixture (the 'fixture' field, for example
orders-api or aspnetcore-trap) as one commit on main. A review case has its base fixture (the
'base' field, default orders-api) on main and the review fixture as one commit on the branch 'feature', so that the
reviewer can see the change with 'git diff main...feature'.

Usage:
    python3 evals/dev-stack/prepare.py <runs> [--eval <name> ...] [--runs-per-variant 3]
"""
import argparse
import json
import pathlib
import sys
import shutil
import subprocess
sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent))
import cases

HERE = pathlib.Path(__file__).resolve().parent
FIXTURES = HERE / "fixtures"
IGNORE = shutil.ignore_patterns("bin", "obj", "TestResults")
GIT_ENV = ["-c", "user.name=Fixture", "-c", "user.email=fixture@example.com", "-c", "commit.gpgsign=false"]


def git(repo, *args):
    subprocess.run(["git", *GIT_ENV, "-C", str(repo), *args], check=True, capture_output=True, text=True)


def copy_tree(src, dst):
    for item in dst.iterdir():
        if item.name != ".git":
            shutil.rmtree(item) if item.is_dir() else item.unlink()
    shutil.copytree(src, dst, ignore=IGNORE, dirs_exist_ok=True)


def make_repo(ev, repo):
    repo.mkdir(parents=True)
    git(repo, "init", "-q", "-b", "main")
    copy_tree(FIXTURES / (ev.get("base", "orders-api") if ev["kind"] == "review" else ev["fixture"]), repo)
    git(repo, "add", "-A")
    git(repo, "commit", "-q", "-m", "Orders API")
    if ev["kind"] == "review":
        git(repo, "checkout", "-q", "-b", "feature")
        copy_tree(FIXTURES / ev["fixture"], repo)
        git(repo, "add", "-A")
        git(repo, "commit", "-q", "-m", "Changes for review")


def main():
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("runs", type=pathlib.Path)
    parser.add_argument("--eval", action="append", help="limit to these eval names")
    parser.add_argument("--runs-per-variant", type=int, default=3)
    args = parser.parse_args()
    evals = cases.load_list()
    for ev in evals:
        if args.eval and ev["name"] not in args.eval:
            continue
        for variant in ("baseline", "skill"):
            for n in range(1, args.runs_per_variant + 1):
                repo = args.runs / ev["name"] / variant / str(n) / "repo"
                if repo.exists():
                    continue
                make_repo(ev, repo)
                print(repo)


if __name__ == "__main__":
    main()
