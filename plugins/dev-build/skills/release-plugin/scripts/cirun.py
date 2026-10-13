#!/usr/bin/env python3
"""Run the `run:` steps of the GitHub Actions workflows locally, before a push.

    python3 cirun.py [repo folder] [--workflow ci.yml] [--list]

The script reads each workflow in .github/workflows/ that runs on pull_request.
It runs each `run:` step with bash in the repository root, in order.
It skips a step that uses a `${{ }}` expression, because only GitHub can fill it in.
It removes a line that installs a tool (npm, pip, apt), because the tool must
already be on this computer. A missing tool shows as a failed step.

Exit 0 when all steps pass, else 1. The output has one line per step:
    PASS <workflow> / <step>
    FAIL <workflow> / <step>   followed by the last lines of its output
    SKIP <workflow> / <step>   with the reason
"""
import argparse
import pathlib
import re
import subprocess
import sys

INSTALL_LINE = re.compile(r"^\s*(sudo\s+)?(npm|pnpm|yarn|pip3?|python3? -m pip|apt(-get)?)\b.*\binstall\b")
TAIL_LINES = 25  # lines of output to show for a failed step


def steps_of(text: str):
    """Yield (name, script) for each run step. A small parser for the usual workflow form."""
    lines = text.splitlines()
    name, i = None, 0
    while i < len(lines):
        line = lines[i]
        m_name = re.match(r"^\s*-\s+name:\s*(.+)$", line)
        if m_name:
            name = m_name.group(1).strip().strip("'\"")
        m_run = re.match(r"^(\s*)(?:-\s+)?run:\s*(.*)$", line)
        if m_run:
            indent, value = len(m_run.group(1)), m_run.group(2).strip()
            if value in ("|", ">", "|-", ">-"):
                block, i = [], i + 1
                while i < len(lines) and (not lines[i].strip() or len(lines[i]) - len(lines[i].lstrip()) > indent):
                    block.append(lines[i])
                    i += 1
                strip = min((len(b) - len(b.lstrip()) for b in block if b.strip()), default=0)
                script = "\n".join(b[strip:] for b in block).strip()
                if value.startswith(">"):
                    script = " ".join(script.split("\n"))
                yield name or "unnamed step", script
                name = None
                continue
            yield name or "unnamed step", value
            name = None
        i += 1


def runs_on_pull_request(text: str) -> bool:
    head = text.split("jobs:", 1)[0]
    return "pull_request" in head


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("repo", nargs="?", default=".")
    parser.add_argument("--workflow", action="append", help="only this workflow file (repeatable)")
    parser.add_argument("--list", action="store_true", help="show the steps, run nothing")
    args = parser.parse_args()
    root = pathlib.Path(subprocess.run(["git", "rev-parse", "--show-toplevel"], cwd=args.repo,
                                       capture_output=True, text=True).stdout.strip() or args.repo)
    folder = root / ".github" / "workflows"
    files = sorted(folder.glob("*.y*ml")) if folder.is_dir() else []
    if args.workflow:
        files = [f for f in files if f.name in args.workflow]
    if not files:
        print("No workflow found in .github/workflows/. Nothing to run.")
        return 0
    failed = ran = 0
    for wf in files:
        text = wf.read_text(encoding="utf-8")
        if not runs_on_pull_request(text):
            print(f"SKIP {wf.name}: does not run on pull_request")
            continue
        for name, script in steps_of(text):
            label = f"{wf.name} / {name}"
            if "${{" in script:
                print(f"SKIP {label}: uses a GitHub expression")
                continue
            kept = [ln for ln in script.splitlines() if not INSTALL_LINE.match(ln)]
            if not any(ln.strip() for ln in kept):
                print(f"SKIP {label}: only installs a tool")
                continue
            if args.list:
                print(f"STEP {label}\n  " + "\n  ".join(kept))
                continue
            ran += 1
            out = subprocess.run(["bash", "-e", "-o", "pipefail", "-c", "\n".join(kept)], cwd=root,
                                 capture_output=True, text=True)
            if out.returncode == 0:
                print(f"PASS {label}")
            else:
                failed += 1
                print(f"FAIL {label}")
                tail = (out.stdout + out.stderr).strip().splitlines()[-TAIL_LINES:]
                print("  " + "\n  ".join(tail))
    if not args.list:
        print(f"{ran} step(s) run, {failed} failed")
    return 1 if failed else 0


if __name__ == "__main__":
    sys.exit(main())
