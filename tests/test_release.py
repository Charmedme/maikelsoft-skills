"""Tests of release.py and cirun.py of the release-plugin skill."""
import json
import pathlib
import subprocess
import sys
import tempfile
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[1]
SCRIPTS = ROOT / "plugins/dev-build/skills/release-plugin/scripts"
sys.path.insert(0, str(SCRIPTS))
import cirun  # noqa: E402
import release  # noqa: E402

GIT = ["git", "-c", "user.name=T", "-c", "user.email=t@example.com", "-c", "commit.gpgsign=false",
       "-c", "init.defaultBranch=main"]

CHANGELOG = """# Changelog

## [Unreleased]
{unreleased}
## [1.0.0] - 2026-09-01

### Added

- First release.

[Unreleased]: https://github.com/o/r/compare/demo--v1.0.0...HEAD
[1.0.0]: https://github.com/o/r/releases/tag/demo--v1.0.0
"""


def make_repo(tmp, unreleased="\n### Added\n\n- A new skill.\n", readme_version="1.0.0"):
    root = pathlib.Path(tmp)
    (root / ".claude-plugin").mkdir()
    (root / ".claude-plugin/marketplace.json").write_text(json.dumps(
        {"name": "m", "owner": {"name": "o"}, "plugins": [{"name": "demo", "source": "./plugins/demo"}]}))
    plugin = root / "plugins/demo"
    (plugin / ".claude-plugin").mkdir(parents=True)
    (plugin / ".claude-plugin/plugin.json").write_text('{\n  "name": "demo",\n  "version": "1.0.0"\n}\n')
    (plugin / "CHANGELOG.md").write_text(CHANGELOG.format(unreleased=unreleased))
    (root / "README.md").write_text(f"| Plugin | Version |\n|---|---|\n| demo | {readme_version} |\n")
    subprocess.run([*GIT, "init", "-q"], cwd=root, check=True)
    subprocess.run([*GIT, "add", "-A"], cwd=root, check=True)
    subprocess.run([*GIT, "commit", "-qm", "init"], cwd=root, check=True)
    return root, plugin


class ProposeBump(unittest.TestCase):
    def test_rules(self):
        cases = [
            ({"Added": ["x"]}, (1, 0, 0), "minor"),
            ({"Fixed": ["x"]}, (1, 0, 0), "patch"),
            ({"Fixed": ["x"], "Security": ["y"]}, (1, 0, 0), "patch"),
            ({"Removed": ["x"]}, (1, 0, 0), "major"),
            ({"Changed": ["BREAKING: x"]}, (1, 0, 0), "major"),
            ({"Removed": ["x"]}, (0, 3, 0), "minor"),
            ({}, (1, 0, 0), None),
        ]
        for sections, version, want in cases:
            with self.subTest(sections=sections, version=version):
                self.assertEqual(release.propose_bump(version, sections)[0], want)

    def test_bumped(self):
        self.assertEqual(release.bumped((1, 4, 2), "major"), "2.0.0")
        self.assertEqual(release.bumped((1, 4, 2), "minor"), "1.5.0")
        self.assertEqual(release.bumped((1, 4, 2), "patch"), "1.4.3")


class Status(unittest.TestCase):
    def test_clean_repo_proposes_minor(self):
        with tempfile.TemporaryDirectory() as tmp:
            _, plugin = make_repo(tmp)
            s = release.status(plugin)
            self.assertEqual(s["problems"], [])
            self.assertEqual(s["proposed_version"], "1.1.0")

    def test_finds_old_tag_form_and_readme_mismatch(self):
        with tempfile.TemporaryDirectory() as tmp:
            _, plugin = make_repo(tmp, readme_version="0.9.0")
            text = (plugin / "CHANGELOG.md").read_text().replace("demo--v1.0.0", "demo-v1.0.0")
            (plugin / "CHANGELOG.md").write_text(text)
            problems = release.status(plugin)["problems"]
            self.assertTrue(any("tag form" in p for p in problems))
            self.assertTrue(any("README" in p for p in problems))

    def test_empty_unreleased(self):
        with tempfile.TemporaryDirectory() as tmp:
            _, plugin = make_repo(tmp, unreleased="\n")
            self.assertIsNone(release.status(plugin)["proposed_bump"])


