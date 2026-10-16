#!/usr/bin/env python3
"""Score grill runs: python3 score.py <run dir> [<run dir> ...]

Reads <run>/.oracle/log.jsonl and prints the hidden facts that each run found,
with the number of the question that found it.
"""
import json
import pathlib
import sys

KEYS = ["N1-current-work", "N2-pain", "N3-exception", "N4-segments", "N5-success",
        "I1-account-admin", "I2-reporting", "I3-roles"]
print("run | calls | " + " | ".join(k.split("-")[0] for k in KEYS) + " | need (N) | impact (I)")
for run in sys.argv[1:]:
    log = pathlib.Path(run, ".oracle", "log.jsonl")
    rows = [json.loads(line) for line in log.read_text(encoding="utf-8").splitlines()] if log.exists() else []
    first = {}
    for n, row in enumerate(rows, 1):
        for key in row["hits"]:
            first.setdefault(key, n)
    cells = [str(first.get(k, "-")) for k in KEYS]
    need = sum(k in first for k in KEYS[:5])
    impact = sum(k in first for k in KEYS[5:])
    print(f"{pathlib.Path(run).name} | {len(rows)} | " + " | ".join(cells) + f" | {need}/5 | {impact}/3")
