#!/usr/bin/env python3
"""Check a skill folder against the Agent Skills spec and the build-skill writing rules.

    python3 skillcheck.py <skill-folder> [--json]

Output: path:line: severity RULE message, then one summary line.
Exit code 1 when there is at least one error.
"""
import argparse
import json
import pathlib
import re
import sys

SPEC_FIELDS = {"name", "description", "license", "compatibility", "metadata", "allowed-tools"}
CC_FIELDS = {
    "when_to_use", "disable-model-invocation", "user-invocable", "argument-hint", "arguments",
    "disallowed-tools", "model", "effort", "context", "agent", "background", "hooks", "paths", "shell",
}
NAME_RE = re.compile(r"^[a-z0-9]+(-[a-z0-9]+)*$")
LINK_RE = re.compile(r"\]\(([^)#\s]+)(?:#[^)]*)?\)")
CAPS_RE = re.compile(r"\b(MUST|NEVER|ALWAYS|CRITICAL|IMPORTANT)\b")


def parse_frontmatter(lines):
    """Return (dict, end_line_index) or (None, 0). Flat key: value only; enough for the checks."""
    if not lines or lines[0].strip() != "---":
        return None, 0
    data, key = {}, None
    for i in range(1, len(lines)):
        line = lines[i]
        if line.strip() == "---":
            return data, i
        m = re.match(r"^([A-Za-z_-][\w-]*):\s*(.*)$", line)
        if m:
            key = m.group(1)
            val = m.group(2).strip()
            if val in (">", ">-", "|", "|-", ""):
                val = ""
            data[key] = val.strip("\"'")
        elif key and line.startswith((" ", "\t")):
            data[key] = (data[key] + " " + line.strip()).strip()
    return None, 0


