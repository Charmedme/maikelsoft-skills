"""Tests for heldback.py of dev-flow:build and the held-back block of the guard hook.
Run: python3 -m unittest tests.test_heldback"""
import json
import os
import pathlib
import subprocess
import sys
import tempfile
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[1]
HELDBACK = ROOT / "plugins" / "dev-flow" / "skills" / "build" / "scripts" / "heldback.py"
GUARD = ROOT / "plugins" / "dev-flow" / "hooks" / "guard.py"


class HeldBack(unittest.TestCase):
    def setUp(self):
        tmp = tempfile.TemporaryDirectory()
        self.addCleanup(tmp.cleanup)
        self.tmp = pathlib.Path(tmp.name)
        self.repo = self.tmp / "repo"
        self.repo.mkdir()
        subprocess.run(["git", "init", "-q", str(self.repo)], check=True)
        self.store = self.tmp / "store"
        self.env = dict(os.environ, DEV_FLOW_HELDBACK=str(self.store))

    def heldback(self, *args, stdin=None):
        return subprocess.run([sys.executable, str(HELDBACK), *args], cwd=self.repo, env=self.env,
                              input=stdin, capture_output=True, text=True)

    def guard(self, tool, tool_input):
        event = {"tool_name": tool, "tool_input": tool_input, "cwd": str(self.repo)}
        return subprocess.run([sys.executable, str(GUARD)], input=json.dumps(event), env=self.env,
                              capture_output=True, text=True)

    def test_add_saves_the_test_outside_the_repository(self):
        out = self.heldback("add", "--item", "SHOP-1", "--name", "r1_test.py", stdin="assert True\n")
        self.assertEqual(out.returncode, 0, out.stderr)
        saved = list(self.store.rglob("r1_test.py"))
        self.assertEqual(len(saved), 1)
        self.assertNotIn(str(self.repo), str(saved[0]))

    def test_list_gives_names_and_count_but_not_the_text(self):
        self.heldback("add", "--item", "SHOP-1", "--name", "r1_test.py", stdin="SECRET_ASSERT = 1\n")
        out = self.heldback("list", "--item", "SHOP-1")
        self.assertIn("r1_test.py", out.stdout)
        self.assertIn("1 held-back test file", out.stdout)
        self.assertNotIn("SECRET_ASSERT", out.stdout)

    def test_run_gives_the_exit_code_and_removes_the_copy(self):
        self.heldback("add", "--item", "SHOP-1", "--name", "check.sh", stdin="exit 3\n")
        out = self.heldback("run", "--item", "SHOP-1", "--dest", "held", "--test", "sh held/check.sh")
        self.assertEqual(out.returncode, 3, out.stdout + out.stderr)
        self.assertIn("exit 3", out.stdout)
        self.assertFalse((self.repo / "held").exists())

    def test_run_passes_when_the_tests_pass(self):
        self.heldback("add", "--item", "SHOP-1", "--name", "check.sh", stdin="echo ok\n")
        out = self.heldback("run", "--item", "SHOP-1", "--dest", "held", "--test", "sh held/check.sh")
        self.assertEqual(out.returncode, 0, out.stdout + out.stderr)
        self.assertFalse((self.repo / "held").exists())

    def test_run_refuses_a_folder_that_exists(self):
        self.heldback("add", "--item", "SHOP-1", "--name", "check.sh", stdin="echo ok\n")
        (self.repo / "held").mkdir()
        (self.repo / "held" / "keep.txt").write_text("x", encoding="utf-8")
        out = self.heldback("run", "--item", "SHOP-1", "--dest", "held", "--test", "true")
        self.assertEqual(out.returncode, 2)
        self.assertTrue((self.repo / "held" / "keep.txt").exists())

    def test_run_refuses_a_folder_outside_the_repository(self):
        self.heldback("add", "--item", "SHOP-1", "--name", "check.sh", stdin="echo ok\n")
        out = self.heldback("run", "--item", "SHOP-1", "--dest", "../outside", "--test", "true")
        self.assertEqual(out.returncode, 2)

    def test_add_refuses_a_name_with_a_folder(self):
        out = self.heldback("add", "--item", "SHOP-1", "--name", "../x.py", stdin="x\n")
        self.assertEqual(out.returncode, 2)

    def test_two_checkouts_do_not_share_tests(self):
        self.heldback("add", "--item", "SHOP-1", "--name", "a.py", stdin="x\n")
        other = self.tmp / "other"
        other.mkdir()
        subprocess.run(["git", "init", "-q", str(other)], check=True)
        out = subprocess.run([sys.executable, str(HELDBACK), "list", "--item", "SHOP-1"], cwd=other,
                             env=self.env, capture_output=True, text=True)
        self.assertIn("0 held-back test file", out.stdout)

    def test_guard_blocks_a_read_of_the_held_back_folder(self):
        self.heldback("add", "--item", "SHOP-1", "--name", "a.py", stdin="x\n")
        target = next(self.store.rglob("a.py"))
        self.assertEqual(self.guard("Read", {"file_path": str(target)}).returncode, 2)

    def test_guard_blocks_a_grep_of_the_held_back_folder(self):
        self.assertEqual(self.guard("Grep", {"pattern": "x", "path": str(self.store)}).returncode, 2)

    def test_guard_blocks_a_bash_read_of_the_held_back_folder(self):
        self.assertEqual(self.guard("Bash", {"command": f"cat {self.store}/*/SHOP-1/a.py"}).returncode, 2)

    def test_guard_allows_heldback_py(self):
        command = f"DEV_FLOW_HELDBACK={self.store} python3 {HELDBACK} run --item SHOP-1 --dest h --test 'make test'"
        self.assertEqual(self.guard("Bash", {"command": command}).returncode, 0)

    def test_guard_blocks_a_second_command_after_heldback_py(self):
        command = f"python3 {HELDBACK} list --item SHOP-1 && cat {self.store}/x/SHOP-1/a.py"
        self.assertEqual(self.guard("Bash", {"command": command}).returncode, 2)

    def test_guard_allows_a_read_in_the_repository(self):
        (self.repo / "spec.md").write_text("# Spec\n", encoding="utf-8")
        self.assertEqual(self.guard("Read", {"file_path": str(self.repo / "spec.md")}).returncode, 0)


if __name__ == "__main__":
    unittest.main()
