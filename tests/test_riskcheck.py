"""Tests of shared/riskcheck.py on small real git repositories."""
import pathlib
import subprocess
import sys
import tempfile
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / "shared"))
import riskcheck  # noqa: E402


class Repo:
    def __init__(self, folder, branch="main"):
        self.dir = pathlib.Path(folder)
        self.git("init", "-q", "-b", branch)
        self.commit("a.txt", "a\n")

    def git(self, *args):
        cmd = ["git", "-c", "user.name=t", "-c", "user.email=t@e", "-c", "commit.gpgsign=false", "-C", str(self.dir), *args]
        return subprocess.run(cmd, check=True, capture_output=True, text=True).stdout.strip()

    def commit(self, path, text):
        file = self.dir / path
        file.parent.mkdir(parents=True, exist_ok=True)
        file.write_text(text)
        self.git("add", "-A")
        self.git("commit", "-qm", path)
        return self.git("rev-parse", "HEAD")

    def risk(self, base, default=None):
        return riskcheck.run(str(self.dir), base, default)


class RepoCase(unittest.TestCase):
    def setUp(self):
        tmp = tempfile.TemporaryDirectory()
        self.addCleanup(tmp.cleanup)
        self.repo = Repo(tmp.name)
        self.base = self.repo.git("rev-parse", "HEAD")

    def story(self):
        self.repo.git("switch", "-qc", "story")
        self.repo.commit("story.py", "x\n")

    def parent_and_story(self):
        self.repo.git("switch", "-qc", "parent")
        parent = self.repo.commit("Auth.py", "p\n")
        self.story()
        return parent


class ComparePoint(RepoCase):
    def test_normal_branch_uses_base(self):
        self.story()
        result = self.repo.risk(self.base)
        self.assertEqual(result["point"], self.base)
        self.assertEqual(result["files"], ["story.py"])

    def test_merge_of_main_uses_the_merge_base(self):
        self.story()
        self.repo.git("switch", "-q", "main")
        tip = self.repo.commit("Auth.py", "m\n")
        self.repo.git("switch", "-q", "story")
        self.repo.git("merge", "-q", "--no-edit", "main")
        result = self.repo.risk(self.base)
        self.assertEqual(result["point"], tip)
        self.assertEqual((result["files"], result["level"]), (["story.py"], "normal"))

    def test_rebase_uses_the_merge_base(self):
        self.story()
        self.repo.git("switch", "-q", "main")
        tip = self.repo.commit("Auth.py", "m\n")
        self.repo.git("switch", "-q", "story")
        self.repo.git("rebase", "-q", "main")
        self.assertEqual(self.repo.risk(self.base)["point"], tip)

    def test_open_stacked_parent_keeps_base(self):
        parent = self.parent_and_story()
        result = self.repo.risk(parent)
        self.assertEqual((result["point"], result["level"]), (parent, "normal"))

    def test_squash_merged_parent_merged_into_story(self):
        parent = self.parent_and_story()
        self.repo.git("switch", "-q", "main")
        self.repo.git("merge", "-q", "--squash", "parent")
        self.repo.git("commit", "-qm", "squash")
        squash = self.repo.git("rev-parse", "HEAD")
        self.repo.git("switch", "-q", "story")
        self.repo.git("merge", "-q", "--no-edit", "main")
        result = self.repo.risk(parent)
        self.assertEqual((result["point"], result["files"]), (squash, ["story.py"]))

    def test_work_already_on_main_uses_base_and_is_not_a_false_normal(self):
        self.repo.commit("auth/Auth.py", "x\n")
        result = self.repo.risk(self.base)
        self.assertEqual((result["point"], result["level"]), (self.base, "high"))

    def test_base_at_head_on_main_exits_2(self):
        self.assertEqual(riskcheck.main(["--root", str(self.repo.dir), "--base", self.base]), 2)

    def test_missing_base_exits_2(self):
        self.story()
        self.assertEqual(riskcheck.main(["--root", str(self.repo.dir), "--base", "deadbeef"]), 2)

    def test_no_default_branch_exits_2(self):
        self.repo.git("branch", "-m", "main", "develop")
        self.story()
        self.assertEqual(riskcheck.main(["--root", str(self.repo.dir), "--base", self.base]), 2)
        self.assertEqual(self.repo.risk(self.base, "develop")["level"], "normal")

    def test_local_main_ahead_of_origin_wins(self):
        self.repo.git("update-ref", "refs/remotes/origin/main", self.base)
        self.repo.git("symbolic-ref", "refs/remotes/origin/HEAD", "refs/remotes/origin/main")
        self.story()
        self.repo.git("switch", "-q", "main")
        tip = self.repo.commit("Auth.py", "m\n")
        self.repo.git("switch", "-q", "story")
        self.repo.git("merge", "-q", "--no-edit", "main")
        result = self.repo.risk(self.base)
        self.assertEqual((result["point"], result["level"]), (tip, "normal"))


