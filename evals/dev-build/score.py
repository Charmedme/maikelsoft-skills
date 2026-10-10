#!/usr/bin/env python3
"""Score the 'check' assertions of evals.json.

Layout: <runs>/<eval name>/<variant>/<run number>/<output file>
Usage:  python3 evals/dev-build/score.py <runs> [evals-agent.json]
Writes <runs>/scores.json. The 'judge' assertions need a grader agent (grader.md).
"""
import hashlib
import shutil
import tempfile
import json
import pathlib
import re
import sys

HERE = pathlib.Path(__file__).resolve().parent
ROOT = HERE.parents[1]
sys.path.insert(0, str(ROOT / "plugins/dev-build/skills/build-skill/scripts"))
sys.path.insert(0, str(ROOT / "plugins/dev-build/skills/build-agent/scripts"))
import skillcheck  # noqa: E402
import agentcheck  # noqa: E402


def score(ev, run_dir):
    out = run_dir / ev["output"]
    results = []
    for a in ev["assertions"]:
        if "check" not in a:
            continue
        kind, value = a["check"], a["value"]
        folder = out.parent
        m = re.search(r"(?m)^name:\s*(\S+)", out.read_text(encoding="utf-8")) if out.is_file() else None
        tmp = None
        if m and folder.name != m.group(1):  # check a copy named after the skill, so the folder name is not an error
            tmp = tempfile.TemporaryDirectory()
            copy = pathlib.Path(tmp.name) / m.group(1).strip("\"'")
            shutil.copytree(folder, copy)
            folder = copy
        if not out.is_file():
            results.append({"id": a["id"], "pass": False, "got": "output missing"})
            continue
        text = out.read_text(encoding="utf-8")
        if kind == "skillcheck_errors_max":
            got = sum(r["severity"] == "error" for r in skillcheck.check(folder))
            ok = got <= value
        elif kind == "agentcheck_errors_max":
            out_list = []
            if ev.get("runtime") == "openclaw":
                agentcheck.check_openclaw(folder, out_list)
            else:
                agentcheck.check_md(out, ev.get("runtime", "claude-code"), out_list)
            got = sum(r["severity"] == "error" for r in out_list)
            ok = got <= value
        elif kind == "tools_excludes":
            m2 = re.search(r"(?m)^tools:\s*(.*)$", text)
            got = [x for x in value if m2 and re.search(r"\b" + x + r"\b", m2.group(1))]
            ok = bool(m2) and not got
        elif kind == "has_field":
            got = bool(re.search(r"(?m)^" + value + r":", text))
            ok = got
        elif kind == "line_max":
            got = len(text.splitlines())
            ok = got <= value
        elif kind == "not_contains":
            got = len(re.findall(value, text))
            ok = got == 0
        elif kind == "exists":
            got = [f for f in value if not (folder / f).is_file()]
            ok = not got
        elif kind == "unchanged_from_fixture":
            fix = HERE / ev["fixture"]
            if fix.is_dir():
                fix = fix / "SKILL.md"
            got = hashlib.sha256(text.encode()).hexdigest()[:8]
            ok = text == fix.read_text(encoding="utf-8")
        else:
            raise SystemExit(f"unknown check {kind}")
        results.append({"id": a["id"], "pass": ok, "got": got})
    return results


def main():
    runs = pathlib.Path(sys.argv[1])
    evals_file = sys.argv[2] if len(sys.argv) > 2 else "evals.json"
    evals = {e["name"]: e for e in json.loads((HERE / evals_file).read_text())["evals"]}
    all_scores = {}
    for name, ev in evals.items():
        for variant_dir in sorted((runs / name).glob("*")) if (runs / name).is_dir() else []:
            for run_dir in sorted(variant_dir.glob("*")):
                res = score(ev, run_dir)
                all_scores[f"{name}/{variant_dir.name}/{run_dir.name}"] = res
                print(f"{name}/{variant_dir.name}/{run_dir.name}: " + " ".join(f"{r['id']}={'Y' if r['pass'] else 'N'}" for r in res))
    (runs / "scores.json").write_text(json.dumps(all_scores, indent=2))


if __name__ == "__main__":
    main()
