#!/usr/bin/env python3
"""Collect the facts for a plugin audit. The script reads; it changes no file.

    python3 auditfacts.py <plugin dir> [--json]

It runs:
- skillcheck.py on each skill (from the build-skill skill next to this skill)
- doccheck.py on each Markdown file of the plugin
- claude plugin validate --strict, and claude plugin details (token cost)
- a read of the newest eval results: each assertion below full marks

The output is Markdown with one section per source, or JSON with --json.
A tool that is not there gives a "NOT RUN" line, not an error.
"""
import argparse
import json
import pathlib
import re
import shutil
import subprocess
import sys

HERE = pathlib.Path(__file__).resolve().parent
SKILLS = HERE.parents[1]  # the skills folder of this plugin
TIMEOUT = 300  # seconds for one external command


def run(cmd, cwd=None):
    try:
        proc = subprocess.run(cmd, capture_output=True, text=True, timeout=TIMEOUT, cwd=cwd)
        return proc.returncode, (proc.stdout + proc.stderr).strip()
    except FileNotFoundError:
        return None, f"NOT RUN: {cmd[0]} is not installed"
    except subprocess.TimeoutExpired:
        return None, f"NOT RUN: {' '.join(cmd[:3])} took more than {TIMEOUT} s"


def find_tool(name: str, plugin: pathlib.Path):
    for base in (SKILLS / "build-skill" / "scripts", HERE, plugin):
        hits = sorted(base.rglob(name)) if base.is_dir() else []
        if hits:
            return hits[0]
    return None


def skill_checks(plugin):
    tool = find_tool("skillcheck.py", plugin)
    results = []
    for skill in sorted((plugin / "skills").glob("*/SKILL.md")):
        if not tool:
            results.append({"skill": skill.parent.name, "errors": None, "warnings": None,
                            "lines": ["NOT RUN: skillcheck.py not found"]})
            continue
        code, out = run([sys.executable, "-I", str(tool), "--json", str(skill.parent)])
        try:
            report = json.loads(out)  # {"errors": n, "warnings": n, "findings": [...]}
        except json.JSONDecodeError:
            report = {"errors": None, "warnings": None, "findings": [{"severity": "error", "message": out[-300:]}]}
        findings = report.get("findings", [])
        results.append({"skill": skill.parent.name,
                        "errors": report.get("errors"),
                        "warnings": report.get("warnings"),
                        "lines": [f"{f.get('severity', '').upper()} {f.get('rule', '')} {f.get('message', '')}"
                                  for f in findings]})
    return results


def doc_checks(plugin):
    tool = find_tool("doccheck.py", plugin)
    if not tool:
        return {"files": 0, "errors": None, "lines": ["NOT RUN: doccheck.py not found"]}
    files = [p for p in sorted(plugin.rglob("*.md"))
             if "/references/" not in p.as_posix() and "/evals/results" not in p.as_posix()]
    errors, lines = 0, []
    for f in files:
        _, out = run([sys.executable, "-I", str(tool), "--level", "standard", str(f)])
        for line in out.splitlines():
            if " ERROR " in line:
                errors += 1
                lines.append(line.replace(str(plugin) + "/", ""))
    return {"files": len(files), "errors": errors, "lines": lines}


def validate(plugin):
    code, out = run(["claude", "plugin", "validate", "--strict", str(plugin)])
    return {"passed": code == 0, "lines": out.splitlines()[-8:]}


def details(plugin):
    name = json.loads((plugin / ".claude-plugin" / "plugin.json").read_text(encoding="utf-8"))["name"]
    code, out = run(["claude", "--plugin-dir", str(plugin), "plugin", "details", name])
    rows = re.findall(r"^\s{2}([\w-]+)\s+~([\d.]+k?)\s+~([\d.]+k?)\s*$", out, re.M)
    always = re.search(r"Always-on:\s+~([\d.,]+k?)", out)
    return {"always_on": always.group(1) if always else None,
            "components": [{"name": n, "always_on": a, "on_invoke": i} for n, a, i in rows],
            "lines": [] if rows else out.splitlines()[-5:]}


