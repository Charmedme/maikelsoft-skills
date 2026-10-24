"""Tests for mutate.py of dev-flow. Run: python3 -m unittest discover -s tests"""
import json
import pathlib
import subprocess
import sys
import tempfile
import unittest

MUTATE = pathlib.Path(__file__).resolve().parents[1] / "plugins" / "dev-flow" / "skills" / "plan-and-build" / "scripts" / "mutate.py"
GIT = ["git", "-c", "user.name=T", "-c", "user.email=t@example.com", "-c", "commit.gpgsign=false"]

RULE = "def late(elapsed, limit):\n    return elapsed {op} limit\n"
WEAK_TEST = "from rule import late\nassert late(130, 120)\nassert not late(60, 120)\n"
STRONG_TEST = WEAK_TEST + "assert late(120, 120)\n"


class Mutate(unittest.TestCase):
    def repo(self, test_body, op=">="):
        d = pathlib.Path(tempfile.mkdtemp())
        subprocess.run(GIT + ["init", "-q", "-b", "main"], cwd=d, check=True)
        (d / "rule.py").write_text("def late(elapsed, limit):\n    return False\n", encoding="utf-8")
        subprocess.run(GIT + ["add", "-A"], cwd=d, check=True)
        subprocess.run(GIT + ["commit", "-q", "-m", "base"], cwd=d, check=True)
        (d / "rule.py").write_text(RULE.format(op=op), encoding="utf-8")
        (d / "check_test.py").write_text(test_body, encoding="utf-8")
        return d

    def run_mutate(self, d):
        p = subprocess.run([sys.executable, str(MUTATE), "--base", "HEAD", "--test", f"{sys.executable} check_test.py",
                            "--format", "json"], cwd=d, capture_output=True, text=True)
        return p.returncode, json.loads(p.stdout) if p.stdout.strip().startswith("{") else p.stderr

    def test_boundary_without_example_survives(self):
        code, out = self.run_mutate(self.repo(WEAK_TEST))
        self.assertEqual(code, 1)
        self.assertEqual(out["survived"][0]["from"], ">=")

    def test_boundary_with_example_is_killed(self):
        code, out = self.run_mutate(self.repo(STRONG_TEST))
        self.assertEqual(code, 0)
        self.assertEqual(out["killed"], 1)

    def test_file_is_restored(self):
        d = self.repo(WEAK_TEST)
        self.run_mutate(d)
        self.assertIn("elapsed >= limit", (d / "rule.py").read_text())

    def test_failing_tests_first_is_refused(self):
        d = self.repo("assert False\n")
        code, out = self.run_mutate(d)
        self.assertEqual(code, 2)


if __name__ == "__main__":
    unittest.main()
