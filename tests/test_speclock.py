"""Tests for speclock.py and the guard hook of dev-flow. Run: python3 -m unittest discover -s tests"""
import json
import os
import pathlib
import subprocess
import sys
import tempfile
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[1]
SPECLOCK = ROOT / "plugins" / "dev-flow" / "skills" / "plan-and-build" / "scripts" / "speclock.py"
GUARD = ROOT / "plugins" / "dev-flow" / "hooks" / "guard.py"


def run(cmd, cwd, stdin=None):
    return subprocess.run(cmd, cwd=cwd, input=stdin, capture_output=True, text=True)


class SpecLock(unittest.TestCase):
    def setUp(self):
        self.tmp = tempfile.TemporaryDirectory()
        self.repo = pathlib.Path(self.tmp.name)
        run(["git", "init", "-q"], self.repo)
        spec = self.repo / "tests" / "Acceptance" / "Specs"
        spec.mkdir(parents=True)
        self.spec = spec / "CancelSpecs.cs"
        self.spec.write_text("// cancel inside window\n", encoding="utf-8")
        self.other = spec.parent / "Drivers.cs"
        self.other.write_text("// driver\n", encoding="utf-8")

    def tearDown(self):
        self.tmp.cleanup()

    def lock(self):
        return run([sys.executable, str(SPECLOCK), "approve", "--item", "ORD-1", "--by", "human", str(self.spec)], self.repo)

    def hook(self, tool, tool_input):
        event = {"tool_name": tool, "tool_input": tool_input, "cwd": str(self.repo)}
        return run([sys.executable, str(GUARD)], self.repo, json.dumps(event))

    def test_check_passes_after_approve(self):
        self.assertEqual(self.lock().returncode, 0)
        r = run([sys.executable, str(SPECLOCK), "check"], self.repo)
        self.assertEqual(r.returncode, 0, r.stdout)

    def test_check_finds_a_change(self):
        self.lock()
        self.spec.write_text("// weakened\n", encoding="utf-8")
        r = run([sys.executable, str(SPECLOCK), "check"], self.repo)
        self.assertEqual(r.returncode, 1)
        self.assertIn("changed after approval", r.stdout)

    def test_check_finds_a_deletion(self):
        self.lock()
        os.remove(self.spec)
        r = run([sys.executable, str(SPECLOCK), "check"], self.repo)
        self.assertIn("deleted", r.stdout)

    def test_watched_file_change_is_listed_not_failed(self):
        dsl = self.repo / "tests" / "Acceptance" / "Dsl.cs"
        dsl.write_text("// dsl\n", encoding="utf-8")
        run([sys.executable, str(SPECLOCK), "approve", "--item", "ORD-1", "--by", "human", str(self.spec), "--watch", str(dsl)], self.repo)
        dsl.write_text("// dsl with a new method\n", encoding="utf-8")
        r = run([sys.executable, str(SPECLOCK), "check"], self.repo)
        self.assertEqual(r.returncode, 0)
        self.assertIn("WATCH", r.stdout)
        self.assertEqual(self.hook("Edit", {"file_path": str(dsl)}).returncode, 0)

    def test_unlock_then_change_passes(self):
        self.lock()
        run([sys.executable, str(SPECLOCK), "unlock", "--by", "human", str(self.spec)], self.repo)
        self.spec.write_text("// changed with approval\n", encoding="utf-8")
        self.assertEqual(run([sys.executable, str(SPECLOCK), "check"], self.repo).returncode, 0)

    def test_hook_blocks_edit_of_locked_file(self):
        self.lock()
        r = self.hook("Edit", {"file_path": str(self.spec)})
        self.assertEqual(r.returncode, 2)
        self.assertIn("approved acceptance test", r.stderr)

    def test_hook_allows_edit_of_other_file(self):
        self.lock()
        self.assertEqual(self.hook("Write", {"file_path": str(self.other)}).returncode, 0)

    def test_hook_blocks_bash_write(self):
        self.lock()
        r = self.hook("Bash", {"command": "sed -i 's/a/b/' tests/Acceptance/Specs/CancelSpecs.cs"})
        self.assertEqual(r.returncode, 2)
        r = self.hook("Bash", {"command": "echo x > tests/Acceptance/Specs/CancelSpecs.cs"})
        self.assertEqual(r.returncode, 2)

    def test_hook_allows_bash_read(self):
        self.lock()
        r = self.hook("Bash", {"command": "cat tests/Acceptance/Specs/CancelSpecs.cs 2>/dev/null; dotnet test 2>&1 | tail -5"})
        self.assertEqual(r.returncode, 0)

    def test_hook_allows_file_name_in_commit_message(self):
        self.lock()
        cmd = "sed -i 's/a/b/' docs/plan.md\ngit commit -q -m \"Test CancelSpecs.cs passes\""
        self.assertEqual(self.hook("Bash", {"command": cmd}).returncode, 0)

    def test_hook_passes_without_lock(self):
        self.assertEqual(self.hook("Edit", {"file_path": str(self.spec)}).returncode, 0)

    def test_hook_fails_open_on_bad_input(self):
        r = run([sys.executable, str(GUARD)], self.repo, "not json")
        self.assertEqual(r.returncode, 0)


if __name__ == "__main__":
    unittest.main()
