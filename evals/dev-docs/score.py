#!/usr/bin/env python3
"""Score the 'check' assertions of evals.json for each run.

Layout of a runs folder:
    <runs>/<eval name>/<variant>/<run number>/<output file>

The output can be a glob pattern. A run folder without the output fails each check.

Usage:
    python3 evals/dev-docs/score.py <runs> --input gron=<path> --input haal-centraal-brp=<path>

Prints one line for each run and a summary for each eval and variant. Writes
<runs>/scores.json. The 'judge' assertions need a grader agent (see evals.json).
"""
import argparse
import json
import pathlib
import re
import sys

HERE = pathlib.Path(__file__).resolve().parent
sys.path.insert(0, str(HERE.parents[1] / "shared"))
import doccheck  # noqa: E402

KEEP = {"KEEP-CODE", "KEEP-LINK"}


def resolve(value, inputs):
    for key, path in inputs.items():
        value = value.replace(f"<{key}>", path)
    return value


def score_run(ev, out_path, inputs, run_dir=None):
    run_dir = run_dir or out_path.parent
    text = out_path.read_text(encoding="utf-8")
    source = resolve(ev["source"], inputs) if ev.get("source") else None
    findings, metrics = doccheck.check(text, str(out_path), "auto", "standard")
    if source:
        findings += doccheck.compare(pathlib.Path(source).read_text(encoding="utf-8"), text, str(out_path))
    errors = sum(f.severity == "error" and f.rule not in KEEP for f in findings)
    keep = sum(f.rule in KEEP for f in findings)
    anchors = sum(f.rule == "KEEP-ANCHOR" for f in findings)
    results = []
    for a in ev["assertions"]:
        if "check" not in a:
            continue
        kind, value = a["check"], a["value"]
        if kind == "errors_max":
            ok, got = errors <= value, errors
        elif kind == "keep_errors_max":
            ok, got = keep <= value, keep
        elif kind == "anchors_max":
            ok, got = anchors <= value, anchors
        elif kind == "lang":
            ok, got = metrics["language"] == value, metrics["language"]
        elif kind == "not_contains":
            hits = re.findall(value, text)
            ok, got = not hits, len(hits)
        elif kind in ("exists", "absent"):
            found = [f for f in value if any(run_dir.glob(f))]
            wrong = [f for f in value if f not in found] if kind == "exists" else found
            ok, got = not wrong, wrong
        elif kind == "contains":
            hits = re.findall(value, text, re.MULTILINE | re.IGNORECASE)
            ok, got = bool(hits), len(hits)
        elif kind == "file_contains":
            files = sorted(run_dir.glob(value["file"]))
            other = files[0].read_text(encoding="utf-8") if files else ""
            ok = bool(files) and re.search(value["pattern"], other, re.MULTILINE | re.IGNORECASE) is not None
            got = value["file"] if not files else ok
        elif kind == "go_flags_exist":
            code = pathlib.Path(resolve(value, inputs)).read_text(encoding="utf-8")
            names = set(re.findall(r'flag\.\w+Var\(&\w+, "([\w-]+)"', code)) | {"h", "help"}
            real = {("--" if len(n) > 1 else "-") + n for n in names}
            used = {u for u in re.findall(r"`(?:gron\s+)?(--?[a-z][a-z-]*)`", text)}
            fake = sorted(used - real)
            ok, got = not fake, fake
        else:
            raise SystemExit(f"unknown check: {kind}")
        results.append({"check": kind, "value": value, "passed": ok, "got": got})
    return {"metrics": metrics, "checks": results}


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("runs")
    parser.add_argument("--input", action="append", default=[], help="name=path, for <name> in evals.json")
    args = parser.parse_args()
    inputs = dict(i.split("=", 1) for i in args.input)
    inputs.setdefault("fixtures", str(HERE / "fixtures"))
    evals = json.loads((HERE / "evals.json").read_text(encoding="utf-8"))["evals"]
    runs = pathlib.Path(args.runs)
    report = {}
    for ev in evals:
        if not ev.get("output"):
            continue
        for run_dir in sorted(p for p in runs.glob(f"{ev['name']}/*/*") if p.is_dir()):
            variant, run = run_dir.parent.name, run_dir.name
            outs = sorted(run_dir.glob(ev["output"]))
            if not outs:
                print(f"{ev['name']:32} {variant:10} run {run}: NO OUTPUT {ev['output']}")
                n = sum("check" in a for a in ev["assertions"])
                r = {"metrics": {}, "checks": [{"check": "output", "passed": False, "got": None}] * n}
                report.setdefault(ev["name"], {}).setdefault(variant, {})[run] = r
                continue
            r = score_run(ev, outs[0], inputs, run_dir)
            report.setdefault(ev["name"], {}).setdefault(variant, {})[run] = r
            passed = sum(c["passed"] for c in r["checks"])
            print(f"{ev['name']:32} {variant:10} run {run}: {passed}/{len(r['checks'])} checks"
                  f" | max {r['metrics']['max_words_per_sentence']} words"
                  + "".join(f" | FAIL {c['check']}={c['got']}" for c in r["checks"] if not c["passed"]))
    print()
    for name, variants in report.items():
        for variant, runs_ in variants.items():
            total = sum(len(r["checks"]) for r in runs_.values())
            passed = sum(c["passed"] for r in runs_.values() for c in r["checks"])
            print(f"{name:32} {variant:10} {passed}/{total} checks over {len(runs_)} runs")
    (runs / "scores.json").write_text(json.dumps(report, indent=2), encoding="utf-8")


if __name__ == "__main__":
    main()
