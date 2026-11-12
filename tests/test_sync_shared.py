"""Tests for scripts/sync_shared.py. Run: python3 -m unittest discover -s tests"""
import importlib.util
import pathlib
import tempfile
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[1]
SCRIPT = ROOT / "scripts" / "sync_shared.py"
spec = importlib.util.spec_from_file_location("sync_shared", SCRIPT)
sync = importlib.util.module_from_spec(spec)
spec.loader.exec_module(sync)

NEW = ["context.md", "cycle.md", "review-rules.md", "review-template.md",
       "runheader.py", "specsync.py", "docscheck.py", "speclock.py", "mutate.py", "heldback.py"]


class SyncShared(unittest.TestCase):
    def skill(self, text):
        tmp = tempfile.TemporaryDirectory()
        self.addCleanup(tmp.cleanup)
        skill = pathlib.Path(tmp.name) / "plugins" / "demo" / "skills" / "demo"
        skill.mkdir(parents=True)
        (skill / "SKILL.md").write_text(text, encoding="utf-8")
        return skill

    def test_each_new_file_exists_and_is_mapped(self):
        mapped = {p.name for p in sync.NAMED_COPIES}
        for name in NEW:
            self.assertTrue((ROOT / "shared" / name).is_file(), name)
            self.assertIn(name, mapped)

    def test_a_skill_that_names_a_file_gets_it(self):
        skill = self.skill("Read `context.md` and `cycle.md`. Run `specsync.py check`.\n")
        files = {p.relative_to(skill).as_posix() for p in sync.expected_files(skill)}
        self.assertIn("references/context.md", files)
        self.assertIn("references/cycle.md", files)
        self.assertIn("scripts/specsync.py", files)
        self.assertNotIn("scripts/docscheck.py", files)

    def test_a_longer_name_does_not_name_the_file(self):
        skill = self.skill("Read `backlog-context.md` and `my_cycle.md`.\n")
        files = {p.relative_to(skill).as_posix() for p in sync.expected_files(skill)}
        self.assertNotIn("references/context.md", files)
        self.assertNotIn("references/cycle.md", files)

    def test_the_copies_are_current(self):
        for skill in sync.skill_dirs():
            for target, content in sync.expected_files(skill).items():
                self.assertTrue(target.is_file(), target)
                self.assertEqual(target.read_text(encoding="utf-8"), content, target)


if __name__ == "__main__":
    unittest.main()
