#!/usr/bin/env python3
"""Make the run folders for the dev-stack evals.

For each eval, variant, and run number, this script makes <runs>/<eval>/<variant>/<n>/repo:
a git repository with the fixture. A write case has its fixture (the 'fixture' field, for example
orders-api or aspnetcore-trap) as one commit on main. A review case has its base fixture (the
'base' field, default orders-api) on main and the review fixture as one commit on the branch 'feature', so that the
reviewer can see the change with 'git diff main...feature'.
For a case with "stack": "angular", the script runs 'npm ci' in the repository after the commits
(the .gitignore of the fixture keeps node_modules out of git). A case with a "node_dir" field (for example
"frontend") gets 'npm ci' in that folder instead.
A fixture can name files that no run repository gets, one glob pattern for each line of <fixture>/.eval-exclude
(paths relative to the fixture root, '#' starts a comment). An example is a license notice that names the
project that the fixture derives from.

Usage:
    python3 evals/dev-stack/prepare.py <runs> [--eval <name> ...] [--runs-per-variant 3]
"""
import argparse
import fnmatch
import json
import pathlib
import sys
import shutil
import subprocess
sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent))
import cases

HERE = pathlib.Path(__file__).resolve().parent
FIXTURES = HERE / "fixtures"
IGNORE = shutil.ignore_patterns("bin", "obj", "TestResults", "node_modules", "dist", ".angular")
GIT_ENV = ["-c", "user.name=Fixture", "-c", "user.email=fixture@example.com", "-c", "commit.gpgsign=false"]
EXCLUDE_FILE = ".eval-exclude"


def git(repo, *args):
    subprocess.run(["git", *GIT_ENV, "-C", str(repo), *args], check=True, capture_output=True, text=True)


def excluded(fixture):
    """The patterns of <fixture>/.eval-exclude, and the file itself."""
    path = pathlib.Path(fixture) / EXCLUDE_FILE
    lines = path.read_text(encoding="utf-8").splitlines() if path.is_file() else []
    return [EXCLUDE_FILE] + [l.strip() for l in lines if l.strip() and not l.lstrip().startswith("#")]


def is_excluded(rel, patterns):
    """True when the relative path, or a folder above it, matches one of the patterns."""
    parts = pathlib.PurePosixPath(rel).parts
    return any(fnmatch.fnmatch("/".join(parts[:n]), p) for n in range(1, len(parts) + 1) for p in patterns)


def fixture_ignore(fixture, base=IGNORE):
    """An ignore function for shutil.copytree: the names of 'base', and the excluded paths of the fixture."""
    fixture = pathlib.Path(fixture)
    patterns = excluded(fixture)

    def ignore(folder, names):
        rel = pathlib.Path(folder).relative_to(fixture)
        return set(base(folder, names)) | {n for n in names if is_excluded((rel / n).as_posix(), patterns)}
    return ignore


def copy_tree(src, dst):
    for item in dst.iterdir():
        if item.name != ".git":
            shutil.rmtree(item) if item.is_dir() else item.unlink()
    shutil.copytree(src, dst, ignore=fixture_ignore(src), dirs_exist_ok=True)


def node_dir(ev):
    """The folder of the case that gets 'npm ci', or None."""
    return ev.get("node_dir") or ("." if ev.get("stack") == "angular" else None)


def copy_fixture(name, dst):
    """Copy the fixture 'name' into dst. A fixture folder with an overlay.json file holds only changes:
    the script copies the fixture that its "from" field names first (this can be an overlay too), then
    the files of the overlay, and deletes the paths of its "delete" list."""
    src = FIXTURES / name
    meta = src / "overlay.json"
    if not meta.is_file():
        copy_tree(src, dst)
        return
    spec = json.loads(meta.read_text(encoding="utf-8"))
    copy_fixture(spec["from"], dst)
    shutil.copytree(src, dst, dirs_exist_ok=True,
                    ignore=lambda folder, names: set(IGNORE(folder, names)) | ({"overlay.json"} & set(names)))
    for rel in spec.get("delete", []):
        (dst / rel).unlink(missing_ok=True)


def make_repo(ev, repo):
    repo.mkdir(parents=True)
    git(repo, "init", "-q", "-b", "main")
    copy_fixture(ev.get("base", "orders-api") if ev["kind"] == "review" else ev["fixture"], repo)
    git(repo, "add", "-A")
    git(repo, "commit", "-q", "-m", {"angular": "Products admin", "kiosk": "Shop service"}.get(ev.get("stack"), "Orders API"))
    if ev["kind"] == "review":
        git(repo, "checkout", "-q", "-b", "feature")
        copy_fixture(ev["fixture"], repo)
        git(repo, "add", "-A")
        git(repo, "commit", "-q", "-m", "Changes for review")
    if node_dir(ev):
        subprocess.run(["npm", "ci", "--prefer-offline", "--no-audit", "--no-fund"], cwd=repo / node_dir(ev), check=True,
                       capture_output=True, text=True)


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
