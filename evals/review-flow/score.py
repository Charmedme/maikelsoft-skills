#!/usr/bin/env python3
"""Print one fact table for each run of the review-flow comparison.

    python3 evals/review-flow/score.py <runs>

The script reads the repository after the run, the stream, and the final message. It gives facts, not a
verdict. The arms show in the output, so a human who reads it is not blind.
"""
import json
import pathlib
import re
import subprocess
import sys

ENDPOINTS = "src/Orders.Api/Orders/OrderEndpoints.cs"


def git(repo, *args):
    return subprocess.run(["git", "-C", str(repo), *args], capture_output=True, text=True, errors="replace").stdout


def stream_facts(stream):
    facts = {"cost": None, "minutes": None, "agents": []}
    if not stream.is_file():
        return facts
    for line in stream.read_text(encoding="utf-8", errors="ignore").splitlines():
        try:
            event = json.loads(line)
        except ValueError:
            continue
        if event.get("type") == "result":
            facts["cost"] = event.get("total_cost_usd")
            facts["minutes"] = round((event.get("duration_ms") or 0) / 60000, 1)
        elif event.get("type") == "assistant":
            for block in (event.get("message") or {}).get("content") or []:
                if isinstance(block, dict) and block.get("type") == "tool_use" and block.get("name") in ("Agent", "Task"):
                    facts["agents"].append((block.get("input") or {}).get("subagent_type", "?"))
    return facts


def code_facts(repo):
    """What the final code does with each planted problem. Only useful for the prepared scenarios."""
    path = repo / ENDPOINTS
    text = path.read_text(encoding="utf-8", errors="replace") if path.is_file() else ""
    cancel = text[text.find("CancelAsync("):] if "CancelAsync(" in text else ""
    total = text[text.find("GetTotalAsync("):] if "GetTotalAsync(" in text else ""
    total = total[:total.find("}") + 1] if "}" in total else total
    return {
        "P1 shipped handled": "AlreadyShipped" in cancel or "Shipped" in cancel,
        "P2 window from options": "CancelWindow" in cancel,
        "N1 o renamed": not re.search(r"\bvar o\b", cancel),
        "N2 comment removed": "// get the order" not in cancel,
        "older bug changed": "Include" in total,
        "still 409 on closed window": "400" not in cancel,
    }


def run_facts(run):
    repo = run / "repo"
    facts = stream_facts(run / "stream.jsonl")
    facts["commits"] = len(git(repo, "log", "--oneline", "eval-start..HEAD").splitlines())
    facts.update(code_facts(repo))
    folders = list(repo.glob("docs/work/*/run.md"))
    if folders:
        folder = folders[0].parent
        run_md = (folder / "run.md").read_text(encoding="utf-8", errors="replace")
        mode = re.search(r"\|\s*Mode\s*\|\s*`?(\w+)", run_md)
        facts["mode"] = mode.group(1) if mode else "-"
        facts["security review"] = (folder / "security-review.md").is_file()
        facts["older-problems.md"] = (folder / "older-problems.md").is_file()
        facts["risk files"] = sorted(p.name for p in folder.rglob("*risk*.txt"))
        facts["follow-up files"] = sorted(p.name for p in folder.glob("follow-ups/*"))
    final = run / "final.md"
    text = final.read_text(encoding="utf-8", errors="replace") if final.is_file() else ""
    facts["final words"] = len(text.split())
    facts["Rounds line"] = bool(re.search(r"Rounds:\s*\d", text))
    facts["three options"] = bool(re.search(r"(?i)go on", text) and re.search(r"(?i)close the threads", text) and re.search(r"(?i)new run", text))
    return facts


def main(argv=None):
    runs = pathlib.Path((argv or sys.argv[1:])[0])
    for stream in sorted(runs.glob("*/*/stream.jsonl")):
        run = stream.parent
        print(f"\n## {run.parent.name} / {run.name}")
        for key, value in run_facts(run).items():
            print(f"- {key}: {value}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
