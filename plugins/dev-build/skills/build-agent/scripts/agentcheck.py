#!/usr/bin/env python3
"""Check an agent definition file or folder.

    python3 agentcheck.py <file-or-folder> [--runtime claude-code|managed|openclaw] [--json]

claude-code: a Markdown file with frontmatter (subagent). Also used for plugin agents.
managed:     a Markdown file for `ant apply` (Claude Managed Agents).
openclaw:    a workspace folder (AGENTS.md, SOUL.md, USER.md, ...).
Output: path:line: severity RULE message, then a summary line. Exit 1 on errors.
"""
import argparse
import json
import pathlib
import re
import sys

CC_FIELDS = {
    "name", "description", "tools", "disallowedTools", "model", "permissionMode", "maxTurns", "skills",
    "mcpServers", "hooks", "memory", "background", "effort", "isolation", "color", "omitClaudeMd",
    "initialPrompt", "experimental",
}
PLUGIN_IGNORED = {"hooks", "mcpServers", "permissionMode", "initialPrompt"}
MODELS = {"sonnet", "opus", "haiku", "fable", "inherit"}
PERMISSION = {"default", "acceptEdits", "auto", "dontAsk", "bypassPermissions", "plan", "manual"}
EFFORT = {"low", "medium", "high", "xhigh", "max"}
TOOLS = {
    "Read", "Write", "Edit", "Glob", "Grep", "Bash", "WebFetch", "WebSearch", "NotebookEdit", "Agent",
    "Task", "TodoWrite", "SendMessage", "Skill",
}
MANAGED_FIELDS = {"name", "description", "model", "tools", "mcp_servers", "skills", "system"}
SNAKE = re.compile(r"^[a-z]+_[a-z_]+$")
CAPS = re.compile(r"\b(MUST|NEVER|ALWAYS|CRITICAL|IMPORTANT)\b")
OPENCLAW_LIMIT = {"USER.md": 4000}
OPENCLAW_DEFAULT_LIMIT = 20000


def split_frontmatter(text):
    lines = text.splitlines()
    if not lines or lines[0].strip() != "---":
        return None, text, 0
    fm, key = {}, None
    for i in range(1, len(lines)):
        line = lines[i]
        if line.strip() == "---":
            return fm, "\n".join(lines[i + 1:]), i + 1
        m = re.match(r"^([A-Za-z_][\w-]*):\s*(.*)$", line)
        if m:
            key = m.group(1)
            fm[key] = m.group(2).strip().strip("\"'")
        elif key and line.startswith((" ", "\t", "-")):
            fm[key] = (fm[key] + " " + line.strip()).strip()
    return None, text, 0


def tool_names(value):
    value = value.strip("[]")
    return [t.strip().strip("\"'") for t in re.split(r"[,\s]+", value) if t.strip().strip("\"'") and not t.startswith("type:")]