class Rules(RepoCase):
    def change(self, path, old, new):
        base = self.repo.commit(path, old)
        self.repo.git("switch", "-qc", "s")
        self.repo.commit(path, new)
        return self.repo.risk(base)

    def test_path_names(self):
        cases = [("Shop.Modules.Identity/X.cs", True), ("AuthController.cs", True), ("AuthorService.cs", False),
                 ("src/SignIn.tsx", True), ("OAuthHandler.cs", True), ("JWTService.cs", True), ("tests/AuthTests.cs", False),
                 ("docs/auth.md", False), ("db/Migrations/1.cs", True), (".github/workflows/ci.yml", True), ("run.log", False)]
        for path, high in cases:
            self.assertEqual(bool(riskcheck.path_reasons(path, [])), high, path)

    def test_minor_version_is_normal(self):
        line = '<PackageReference Include="A" Version="%s" />\n'
        self.assertEqual(self.change("p.csproj", line % "1.2.0", line % "1.3.0")["level"], "normal")

    def test_major_version_is_high(self):
        line = '<PackageReference Include="A" Version="%s" />\n'
        self.assertEqual(self.change("p.csproj", line % "1.2.0", line % "2.0.0")["reasons"], [("major-version", "p.csproj")])

    def test_new_package_is_high(self):
        result = self.change("p.csproj", "", '<PackageReference Include="B" Version="1.0.0" />\n')
        self.assertEqual(result["reasons"], [("new-package", "p.csproj")])

    def test_package_that_the_repository_has_is_not_new(self):
        self.repo.commit("old.csproj", '<PackageReference Include="B" Version="1.0.0" />\n')
        self.assertEqual(self.change("p.csproj", "", '<PackageReference Include="B" Version="1.0.0" />\n')["level"], "normal")

    def test_anonymous_in_code_and_not_in_a_comment(self):
        self.assertEqual(self.change("a.cs", "", "[AllowAnonymous]\n")["reasons"], [("anonymous", "a.cs")])

    def test_anonymous_in_a_comment_is_normal(self):
        self.assertEqual(self.change("a.cs", "", "// [AllowAnonymous] was here\n")["level"], "normal")

    def test_guide_file_gives_a_line_and_no_raise(self):
        result = self.change("CLAUDE.md", "a\n", "b\n")
        self.assertEqual((result["level"], result["guide"]), ("normal", ["CLAUDE.md"]))

    def test_review_md_comes_from_the_compare_point(self):
        base = self.repo.commit("REVIEW.md", "# R\n\n## Sensitive paths\nsrc/pay/\n")
        self.repo.git("switch", "-qc", "story")
        self.repo.commit("REVIEW.md", "# R\n")
        self.repo.commit("src/pay/x.py", "1\n")
        self.assertIn(("sensitive-path", "src/pay/x.py"), self.repo.risk(base)["reasons"])

    def test_test_edits_are_reported_and_do_not_raise(self):
        result = self.change("tests/t.py", "self.assertEqual(a,1)\nself.assertEqual(b,2)\n", "self.assertEqual(a,1)\n@unittest.skip('x')\n")
        self.assertEqual((len(result["lost"]), len(result["skips"]), result["level"]), (1, 1, "normal"))

    def test_reformatted_assertion_is_not_lost(self):
        self.assertEqual(self.change("tests/t.py", "self.assertEqual(a, 1)\n", "self.assertEqual(a,1)\n")["lost"], [])

    def test_since_prints_only_new_reasons(self):
        result = self.change("auth/a.py", "1\n", "2\n")
        self.assertIn("new reason: auth-name auth/a.py", riskcheck.render(result, []))
        self.assertNotIn("new reason:", riskcheck.render(result, ["reason: auth-name auth/a.py"]))


if __name__ == "__main__":
    unittest.main()
