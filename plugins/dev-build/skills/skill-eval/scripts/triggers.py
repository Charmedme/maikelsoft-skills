#!/usr/bin/env python3
"""Trigger test: does a model pick the correct skill from its description?

    python3 triggers.py <queries.json> --plugins <plugin dir> [<plugin dir> ...]
                        [--runs 3] [--model haiku] [--out results.md]

queries.json:
    {"<skill name>": {"must": ["request", ...], "must_not": ["request", ...]}, ...}

The model sees the name and the description of each skill in the given plugins,
and one request. It answers with one skill name or "none". A "must" request
passes when the model picks the skill. A "must_not" request passes when it picks
any other answer. Put competing plugins in --plugins too, so the test is real.
"""
import argparse
import json
import os
import pathlib
import re
import subprocess
import sys
import tempfile

PROMPT = """You select a skill for a request. These skills are available:

{skills}

Request: "{query}"

Which skill fits this request best? Reply with the skill name only, or with the word none when no skill fits."""


def descriptions(plugins):
    found = {}
    for plugin in plugins:
        for skill in sorted(pathlib.Path(plugin).glob("skills/*/SKILL.md")):
            text = skill.read_text(encoding="utf-8")
            name = re.search(r"(?m)^name:\s*(.+)$", text)
            desc = re.search(r"(?m)^description:\s*(.+)$", text)
            if name and desc:
                found[name.group(1).strip()] = desc.group(1).strip()
    return found


def ask(prompt, model, config):
    env = {"PATH": os.environ.get("PATH", ""), "HOME": os.environ.get("HOME", ""), "CLAUDE_CONFIG_DIR": config}
    proc = subprocess.run(["claude", "-p", prompt, "--model", model, "--tools", "", "--restricted",
                           "--strict-mcp-config"], capture_output=True, text=True, timeout=180, env=env)
    return proc.stdout.strip().strip("`*. ").split()[0].lower() if proc.stdout.strip() else "error"


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("queries")
    parser.add_argument("--plugins", nargs="+", required=True)
    parser.add_argument("--runs", type=int, default=3)
    parser.add_argument("--model", default="haiku")
    parser.add_argument("--out")
    args = parser.parse_args()
    skills = descriptions(args.plugins)
    listing = "\n".join(f"- {n}: {d}" for n, d in skills.items())
    queries = json.loads(pathlib.Path(args.queries).read_text(encoding="utf-8"))
    rows, config = [], tempfile.mkdtemp(prefix="triggers-")
    for skill, sets in queries.items():
        if skill not in skills:
            raise SystemExit(f"{skill}: not in the given plugins")
        for kind in ("must", "must_not"):
            for query in sets.get(kind, []):
                picks = [ask(PROMPT.format(skills=listing, query=query), args.model, config)
                         for _ in range(args.runs)]
                hits = sum((p == skill) == (kind == "must") for p in picks)
                rows.append((skill, kind, query, hits, picks))
                print(f"{hits}/{args.runs} {skill} {kind}: {query} -> {', '.join(sorted(set(picks)))}", flush=True)
    lines = ["| Skill | Must start | Must not start |", "|-------|-----------|----------------|"]
    for skill in queries:
        cell = {}
        for kind in ("must", "must_not"):
            mine = [r for r in rows if r[0] == skill and r[1] == kind]
            cell[kind] = f"{sum(r[3] for r in mine)}/{len(mine) * args.runs}"
        lines.append(f"| {skill} | {cell['must']} | {cell['must_not']} |")
    lines += ["", "Misses:", ""]
    lines += [f"- {s} ({k}): \"{q}\" picked {', '.join(sorted(set(p)))}" for s, k, q, h, p in rows if h < args.runs]
    text = "\n".join(lines) + "\n"
    print(text)
    if args.out:
        pathlib.Path(args.out).write_text(text, encoding="utf-8")
    return 0


if __name__ == "__main__":
    sys.exit(main())
