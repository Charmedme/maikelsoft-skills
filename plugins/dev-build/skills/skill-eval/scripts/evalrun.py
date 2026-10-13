#!/usr/bin/env python3
"""Run the eval set of a plugin and make one results table.

    python3 evalrun.py <plugin dir> [--variant NAME=PATH ...] [--runs 3] [--case GLOB]
                       [--allow-tools TOOL ...] [--max-cost-usd 10] [--out DIR]
                       [--model M] [--judge-model M] [--scaffold] [--no-baseline]

The eval set is in <plugin dir>/evals/<case>/. A case has one of two forms:
- prompt.md + graders/*.md: a one-turn case. `claude plugin eval` runs it.
- request.md + human.md + graders/*.md: a case with a simulated human. simhuman.py runs it.
Both forms can have checks.json: commands that score the output files.

Variants: without --variant, the plugin dir is the only variant. With --variant,
each PATH is a plugin folder (for example an older copy). All variants run the
cases of <plugin dir>, so the cases are the same. The first variant also gets
the baseline arm (no plugin), unless --no-baseline.

Blind: each variant and the baseline get a random label (A, B, C...). The
plugin copy is in a folder with that label, so no output and no grader input
shows the variant name. labels.json maps the labels after all grading.

Output in --out (default <plugin dir>/evals/results/<time>/):
    runs/<case>/<label>/<n>/cwd/   the files of each run
    native-<label>.json            the result of claude plugin eval
    results.json, results.md       one row per assertion, one column per variant
"""
import argparse
import datetime
import fnmatch
import json
import os
import pathlib
import random
import re
import shutil
import string
import subprocess
import sys
import tempfile

HERE = pathlib.Path(__file__).resolve().parent
CHECK_TIMEOUT = 120  # seconds for one check command


def copy_plugin(src: pathlib.Path, dest: pathlib.Path, cases: pathlib.Path, repo: pathlib.Path):
    """Copy a plugin without its evals, then copy the shared cases into it.

    claude plugin eval refuses an evals link that points outside the plugin. So the
    cases are a copy, and evals/.repo-root tells a scaffold script where the repository is."""
    shutil.copytree(src, dest, ignore=shutil.ignore_patterns("evals", ".git", "__pycache__"))
    shutil.copytree(cases, dest / "evals", ignore=shutil.ignore_patterns("results"))
    (dest / "evals" / ".repo-root").write_text(str(repo), encoding="utf-8")


def list_cases(evals: pathlib.Path, pattern: str):
    native, human = [], []
    for case in sorted(p for p in evals.iterdir() if p.is_dir() and p.name != "results"):
        if pattern and not fnmatch.fnmatch(case.name, pattern):
            continue
        if (case / "prompt.md").is_file():
            native.append(case)
        elif (case / "human.md").is_file() and (case / "request.md").is_file():
            human.append(case)
    return native, human


def remove_kept(folder: pathlib.Path):
    """Remove a run folder that claude plugin eval kept. Its files can be read-only."""
    subprocess.run(["chmod", "-R", "u+rwX", str(folder)], capture_output=True)
    shutil.rmtree(folder, ignore_errors=True)


def run_native(plugin: pathlib.Path, label: str, args, out: pathlib.Path, ablation: str):
    result_file = out / f"native-{label}.json"
    cmd = ["claude", "plugin", "eval", str(plugin), "--trust-plugin", "--keep-temp", "--no-publish",
           "--json", str(result_file), "--runs", str(args.runs), "--ablation", ablation,
           "--output-dir", str(out / f"native-{label}")]
    if args.case:
        cmd += ["--case", args.case]
    if args.allow_tools:
        cmd += ["--allow-tools", *args.allow_tools]
    if args.max_cost_usd:
        cmd += ["--max-cost-usd", str(args.max_cost_usd)]
    if args.model:
        cmd += ["--model", args.model]
    if args.judge_model:
        cmd += ["--judge-model", args.judge_model]
    if args.scaffold:
        cmd += ["--scaffold"]
    print(f"RUN  claude plugin eval for variant {label} ({ablation})", flush=True)
    proc = subprocess.run(cmd, capture_output=True, text=True, cwd=plugin)
    print("\n".join("     " + ln for ln in proc.stdout.strip().splitlines()[-12:]), flush=True)
    if not result_file.is_file():
        raise SystemExit(f"claude plugin eval wrote no result (exit {proc.returncode}): {proc.stderr[-800:]}")
    return json.loads(result_file.read_text(encoding="utf-8"))


