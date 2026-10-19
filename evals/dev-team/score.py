#!/usr/bin/env python3
"""Score the 'check' assertions of evals.json for each run of the dev-team evals.

Layout of a runs folder:
    <runs>/<eval name>/<arm>/<run number>/repo/          the repository after the run
    <runs>/<eval name>/<arm>/<run number>/stream.jsonl   the stream-json output of claude -p
    <runs>/<eval name>/<arm>/<run number>/final.md       the final message (written by this script when missing)

Usage:
    python3 evals/dev-team/score.py <runs>
    python3 evals/dev-team/score.py --run <run folder> --eval <eval name> --arm team|baseline [--only D5 ...]

The stack checks (build, tests, packages, probe tests) come from evals/dev-stack/score.py.
The team checks are below. The 'judge' assertions need a blind grader (evals/dev-stack/grader.md).

Needs Python 3.9 or later, git, the dotnet CLI, and Docker for the integration tests.
"""
import argparse
import importlib.util
import json
import pathlib
import subprocess
import sys
import tempfile

HERE = pathlib.Path(__file__).resolve().parent
ROOT = HERE.parents[1]
SHARED = ROOT / "plugins" / "dev-team" / "shared"
STACK_RULES = sorted(str(p) for p in (ROOT / "plugins" / "dev-stack" / "skills").glob("*/rules.json"))

spec = importlib.util.spec_from_file_location("stackscore", HERE.parent / "dev-stack" / "score.py")
ds = importlib.util.module_from_spec(spec)
spec.loader.exec_module(ds)
ds.PROBES = HERE / "probes"

STACK_CHECKS = {"build_warnaserror", "tests_pass", "tests_added_min", "package_policy", "probe_tests", "format_clean"}
CODE_DIRS = ("src/", "tests/")


def git(repo, *args):
    p = subprocess.run(["git", "-C", str(repo), *args], capture_output=True, text=True)
    return p.returncode, p.stdout.strip()


def commands(run):
    """The Bash commands of all agents in the run, from stream.jsonl."""
    out = []
    path = run / "stream.jsonl"
    if not path.is_file():
        return out
    for line in path.read_text(encoding="utf-8", errors="ignore").splitlines():
        try:
            d = json.loads(line)
        except ValueError:
            continue
        if d.get("type") != "assistant":
            continue
        for c in d.get("message", {}).get("content", []):
            if c.get("type") == "tool_use" and c.get("name") == "Bash":
                out.append(c.get("input", {}).get("command", ""))
    return out


def usage(run):
    """Cost, time, and final message. A run can end its turn more than one time (for example while it
    waits for a background agent): each end writes a result line. The cost in each line is the total of
    the session, so take the largest. The time is the sum of the turns."""
    path = run / "stream.jsonl"
    results = []
    if path.is_file():
        for line in path.read_text(encoding="utf-8", errors="ignore").splitlines():
            if '"type":"result"' in line.replace(" ", ""):
                try:
                    results.append(json.loads(line))
                except ValueError:
                    pass
    last = results[-1] if results else {}
    return {
        "cost_usd": round(max((r.get("total_cost_usd", 0.0) for r in results), default=0.0), 2),
        "minutes": round(sum(r.get("duration_ms", 0) for r in results) / 60000, 1),
        "turns": sum(r.get("num_turns") or 0 for r in results),
        "final": last.get("result", ""),
    }


def check_work_committed(repo, arm):
    code, branch = git(repo, "rev-parse", "--abbrev-ref", "HEAD")
    _, ahead = git(repo, "rev-list", "--count", "main..HEAD")
    n = int(ahead or 0)
    ok = n > 0 and (arm != "team" or branch.startswith("dev-team/"))
    return ok, f"{branch}, {n} commits since main"


def check_code_unchanged(repo):
    _, diff = git(repo, "diff", "--name-only", "main")
    _, untracked = git(repo, "ls-files", "--others", "--exclude-standard")
    _, committed = git(repo, "diff", "--name-only", "main...HEAD")
    changed = sorted({f for f in (diff + "\n" + untracked + "\n" + committed).splitlines() if f.startswith(CODE_DIRS)})
    return not changed, changed[:8]


def check_no_push(run):
    pushes = [c for c in commands(run) if "git push" in c or "gh pr create" in c or "/pulls" in c]
    return not pushes, pushes[:3]


def run_folder(repo):
    folders = sorted(p.parent for p in repo.glob("docs/work/*/run.md"))
    return folders[-1] if folders else None


