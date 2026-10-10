"""Tests for skillcheck.py of the build-skill skill. Run: python3 -m unittest discover -s tests"""
import pathlib
import sys
import tempfile
import unittest

SCRIPTS = pathlib.Path(__file__).resolve().parents[1] / "plugins" / "dev-build" / "skills" / "build-skill" / "scripts"
sys.path.insert(0, str(SCRIPTS))
import skillcheck  # noqa: E402

GOOD = "---\nname: my-skill\ndescription: Checks things. Use when the user wants things checked.\n---\n\n# My skill\n\nDo it.\n"


def make(files, name="my-skill"):
    tmp = tempfile.TemporaryDirectory()
    folder = pathlib.Path(tmp.name) / name
    for rel, content in files.items():
        p = folder / rel
        p.parent.mkdir(parents=True, exist_ok=True)
        p.write_text(content, encoding="utf-8")
    return tmp, folder


def rules(folder, severity=None):
    return {r["rule"] for r in skillcheck.check(folder) if severity in (None, r["severity"])}


class SkillCheck(unittest.TestCase):
    def test_good_skill_has_no_errors(self):
        tmp, f = make({"SKILL.md": GOOD, "references/language.md": "x"})
        self.assertEqual(rules(f, "error"), set())
        self.assertEqual(rules(f), set())

    def test_missing_skill_md(self):
        tmp, f = make({"other.md": "x"})
        self.assertIn("SK-FILE", rules(f, "error"))

    def test_name_must_match_folder_and_charset(self):
        tmp, f = make({"SKILL.md": GOOD.replace("my-skill", "My_Skill")})
        self.assertIn("SK-NAME", rules(f, "error"))
        tmp2, f2 = make({"SKILL.md": GOOD}, name="other")
        self.assertIn("SK-NAME", rules(f2, "error"))

    def test_reserved_word_in_name(self):
        tmp, f = make({"SKILL.md": GOOD.replace("my-skill", "claude-helper")}, name="claude-helper")
        self.assertIn("SK-NAME", rules(f, "error"))

    def test_description_too_long_and_tags(self):
        long = GOOD.replace("Checks things.", "x" * 1100)
        tmp, f = make({"SKILL.md": long})
        self.assertIn("SK-DESC", rules(f, "error"))
        tmp2, f2 = make({"SKILL.md": GOOD.replace("Checks things.", "Checks <b>things</b>.")})
        self.assertIn("SK-DESC", rules(f2, "error"))

    def test_first_person_and_missing_trigger(self):
        tmp, f = make({"SKILL.md": "---\nname: my-skill\ndescription: I help with things.\n---\nbody\n"})
        r = rules(f, "warning")
        self.assertIn("SK-DESC3", r)
        self.assertIn("SK-TRIG", r)

    def test_unknown_and_cc_fields(self):
        tmp, f = make({"SKILL.md": GOOD.replace("---\n\n#", "foo: 1\nmodel: haiku\n---\n\n#", 1)})
        r = rules(f, "warning")
        self.assertIn("SK-FIELD", r)
        self.assertIn("SK-PORT", r)

    def test_length_limits(self):
        tmp, f = make({"SKILL.md": GOOD + "line\n" * 320})
        self.assertIn("SK-LEN", rules(f, "warning"))
        tmp2, f2 = make({"SKILL.md": GOOD + "line\n" * 520})
        self.assertIn("SK-LEN", rules(f2, "error"))

    def test_em_dash_and_caps(self):
        tmp, f = make({"SKILL.md": GOOD + "Do this — now.\nYou MUST do it.\n"})
        self.assertIn("HOUSE-DASH", rules(f, "error"))
        self.assertIn("SK-CAPS", rules(f, "warning"))

    def test_broken_link_and_deep_reference(self):
        tmp, f = make({"SKILL.md": GOOD + "See [a](references/a.md) and [b](missing.md).\n",
                       "references/a.md": "See [c](c.md).\n", "references/c.md": "x"})
        r = rules(f)
        self.assertIn("SK-LINK", r)
        self.assertIn("SK-DEEP", r)

    def test_missing_script(self):
        tmp, f = make({"SKILL.md": GOOD + "Run scripts/nope.py now.\n"})
        self.assertIn("SK-SCRIPT", rules(f, "error"))

    def test_long_reference_needs_contents(self):
        tmp, f = make({"SKILL.md": GOOD + "[a](references/a.md)\n", "references/a.md": "line\n" * 120})
        self.assertIn("SK-TOC", rules(f, "warning"))


if __name__ == "__main__":
    unittest.main()
