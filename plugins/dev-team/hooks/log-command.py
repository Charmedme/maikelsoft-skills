#!/usr/bin/env python3
"""PostToolUse hook of the dev-team plugin: append each Bash command to the commands log of the active run.

The hook writes only while a story runs. The file <project>/.dev-team/active-run then holds the path
of the run folder (for example docs/work/order-cancel). Without that file, the hook does nothing.

One JSON line per command in <run folder>/commands.jsonl:
    {"time", "session_id", "agent_id", "agent_type", "command", "exit_code"}
agent_type is null for the main session. runcheck.py compares this file with log.md.

The hook never blocks a tool call: on any error it exits 0 without output.
Python 3.9 or later, standard library only.
"""
import datetime
import json
import os
import pathlib
import sys


def exit_code(response):
    if isinstance(response, dict):
        for key in ("exit_code", "exitCode", "returnCode", "code"):
            if isinstance(response.get(key), int):
                return response[key]
        if response.get("interrupted"):
            return -1
    return None


def main():
    try:
        event = json.load(sys.stdin)
        if event.get("tool_name") != "Bash":
            return 0
        project = pathlib.Path(os.environ.get("CLAUDE_PROJECT_DIR") or event.get("cwd") or ".")
        marker = project / ".dev-team" / "active-run"
        if not marker.is_file():
            return 0
        run = pathlib.Path(marker.read_text(encoding="utf-8").strip())
        run = run if run.is_absolute() else project / run
        if not run.is_dir():
            return 0
        entry = {
            "time": datetime.datetime.now(datetime.timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ"),
            "session_id": event.get("session_id"),
            "agent_id": event.get("agent_id"),
            "agent_type": event.get("agent_type"),
            "command": (event.get("tool_input") or {}).get("command", ""),
            "exit_code": exit_code(event.get("tool_response")),
        }
        with open(run / "commands.jsonl", "a", encoding="utf-8") as f:
            f.write(json.dumps(entry) + "\n")
    except Exception:  # noqa: BLE001 - a logging hook must never break the session
        return 0
    return 0


if __name__ == "__main__":
    sys.exit(main())