def check_runcheck(repo):
    folder = run_folder(repo)
    if folder is None:
        return False, "no run folder"
    p = subprocess.run([sys.executable, str(SHARED / "runcheck.py"), str(folder), "--format", "json"],
                       capture_output=True, text=True)
    try:
        findings = json.loads(p.stdout)
    except ValueError:
        return False, p.stdout[-300:] or p.stderr[-300:]
    return not findings, [f"{f['rule']}: {f['message']}" for f in findings][:8]


def decision_findings(root):
    p = subprocess.run([sys.executable, str(SHARED / "decisioncheck.py"), "--root", str(root), "--format", "json",
                        "--rules", *STACK_RULES], capture_output=True, text=True)
    try:
        return {(f["rule"], f["message"]) for f in json.loads(p.stdout)}
    except ValueError:
        return {("ERROR", p.stderr[-200:])}


def check_decisions(repo):
    with tempfile.TemporaryDirectory() as tmp:
        base = pathlib.Path(tmp) / "main"
        git(repo, "worktree", "add", "--detach", str(base), "main")
        try:
            before = decision_findings(base)
        finally:
            git(repo, "worktree", "remove", "--force", str(base))
    new = sorted(decision_findings(repo) - before)
    return not new, [f"{r}: {m}" for r, m in new][:8]


def score(ev, run, arm, only=None):
    repo = run / "repo"
    results = []
    stack = [a for a in ev["assertions"] if a.get("check") in STACK_CHECKS and (not only or a["id"] in only)]
    if stack:
        sub = {"name": ev["name"], "fixture": "orders-api", "kind": "write", "assertions": stack}
        results += ds.score_run(sub, repo)
    for a in ev["assertions"]:
        kind = a.get("check")
        if kind is None or kind in STACK_CHECKS or (only and a["id"] not in only):
            continue
        if a.get("arm", arm) != arm:
            continue
        if kind == "work_committed":
            ok, got = check_work_committed(repo, arm)
        elif kind == "code_unchanged":
            ok, got = check_code_unchanged(repo)
        elif kind == "no_push":
            ok, got = check_no_push(run)
        elif kind == "runcheck_clean":
            ok, got = check_runcheck(repo)
        elif kind == "decisioncheck_no_new":
            ok, got = check_decisions(repo)
        else:
            raise SystemExit(f"Unknown check: {kind}")
        results.append({"id": a["id"], "check": kind, "ok": bool(ok), "got": got})
    order = {a["id"]: i for i, a in enumerate(ev["assertions"])}
    return sorted(results, key=lambda r: order.get(r["id"], 99))


def load():
    return {e["name"]: e for e in json.loads((HERE / "evals.json").read_text(encoding="utf-8"))["evals"]}


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("runs", nargs="?", type=pathlib.Path)
    parser.add_argument("--run", type=pathlib.Path)
    parser.add_argument("--eval")
    parser.add_argument("--arm", default="team")
    parser.add_argument("--only", nargs="*")
    args = parser.parse_args(argv)
    evals = load()
    if args.run:
        out = {"results": score(evals[args.eval], args.run, args.arm, args.only), "usage": usage(args.run)}
        print(json.dumps(out, indent=1))
        return 0
    if not args.runs:
        parser.error("give a runs folder or --run")
    table = []
    for run in sorted(args.runs.glob("*/*/*/repo")):
        run = run.parent
        name, arm, n = run.parts[-3], run.parts[-2], run.parts[-1]
        if name not in evals:
            continue
        res = score(evals[name], run, arm)
        u = usage(run)
        if u["final"] and not (run / "final.md").exists():
            (run / "final.md").write_text(u["final"], encoding="utf-8")
        (run / "checks.json").write_text(json.dumps({"results": res, "usage": u}, indent=1), encoding="utf-8")
        passed = sum(r["ok"] for r in res)
        fails = ",".join(r["id"] for r in res if not r["ok"]) or "-"
        print(f"{name:13} {arm:9} {n:3} {passed}/{len(res)} fail={fails} ${u['cost_usd']} {u['minutes']}min")
        table.append({"eval": name, "arm": arm, "run": n, "passed": passed, "total": len(res), "fails": fails, **{k: u[k] for k in ("cost_usd", "minutes", "turns")}})
    (args.runs / "scores.json").write_text(json.dumps(table, indent=1), encoding="utf-8")
    return 0


if __name__ == "__main__":
    sys.exit(main())
