#!/usr/bin/env python3
"""PreToolUse hook of dev-flow: block an edit of an approved acceptance test.

Claude Code sends the tool call as JSON on stdin. When the target file is in
.dev-flow/spec-lock.json of its git repository, the hook exits 2: Claude Code
blocks the call and gives the message on stderr to Claude. A Bash command that
names a locked file and can write is blocked too. Any other call passes (exit 0).
The hook fails open: an error in the hook never blocks work.
"""
import json
import pathlib
import re
import subprocess
import sys

WRITE_TOOLS = {"Edit", "Write", "MultiEdit", "NotebookEdit"}
WRITE_WORDS = re.compile(r"(^|[\s;&|(])(rm|mv|cp|sed|perl|truncate|tee|git\s+(checkout|restore|rm|mv))(\s|$)|\bopen\(|write_text|WriteAllText")
MESSAGE = ("{file} is an approved acceptance test (item {item}, approved by {by}). Do not change, skip, or delete it, "
           "and do not change the DSL to make it pass. Tell the human which test seems wrong and why. "
           "After the human approves a change, run speclock.py unlock.")


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
            writes = bool(WRITE_WORDS.search(command))
            for name, e in files.items():
                base = re.escape(pathlib.Path(name).name)
                redirect = re.search(r">>?\s*['\"]?\S*" + base, command)
                if redirect or (writes and re.search(base, command)):
                    print(MESSAGE.format(file=name, item=e.get("item"), by=e.get("by")), file=sys.stderr)
                    return 2
        return 0
    except Exception:  # noqa: BLE001 - fail open
        return 0


if __name__ == "__main__":
    sys.exit(main())