def collect_native(data: dict, label_for_arm: dict, out: pathlib.Path):
    """Copy each kept run folder to runs/<case>/<label>/<n>/ and return grader results."""
    rows = []
    for case in data.get("cases", []):
        for arm, runs in case.get("arms", {}).items():
            label = label_for_arm.get(arm)
            if not label:
                continue
            for n, run in enumerate(runs, 1):
                dest = out / "runs" / case["name"] / label / str(n)
                trace = pathlib.Path(run.get("tracePath") or "")
                kept = trace.parent.parent if trace.name else None
                if kept and (kept / "home" / "cwd").is_dir():
                    shutil.copytree(kept / "home" / "cwd", dest / "cwd", symlinks=True)
                    shutil.copy2(trace, dest / "trace.jsonl")
                    remove_kept(kept)
                else:
                    (dest / "cwd").mkdir(parents=True, exist_ok=True)
                for g in run.get("graders", []):
                    if g.get("scored", True) or g.get("withOnly"):
                        rows.append({"case": case["name"], "id": g["name"], "label": label, "run": n,
                                     "pass": bool(g.get("passed")), "evidence": g.get("explanation", "")[:200]})
                if run.get("error"):
                    rows.append({"case": case["name"], "id": "run-error", "label": label, "run": n,
                                 "pass": False, "evidence": str(run["error"])[:200]})
    return rows


def fill(cmd: str, **values) -> str:
    """Replace {cwd}, {repo}, {case}, and {run}. Other braces stay, for example a regex {4}."""
    for key, value in values.items():
        cmd = cmd.replace("{" + key + "}", str(value))
    return cmd


def run_checks(case_dir: pathlib.Path, out: pathlib.Path, repo: pathlib.Path):
    """Run the commands of checks.json on each copied run. Exit 0 is a pass."""
    checks_file = case_dir / "checks.json"
    if not checks_file.is_file():
        return []
    checks = json.loads(checks_file.read_text(encoding="utf-8"))
    rows = []
    for label_dir in sorted((out / "runs" / case_dir.name).glob("*")):
        for run_dir in sorted(label_dir.glob("*"), key=lambda p: int(p.name)):
            for check in checks:
                cmd = fill(check["cmd"], cwd=run_dir / "cwd", repo=repo, case=case_dir, run=run_dir)
                try:
                    proc = subprocess.run(["bash", "-c", cmd], capture_output=True, text=True,
                                          timeout=CHECK_TIMEOUT, env={**os.environ, "PYTHONSAFEPATH": "1"})
                    ok, got = proc.returncode == 0, (proc.stdout + proc.stderr).strip().splitlines()[-1:]
                except subprocess.TimeoutExpired:
                    ok, got = False, ["timeout"]
                rows.append({"case": case_dir.name, "id": check["id"], "label": label_dir.name,
                             "run": int(run_dir.name), "pass": ok, "evidence": (got[0] if got else "")[:200]})
    return rows


def table(rows, columns, titles):
    """Markdown table: one row per (case, assertion), one column per label: passes/runs."""
    keys = sorted({(r["case"], r["id"]) for r in rows})
    lines = ["| Case | Assertion | " + " | ".join(titles[c] for c in columns) + " |",
             "|------|-----------|" + "|".join("----" for _ in columns) + "|"]
    for case, aid in keys:
        cells = []
        for col in columns:
            hits = [r for r in rows if r["case"] == case and r["id"] == aid and r["label"] == col]
            cells.append(f"{sum(r['pass'] for r in hits)}/{len(hits)}" if hits else "-")
        lines.append(f"| {case} | {aid} | " + " | ".join(cells) + " |")
    return "\n".join(lines)