class Apply(unittest.TestCase):
    def test_writes_all_files(self):
        with tempfile.TemporaryDirectory() as tmp:
            root, plugin = make_repo(tmp)
            release.apply(plugin, "1.1.0", "2026-10-13")
            self.assertIn('"version": "1.1.0"', (plugin / ".claude-plugin/plugin.json").read_text())
            text = (plugin / "CHANGELOG.md").read_text()
            self.assertIn("## [Unreleased]\n\n## [1.1.0] - 2026-10-13\n\n### Added\n\n- A new skill.", text)
            self.assertIn("[Unreleased]: https://github.com/o/r/compare/demo--v1.1.0...HEAD", text)
            self.assertIn("[1.1.0]: https://github.com/o/r/compare/demo--v1.0.0...demo--v1.1.0", text)
            self.assertIn("[1.0.0]: https://github.com/o/r/releases/tag/demo--v1.0.0", text)
            self.assertIn("| demo | 1.1.0 |", (root / "README.md").read_text())
            self.assertEqual(release.status(plugin)["problems"], [])

    def test_refuses_lower_version_and_empty_unreleased(self):
        with tempfile.TemporaryDirectory() as tmp:
            _, plugin = make_repo(tmp)
            with self.assertRaises(SystemExit):
                release.apply(plugin, "1.0.0", "2026-10-13")
        with tempfile.TemporaryDirectory() as tmp:
            _, plugin = make_repo(tmp, unreleased="\n")
            with self.assertRaises(SystemExit):
                release.apply(plugin, "1.1.0", "2026-10-13")


class BumpCheck(unittest.TestCase):
    def run_check(self, root):
        return subprocess.run([sys.executable, str(SCRIPTS / "release.py"), "bump-check", "--base", "main"],
                              cwd=root, capture_output=True, text=True)

    def test_change_without_version_fails_and_evals_pass(self):
        with tempfile.TemporaryDirectory() as tmp:
            root, plugin = make_repo(tmp)
            subprocess.run([*GIT, "checkout", "-qb", "feat"], cwd=root, check=True)
            (plugin / "evals").mkdir()
            (plugin / "evals/case.md").write_text("x")
            subprocess.run([*GIT, "add", "-A"], cwd=root, check=True)
            subprocess.run([*GIT, "commit", "-qm", "evals"], cwd=root, check=True)
            self.assertEqual(self.run_check(root).returncode, 0)
            (plugin / "README.md").write_text("changed")
            subprocess.run([*GIT, "add", "-A"], cwd=root, check=True)
            subprocess.run([*GIT, "commit", "-qm", "change"], cwd=root, check=True)
            self.assertEqual(self.run_check(root).returncode, 1)
            release.apply(plugin, "1.1.0", "2026-10-13")
            subprocess.run([*GIT, "commit", "-qam", "release"], cwd=root, check=True)
            self.assertEqual(self.run_check(root).returncode, 0)


class CiRun(unittest.TestCase):
    WORKFLOW = """name: CI
on:
  pull_request:
jobs:
  checks:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - name: One line
        run: echo one
      - name: Block
        run: |
          npm install -g tool
          echo two
          echo three
      - name: Expression
        run: echo ${{ github.base_ref }}
"""

    def test_steps(self):
        steps = list(cirun.steps_of(self.WORKFLOW))
        self.assertEqual([name for name, _ in steps], ["One line", "Block", "Expression"])
        self.assertEqual(steps[1][1], "npm install -g tool\necho two\necho three")
        self.assertTrue(cirun.INSTALL_LINE.match("npm install -g tool"))
        self.assertFalse(cirun.INSTALL_LINE.match("echo two"))

    def test_runs_on_pull_request(self):
        self.assertTrue(cirun.runs_on_pull_request(self.WORKFLOW))
        self.assertFalse(cirun.runs_on_pull_request("on:\n  push:\njobs:\n  a:\n"))


if __name__ == "__main__":
    unittest.main()
