"""Load the eval cases: evals.json and one file for each extra case in cases/<name>.json.

A file in cases/ holds one case object, in the same form as an entry of evals.json "evals".
The split lets more than one person (or agent) add cases without edits to the same file.
"""
import json
import pathlib

HERE = pathlib.Path(__file__).resolve().parent


def load_list():
    evals = list(json.loads((HERE / "evals.json").read_text(encoding="utf-8"))["evals"])
    names = {e["name"] for e in evals}
    for path in sorted((HERE / "cases").glob("*.json")):
        case = json.loads(path.read_text(encoding="utf-8"))
        if case["name"] != path.stem:
            raise SystemExit(f"{path}: name must equal the file name")
        if case["name"] in names:
            raise SystemExit(f"{path}: case {case['name']} exists twice")
        names.add(case["name"])
        evals.append(case)
    return evals


def load():
    return {e["name"]: e for e in load_list()}


def load_expected():
    """selftest/expected.json, plus selftest/<case>/expected.json for each case that has one."""
    expected = json.loads((HERE / "selftest" / "expected.json").read_text(encoding="utf-8"))
    for path in sorted((HERE / "selftest").glob("*/expected.json")):
        expected[path.parent.name] = json.loads(path.read_text(encoding="utf-8"))
    return expected
