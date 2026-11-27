"""Tests for evals/dev-stack/prepare.py: the exclude list of a fixture and the npm folder of a case.
No network: npm is not called. Run: python3 -m unittest discover -s tests"""
import pathlib
import subprocess
import sys
import tempfile
import unittest
from unittest import mock

DEV_STACK = pathlib.Path(__file__).resolve().parents[1] / "evals" / "dev-stack"
sys.path.insert(0, str(DEV_STACK))
import prepare  # noqa: E402
import score  # noqa: E402
import selftest  # noqa: E402


def write(root, rel, text="x\n"):
    path = pathlib.Path(root, rel)
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(text, encoding="utf-8")


def fixture(root):
    """A small fixture with an exclude list for a file and a folder."""
    write(root, "app/README.md", "# App\n")
    write(root, "app/src/Program.cs", "class P {}\n")
    write(root, "app/LICENSE-upstream", "MIT\n")
    write(root, "app/notes/private/source.md", "upstream\n")
    write(root, "app/src/bin/Debug/app.dll", "binary\n")
    write(root, "app/.eval-exclude", "# not for the run agents\nLICENSE-upstream\n\nnotes/private\n")
    return pathlib.Path(root) / "app"


def tree(root):
    root = pathlib.Path(root)
    return sorted(p.relative_to(root).as_posix() for p in root.rglob("*") if p.is_file() and ".git" not in p.parts)


class ExcludeList(unittest.TestCase):
    def test_patterns_come_from_the_file_without_comments_and_blank_lines(self):
        with tempfile.TemporaryDirectory() as tmp:
            self.assertEqual(prepare.excluded(fixture(tmp)), [".eval-exclude", "LICENSE-upstream", "notes/private"])

    def test_a_fixture_without_the_file_excludes_only_the_file_name(self):
        with tempfile.TemporaryDirectory() as tmp:
            self.assertEqual(prepare.excluded(tmp), [".eval-exclude"])

    def test_a_pattern_matches_the_path_or_a_folder_above_it(self):
        patterns = ["LICENSE-*", "notes/private"]
        self.assertTrue(prepare.is_excluded("LICENSE-upstream", patterns))
        self.assertTrue(prepare.is_excluded("notes/private/a/b.md", patterns))
        self.assertFalse(prepare.is_excluded("notes/public.md", patterns))
        self.assertFalse(prepare.is_excluded("src/LICENSE-upstream", patterns))


class MakeRepo(unittest.TestCase):
    def test_the_run_repository_does_not_get_the_excluded_files(self):
        with tempfile.TemporaryDirectory() as tmp:
            fixture(tmp)
            repo = pathlib.Path(tmp) / "runs" / "repo"
            with mock.patch.object(prepare, "FIXTURES", pathlib.Path(tmp)):
                prepare.make_repo({"kind": "write", "fixture": "app"}, repo)
            self.assertEqual(tree(repo), ["README.md", "src/Program.cs"])
            log = subprocess.run(["git", "-C", str(repo), "ls-files"], capture_output=True, text=True).stdout.split()
            self.assertEqual(sorted(log), ["README.md", "src/Program.cs"])

    def test_npm_ci_runs_in_the_node_dir_of_the_case(self):
        calls = []
        real = subprocess.run

        def fake(cmd, *args, **kwargs):
            if cmd[0] == "npm":
                calls.append((cmd[:2], pathlib.Path(kwargs["cwd"])))
                return subprocess.CompletedProcess(cmd, 0, "", "")
            return real(cmd, *args, **kwargs)

        with tempfile.TemporaryDirectory() as tmp:
            write(tmp, "app/frontend/package.json", "{}\n")
            repo = pathlib.Path(tmp) / "repo"
            with mock.patch.object(prepare, "FIXTURES", pathlib.Path(tmp)), \
                    mock.patch.object(prepare.subprocess, "run", side_effect=fake):
                prepare.make_repo({"kind": "write", "fixture": "app", "stack": "kiosk", "node_dir": "frontend"}, repo)
        self.assertEqual(calls, [(["npm", "ci"], repo / "frontend")])

    def test_node_dir_defaults(self):
        self.assertEqual(prepare.node_dir({"stack": "angular"}), ".")
        self.assertEqual(prepare.node_dir({"stack": "kiosk", "node_dir": "frontend"}), "frontend")
        self.assertIsNone(prepare.node_dir({}))


