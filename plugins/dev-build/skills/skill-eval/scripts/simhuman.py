#!/usr/bin/env python3
"""Run one eval case with a simulated human, then grade it.

    python3 simhuman.py <case dir> --out <run dir> [--plugin <plugin dir>]
                        [--model M] [--human-model haiku] [--judge-model sonnet]
                        [--max-turns 8] [--max-cost-usd 3] [--scaffold]

The case folder has:
    request.md      the first message of the human
    human.md        the answer sheet. The simulated human answers only from it.
    graders/*.md    `llm` graders (judged here) and `regex` graders
    case.yaml       optional: context.scaffold_script, as in claude plugin eval
    agent.md        optional: frontmatter `tools:` (default Read Write Edit Glob Grep Skill)
                    and `allow:` (Bash patterns, for example "Bash(python3 *)")

The agent runs as `claude -p --restricted` in an empty work folder, with only
the plugin under test. Without --plugin it runs with no plugin (the baseline).
A turn of the agent ends when it stops. The human model writes the reply.
The run ends when the agent writes RUN COMPLETE, the human says "done", or
after --max-turns agent turns.

Writes to --out: cwd/ (the work folder), transcript.md, grades.json.
"""
import argparse
import json
import os
import pathlib
import re
import shutil
import subprocess
import sys
import tempfile
import uuid

DEFAULT_TOOLS = ["Read", "Write", "Edit", "Glob", "Grep", "Skill"]
TURN_TIMEOUT = 900  # seconds for one agent turn
END_AGENT = "RUN COMPLETE"
RUN_RULES = """You work in the current folder. A human gives you a task.
When you need an answer, a decision, or an approval from the human, end your turn with your message to the human.
The next message is the reply of the human. Never invent a reply of the human.
When the human says the work is done, end your turn with the line RUN COMPLETE."""
HUMAN_PROMPT = """You play a human who works with an AI agent. Answer only from the answer sheet below.
Keep each answer short. Give a fact only when the agent asks about its topic.
When the sheet does not cover a question, say: "Use your recommendation."
When the agent reports that the work is finished and asks for nothing, reply exactly: done

# Answer sheet

{sheet}

# Conversation so far

{transcript}

Write only your next reply to the agent."""
JUDGE_PROMPT = """You grade one test run of an AI agent. You see the conversation and the files it made.
You do not know which version of the agent made it. Grade each assertion yes or no.
Give one line of evidence: a quote of at most 15 words, or a file name.
Do not reward length.

# Conversation

{transcript}

# Files after the run

{files}

# Assertions

{assertions}

Reply with JSON only: {{"results": [{{"id": "<id>", "pass": true, "evidence": "..."}}]}}"""
MAX_FILE_CHARS = 6000  # characters of one file shown to the judge


def frontmatter(text: str):
    m = re.match(r"^---\n(.*?)\n---\n?(.*)$", text, re.S)
    if not m:
        return {}, text
    meta = {}
    for line in m.group(1).splitlines():
        if ":" in line:
            key, value = line.split(":", 1)
            meta[key.strip()] = value.strip().strip("'\"")
    return meta, m.group(2).strip()


def claude(args, prompt, cwd, timeout, config):
    """Run claude -p in a clean environment: no settings, skills, plugins, or project text of this computer."""
    env = {"PATH": os.environ.get("PATH", ""), "HOME": os.environ.get("HOME", ""),
           "CLAUDE_CONFIG_DIR": str(config)}
    proc = subprocess.run(["claude", "-p", prompt, "--output-format", "json", *args], cwd=cwd,
                          capture_output=True, text=True, timeout=timeout, env=env)
    try:
        data = json.loads(proc.stdout)
    except json.JSONDecodeError:
        return {"result": "", "error": (proc.stderr or proc.stdout)[-500:], "total_cost_usd": 0}
    return data


def file_listing(cwd: pathlib.Path, focus: set):
    parts = []
    for path in sorted(p for p in cwd.rglob("*") if p.is_file() and ".git" not in p.parts):
        rel = path.relative_to(cwd).as_posix()
        if rel in focus or not focus:
            try:
                body = path.read_text(encoding="utf-8")[:MAX_FILE_CHARS]
            except UnicodeDecodeError:
                body = "(binary file)"
            parts.append(f"## {rel}\n\n{body}")
        else:
            parts.append(f"## {rel}\n\n(not shown)")
    return "\n\n".join(parts) or "(no files)"