def eval_results(plugin):
    """Newest aggregate-result.json of plugin eval, plus the newest results-*.md files."""
    repo_code, repo_root = run(["git", "rev-parse", "--show-toplevel"], cwd=plugin)
    found = {"native": None, "below_full": [], "reports": []}
    aggregates = sorted(plugin.glob("evals/results/*/aggregate-result.json")) + \
        sorted(plugin.glob("evals/results/*/native-*.json"))
    if aggregates:
        newest = aggregates[-1]
        data = json.loads(newest.read_text(encoding="utf-8"))
        found["native"] = newest.relative_to(plugin).as_posix()
        for case in data.get("cases", []):
            for arm, runs in case.get("arms", {}).items():
                if arm != "with":
                    continue
                names = {g["name"] for r in runs for g in r.get("graders", [])}
                for name in sorted(names):
                    passed = sum(1 for r in runs for g in r.get("graders", []) if g["name"] == name and g.get("passed"))
                    total = sum(1 for r in runs for g in r.get("graders", []) if g["name"] == name)
                    if passed < total:
                        found["below_full"].append(f"{case['name']} / {name}: {passed}/{total}")
    reports = list(plugin.glob("evals/results-*.md"))
    if repo_code == 0:
        reports += list(pathlib.Path(repo_root).glob(f"evals/{plugin.name}/results-*.md"))
    found["reports"] = [p.as_posix() for p in sorted(reports, key=lambda p: p.name)[-3:]]
    return found


def markdown(f):
    out = [f"# Audit facts: {f['plugin']} {f['version']}", ""]
    out += ["## skillcheck.py", "", "| Skill | Errors | Warnings |", "|-------|--------|----------|"]
    out += [f"| {s['skill']} | {s['errors']} | {s['warnings']} |" for s in f["skills"]]
    for s in f["skills"]:
        out += [f"- {s['skill']}: {line}" for line in s["lines"] if not line.startswith("INFO")]
    d = f["docs"]
    out += ["", "## doccheck.py", "", f"{d['files']} file(s), {d['errors']} error(s)."]
    out += [f"- {line}" for line in d["lines"][:40]]
    v = f["validate"]
    out += ["", "## claude plugin validate --strict", "", "Passed." if v["passed"] else "Failed."]
    out += [f"- {line}" for line in v["lines"] if line.strip() and not v["passed"]]
    t = f["details"]
    out += ["", "## Token cost (claude plugin details)", "", f"Always-on: ~{t['always_on']} tokens.", "",
            "| Component | Always-on | On invoke |", "|-----------|-----------|-----------|"]
    out += [f"| {c['name']} | ~{c['always_on']} | ~{c['on_invoke']} |" for c in t["components"]]
    out += [f"- {line}" for line in t["lines"]]
    e = f["evals"]
    out += ["", "## Eval results", "", f"Newest plugin eval result: {e['native'] or 'none'}."]
    out += [f"- Below full marks: {line}" for line in e["below_full"]]
    out += [f"- Report: {r}" for r in e["reports"]] or ["- No results report found."]
    return "\n".join(out) + "\n"


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("plugin")
    parser.add_argument("--json", action="store_true")
    args = parser.parse_args()
    plugin = pathlib.Path(args.plugin).resolve()
    manifest = plugin / ".claude-plugin" / "plugin.json"
    if not manifest.is_file():
        raise SystemExit(f"{plugin}: no .claude-plugin/plugin.json. Give the folder of one plugin.")
    meta = json.loads(manifest.read_text(encoding="utf-8"))
    facts = {"plugin": meta.get("name"), "version": meta.get("version"),
             "skills": skill_checks(plugin), "docs": doc_checks(plugin), "validate": validate(plugin),
             "details": details(plugin) if shutil.which("claude") else {"always_on": None, "components": [],
                                                                         "lines": ["NOT RUN: claude is not installed"]},
             "evals": eval_results(plugin)}
    print(json.dumps(facts, indent=2) if args.json else markdown(facts))
    return 0


if __name__ == "__main__":
    sys.exit(main())