class OtherCopies(unittest.TestCase):
    def test_selftest_copy_leaves_out_the_excluded_files(self):
        with tempfile.TemporaryDirectory() as tmp:
            fixture(tmp)
            dest = pathlib.Path(tmp) / "copy"
            with mock.patch.object(score, "FIXTURES", pathlib.Path(tmp)):
                selftest.build({"name": "none", "fixture": "app"}, "fixture", dest)
            self.assertEqual(tree(dest), ["README.md", "src/Program.cs"])

    def test_files_unchanged_does_not_count_excluded_files_as_removed(self):
        with tempfile.TemporaryDirectory() as tmp:
            fixture(tmp)
            repo = pathlib.Path(tmp) / "repo"
            with mock.patch.object(prepare, "FIXTURES", pathlib.Path(tmp)):
                prepare.make_repo({"kind": "write", "fixture": "app"}, repo)
            with mock.patch.object(score, "FIXTURES", pathlib.Path(tmp)):
                ok, got = score.check_unchanged(repo, {"fixture": "app"})
            self.assertTrue(ok, got)

    def test_files_unchanged_ignores_files_that_the_repository_ignores(self):
        with tempfile.TemporaryDirectory() as tmp:
            fixture(tmp)
            write(tmp, "app/.gitignore", "uploads/\n")
            repo = pathlib.Path(tmp) / "repo"
            with mock.patch.object(prepare, "FIXTURES", pathlib.Path(tmp)):
                prepare.make_repo({"kind": "write", "fixture": "app"}, repo)
            write(repo, "uploads/a.html", "test output\n")
            with mock.patch.object(score, "FIXTURES", pathlib.Path(tmp)):
                ok, got = score.check_unchanged(repo, {"fixture": "app"})
                self.assertTrue(ok, got)
                write(repo, "src/Extra.cs", "class E {}\n")
                ok, got = score.check_unchanged(repo, {"fixture": "app"})
                self.assertFalse(ok, got)
                self.assertEqual(got["added"], ["src/Extra.cs"])

    def test_kiosk_fixture_keeps_the_upstream_notice_out_of_runs(self):
        patterns = prepare.excluded(DEV_STACK / "fixtures" / "kiosk")
        self.assertTrue(prepare.is_excluded("LICENSE-juice-shop", patterns))
        self.assertTrue(prepare.is_excluded("README.md", patterns))
        readme = (DEV_STACK / "fixtures" / "kiosk" / "README.md").read_text(encoding="utf-8")
        self.assertNotIn("juice", readme.lower())


class OverlayFixture(unittest.TestCase):
    def test_overlay_copies_its_base_then_its_files_then_deletes(self):
        with tempfile.TemporaryDirectory() as tmp:
            fixtures = pathlib.Path(tmp) / "fixtures"
            fixture(fixtures)
            write(fixtures, "change/overlay.json", '{"from": "app", "delete": ["src/Program.cs"]}\n')
            write(fixtures, "change/src/New.cs", "class N {}\n")
            write(fixtures, "change/README.md", "# Changed\n")
            write(fixtures, "next/overlay.json", '{"from": "change"}\n')
            write(fixtures, "next/docs/api.yaml", "openapi: 3.1.2\n")
            dest = pathlib.Path(tmp) / "out"
            dest.mkdir()
            with mock.patch.object(prepare, "FIXTURES", fixtures):
                prepare.copy_fixture("next", dest)
            self.assertEqual(tree(dest), ["README.md", "docs/api.yaml", "src/New.cs"])
            self.assertEqual((dest / "README.md").read_text(encoding="utf-8"), "# Changed\n")

    def test_files_unchanged_compares_with_the_built_overlay(self):
        with tempfile.TemporaryDirectory() as tmp:
            fixtures = pathlib.Path(tmp) / "fixtures"
            fixture(fixtures)
            write(fixtures, "change/overlay.json", '{"from": "app"}\n')
            write(fixtures, "change/src/New.cs", "class N {}\n")
            repo = pathlib.Path(tmp) / "repo"
            repo.mkdir()
            with mock.patch.object(prepare, "FIXTURES", fixtures), mock.patch.object(score, "FIXTURES", fixtures), \
                    mock.patch.dict(score._CACHE, clear=True):
                prepare.copy_fixture("change", repo)
                ok, got = score.check_unchanged(repo, {"fixture": "change"})
            self.assertTrue(ok, got)


if __name__ == "__main__":
    unittest.main()
