"""Tests for shared/convert.py. Run: python3 -m unittest discover -s tests"""
import contextlib
import io
import pathlib
import shutil
import sys
import tempfile
import unittest

sys.path.insert(0, str(pathlib.Path(__file__).resolve().parents[1] / "shared"))
import convert  # noqa: E402


def which_from(installed):
    return lambda name: f"/usr/bin/{name}" if name in installed else None


class Commands(unittest.TestCase):
    def test_pdf_engine_order(self):
        self.assertEqual(convert.pdf_engine(which_from({"xelatex", "wkhtmltopdf"})), "wkhtmltopdf")
        self.assertIsNone(convert.pdf_engine(which_from(set())))

    def test_html_is_standalone_with_title(self):
        cmd = convert.command(pathlib.Path("docs/guide.md"), "html")
        self.assertEqual(cmd[:4], ["pandoc", "docs/guide.md", "-o", "docs/guide.html"])
        self.assertIn("--standalone", cmd)
        self.assertIn("pagetitle=guide", cmd)

    def test_pdf_uses_engine(self):
        self.assertIn("--pdf-engine=typst", convert.command(pathlib.Path("a.md"), "pdf", "typst"))


class Main(unittest.TestCase):
    def setUp(self):
        self.dir = tempfile.mkdtemp()
        self.src = pathlib.Path(self.dir) / "guide.md"
        self.src.write_text("# Guide\n\nText.\n")

    def tearDown(self):
        shutil.rmtree(self.dir)

    def run_main(self, argv, installed, returncode=0):
        calls = []

        def fake_run(cmd, **kwargs):
            calls.append(cmd)
            return type("R", (), {"returncode": returncode, "stderr": "boom"})()

        out = io.StringIO()
        with contextlib.redirect_stdout(out), contextlib.redirect_stderr(io.StringIO()):
            code = convert.main(argv, which=which_from(installed), run=fake_run)
        return code, calls, out.getvalue()

    def test_all_made(self):
        code, calls, out = self.run_main([str(self.src), "--to", "html,docx,pdf"], {"pandoc", "typst"})
        self.assertEqual(code, 0)
        self.assertEqual(len(calls), 3)
        self.assertIn("MADE", out)

    def test_missing_pandoc_gives_command_and_exit_2(self):
        code, calls, out = self.run_main([str(self.src), "--to", "docx"], set())
        self.assertEqual(code, 2)
        self.assertEqual(calls, [])
        self.assertIn("pandoc", out)
        self.assertIn("guide.docx", out)

    def test_missing_pdf_engine_still_makes_other_formats(self):
        code, calls, out = self.run_main([str(self.src), "--to", "docx,pdf"], {"pandoc"})
        self.assertEqual(code, 2)
        self.assertEqual(len(calls), 1)
        self.assertIn("NOT MADE pdf", out)

    def test_pandoc_error_exits_1(self):
        code, _, out = self.run_main([str(self.src), "--to", "html"], {"pandoc"}, returncode=3)
        self.assertEqual(code, 1)
        self.assertIn("FAILED html", out)

    def test_unknown_format_exits_2(self):
        code, calls, _ = self.run_main([str(self.src), "--to", "epub"], {"pandoc"})
        self.assertEqual(code, 2)
        self.assertEqual(calls, [])

    @unittest.skipUnless(shutil.which("pandoc"), "pandoc is not installed")
    def test_real_docx(self):
        code = convert.main([str(self.src), "--to", "docx"])
        self.assertEqual(code, 0)
        self.assertTrue(self.src.with_suffix(".docx").is_file())


if __name__ == "__main__":
    unittest.main()
