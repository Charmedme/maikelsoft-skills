#!/usr/bin/env python3
"""Score the 'check' assertions of the dev-setup evals.

    python3 evals/dev-setup/score.py <runs> [--format text|json]

Reads each <runs>/<eval>/<variant>/<n>/ that run.py made, and prints one row for each check.
A check with a 'variant' field counts only for that variant.
"""
from __future__ import annotations

import argparse
import json
import pathlib
import subprocess
import sys

HERE = pathlib.Path(__file__).resolve().parent


def roles(run: pathlib.Path) -> dict:
    f = run / "data" / "roles.json"
    try:
        return json.loads(f.read_text(encoding="utf-8")) if f.is_file() else {}
    except ValueError:
        return {}


def entry(data: dict, a: dict):
    if a["scope"] == "global":
        return data.get("global")
    return (data.get("projects") or {}).get(a["key"])


def git(repo: pathlib.Path, *args) -> str:
    return subprocess.run(["git", *args], cwd=repo, capture_output=True, text=True).stdout


def check(a: dict, run: pathlib.Path):
    repo = run / "repo"
    kind = a["check"]
    if kind == "role":
        e = entry(roles(run), a) or {}
        return e.get("role") == a["role"], f"role {e.get('role')!r}"
    if kind == "no_role":
        data = roles(run)
        e = entry(data, a) if a["scope"] == "global" or "key" in a else (data.get("projects") or None)
        return not e, f"entry {e!r}"
    if kind == "file_exists":
        return (repo / a["path"]).is_file(), a["path"]
    if kind == "file_contains":
        f = repo / a["path"]
        return f.is_file() and a["text"] in f.read_text(encoding="utf-8", errors="replace"), a["path"]
    if kind == "any_file_contains":
        hits = [p for p in a["paths"] if (repo / p).is_file()
                and a["text"] in (repo / p).read_text(encoding="utf-8", errors="replace")]
        if not hits:  # the agent can pick another name: search the text in each new file
            new = git(repo, "ls-files", "--others", "--exclude-standard").splitlines()
            hits = [p for p in new if a["text"] in (repo / p).read_text(encoding="utf-8", errors="replace")
                    and not p.endswith(".py")]
        return bool(hits), ", ".join(hits) or "none"
    if kind == "unchanged":
        changed = [line[3:] for line in git(repo, "status", "--porcelain").splitlines() if not line.startswith("??")]
        changed = [p for p in changed if p not in a.get("except", [])]
        first = git(repo, "rev-list", "--max-parents=0", "HEAD").strip()
        committed = [p for p in git(repo, "diff", "--name-only", "--diff-filter=MDR", first, "HEAD").split()
                     if p not in a.get("except", [])]
        bad = sorted(set(changed + committed))
        return not bad, ", ".join(bad) or "none"
    if kind == "no_new_files":
        first = git(repo, "rev-list", "--max-parents=0", "HEAD").strip()
        new = git(repo, "ls-files", "--others", "--exclude-standard").split()
        new += git(repo, "diff", "--name-only", "--diff-filter=A", first, "HEAD").split()
        return not new, ", ".join(new) or "none"
    if kind == "gitignore_has":
        f = repo / ".gitignore"
        lines = {line.strip() for line in f.read_text(encoding="utf-8").splitlines()} if f.is_file() else set()
        miss = [line for line in a["lines"] if line not in lines and "/" + line not in lines]
        return not miss, "missing " + ", ".join(miss) if miss else "all"
    if kind == "json_valid":
        f = repo / a["path"]
        try:
            json.loads(f.read_text(encoding="utf-8"))
            return True, a["path"]
        except (OSError, ValueError) as e:
            return False, str(e)[:80]
    if kind == "dirs_exist":
        miss = [p for p in a["paths"] if not (repo / p).is_dir()]
        return not miss, "missing " + ", ".join(miss) if miss else "all"
    if kind == "no_path":
        return not (repo / a["path"]).exists(), a["path"]
    if kind == "file_mentions":
        f = repo / a["path"]
        text = f.read_text(encoding="utf-8", errors="replace") if f.is_file() else ""
        miss = [w for w in a["words"] if w not in text]
        return not miss, "missing " + ", ".join(miss) if miss else "all"
    raise SystemExit(f"score.py: unknown check {kind}")


def main(argv=None) -> int:
    ap = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    ap.add_argument("runs", type=pathlib.Path)
    ap.add_argument("--format", choices=("text", "json"), default="text")
    args = ap.parse_args(argv)
    cases = json.loads((HERE / "evals.json").read_text(encoding="utf-8"))["evals"]
    rows = []
    for ev in cases:
        for run in sorted((args.runs / ev["name"]).glob("*/*")):
            if not (run / "repo").is_dir():
                continue
            variant = run.parent.name
            for i, a in enumerate(ev["assertions"]):
                if "check" not in a or a.get("variant", variant) != variant:
                    continue
                ok, why = check(a, run)
                rows.append({"eval": ev["name"], "variant": variant, "run": run.name, "assertion": i,
                             "check": a["check"], "pass": ok, "detail": why})
    if args.format == "json":
        print(json.dumps(rows, indent=2))
    else:
        for r in rows:
            print(f"{r['eval']:<11} {r['variant']:<8} {r['run']} A{r['assertion']:<2} {'PASS' if r['pass'] else 'FAIL'} "
                  f"{r['check']}: {r['detail']}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
