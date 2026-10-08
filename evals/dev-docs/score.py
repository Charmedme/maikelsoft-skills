#!/usr/bin/env python3
"""Score the 'check' assertions of evals.json for each run.

Layout of a runs folder:
    <runs>/<eval name>/<variant>/<run number>/<output file>

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


def score_run(ev, out_path, inputs):
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
        for out in sorted(runs.glob(f"{ev['name']}/*/*/{ev['output']}")):
            variant, run = out.parent.parent.name, out.parent.name
            r = score_run(ev, out, inputs)
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
