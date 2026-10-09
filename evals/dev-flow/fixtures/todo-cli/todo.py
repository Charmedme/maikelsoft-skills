"""todo: a small to-do list on the command line.

Usage:
    python3 todo.py add "Buy milk"
    python3 todo.py list
    python3 todo.py done 1

The tasks are stored in a JSON file. Set TODO_FILE to change the path.
"""
from __future__ import annotations

import argparse
import json
import os
import pathlib
import sys


def store_path() -> pathlib.Path:
    return pathlib.Path(os.environ.get("TODO_FILE", pathlib.Path.home() / ".todo.json"))


def load(path: pathlib.Path) -> list[dict]:
    if not path.exists():
        return []
    return json.loads(path.read_text(encoding="utf-8"))


def save(path: pathlib.Path, tasks: list[dict]) -> None:
    path.write_text(json.dumps(tasks, indent=2), encoding="utf-8")


def add(tasks: list[dict], title: str) -> list[dict]:
    next_id = max((t["id"] for t in tasks), default=0) + 1
    return tasks + [{"id": next_id, "title": title, "done": False}]


def complete(tasks: list[dict], task_id: int) -> list[dict]:
    if not any(t["id"] == task_id for t in tasks):
        raise KeyError(task_id)
    return [dict(t, done=True) if t["id"] == task_id else t for t in tasks]


def render(tasks: list[dict]) -> str:
    lines = [f"[{'x' if t['done'] else ' '}] {t['id']} {t['title']}" for t in tasks]
    return "\n".join(lines) if lines else "No tasks."


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description="A small to-do list.")
    sub = parser.add_subparsers(dest="command", required=True)
    p_add = sub.add_parser("add", help="add a task")
    p_add.add_argument("title")
    sub.add_parser("list", help="list the tasks")
    p_done = sub.add_parser("done", help="mark a task as done")
    p_done.add_argument("id", type=int)
    args = parser.parse_args(argv)

    path = store_path()
    tasks = load(path)
    if args.command == "add":
        tasks = add(tasks, args.title)
        save(path, tasks)
        print(f"Added {tasks[-1]['id']}")
    elif args.command == "list":
        print(render(tasks))
    elif args.command == "done":
        try:
            tasks = complete(tasks, args.id)
        except KeyError:
            print(f"No task {args.id}", file=sys.stderr)
            return 1
        save(path, tasks)
        print(f"Done {args.id}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