def main():
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("plugin")
    parser.add_argument("--variant", action="append", default=[], metavar="NAME=PATH")
    parser.add_argument("--runs", type=int, default=3)
    parser.add_argument("--case", default="")
    parser.add_argument("--allow-tools", nargs="*", default=[])
    parser.add_argument("--max-cost-usd", type=float, default=10.0)
    parser.add_argument("--out")
    parser.add_argument("--model")
    parser.add_argument("--judge-model")
    parser.add_argument("--human-model", default="haiku")
    parser.add_argument("--max-turns", type=int, default=8, help="agent turns in a simulated-human case")
    parser.add_argument("--scaffold", action="store_true")
    parser.add_argument("--no-baseline", action="store_true")
    args = parser.parse_args()

    plugin = pathlib.Path(args.plugin).resolve()
    evals = plugin / "evals"
    if not evals.is_dir():
        raise SystemExit(f"{plugin}: no evals/ folder. Write the cases first (see references/cases.md).")
    repo = pathlib.Path(subprocess.run(["git", "rev-parse", "--show-toplevel"], cwd=plugin,
                                       capture_output=True, text=True).stdout.strip() or plugin)
    variants = [v.split("=", 1) for v in args.variant] or [["skill", str(plugin)]]
    stamp = datetime.datetime.now().strftime("%Y-%m-%dT%H-%M-%S")
    out = pathlib.Path(args.out).resolve() if args.out else evals / "results" / stamp
    out.mkdir(parents=True, exist_ok=True)

    names = [name for name, _ in variants] + ([] if args.no_baseline else ["baseline"])
    letters = random.sample(string.ascii_uppercase[:max(len(names), 2)], len(names))
    labels = dict(zip(names, letters))  # variant name -> blind label
    native_cases, human_cases = list_cases(evals, args.case)
    work = pathlib.Path(tempfile.mkdtemp(prefix="skill-eval-"))
    rows = []
    try:
        for i, (name, path) in enumerate(variants):
            label = labels[name]
            copy = work / label / pathlib.Path(path).resolve().name
            copy_plugin(pathlib.Path(path).resolve(), copy, evals, repo)
            with_baseline = i == 0 and not args.no_baseline
            if native_cases:
                data = run_native(copy, label, args, out, "with-without" if with_baseline else "none")
                arms = {"with": label}
                if with_baseline:
                    arms["without"] = labels["baseline"]
                rows += collect_native(data, arms, out)
            human_copies = [copy / "evals" / c.name for c in human_cases]
            for case in human_copies:
                arm_list = [(label, copy)] + ([(labels["baseline"], None)] if with_baseline else [])
                for arm_label, arm_plugin in arm_list:
                    for n in range(1, args.runs + 1):
                        cmd = [sys.executable, str(HERE / "simhuman.py"), str(case),
                               "--out", str(out / "runs" / case.name / arm_label / str(n)),
                               "--human-model", args.human_model, "--max-turns", str(args.max_turns),
                               "--max-cost-usd", str(args.max_cost_usd / max(args.runs, 1))]
                        if arm_plugin:
                            cmd += ["--plugin", str(arm_plugin)]
                        if args.model:
                            cmd += ["--model", args.model]
                        if args.judge_model:
                            cmd += ["--judge-model", args.judge_model]
                        if args.scaffold:
                            cmd += ["--scaffold"]
                        print(f"RUN  {case.name} {arm_label} run {n} (simulated human)", flush=True)
                        proc = subprocess.run(cmd, capture_output=True, text=True)
                        result = out / "runs" / case.name / arm_label / str(n) / "grades.json"
                        if result.is_file():
                            for g in json.loads(result.read_text(encoding="utf-8")):
                                rows.append({"case": case.name, "label": arm_label, "run": n, **g})
                        else:
                            rows.append({"case": case.name, "id": "run-error", "label": arm_label, "run": n,
                                         "pass": False, "evidence": proc.stderr[-200:]})
        for case in native_cases + human_cases:
            rows += run_checks(case, out, repo)
    finally:
        shutil.rmtree(work, ignore_errors=True)

    columns = [labels["baseline"]] if not args.no_baseline else []
    columns += [labels[name] for name, _ in variants]
    titles = {label: name for name, label in labels.items()}
    (out / "labels.json").write_text(json.dumps(labels, indent=2), encoding="utf-8")
    (out / "results.json").write_text(json.dumps(rows, indent=2), encoding="utf-8")
    md = table(rows, columns, titles)
    (out / "results.md").write_text(md + "\n", encoding="utf-8")
    print(md)
    print(f"Results in {out}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
