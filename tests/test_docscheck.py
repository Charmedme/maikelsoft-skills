"""Tests for shared/docscheck.py. Run: python3 -m unittest discover -s tests"""
import json
import pathlib
import subprocess
import sys
import tempfile
import unittest

SCRIPT = pathlib.Path(__file__).resolve().parents[1] / "shared" / "docscheck.py"
GIT = ["git", "-c", "user.name=T", "-c", "user.email=t@example.com", "-c", "commit.gpgsign=false"]


def run(cmd, cwd):
    return subprocess.run(cmd, cwd=cwd, capture_output=True, text=True)


class DocsCheck(unittest.TestCase):
    def setUp(self):
        self.tmp = tempfile.TemporaryDirectory()
        self.repo = pathlib.Path(self.tmp.name)
        run(GIT + ["init", "-q"], self.repo)
        self.write("README.md", "# Shop\n\nRun `shop --export` to export.\n")
        self.write("docs/returns.md", "# Returns\n\nThe ReturnPolicy class sets RETURN_DAYS. See GET \"/api/returns/{id}\".\n")
        self.write("docs/other.md", "# Other\n\nNothing about the change.\n")
        self.write("CHANGELOG.md", "# Changelog\n")
        self.write("src/ReturnPolicy.cs", "class ReturnPolicy { int Days = 30; }\n")
        self.write("docs/work/SHOP-1/plan.md", "ReturnPolicy\n")
        run(GIT + ["add", "-A"], self.repo)
        run(GIT + ["commit", "-q", "-m", "base"], self.repo)
        self.base = run(["git", "rev-parse", "HEAD"], self.repo).stdout.strip()

    def tearDown(self):
        self.tmp.cleanup()

    def write(self, rel, text):
        p = self.repo / rel
        p.parent.mkdir(parents=True, exist_ok=True)
        p.write_text(text, encoding="utf-8")

    def check(self, *extra):
        r = run([sys.executable, str(SCRIPT), "--base", self.base, "--format", "json", *extra], self.repo)
        return r.returncode, json.loads(r.stdout)["findings"]

    def ids(self, findings):
        return sorted((f["id"], f["doc"]) for f in findings)

    def test_no_change_has_no_findings_except_changelog(self):
        code, findings = self.check()
        self.assertEqual(self.ids(findings), [("DOC-CHANGELOG", "CHANGELOG.md")])
        self.assertEqual(code, 1)

    def test_changed_code_names_the_doc(self):
        self.write("src/ReturnPolicy.cs", "class ReturnPolicy { int Days = 31; }\n")
        _, findings = self.check()
        names = [f for f in findings if f["id"] == "DOC-NAMES"]
        self.assertEqual([f["doc"] for f in names], ["docs/returns.md"])
        self.assertIn("ReturnPolicy", names[0]["names"])

    def test_option_setting_and_endpoint_in_a_changed_line(self):
        self.write("src/Cli.cs", 'var o = "--export"; var s = "RETURN_DAYS"; app.MapGet("/api/returns/{id}");\n')
        _, findings = self.check()
        docs = {f["doc"]: f["names"] for f in findings if f["id"] == "DOC-NAMES"}
        self.assertEqual(docs["README.md"], ["--export"])
        self.assertEqual(docs["docs/returns.md"], ["/api/returns/{id}", "RETURN_DAYS"])

    def test_updated_doc_and_changelog_pass(self):
        self.write("src/ReturnPolicy.cs", "class ReturnPolicy { int Days = 31; }\n")
        self.write("docs/returns.md", "# Returns\n\nThe ReturnPolicy class gives 31 days.\n")
        self.write("CHANGELOG.md", "# Changelog\n\n- Returns: 31 days.\n")
        code, findings = self.check()
        self.assertEqual((code, findings), (0, []))

    def test_ok_accepts_a_doc(self):
        self.write("src/ReturnPolicy.cs", "class ReturnPolicy { int Days = 31; }\n")
        _, findings = self.check("--ok", str(self.repo / "docs/returns.md"), str(self.repo / "CHANGELOG.md"))
        self.assertEqual(findings, [])

    def test_work_folder_is_not_a_doc(self):
        self.write("src/ReturnPolicy.cs", "class ReturnPolicy { int Days = 31; }\n")
        _, findings = self.check()
        self.assertNotIn("docs/work/SHOP-1/plan.md", [f["doc"] for f in findings])

    def test_no_readme_proposes_one(self):
        (self.repo / "README.md").unlink()
        _, findings = self.check()
        self.assertIn(("DOC-README", "README.md"), self.ids(findings))


if __name__ == "__main__":
    unittest.main()
