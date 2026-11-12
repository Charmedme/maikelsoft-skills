#!/usr/bin/env python3
"""PreToolUse hook of dev-flow: block an edit of an approved acceptance test, and each
direct access to the held-back tests.

Claude Code sends the tool call as JSON on stdin. When the target file is in
.dev-flow/spec-lock.json of its git repository, the hook exits 2: Claude Code
blocks the call and gives the message on stderr to Claude. A Bash command that
names a locked file and can write is blocked too.

The held-back tests are in the data folder of the plugin (see heldback.py of the build
skill). A read, a search, or a write of that folder is blocked, and a Bash command
that names it is blocked, unless the command runs heldback.py. Any other call passes
(exit 0). The hook fails open: an error in the hook never blocks work.
"""
import json
import os
import pathlib
import re
import subprocess
import sys

WRITE_TOOLS = {"Edit", "Write", "MultiEdit", "NotebookEdit"}
WRITE_WORDS = re.compile(r"(^|[\s;&|(])(rm|mv|cp|sed|perl|truncate|tee|git\s+(checkout|restore|rm|mv))(\s|$)|\bopen\(|write_text|WriteAllText")
MESSAGE = ("{file} is an approved acceptance test (item {item}, approved by {by}). Do not change, skip, or delete it, "
           "and do not change the DSL to make it pass. Tell the human which test seems wrong and why. "
           "After the human approves a change, run speclock.py unlock.")


HELD_TOOLS = {"Read", "Grep", "Glob", "NotebookRead", "Edit", "Write", "MultiEdit", "NotebookEdit"}
HELD_MESSAGE = ("{path} holds the held-back tests. The builder does not read them. "
                "Run them with heldback.py run, and read only its result.")


def held_base() -> pathlib.Path:
    """The folder of the held-back tests. Keep it the same as base() in heldback.py."""
    env = os.environ.get("DEV_FLOW_HELDBACK")
    if env:
        return pathlib.Path(env).expanduser()
    return pathlib.Path.home() / ".claude" / "plugins" / "data" / "dev-flow" / "heldback"


def held_hit(args: dict, cwd: pathlib.Path, tool: str) -> str:
    """Return the path when the call touches the held-back folder, else an empty text."""
    held = held_base().resolve()
    if tool in HELD_TOOLS:
        for key in ("file_path", "notebook_path", "path"):
            value = args.get(key)
            if not value:
                continue
            target = pathlib.Path(value).expanduser()
            target = (target if target.is_absolute() else cwd / target).resolve()
            if target == held or held in target.parents:
                return str(target)
        pattern = str(args.get("pattern") or "")
        if tool == "Glob" and ("heldback" in pattern or str(held) in pattern):
            return pattern
        return ""
    if tool == "Bash":
        command = args.get("command", "")
        names = {str(held), "dev-flow/heldback", "plugins/data/dev-flow", "DEV_FLOW_HELDBACK"}
        if not any(n in command for n in names):
            return ""
        segments = [s.strip() for s in re.split(r"\n|&&|\|\||;|\|", command) if s.strip()]
        for seg in segments:
            if any(n in seg for n in names) and not re.search(r"\bheldback\.py\s+(add|list|run)\b", seg):
                return str(held)
    return ""


def lock_for(start: pathlib.Path):
    p = subprocess.run(["git", "-C", str(start), "rev-parse", "--show-toplevel"], capture_output=True, text=True)
    if p.returncode != 0:
        return None, None
    root = pathlib.Path(p.stdout.strip())
    f = root / ".dev-flow" / "spec-lock.json"
    if not f.is_file():
        return root, None
    return root, json.loads(f.read_text(encoding="utf-8")).get("files", {})


def main() -> int:
    try:
        event = json.load(sys.stdin)
        tool = event.get("tool_name", "")
        args = event.get("tool_input", {}) or {}
        cwd = pathlib.Path(event.get("cwd") or ".")
        held = held_hit(args, cwd, tool)
        if held:
            print(HELD_MESSAGE.format(path=held), file=sys.stderr)
            return 2
        if tool in WRITE_TOOLS:
            target = pathlib.Path(args.get("file_path") or args.get("notebook_path") or "")
            if not target.is_absolute():
                target = cwd / target
            root, files = lock_for(target.parent if target.parent.exists() else cwd)
            if not files:
                return 0
            try:
                name = target.resolve().relative_to(root.resolve()).as_posix()
            except ValueError:
                return 0
            if name in files:
                e = files[name]
                print(MESSAGE.format(file=name, item=e.get("item"), by=e.get("by")), file=sys.stderr)
                return 2
            return 0
        if tool == "Bash":
            command = args.get("command", "")
            root, files = lock_for(cwd)
            if not files:
                return 0
            # Look at each simple command alone, so that a file name in a commit message does not count.
            segments = [s.strip() for s in re.split(r"\n|&&|\|\||;|\|", command) if s.strip()]
            for name, e in files.items():
                base = re.escape(pathlib.Path(name).name)
                hit = False
                for seg in segments:
                    if re.search(r">>?\s*['\"]?\S*" + base, seg):
                        hit = True
                    elif WRITE_WORDS.match(seg) or re.match(r"^(\w+=\S+\s+)*(rm|mv|cp|sed|perl|truncate|tee|git\s+(checkout|restore|rm|mv))\b", seg):
                        hit = hit or bool(re.search(base, seg))
                if hit:
                    print(MESSAGE.format(file=name, item=e.get("item"), by=e.get("by")), file=sys.stderr)
                    return 2
        return 0
    except Exception:  # noqa: BLE001 - fail open
        return 0


if __name__ == "__main__":
    sys.exit(main())
