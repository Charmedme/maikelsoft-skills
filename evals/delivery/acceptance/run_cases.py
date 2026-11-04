#!/usr/bin/env python3
"""Run the hidden cases of one story against a running system and print one JSON result.

    SUT_URL=http://127.0.0.1:5810 python3 -I run_cases.py <hidden folder>

Output: {"<case id>": ["Passed" | "Failed" | "Error" | "Skipped", "<message>"], ...}
"""
import json
import pathlib
import sys
import unittest

HERE = pathlib.Path(__file__).resolve().parent


class Collect(unittest.TestResult):
    def __init__(self):
        super().__init__()
        self.outcomes = {}

    def addFailure(self, test, err):
        self.outcomes[_name(test)] = ["Failed", _short(err)]

    def addError(self, test, err):
        self.outcomes[_name(test)] = ["Error", _short(err)]

    def addSubTest(self, test, subtest, err):
        # a failing sub test fails the whole case; a passing one adds nothing
        if err is not None:
            outcome = "Failed" if issubclass(err[0], test.failureException) else "Error"
            self.outcomes[_name(test)] = [outcome, f"{subtest._subDescription()}: {_short(err)}"]

    def addSuccess(self, test):
        if _name(test) not in self.outcomes:
            self.outcomes[_name(test)] = ["Passed", ""]

    def addSkip(self, test, reason):
        self.outcomes[_name(test)] = ["Skipped", reason]


def _name(test):
    parts = test.id().split(".")
    return ".".join(parts[-2:])


def _short(err):
    return str(err[1]).splitlines()[0][:400] if str(err[1]) else err[0].__name__


def main(folder):
    folder = pathlib.Path(folder).resolve()
    sys.path[:0] = [str(HERE), str(folder)]
    suite = unittest.defaultTestLoader.discover(str(folder), pattern="test_*.py", top_level_dir=str(folder))
    result = Collect()
    suite.run(result)
    print(json.dumps(result.outcomes))
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1]))