def check_md(path, runtime, out):
    def add(line, sev, rule, msg):
        out.append({"path": str(path), "line": line, "severity": sev, "rule": rule, "message": msg})

    text = path.read_text(encoding="utf-8")
    fm, body, body_start = split_frontmatter(text)
    if fm is None:
        add(1, "error", "AG-FM", "Frontmatter is missing, not closed, or '---' is not on line 1. Claude Code skips such a file.")
        fm, body, body_start = {}, text, 0
    name = fm.get("name", "")
    if not name:
        add(1, "error", "AG-NAME", "name is missing. Claude Code skips a file without name.")
    elif runtime == "claude-code":
        if len(name) > 256 or name.startswith("-") or ":" in name:
            add(1, "error", "AG-NAME", "name must be up to 256 chars, not start with '-', and not contain ':'.")
        if not re.match(r"^[a-z0-9]+(-[a-z0-9]+)*$", name):
            add(1, "warning", "AG-NAME", "Use lowercase letters, digits, and single hyphens in name.")
    if runtime == "claude-code":
        desc = fm.get("description", "")
        if not desc:
            add(1, "error", "AG-DESC", "description is missing. Claude Code skips a file without it.")
        else:
            if len(desc) > 600:
                add(1, "warning", "AG-DESC", f"description has {len(desc)} chars. Keep it short. All descriptions load into the parent context.")
            if not re.search(r"(?i)\buse (it |this )?(when|for|proactively|after|before)\b|\bwhen\b", desc):
                add(1, "warning", "AG-TRIG", "description has no 'Use when ...' trigger. The parent agent cannot decide when to call it.")
        known = CC_FIELDS
    else:
        known = MANAGED_FIELDS
    for key in fm:
        if key in known:
            if runtime == "claude-code" and "plugins" in path.parts and key in PLUGIN_IGNORED:
                add(1, "warning", "AG-PLUGIN", f"'{key}' is ignored for plugin agents.")
            continue
        if SNAKE.match(key) and runtime == "claude-code":
            add(1, "warning", "AG-FIELD", f"'{key}': field names are camelCase (for example disallowedTools). Claude Code ignores an unknown field without an error.")
        else:
            add(1, "warning", "AG-FIELD", f"'{key}' is not a known field. The runtime may ignore it without an error.")
    if runtime == "claude-code":
        for t in tool_names(fm.get("tools", "")) + tool_names(fm.get("disallowedTools", "")):
            base = t.split("(")[0]
            if base and base not in TOOLS and not base.startswith("mcp__"):
                add(1, "warning", "AG-TOOL", f"Tool '{t}' is not a known tool name. Check the spelling.")
        if "Skill" in tool_names(fm.get("tools", "")):
            add(1, "warning", "AG-TOOL", "To preload skills, use the 'skills' field, not the 'Skill' tool.")
        model = fm.get("model")
        if model and model not in MODELS and not model.startswith("claude-"):
            add(1, "warning", "AG-MODEL", f"model '{model}' is not an alias (sonnet, opus, haiku, fable, inherit) or a full ID.")
        if fm.get("permissionMode") and fm["permissionMode"] not in PERMISSION:
            add(1, "error", "AG-PERM", "permissionMode value is not valid.")
        if fm.get("effort") and fm["effort"] not in EFFORT:
            add(1, "error", "AG-EFFORT", "effort must be low, medium, high, xhigh, or max.")
        if fm.get("maxTurns") and not fm["maxTurns"].isdigit():
            add(1, "error", "AG-TURNS", "maxTurns must be a number.")
        if "tools" not in fm:
            add(1, "warning", "AG-TOOLS", "tools is not set. The agent inherits all tools. Set the least tools the job needs.")
    else:
        if "model" not in fm:
            add(1, "error", "AG-MODEL", "model is missing.")
        if "agent_toolset" not in fm.get("tools", "") and "tools" not in fm:
            add(1, "warning", "AG-TOOLS", "No tools set.")
    if not body.strip():
        add(body_start + 1, "error", "AG-BODY", "The system prompt (the body) is empty.")
    nlines = len(body.splitlines())
    if nlines > 200:
        add(body_start + 1, "warning", "AG-LEN", f"The system prompt has {nlines} lines. Aim for under 100. Move facts to a skill or a file.")
    for n, line in enumerate(text.splitlines(), 1):
        if "—" in line:
            add(n, "error", "HOUSE-DASH", "Em-dash found.")
        if n > body_start and CAPS.search(line):
            add(n, "warning", "AG-CAPS", "ALL-CAPS emphasis. Say the reason in plain words.")
    if body.strip() and not re.search(r"(?i)\b(report|return|reply|answer|output|respond)\b", body):
        add(body_start + 1, "warning", "AG-OUT", "The prompt does not say what the agent returns. The parent sees only the final message.")


def check_openclaw(folder, out):
    def add(path, sev, rule, msg):
        out.append({"path": str(path), "line": 0, "severity": sev, "rule": rule, "message": msg})

    if not folder.is_dir():
        add(folder, "error", "OC-DIR", "Give the workspace folder.")
        return
    for required in ("AGENTS.md", "SOUL.md"):
        if not (folder / required).exists():
            add(folder / required, "error", "OC-FILE", f"{required} is missing.")
    total = 0
    for f in sorted(folder.glob("*.md")):
        text = f.read_text(encoding="utf-8")
        total += len(text)
        limit = OPENCLAW_LIMIT.get(f.name, OPENCLAW_DEFAULT_LIMIT)
        if len(text) > limit:
            add(f, "error", "OC-SIZE", f"{len(text)} chars. The limit is {limit}. OpenClaw cuts the rest.")
        if "—" in text:
            add(f, "error", "HOUSE-DASH", "Em-dash found.")
    if total > 60000:
        add(folder, "error", "OC-SIZE", f"All bootstrap files together have {total} chars. The limit is 60000.")
    if (folder / "BOOTSTRAP.md").exists():
        add(folder / "BOOTSTRAP.md", "warning", "OC-BOOT", "BOOTSTRAP.md is for the first run only. Delete it after the setup.")
    for old in ("TOOLS.md", "HEARTBEAT.md"):
        if (folder / old).exists():
            add(folder / old, "warning", "OC-OLD", f"{old} is marked as retired in the docs. Check the current docs before you use it.")
    ag = folder / "AGENTS.md"
    if ag.exists() and not re.search(r"(?im)^#+\s.*(memory|tool)", ag.read_text(encoding="utf-8")):
        add(ag, "warning", "OC-MEM", "AGENTS.md has no section for memory use or tools notes.")


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("target")
    ap.add_argument("--runtime", choices=["claude-code", "managed", "openclaw"])
    ap.add_argument("--json", action="store_true")
    args = ap.parse_args()
    target = pathlib.Path(args.target).resolve()
    runtime = args.runtime or ("openclaw" if target.is_dir() else "claude-code")
    out = []
    if runtime == "openclaw":
        check_openclaw(target, out)
    else:
        files = sorted(target.glob("**/*.md")) if target.is_dir() else [target]
        for f in files:
            check_md(f, runtime, out)
    errs = sum(r["severity"] == "error" for r in out)
    warns = len(out) - errs
    if args.json:
        print(json.dumps({"errors": errs, "warnings": warns, "findings": out}, indent=2))
    else:
        for r in out:
            print(f"{r['path']}:{r['line']}: {r['severity']} {r['rule']} {r['message']}")
        print(f"{errs} errors, {warns} warnings")
    return 1 if errs else 0


if __name__ == "__main__":
    sys.exit(main())