def main():
    p = argparse.ArgumentParser()
    p.add_argument("case")
    p.add_argument("--out", required=True)
    p.add_argument("--plugin")
    p.add_argument("--model")
    p.add_argument("--human-model", default="haiku")
    p.add_argument("--judge-model", default="sonnet")
    p.add_argument("--max-turns", type=int, default=8)
    p.add_argument("--max-cost-usd", type=float, default=3.0)
    p.add_argument("--scaffold", action="store_true")
    a = p.parse_args()

    case, out = pathlib.Path(a.case).resolve(), pathlib.Path(a.out).resolve()
    out.mkdir(parents=True, exist_ok=True)
    temp = pathlib.Path(tempfile.mkdtemp(prefix="simhuman-"))
    work, config = temp / "cwd", temp / "config"  # one config folder per run, so --resume finds the session
    work.mkdir()
    config.mkdir()
    yaml = (case / "case.yaml").read_text(encoding="utf-8") if (case / "case.yaml").is_file() else ""
    script = re.search(r"scaffold_script:\s*(\S+)", yaml)
    if script and a.scaffold:
        subprocess.run(["bash", str(case / script.group(1))], cwd=work, check=True, timeout=120)
    meta, _ = frontmatter((case / "agent.md").read_text(encoding="utf-8")) if (case / "agent.md").is_file() else ({}, "")
    tools = meta.get("tools", "").split() or DEFAULT_TOOLS
    allow = [x.strip() for x in meta.get("allow", "").split(",") if x.strip()]
    sheet = (case / "human.md").read_text(encoding="utf-8")
    message = (case / "request.md").read_text(encoding="utf-8").strip()

    session = str(uuid.uuid4())
    base = ["--restricted", "--strict-mcp-config", "--tools", ",".join(tools + (["Bash"] if allow else [])),
            "--permission-mode", "dontAsk", "--append-system-prompt", RUN_RULES]
    base += ["--allowedTools", *tools, *allow]  # dontAsk refuses each tool that is not in this list
    if a.plugin:  # --restricted confines file tools to the work folder; the skill must read its own files
        base += ["--plugin-dir", a.plugin, "--add-dir", a.plugin]
    if a.model:
        base += ["--model", a.model]
    transcript, cost, turns = [], 0.0, 0
    while turns < a.max_turns and cost < a.max_cost_usd:
        transcript.append(("Human", message))
        session_args = ["--session-id", session] if turns == 0 else ["--resume", session]
        reply = claude(base + session_args, message, work, TURN_TIMEOUT, config)
        cost += reply.get("total_cost_usd") or 0
        turns += 1
        text = (reply.get("result") or reply.get("error") or "").strip()
        transcript.append(("Agent", text))
        if END_AGENT in text:
            break
        convo = "\n\n".join(f"## {who}\n\n{said}" for who, said in transcript)
        human = claude(["--model", a.human_model, "--tools", "", "--restricted", "--strict-mcp-config"],
                       HUMAN_PROMPT.format(sheet=sheet, transcript=convo), work, 300, config)
        cost += human.get("total_cost_usd") or 0
        message = (human.get("result") or "Use your recommendation.").strip()
        if message.lower().strip(" .!") == "done":
            transcript.append(("Human", "done"))
            break

    convo = "\n\n".join(f"## {who}\n\n{said}" for who, said in transcript)
    (out / "transcript.md").write_text(convo + "\n", encoding="utf-8")
    shutil.copytree(work, out / "cwd", dirs_exist_ok=True, symlinks=True)

    grades, llm = [], []
    for g in sorted((case / "graders").glob("*.md")):
        meta, body = frontmatter(g.read_text(encoding="utf-8"))
        gid = g.stem
        if meta.get("type") == "regex":
            target = meta.get("target", "last_message")
            fm = re.search(r"path:\s*([^\s}]+)", target)
            if fm:
                path = out / "cwd" / fm.group(1)
                text = path.read_text(encoding="utf-8", errors="replace") if path.is_file() else ""
            elif target == "trace":
                text = convo
            else:
                text = transcript[-1][1] if transcript else ""
            flags = re.I if "i" in meta.get("flags", "") else 0
            found = len(re.findall(meta.get("pattern", ""), text, flags))
            want = meta.get("match", "contains")
            ok = found == 0 if want == "not_contains" else (
                found == int(want.split(":")[1]) if want.startswith("count:") else found > 0)
            grades.append({"id": gid, "pass": ok, "evidence": f"{found} match(es)"})
        else:
            llm.append((gid, body, meta.get("focus", "")))
    if llm:
        focus = {f.split("path:")[1].strip(" }") for _, _, f in llm if "path:" in f}
        assertions = "\n".join(f"- {gid}: {body}" for gid, body, _ in llm)
        judge = claude(["--model", a.judge_model, "--tools", "", "--restricted", "--strict-mcp-config"],
                       JUDGE_PROMPT.format(transcript=convo, files=file_listing(out / "cwd", focus),
                                           assertions=assertions), out, 600, config)
        raw = judge.get("result") or ""
        m = re.search(r"\{.*\}", raw, re.S)
        try:
            results = {r["id"]: r for r in json.loads(m.group(0))["results"]} if m else {}
        except (json.JSONDecodeError, KeyError, TypeError):
            results = {}
        for gid, _, _ in llm:
            r = results.get(gid)
            grades.append({"id": gid, "pass": bool(r and r.get("pass")),
                           "evidence": (r or {}).get("evidence", "no grade from the judge")[:200]})
    grades.append({"id": "turns", "pass": True, "evidence": f"{turns} agent turn(s), ${cost:.2f}"})
    (out / "grades.json").write_text(json.dumps(grades, indent=2), encoding="utf-8")
    shutil.rmtree(temp, ignore_errors=True)
    print(f"{case.name}: {turns} turn(s), ${cost:.2f}, {sum(g['pass'] for g in grades)}/{len(grades)} pass")
    return 0


if __name__ == "__main__":
    sys.exit(main())
