#!/usr/bin/env python3
"""Script test for the 'check' assertions of the trap cases.

For each trap case, selftest/<case>/ has overlays: 'common' (always applied) and one folder for
each variant (for example 'naive' and 'good'). The variant 'fixture' is the fixture with no overlay.
The script copies the fixture of the case, applies the overlays, runs the check assertions of the case,
and compares each result with selftest/expected.json.

Usage:
    python3 evals/dev-stack/selftest.py [--case <name> ...] [--variant <name> ...] [--keep <folder>]

Exit code 0 when every check gives the expected result.
"""
import argparse
import json
import pathlib
import shutil
import sys
import tempfile

HERE = pathlib.Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
import score  # noqa: E402


def build(ev, variant, dest):
    """Variant 'fixture' is the fixture with no overlay (the start point of the case)."""
    shutil.copytree(score.FIXTURES / ev["fixture"], dest, ignore=shutil.ignore_patterns(*score.SKIP_DIRS))
    for layer in () if variant == "fixture" else ("common", variant):
        src = HERE / "selftest" / ev["name"] / layer
        if src.is_dir():
            shutil.copytree(src, dest, dirs_exist_ok=True)
        deleted = src / "_delete.txt"
        if deleted.is_file():
            for rel in deleted.read_text(encoding="utf-8").split():
                (dest / rel).unlink(missing_ok=True)
            (dest / "_delete.txt").unlink(missing_ok=True)


def main():
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("--case", action="append")
    parser.add_argument("--variant", action="append")
    parser.add_argument("--keep", type=pathlib.Path, help="keep the built folders here")
    args = parser.parse_args()
    evals = score.load_evals()
    expected = json.loads((HERE / "selftest" / "expected.json").read_text(encoding="utf-8"))
    rows, bad = [], 0
    for case, variants in expected.items():
        if args.case and case not in args.case:
            continue
        ev = evals[case]
        for variant, want in variants.items():
            if args.variant and variant not in args.variant:
                continue
            with tempfile.TemporaryDirectory() as tmp:
                root = (args.keep / case / variant) if args.keep else pathlib.Path(tmp) / "repo"
                if root.exists():
                    shutil.rmtree(root)
                build(ev, variant, root)
                ids = set(want)
                checks = [a for a in ev["assertions"] if a.get("id") in ids]
                results = score.score_run({**ev, "assertions": checks}, root.resolve())
            for r in results:
                match = r["ok"] == want[r["id"]]
                bad += not match
                got = json.dumps(r["got"], default=str)
                rows.append((case, variant, r["id"], r["check"], "pass" if r["ok"] else "fail",
                             "pass" if want[r["id"]] else "fail", "ok" if match else "MISMATCH", got[:110]))
                print(" | ".join(rows[-1]), flush=True)
    print()
    print(f"{len(rows) - bad}/{len(rows)} checks gave the expected result.")
    return 1 if bad else 0


if __name__ == "__main__":
    sys.exit(main())
