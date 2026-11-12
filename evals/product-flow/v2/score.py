#!/usr/bin/env python3
"""Score the 'check' assertions of the product-flow 2.0.0 evals.

    python3 evals/product-flow/v2/score.py <runs> [--format text|json]

An item file is a Markdown file that the run made or changed. A spec is each section under a
heading "Behaviour spec" in an item file. The checks use shared/specsync.py and the
backlogcheck.py of product-flow from this repository.
"""
from __future__ import annotations

import argparse
import importlib.util
import json
import pathlib
import re
import subprocess
import sys

HERE = pathlib.Path(__file__).resolve().parent
ROOT = HERE.parents[2]
BACKLOGCHECK = ROOT / "plugins" / "product-flow" / "shared" / "backlogcheck.py"
_spec = importlib.util.spec_from_file_location("specsync", ROOT / "shared" / "specsync.py")
specsync = importlib.util.module_from_spec(_spec)
_spec.loader.exec_module(specsync)
HEAD = re.compile(r"^(#{1,6})\s*(behaviou?r spec|gedragsspecificatie)\s*$", re.I | re.M)


def git(repo, *args) -> str:
    return subprocess.run(["git", *args], cwd=repo, capture_output=True, text=True).stdout


def item_files(repo: pathlib.Path) -> list:
    first = git(repo, "rev-list", "--max-parents=0", "HEAD").strip()
    names = set(git(repo, "diff", "--name-only", first, "--").split())
    names |= set(git(repo, "ls-files", "--others", "--exclude-standard").split())
    return sorted(repo / n for n in names if n.endswith(".md") and (repo / n).is_file()
                  and not n.startswith((".claude/", "evals/")))


def specs(path: pathlib.Path) -> list:
    """Return the text of each Behaviour spec section of the file."""
    text = path.read_text(encoding="utf-8", errors="replace")
    starts = [m.start() for m in HEAD.finditer(text)]
    return [text[s:(starts[i + 1] if i + 1 < len(starts) else len(text))] for i, s in enumerate(starts)]


def check(a: dict, repo: pathlib.Path):
    files = item_files(repo)
    kind = a["check"]
    if kind == "spec_ok":
        good, bad = 0, []
        for f in files:
            for text in specs(f):
                lines = specsync.read_spec(text, "md")
                errors = specsync.check(lines, a["kind"])[0]
                if errors:
                    bad.append(f"{f.name}: {errors[0]}"[:120])
                else:
                    good += 1
        return good >= a["min"] and not bad, f"{good} good, {len(bad)} bad {bad[:2]}"
    if kind == "item_count_max":
        n = sum(len(specs(f)) for f in files)
        return 0 < n <= a["value"], f"{n} specs"
    if kind == "backlogcheck_ok":
        items = [f for f in files if specs(f)]
        bad = []
        for f in items:
            p = subprocess.run([sys.executable, str(BACKLOGCHECK), str(f)], capture_output=True, text=True)
            if p.returncode != 0:
                bad.append(f"{f.name}: {p.stdout.strip().splitlines()[-1] if p.stdout.strip() else p.stderr[-80:]}")
        return bool(items) and not bad, "; ".join(bad)[:200] or f"{len(items)} files"
    if kind == "text_in_items":
        hit = [f.name for f in files if a["text"] in f.read_text(encoding="utf-8", errors="replace")]
        return bool(hit), ", ".join(hit) or "none"
    if kind == "glob_exists":
        hit = list(repo.glob(a["pattern"]))
        return bool(hit), ", ".join(str(h.relative_to(repo)) for h in hit) or "none"
    if kind == "no_glob":
        hit = list(repo.glob(a["pattern"]))
        return not hit, ", ".join(str(h.relative_to(repo)) for h in hit) or "none"
    if kind == "tracker_has":
        state = repo / ".tracker" / "state.json"
        items = json.loads(state.read_text(encoding="utf-8"))["items"] if state.is_file() else []
        hit = [i for i in items if a["title"] in i.get("title", "") and a["label"] in json.dumps(i)]
        return bool(hit), ", ".join(i["id"] for i in hit) or f"{len(items)} items, none match"
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
                ok, why = check(a, run / "repo")
                rows.append({"eval": ev["name"], "variant": variant, "run": run.name, "assertion": i,
                             "check": a["check"], "pass": ok, "detail": why})
    if args.format == "json":
        print(json.dumps(rows, indent=2))
    else:
        for r in rows:
            print(f"{r['eval']:<18} {r['variant']:<8} {r['run']} A{r['assertion']:<2} "
                  f"{'PASS' if r['pass'] else 'FAIL'} {r['check']}: {r['detail']}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
