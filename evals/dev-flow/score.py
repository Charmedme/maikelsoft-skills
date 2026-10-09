#!/usr/bin/env python3
"""Score the 'check' assertions of evals.json for each run of plan-and-build.

Layout of a runs folder:
    <runs>/<eval name>/<variant>/<run number>/repo/      the git repository
    <runs>/<eval name>/<variant>/<run number>/transcript.md

Usage:
    python3 evals/dev-flow/score.py <runs>

Prints one line for each run and a summary for each eval and variant.
Writes <runs>/scores.json. The 'judge' assertions need a grader agent.
"""
import argparse
import json
import pathlib
import re
import subprocess
import sys
from collections import defaultdict

HERE = pathlib.Path(__file__).resolve().parent
ROOT = HERE.parents[1]
sys.path.insert(0, str(ROOT / "shared"))
import doccheck  # noqa: E402

WORKCHECK = ROOT / "plugins/dev-flow/skills/plan-and-build/scripts/workcheck.py"
TEST_PATTERNS = (re.compile(r"^\s*def test_\w+", re.M), re.compile(r"^\s*(?:test|it)\(\s*[\"'`]", re.M))
SKIP_DIRS = {".git", "node_modules"}


def git(repo, *args):
    return subprocess.run(["git", "-C", str(repo), *args], capture_output=True, text=True).stdout


def files(repo):
    return [p for p in repo.rglob("*") if p.is_file() and not SKIP_DIRS & set(p.relative_to(repo).parts)]


def count_tests(text_files):
    return sum(len(p.findall(t)) for t in text_files for p in TEST_PATTERNS)


def test_files(repo):
    return [p.read_text(encoding="utf-8", errors="ignore") for p in files(repo)
            if re.search(r"(^|/)(tests?/|test_)|\.test\.[jt]s$", str(p.relative_to(repo)))
            and "prototype" not in str(p.relative_to(repo)).lower()]


def fixture_tests(ev):
    fixture = HERE / "fixtures" / ev["fixture"]
    return count_tests(test_files(fixture))


def new_markdown(repo):
    first = git(repo, "rev-list", "--max-parents=0", "HEAD").split()[0]
    changed = set(git(repo, "diff", "--name-only", first, "HEAD").split())
    changed |= set(git(repo, "ls-files", "--others", "--exclude-standard").split())
    return [repo / f for f in sorted(changed)
            if f.endswith(".md") and "prototype" not in f.lower() and (repo / f).is_file()
            and pathlib.Path(f).name not in ("README.md", "AGENTS.md", "CHANGELOG.md")]


def find(repo, pattern):
    return [p for p in files(repo) if re.search(pattern, str(p.relative_to(repo)), re.I)]


def score_run(ev, run, variant):
    repo = run / "repo"
    results = []
    md = new_markdown(repo)
    specs = find(repo, r"(^|/)(specs/[^/]+\.md|spec\.md)$")
    tickets = find(repo, r"(^|/)(tickets|issues)/[^/]+\.md$")
    plans = find(repo, r"(^|/)plan\.md$")
    for a in ev["assertions"]:
        if "check" not in a or a.get("variant", variant) != variant:
            continue
        kind, value = a["check"], a.get("value")
        if kind == "tests_pass":
            r = subprocess.run(ev["test_command"], shell=True, cwd=repo, capture_output=True, text=True, timeout=300)
            ok, got = r.returncode == 0, r.returncode
        elif kind == "tests_added_min":
            got = count_tests(test_files(repo)) - fixture_tests(ev)
            ok = got >= value
        elif kind == "commits_min":
            got = int(git(repo, "rev-list", "--count", "HEAD").strip() or 0) - 1
            ok = got >= value
        elif kind == "commit_format":
            subjects = git(repo, "log", "--format=%s", "HEAD", "--not", git(repo, "rev-list", "--max-parents=0", "HEAD").strip()).splitlines()
            bad = [s for s in subjects if not re.match(value, s)]
            ok, got = bool(subjects) and not bad, bad[:3]
        elif kind == "branch_format":
            got = git(repo, "branch", "--show-current").strip()
            ok = bool(re.match(value, got))
        elif kind == "spec_exists":
            ok, got = bool(specs), len(specs)
        elif kind == "tickets_min":
            ok, got = len(tickets) >= value, len(tickets)
        elif kind == "tickets_max":
            ok, got = 0 < len(tickets) <= value, len(tickets)
        elif kind == "questionnaire_exists":
            got = len(find(repo, r"questionnaire"))
            ok = got > 0
        elif kind == "prototype_in_history":
            in_tree = bool(find(repo, r"prototype"))
            in_log = bool(git(repo, "log", "--all", "--format=%H", "--", "*prototype*", "**/prototype/**").strip())
            ok, got = in_tree or in_log, {"tree": in_tree, "history": in_log}
        elif kind == "doc_errors_max":
            errors = 0
            for p in md:
                findings, _ = doccheck.check(p.read_text(encoding="utf-8"), str(p), "auto", "standard")
                errors += sum(f.severity == "error" for f in findings)
            ok, got = errors <= value, errors
        elif kind == "workcheck_errors_max":
            if not plans:
                ok, got = False, "no plan.md"
            else:
                r = subprocess.run([sys.executable, str(WORKCHECK), str(plans[0].parent), "--format", "json"],
                                   capture_output=True, text=True)
                got = json.loads(r.stdout)["errors"] if r.stdout.strip().startswith("{") else r.stderr.strip()
                ok = isinstance(got, int) and got <= value
        else:
            raise SystemExit(f"Unknown check: {kind}")
        results.append({"check": kind, "ok": ok, "got": got})
    return results


def main():
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("runs", type=pathlib.Path)
    args = parser.parse_args()
    evals = {e["name"]: e for e in json.loads((HERE / "evals.json").read_text())["evals"]}

    scores = {}
    summary = defaultdict(lambda: [0, 0])
    for run in sorted(args.runs.glob("*/*/*")):
        if not (run / "repo").is_dir():
            continue
        name, variant, number = run.relative_to(args.runs).parts
        results = score_run(evals[name], run, variant)
        scores[f"{name}/{variant}/{number}"] = results
        passed = sum(r["ok"] for r in results)
        summary[(name, variant)][0] += passed
        summary[(name, variant)][1] += len(results)
        failed = ", ".join(f"{r['check']}={r['got']}" for r in results if not r["ok"])
        print(f"{name}/{variant}/{number}: {passed}/{len(results)}" + (f" | failed: {failed}" if failed else ""))
    print()
    for (name, variant), (p, t) in sorted(summary.items()):
        print(f"{name} {variant}: {p}/{t}")
    (args.runs / "scores.json").write_text(json.dumps(scores, indent=2, default=str), encoding="utf-8")


if __name__ == "__main__":
    main()
