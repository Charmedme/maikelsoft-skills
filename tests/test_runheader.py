"""Tests for shared/runheader.py. Run: python3 -m unittest discover -s tests"""
import contextlib
import importlib.util
import io
import json
import pathlib
import tempfile
import unittest

SCRIPT = pathlib.Path(__file__).resolve().parents[1] / "shared" / "runheader.py"
spec = importlib.util.spec_from_file_location("runheader", SCRIPT)
rh = importlib.util.module_from_spec(spec)
spec.loader.exec_module(rh)


def call(*argv):
    out = io.StringIO()
    with contextlib.redirect_stdout(out), contextlib.redirect_stderr(io.StringIO()):
        code = rh.main(list(argv))
    return code, out.getvalue()


class RunHeader(unittest.TestCase):
    def setUp(self):
        self.tmp = tempfile.TemporaryDirectory()
        self.folder = pathlib.Path(self.tmp.name) / "SHOP-12"
        self.folder.mkdir()
        self.run = self.folder / "run.md"

    def tearDown(self):
        self.tmp.cleanup()

    def test_write_then_read(self):
        code, _ = call("write", str(self.run), "--flow", "solo", "--item", "SHOP-12", "--size", "small", "--step", "plan gate")
        self.assertEqual(code, 0)
        code, out = call("read", str(self.run), "--format", "json")
        self.assertEqual(code, 0)
        data = json.loads(out)
        self.assertEqual(data["fields"]["baseline"], "none")
        self.assertEqual(data["fields"]["size"], "small")

    def test_write_keeps_the_body(self):
        self.run.write_text("---\nflow: team\nitem: SHOP-12\nsize: large\nstep: G1\nbaseline: set\n---\n# Run\nMode: full\n",
                            encoding="utf-8")
        call("write", str(self.run), "--step", "G2")
        text = self.run.read_text(encoding="utf-8")
        self.assertIn("step: G2", text)
        self.assertIn("Mode: full", text)
        self.assertIn("baseline: set", text)

    def test_write_refuses_a_missing_field(self):
        code, _ = call("write", str(self.run), "--flow", "solo", "--item", "SHOP-12")
        self.assertEqual(code, 1)
        self.assertFalse(self.run.exists())

    def test_read_finds_a_bad_value_and_an_unknown_field(self):
        self.run.write_text("---\nflow: duo\nitem: X\nsize: small\nstep: s\nbaseline: none\ntier: small\n---\n", encoding="utf-8")
        code, out = call("read", str(self.run))
        self.assertEqual(code, 1)
        self.assertIn("'flow' is 'duo'", out)
        self.assertIn("'tier' is not a header field", out)

    def test_read_without_header_fails(self):
        self.run.write_text("# Run\n", encoding="utf-8")
        self.assertEqual(call("read", str(self.run))[0], 1)

    def test_adopt_old_team_run(self):
        self.run.write_text("# Run\nMode: light\n## Counters\n", encoding="utf-8")
        (self.folder / "story.md").write_text("# Story\n", encoding="utf-8")
        call("adopt", str(self.folder))
        fields, body = rh.split(self.run.read_text(encoding="utf-8"))
        self.assertEqual(fields["flow"], "team")
        self.assertEqual(fields["baseline"], "legacy")
        self.assertIn("Mode: light", body)

    def test_adopt_old_solo_run_reads_the_tier(self):
        (self.folder / "plan.md").write_text("# Plan\nTier: small bug. One method.\n", encoding="utf-8")
        call("adopt", str(self.folder))
        fields, _ = rh.split(self.run.read_text(encoding="utf-8"))
        self.assertEqual((fields["flow"], fields["size"], fields["item"]), ("solo", "small", "SHOP-12"))
        self.assertEqual(rh.errors(fields), [])

    def test_adopt_small_run_with_only_examples(self):
        (self.folder / "examples.md").write_text("# Examples\n", encoding="utf-8")
        call("adopt", str(self.folder))
        self.assertTrue(self.run.is_file())

    def test_adopt_leaves_a_new_run_alone(self):
        text = "---\nflow: solo\nitem: SHOP-12\nsize: small\nstep: s\nbaseline: set\n---\n# Run\n"
        self.run.write_text(text, encoding="utf-8")
        call("adopt", str(self.folder))
        self.assertEqual(self.run.read_text(encoding="utf-8"), text)

    def test_adopt_empty_folder_makes_nothing(self):
        call("adopt", str(self.folder))
        self.assertFalse(self.run.exists())


if __name__ == "__main__":
    unittest.main()
