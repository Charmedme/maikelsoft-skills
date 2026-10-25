#!/usr/bin/env python3
"""Runs for batch 2 of the ux-design evals: the prototype (suite A) and the review (suite B).

    python3 evals/ux-design/run2.py <runs> --suite prototype|review [--variant ...] [--runs-per-variant 3]
                                    [--parallel 3] [--budget 12] [--app defects|clean]

Each run gets a fresh copy of the fixture with the approved design in docs/work/<slug>/.
Suite review also gets a copy of the built app in app/, and the runner starts the app on a free port
for the time of the run. The model and the prompt of each variant are in evals2.json.
The child runs have no permission bypass, the same tools as run.py, and a clean environment.
"""
import argparse
import concurrent.futures as cf
import json
import shutil
import socket
import subprocess
import sys
import time
from pathlib import Path

HERE = Path(__file__).resolve().parent
REPO = HERE.parents[1]
sys.path.insert(0, str(HERE))
from run import child_env  # noqa: E402

EV = json.loads((HERE / "evals2.json").read_text(encoding="utf-8"))


def free_port():
    with socket.socket() as s:
        s.bind(("127.0.0.1", 0))
        return s.getsockname()[1]


def prepare(run_dir, suite, app):
    repo = run_dir / "repo"
    if repo.exists():
        shutil.rmtree(repo)
    shutil.copytree(HERE / suite["fixture"], repo, ignore=shutil.ignore_patterns("stories"))
    work = repo / "docs" / "work" / suite["slug"]
    shutil.copytree(HERE / suite["design"], work, ignore=shutil.ignore_patterns("shots", "critique.md"))
    if suite.get("app"):
        shutil.copytree(HERE / suite["app"][app], repo / "app",
                        ignore=shutil.ignore_patterns("check-shots", "DEFECTS.md", "walk.json", "README.md", "__pycache__"))
        # axe-core: run `npm install axe-core@4.10.3` in evals/ux-design/tools/ first.
        axe = HERE / "tools" / "node_modules"
        if axe.exists():
            (repo / "node_modules").symlink_to(axe.resolve())
    return repo, work


def run_one(job, args):
    run_dir, suite, variant, app = job
    if (run_dir / "final.md").exists():
        return run_dir, "skipped", 0.0
    run_dir.mkdir(parents=True, exist_ok=True)
    repo, work = prepare(run_dir, suite, app)
    v = suite["variants"][variant]
    port = free_port()
    server = None
    if suite.get("app"):
        server = subprocess.Popen([sys.executable, "server.py", str(port)], cwd=repo / "app",
                                  stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
        time.sleep(1.0)
        md = repo / "CLAUDE.md"
        md.write_text(md.read_text(encoding="utf-8") + f"\n- Run the app: it runs at http://localhost:{port} (already started)\n- Playwright: installed (Python)\n", encoding="utf-8")
    # A variant with "root": "prev" uses the plugin of an older checkout (--prev-root), for the
    # compare with the previous version. The other variants use the plugin of this repository.
    if v.get("root") == "prev" and not args.prev_root:
        raise SystemExit(f"variant {variant} needs --prev-root <checkout of the previous version>")
    root = Path(args.prev_root) if v.get("root") == "prev" else REPO
    plugin = (root / v["plugin"]).resolve() if v.get("plugin") else None
    prompt = "\n\n".join((HERE / p).read_text(encoding="utf-8") for p in v["prompt"])
    prompt = (prompt.replace("<repo>", str(repo)).replace("<work>", f"docs/work/{suite['slug']}")
              .replace("<plugin>", str(plugin or "")).replace("<port>", str(port)))
    (run_dir / "prompt.md").write_text(prompt, encoding="utf-8")
    cmd = ["claude", "-p", prompt, "--model", v["model"], "--permission-mode", "acceptEdits",
           "--allowedTools", "Read", "Write", "Edit", "Glob", "Grep", "Bash(python3:*)", "Bash(ls:*)",
           "--disallowedTools", "Agent", "Skill", "WebFetch", "WebSearch",
           "--output-format", "stream-json", "--verbose", "--max-budget-usd", str(args.budget)]
    if plugin:
        cmd += ["--add-dir", str(plugin)]
    t0 = time.time()
    try:
        with open(run_dir / "transcript.jsonl", "w") as out, open(run_dir / "stderr.log", "w") as err:
            try:
                code = subprocess.run(cmd, cwd=repo, env=child_env(), stdout=out, stderr=err, timeout=args.timeout).returncode
            except subprocess.TimeoutExpired:
                code = "timeout"
    finally:
        if server:
            server.terminate()
    final, cost = "", 0.0
    for line in (run_dir / "transcript.jsonl").read_text(encoding="utf-8", errors="replace").splitlines():
        try:
            ev = json.loads(line)
        except json.JSONDecodeError:
            continue
        if ev.get("type") == "result":
            final = ev.get("result") or ""
            cost = ev.get("total_cost_usd") or 0.0
    meta = {"suite": args.suite, "variant": variant, "app": app, "exit": code, "cost_usd": cost,
            "seconds": round(time.time() - t0), "model": v["model"]}
    (run_dir / "meta.json").write_text(json.dumps(meta, indent=2), encoding="utf-8")
    (run_dir / "final.md").write_text(final or f"(no final message, exit {code})", encoding="utf-8")
    return run_dir, code, cost


def main():
    p = argparse.ArgumentParser()
    p.add_argument("runs")
    p.add_argument("--suite", required=True, choices=sorted(EV))
    p.add_argument("--variant", nargs="*")
    p.add_argument("--runs-per-variant", type=int, default=3)
    p.add_argument("--parallel", type=int, default=3)
    p.add_argument("--budget", type=float, default=12)
    p.add_argument("--timeout", type=int, default=3600)
    p.add_argument("--prev-root", help="checkout of the previous version, for a variant with root prev")
    p.add_argument("--app", default="defects", choices=["defects", "clean", "hard", "a11y"])
    args = p.parse_args()
    suite = EV[args.suite]
    jobs = []
    cases = suite.get("cases") or [{}]
    for case in cases:
        sub = dict(suite, **case)
        base = Path(args.runs) / args.suite / case["name"] if case else Path(args.runs) / args.suite
        for variant in args.variant or list(suite["variants"]):
            for n in range(1, args.runs_per_variant + 1):
                name = variant if args.app == "defects" else f"{variant}-{args.app}"
                jobs.append((base / name / str(n), sub, variant, args.app))
    total = 0.0
    with cf.ThreadPoolExecutor(max_workers=args.parallel) as ex:
        for run_dir, code, cost in ex.map(lambda j: run_one(j, args), jobs):
            total += cost
            print(f"{run_dir}: exit {code}, ${cost:.2f}", flush=True)
    print(f"total ${total:.2f}")


if __name__ == "__main__":
    sys.exit(main())
