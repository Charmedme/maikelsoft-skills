#!/usr/bin/env python3
"""Score the 'check' assertions of evals.json. Layout: <runs>/<eval>/<variant>/<n>/<output>."""
import json, pathlib, re, sys
evals = json.loads((pathlib.Path(__file__).parent / "evals.json").read_text())["evals"]
runs = pathlib.Path(sys.argv[1])
for ev in evals:
    for run in sorted(runs.glob(f"{ev['name']}/*/*")):
        out = run / (ev["output"] or "none")
        for a in ev["assertions"]:
            if "check" not in a:
                continue
            text = out.read_text() if out.is_file() else ""
            ok = {"line_max": lambda v: out.is_file() and len(text.splitlines()) <= v,
                  "contains": lambda v: bool(re.search(v, text)),
                  "no_file": lambda v: not (run / v).exists()}[a["check"]](a["value"])
            print(run, a["id"], ok)
