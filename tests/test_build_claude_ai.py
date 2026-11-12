"""Tests for scripts/build_claude_ai.py. Run: python3 -m unittest tests.test_build_claude_ai"""
import contextlib
import importlib.util
import io
import pathlib
import tempfile
import unittest
import zipfile

ROOT = pathlib.Path(__file__).resolve().parents[1]
SCRIPT = ROOT / "scripts" / "build_claude_ai.py"
spec = importlib.util.spec_from_file_location("build_claude_ai", SCRIPT)
build = importlib.util.module_from_spec(spec)
spec.loader.exec_module(build)

NORMAL = """---
name: draft-story
description: Writes a user story. Use when a product owner wants a story.
argument-hint: "[topic]"
allowed-tools: Bash(python3 ${CLAUDE_SKILL_DIR}/scripts/check.py *)
---

# Draft story

Run `python3 ${CLAUDE_SKILL_DIR}/scripts/check.py <file>`.
"""

FORWARDER = """---
name: old-name
description: Forwards to draft-story.
user-invocable: false
---

Use draft-story.
"""


def run(argv):
    """Run main and return (exit code, output)."""
    out = io.StringIO()
    with contextlib.redirect_stdout(out):
        code = build.main(argv)
    return code, out.getvalue()


class BuildClaudeAi(unittest.TestCase):
    def setUp(self):
        tmp = tempfile.TemporaryDirectory()
        self.addCleanup(tmp.cleanup)
        self.root = pathlib.Path(tmp.name)
        plugin = self.root / "plugins" / "demo"
        (plugin / ".claude-plugin").mkdir(parents=True)
        (plugin / ".claude-plugin" / "plugin.json").write_text('{"name": "demo"}\n', encoding="utf-8")
        (plugin / "README.md").write_text("# Demo\n", encoding="utf-8")
        (plugin / "shared").mkdir()
        (plugin / "shared" / "source.md").write_text("# Source\n", encoding="utf-8")
        normal = plugin / "skills" / "draft-story"
        (normal / "scripts" / "__pycache__").mkdir(parents=True)
        (normal / "references").mkdir()
        (normal / "SKILL.md").write_text(NORMAL, encoding="utf-8")
        (normal / "scripts" / "check.py").write_text("print('ok')\n", encoding="utf-8")
        (normal / "scripts" / "__pycache__" / "check.cpython-312.pyc").write_bytes(b"x")
        (normal / "scripts" / "stray.pyc").write_bytes(b"x")
        (normal / "references" / "rules.md").write_text("# Rules\n", encoding="utf-8")
        forwarder = plugin / "skills" / "old-name"
        forwarder.mkdir()
        (forwarder / "SKILL.md").write_text(FORWARDER, encoding="utf-8")
        self.plugin = plugin
        self.normal = normal

    def names(self):
        code, output = run(["--root", str(self.root), "--plugin", "demo", "--out", "dist"])
        self.assertEqual(code, 0, output)
        with zipfile.ZipFile(self.root / "dist" / "demo.zip") as archive:
            return archive, archive.namelist()

    def test_the_zip_has_the_plugin_manifest_at_its_top_level(self):
        _, names = self.names()
        self.assertIn(".claude-plugin/plugin.json", names)
        self.assertIn("skills/draft-story/SKILL.md", names)
        self.assertIn("README.md", names)

    def test_each_skill_is_kept_also_a_forwarder(self):
        _, names = self.names()
        self.assertIn("skills/old-name/SKILL.md", names)
        self.assertIn("skills/draft-story/references/rules.md", names)

    def test_the_files_are_unchanged(self):
        self.names()
        with zipfile.ZipFile(self.root / "dist" / "demo.zip") as archive:
            self.assertEqual(archive.read("skills/draft-story/SKILL.md").decode("utf-8"), NORMAL)
            self.assertEqual(archive.read("skills/draft-story/scripts/check.py"), b"print('ok')\n")

    def test_shared_pycache_and_pyc_are_left_out(self):
        _, names = self.names()
        bad = [n for n in names if n.startswith("shared/") or "__pycache__" in n or n.endswith(".pyc")]
        self.assertEqual(bad, [])

    def test_a_too_long_description_fails(self):
        text = NORMAL.replace("Writes a user story.", "x" * 1100)
        (self.normal / "SKILL.md").write_text(text, encoding="utf-8")
        code, output = run(["--root", str(self.root), "--plugin", "demo", "--out", "dist"])
        self.assertEqual(code, 1)
        self.assertIn("description has", output)
        self.assertFalse((self.root / "dist").exists())

    def test_a_description_of_500_characters_passes(self):
        text = NORMAL.replace("Writes a user story.", "x" * 500)
        (self.normal / "SKILL.md").write_text(text, encoding="utf-8")
        code, output = run(["--root", str(self.root), "--plugin", "demo", "--check"])
        self.assertEqual(code, 0, output)

    def test_a_name_that_is_not_the_folder_name_fails(self):
        text = NORMAL.replace("name: draft-story", "name: Draft_Story")
        (self.normal / "SKILL.md").write_text(text, encoding="utf-8")
        code, output = run(["--root", str(self.root), "--plugin", "demo", "--check"])
        self.assertEqual(code, 1)
        self.assertIn("ERROR draft-story", output)

    def test_a_plugin_without_a_manifest_fails(self):
        (self.plugin / ".claude-plugin" / "plugin.json").unlink()
        code, output = run(["--root", str(self.root), "--plugin", "demo", "--check"])
        self.assertEqual(code, 1)
        self.assertIn("plugin.json", output)

    def test_check_writes_no_zip(self):
        code, output = run(["--root", str(self.root), "--plugin", "demo", "--check"])
        self.assertEqual(code, 0, output)
        self.assertFalse((self.root / "dist").exists())

    def test_check_passes_on_the_real_product_flow_plugin(self):
        code, output = run(["--plugin", "product-flow", "--check"])
        self.assertEqual(code, 0, output)
        self.assertNotIn("ERROR", output)


if __name__ == "__main__":
    unittest.main()
