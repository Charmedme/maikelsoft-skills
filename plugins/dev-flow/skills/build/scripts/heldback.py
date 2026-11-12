#!/usr/bin/env python3
"""heldback: keep the held-back tests of an item outside the repository, and run them.

Usage:
    python3 heldback.py add --item KEY --name FILE [--root .]      < test text on stdin
    python3 heldback.py list --item KEY [--root .]
    python3 heldback.py run --item KEY --dest DIR --test "<command>" [--root .] [--lines 40]

A separate agent writes the held-back tests from spec.md and the DSL. The builder never reads
them. The tests live in the data folder of the plugin, not in the repository:

    $DEV_FLOW_HELDBACK, else ~/.claude/plugins/data/dev-flow/heldback
      /<repo key>/<item>/<file>

The repo key is the first 12 characters of the SHA-256 of the path of the git root, so two
checkouts do not share tests. The PreToolUse hook of the plugin (hooks/guard.py) blocks each
direct read or write of this folder. Only this script can add or run the tests.

"add" saves the text of stdin as FILE. "list" gives the file names and the count, not the text.
"run" copies the files into DEST (a folder in the repository that does not exist yet), runs the
test command at the root, prints the exit code and the last lines of the output, and then
removes DEST. The copy is always removed, also after an error.
Exit code: "run" gives the exit code of the test command. 2 means a usage error.
The script uses the Python standard library only.
"""
import argparse
import hashlib
import os
import pathlib
import re
import shutil
import subprocess
import sys

NAME_RE = re.compile(r"^[A-Za-z0-9][A-Za-z0-9._-]*$")


def base() -> pathlib.Path:
    """Return the root folder of all held-back tests."""
    env = os.environ.get("DEV_FLOW_HELDBACK")
    if env:
        return pathlib.Path(env).expanduser()
    return pathlib.Path.home() / ".claude" / "plugins" / "data" / "dev-flow" / "heldback"


def git_root(start: pathlib.Path) -> pathlib.Path:
    p = subprocess.run(["git", "-C", str(start), "rev-parse", "--show-toplevel"], capture_output=True, text=True)
    if p.returncode != 0:
        raise SystemExit(f"heldback: {start} is not in a git repository")
    return pathlib.Path(p.stdout.strip()).resolve()


def item_dir(root: pathlib.Path, item: str) -> pathlib.Path:
    if not NAME_RE.match(item):
        raise SystemExit(f"heldback: the item {item!r} must use only letters, digits, '.', '_' and '-'")
    key = hashlib.sha256(str(root).encode("utf-8")).hexdigest()[:12]
    return base() / key / item


def cmd_add(args) -> int:
    if not NAME_RE.match(args.name):
        print(f"heldback: the name {args.name!r} must be a file name without a folder", file=sys.stderr)
        return 2
    folder = item_dir(git_root(args.root), args.item)
    folder.mkdir(parents=True, exist_ok=True)
    text = sys.stdin.read()
    if not text.strip():
        print("heldback: stdin is empty. Give the test text on stdin.", file=sys.stderr)
        return 2
    (folder / args.name).write_text(text, encoding="utf-8")
    print(f"saved {args.name} for {args.item} ({len(text.splitlines())} lines)")
    return 0


def files(folder: pathlib.Path) -> list:
    return sorted(p for p in folder.iterdir() if p.is_file()) if folder.is_dir() else []


def cmd_list(args) -> int:
    found = files(item_dir(git_root(args.root), args.item))
    for p in found:
        print(p.name)
    print(f"{len(found)} held-back test file(s) for {args.item}")
    return 0


def cmd_run(args) -> int:
    root = git_root(args.root)
    found = files(item_dir(root, args.item))
    if not found:
        print(f"heldback: no held-back tests for {args.item}", file=sys.stderr)
        return 2
    dest = (root / args.dest).resolve()
    try:
        dest.relative_to(root)
    except ValueError:
        print(f"heldback: {args.dest} must be in the repository", file=sys.stderr)
        return 2
    if dest.exists():
        print(f"heldback: {args.dest} exists. Give a new folder.", file=sys.stderr)
        return 2
    try:
        dest.mkdir(parents=True)
        for p in found:
            shutil.copyfile(p, dest / p.name)
        run = subprocess.run(args.test, shell=True, cwd=root, capture_output=True, text=True)
        output = (run.stdout + run.stderr).splitlines()
        print("\n".join(output[-args.lines:]))
        print(f"held-back tests of {args.item}: {len(found)} file(s), exit {run.returncode}")
        return run.returncode
    finally:
        shutil.rmtree(dest, ignore_errors=True)


def main(argv=None) -> int:
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    sub = parser.add_subparsers(dest="command", required=True)
    add = sub.add_parser("add")
    add.add_argument("--item", required=True)
    add.add_argument("--name", required=True)
    add.add_argument("--root", type=pathlib.Path, default=pathlib.Path("."))
    lst = sub.add_parser("list")
    lst.add_argument("--item", required=True)
    lst.add_argument("--root", type=pathlib.Path, default=pathlib.Path("."))
    run = sub.add_parser("run")
    run.add_argument("--item", required=True)
    run.add_argument("--dest", required=True)
    run.add_argument("--test", required=True)
    run.add_argument("--root", type=pathlib.Path, default=pathlib.Path("."))
    run.add_argument("--lines", type=int, default=40)
    args = parser.parse_args(argv)
    return {"add": cmd_add, "list": cmd_list, "run": cmd_run}[args.command](args)


if __name__ == "__main__":
    sys.exit(main())