def check(folder: pathlib.Path):
    out = []

    def add(path, line, sev, rule, msg):
        out.append({"path": str(path), "line": line, "severity": sev, "rule": rule, "message": msg})

    skill_md = folder / "SKILL.md"
    if not skill_md.exists():
        add(folder, 0, "error", "SK-FILE", "SKILL.md is missing.")
        return out
    text = skill_md.read_text(encoding="utf-8")
    lines = text.splitlines()
    fm, end = parse_frontmatter(lines)
    if fm is None:
        add(skill_md, 1, "error", "SK-FM", "Frontmatter is missing or not closed. Line 1 must be '---'.")
        fm, end = {}, 0

    name = fm.get("name", "")
    if not name:
        add(skill_md, 1, "error", "SK-NAME", "name is missing.")
    else:
        if len(name) > 64 or not NAME_RE.match(name):
            add(skill_md, 1, "error", "SK-NAME", "name must be 1-64 chars: lowercase, digits, single hyphens.")
        if name != folder.name:
            add(skill_md, 1, "error", "SK-NAME", f"name '{name}' must match the folder name '{folder.name}'.")
        if "claude" in name or "anthropic" in name:
            add(skill_md, 1, "error", "SK-NAME", "name must not contain 'claude' or 'anthropic'.")

    desc = fm.get("description", "")
    if not desc:
        add(skill_md, 1, "error", "SK-DESC", "description is missing.")
    else:
        if len(desc) > 1024:
            add(skill_md, 1, "error", "SK-DESC", f"description has {len(desc)} chars. Maximum is 1024.")
        if len(desc) + len(fm.get("when_to_use", "")) > 1536:
            add(skill_md, 1, "warning", "SK-DESC", "description + when_to_use is over 1536 chars. The listing cuts it.")
        if re.search(r"<[^>]+>", desc):
            add(skill_md, 1, "error", "SK-DESC", "description must not contain angle brackets or tags.")
        if re.match(r"(?i)^(i|you|we|my|your)\b", desc):
            add(skill_md, 1, "warning", "SK-DESC3", "description must be third person, not first or second person.")
        if not re.search(r"(?i)\buse (it )?(when|for|if)\b|\bwhen the user\b|\btrigger", desc):
            add(skill_md, 1, "warning", "SK-TRIG", "description has no 'Use when ...' trigger.")

    for key in fm:
        if key in SPEC_FIELDS:
            continue
        if key in CC_FIELDS:
            add(skill_md, 1, "warning", "SK-PORT", f"'{key}' is a Claude Code field. Other runtimes and claude.ai may reject it.")
        else:
            add(skill_md, 1, "warning", "SK-FIELD", f"'{key}' is not a known frontmatter field.")

    body = lines[end + 1:] if end else lines
    if len(lines) > 500:
        add(skill_md, 501, "error", "SK-LEN", f"SKILL.md has {len(lines)} lines. Maximum is 500. Move detail to references/.")
    elif len(lines) > 300:
        add(skill_md, 301, "warning", "SK-LEN", f"SKILL.md has {len(lines)} lines. Aim for under 300.")

    for n, line in enumerate(lines, 1):
        if "—" in line:
            add(skill_md, n, "error", "HOUSE-DASH", "Em-dash found. Use a comma, colon, or full stop.")
        if CAPS_RE.search(line) and n > end:
            add(skill_md, n, "warning", "SK-CAPS", "ALL-CAPS emphasis. State the reason in plain words instead.")

    all_md = [skill_md] + sorted((folder / "references").glob("**/*.md")) if (folder / "references").is_dir() else [skill_md]
    for md in all_md:
        mt = md.read_text(encoding="utf-8")
        mlines = mt.splitlines()
        if md != skill_md:
            if len(mlines) > 100 and not re.search(r"(?im)^#{1,3}\s*(contents|table of contents)\b", mt):
                add(md, 1, "warning", "SK-TOC", f"{len(mlines)} lines and no 'Contents' heading.")
            for n, line in enumerate(mlines, 1):
                if "—" in line:
                    add(md, n, "error", "HOUSE-DASH", "Em-dash found.")
        for n, line in enumerate(mlines, 1):
            for target in LINK_RE.findall(line):
                if re.match(r"^[a-z]+:", target) or target.startswith("/"):
                    continue
                dest = (md.parent / target)
                if not dest.exists():
                    add(md, n, "error", "SK-LINK", f"Link target '{target}' does not exist.")
                elif md != skill_md and dest.suffix == ".md" and dest.resolve() != md.resolve():
                    add(md, n, "warning", "SK-DEEP", f"Reference links to '{target}'. Link all files from SKILL.md, one level deep.")
        if "\\" in "".join(re.findall(r"\]\(([^)]+)\)", mt)):
            add(md, 1, "warning", "SK-SLASH", "Backslash in a link path. Use forward slashes.")

    for m in re.finditer(r"scripts/([\w.-]+\.(?:py|sh|js))", text):
        if not (folder / "scripts" / m.group(1)).exists():
            line = text[:m.start()].count("\n") + 1
            add(skill_md, line, "error", "SK-SCRIPT", f"scripts/{m.group(1)} is named but does not exist.")
    if not (folder / "references" / "language.md").exists():
        add(folder, 0, "warning", "SK-LANG", "references/language.md is missing. Run scripts/sync_shared.py.")
    return out


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("folder")
    ap.add_argument("--json", action="store_true")
    args = ap.parse_args()
    folder = pathlib.Path(args.folder).resolve()
    res = check(folder)
    errs = sum(r["severity"] == "error" for r in res)
    warns = len(res) - errs
    if args.json:
        print(json.dumps({"errors": errs, "warnings": warns, "findings": res}, indent=2))
    else:
        for r in res:
            print(f"{r['path']}:{r['line']}: {r['severity']} {r['rule']} {r['message']}")
        print(f"{errs} errors, {warns} warnings")
    return 1 if errs else 0


if __name__ == "__main__":
    sys.exit(main())
