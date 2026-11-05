"""Tests for shared/specsync.py. Run: python3 -m unittest discover -s tests"""
import contextlib
import importlib.util
import io
import json
import pathlib
import tempfile
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[1]
SCRIPT = ROOT / "shared" / "specsync.py"
spec = importlib.util.spec_from_file_location("specsync", SCRIPT)
ss = importlib.util.module_from_spec(spec)
spec.loader.exec_module(ss)

FIX = ROOT / "tests" / "fixtures" / "specsync"
STORY = {name: (FIX / name).read_text(encoding="utf-8") for name in ("story.md", "story.html", "story.adf.json")}
BUG = (FIX / "bug.md").read_text(encoding="utf-8")

DUTCH = """## Gedragsspecificatie

Functionaliteit: Gratis retour

  Regel: R1 Gratis retour binnen 30 dagen (limit: 30 dagen)
    Voorbeeld: Retour onder de grens
      Gegeven een bestelling die 29 dagen geleden is bezorgd
      Als de klant de bestelling terugstuurt
      Dan rekent de winkel "0,00" voor de retour
    Voorbeeld: Retour op de grens
      Stel een bestelling die 30 dagen geleden is bezorgd
      Wanneer de klant de bestelling terugstuurt
      Dan rekent de winkel "0,00" voor de retour
      En krijgt de klant een bevestiging
    Abstract Scenario: Retour boven de grens
      Gegeven een bestelling die <dagen> dagen geleden is bezorgd
      Als de klant de bestelling terugstuurt
      Dan rekent de winkel 4,95 voor de retour
      Voorbeelden:
        | dagen |
        | 31 |
"""


def ids(found):
    return {f[1] for f in found}


def run(argv):
    out = io.StringIO()
    with contextlib.redirect_stdout(out):
        code = ss.main(argv)
    return code, out.getvalue()


def bad(text):
    """Check a small spec in Markdown with a section heading."""
    return ss.check(ss.read_spec("## Behaviour spec\n" + text, "md"))[0]


GOOD_RULE = """Rule: R1 Guest checkout
  Scenario: Guest pays
    Given a guest with 1 product in the cart
    When the guest pays with a valid card
    Then the shop creates the order
"""


class Formats(unittest.TestCase):
    def test_detect(self):
        self.assertEqual(ss.detect(STORY["story.adf.json"]), "adf")
        self.assertEqual(ss.detect(STORY["story.html"]), "html")
        self.assertEqual(ss.detect(STORY["story.md"]), "md")

    def test_same_canonical_text_and_fingerprints(self):
        lines = {n: ss.read_spec(t) for n, t in STORY.items()}
        self.assertEqual(lines["story.md"], lines["story.html"])
        self.assertEqual(lines["story.md"], lines["story.adf.json"])
        prints = {n: ss.fingerprints(l, ss.parse(l)) for n, l in lines.items()}
        self.assertEqual(prints["story.md"], prints["story.html"])
        self.assertEqual(prints["story.md"], prints["story.adf.json"])
        self.assertEqual([r["id"] for r in prints["story.md"]["rules"]], ["R1", "R2"])

    def test_canonical_text_is_plain(self):
        text = "\n".join(ss.read_spec(STORY["story.html"]))
        for mark in ("<div>", "&nbsp;", "&amp;", " ", "“", "  "):
            self.assertNotIn(mark, text)
        self.assertIn('Then the shop charges "0.00" for the return', text)
        self.assertIn("| anne@example | Enter a valid email address & retry |", text)
        self.assertNotIn("Links", text)
        self.assertNotIn("As a customer", text)

    def test_whole_field(self):
        field = "<div>Rule: R1 Guest checkout</div><div>Scenario: Guest pays</div><div>Given a guest</div>" \
                "<div>When the guest pays</div><div>Then the shop creates the order</div>"
        self.assertEqual(ss.check(ss.read_spec(field))[0][0][1], "SPEC-SECTION")
        found, parsed = ss.check(ss.read_spec(field, whole=True))
        self.assertEqual(found, [])
        self.assertEqual(parsed["rules"][0]["id"], "R1")

    def test_dutch_keywords(self):
        found, parsed = ss.check(ss.read_spec(DUTCH))
        self.assertEqual(found, [])
        rule = parsed["rules"][0]
        self.assertEqual((rule["id"], rule["limit"], rule["count"]), ("R1", "30 dagen", 3))


class Checks(unittest.TestCase):
    def test_good_fixtures_have_no_errors(self):
        for text in STORY.values():
            self.assertEqual(ss.check(ss.read_spec(text))[0], [])

    def test_section(self):
        text = (FIX / "no-section.md").read_text(encoding="utf-8")
        self.assertEqual(ids(ss.check(ss.read_spec(text))[0]), {"SPEC-SECTION"})

    def test_rule_id(self):
        self.assertIn("SPEC-RULE-ID", ids(bad(GOOD_RULE.replace("R1 ", ""))))
        self.assertIn("SPEC-RULE-ID", ids(bad(GOOD_RULE + GOOD_RULE)))
        self.assertIn("SPEC-RULE-ID", ids(bad(GOOD_RULE.split("\n", 1)[1])))
        self.assertEqual(bad(GOOD_RULE), [])

    def test_too_many_rules(self):
        text = (FIX / "too-many-rules.md").read_text(encoding="utf-8")
        self.assertEqual(ids(ss.check(ss.read_spec(text))[0]), {"SPEC-RULES"})

    def test_scenarios(self):
        text = (FIX / "too-many-scenarios.md").read_text(encoding="utf-8")
        found = ss.check(ss.read_spec(text))[0]
        self.assertEqual(ids(found), {"SPEC-SCENARIOS"})
        self.assertIn("5 scenarios", found[0][2])
        self.assertIn("SPEC-SCENARIOS", ids(bad("Rule: R1 Empty rule\n")))

    def test_steps(self):
        self.assertEqual(ids(bad(GOOD_RULE.replace("When the guest", "And the guest"))), {"SPEC-STEPS"})
        self.assertEqual(ids(bad(GOOD_RULE.replace("Then the shop", "And the shop"))), {"SPEC-STEPS"})
        self.assertEqual(bad(GOOD_RULE.replace("    Given a guest with 1 product in the cart\n", "")), [])

    def test_limit(self):
        md = STORY["story.md"].replace("Return at the limit", "Return on the last day")
        found = ss.check(ss.read_spec(md))[0]
        self.assertEqual(ids(found), {"SPEC-LIMIT"})
        self.assertIn("no scenario: at", found[0][2])

    def test_bug_spec(self):
        self.assertEqual(ss.check(ss.read_spec(BUG), "bug")[0], [])
        self.assertEqual(ss.check(ss.read_spec(BUG), "story")[0], [])
        no_now = "\n".join(l for l in BUG.splitlines() if "Now:" not in l)
        self.assertEqual(ids(ss.check(ss.read_spec(no_now), "bug")[0]), {"SPEC-BUG-NOW"})
        no_unchanged = BUG.replace("R2 Unchanged", "R2 Return on day 10")
        self.assertEqual(ids(ss.check(ss.read_spec(no_unchanged), "bug")[0]), {"SPEC-BUG-UNCHANGED"})
        self.assertEqual(ids(ss.check(ss.read_spec(STORY["story.md"]), "bug")[0]),
                         {"SPEC-BUG-NOW", "SPEC-BUG-UNCHANGED"})

    def test_check_command(self):
        code, out = run(["check", str(FIX / "story.html"), "--format", "json"])
        self.assertEqual(code, 0)
        self.assertEqual(json.loads(out)["errors"], [])
        code, out = run(["check", str(FIX / "too-many-rules.md")])
        self.assertEqual(code, 1)
        self.assertIn("SPEC-RULES", out)


class CopyAndDiff(unittest.TestCase):
    def setUp(self):
        self.tmp = tempfile.TemporaryDirectory()
        self.dir = pathlib.Path(self.tmp.name)
        self.out = self.dir / "work" / "SHOP-12" / "spec.md"

    def tearDown(self):
        self.tmp.cleanup()

    def copy(self, name="story.md"):
        return run(["copy", str(FIX / name), "--item", "SHOP-12", "--out", str(self.out)])[0]

    def diff(self, text, name="ticket.txt"):
        path = self.dir / name
        path.write_text(text, encoding="utf-8")
        code, out = run(["diff", str(path), "--baseline", str(self.out.parent), "--format", "json"])
        return code, json.loads(out)

    def test_copy_writes_block_and_baseline(self):
        self.assertEqual(self.copy(), 0)
        text = self.out.read_text(encoding="utf-8")
        self.assertTrue(text.startswith("# Spec: SHOP-12\n"))
        self.assertIn("<!-- BEGIN behaviour spec: SHOP-12 -->", text)
        self.assertIn("<!-- END behaviour spec -->", text)
        base = json.loads((self.out.parent / "spec-baseline.json").read_text(encoding="utf-8"))
        self.assertEqual(base["item"], "SHOP-12")
        self.assertTrue(base["time"].endswith("Z"))
        self.assertEqual([r["id"] for r in base["rules"]], ["R1", "R2"])
        for r in base["rules"]:
            self.assertNotIn(r["hash"], text)

    def test_copy_keeps_text_outside_block(self):
        self.copy()
        text = self.out.read_text(encoding="utf-8")
        self.out.write_text(text + "\n## Design rules\n\n- Keep the fee in one place.\n", encoding="utf-8")
        self.copy("bug.md")
        text = self.out.read_text(encoding="utf-8")
        self.assertIn("## Design rules", text)
        self.assertIn("Keep the fee in one place.", text)
        self.assertEqual(text.count("BEGIN behaviour spec"), 1)
        self.assertIn("Now: the shop charges", text)

    def test_copy_refuses_on_error(self):
        code = self.copy("too-many-rules.md")
        self.assertEqual(code, 1)
        self.assertFalse(self.out.exists())

    def test_no_change_across_formats(self):
        self.copy("story.md")
        for name in ("story.html", "story.adf.json"):
            code, out = self.diff(STORY[name])
            self.assertEqual(code, 0, name)
            self.assertFalse(out["changed"] or out["added"] or out["removed"] or out["section_changed"])

    def test_one_changed_rule(self):
        self.copy("story.md")
        for name, text in STORY.items():
            code, out = self.diff(text.replace("31 days ago", "32 days ago"))
            self.assertEqual(code, 1, name)
            self.assertEqual([e["id"] for e in out["changed"]], ["R1"], name)
            self.assertEqual(out["added"] + out["removed"], [])
            self.assertIn("-Given an order that was delivered 31 days ago", out["changed"][0]["diff"])
            self.assertIn("+Given an order that was delivered 32 days ago", out["changed"][0]["diff"])

    def test_added_and_removed_rule(self):
        self.copy("story.md")
        code, out = self.diff(STORY["story.md"].replace("R2 Guest checkout", "R3 Guest checkout"))
        self.assertEqual(code, 1)
        self.assertEqual([e["id"] for e in out["added"]], ["R3"])
        self.assertEqual([e["id"] for e in out["removed"]], ["R2"])
        self.assertIn("-Scenario: Guest pays without an account", out["removed"][0]["diff"])

    def test_change_outside_section_is_no_change(self):
        self.copy("story.md")
        text = STORY["story.md"].replace("by the agent.", "by the agent on 2026-11-04.")
        text = text.replace("with less risk.", "with less risk and more trust.")
        self.assertEqual(self.diff(text)[0], 0)

    def test_markup_change_is_no_change(self):
        self.copy("story.md")
        html = STORY["story.html"].replace("<div>Scenario: Guest pays", "<p>Scenario:&nbsp; Guest pays")
        html = html.replace("without an account</div>", "without an account</p>")
        html = html.replace("Free return within", "<i>Free</i> return&nbsp;within")
        html = html.replace("the shop creates the order", "the  shop &#99;reates the order")
        self.assertEqual(self.diff(html, "ticket.html")[0], 0)

    def test_text_change_in_section_outside_rules(self):
        self.copy("story.md")
        code, out = self.diff(STORY["story.md"].replace("Feature: Free returns", "Feature: Cheap returns"))
        self.assertEqual(code, 1)
        self.assertTrue(out["section_changed"])
        self.assertEqual(out["changed"], [])

    def test_diff_with_baseline_file(self):
        self.copy("story.md")
        code, out = run(["diff", str(FIX / "story.adf.json"), "--baseline",
                         str(self.out.parent / "spec-baseline.json")])
        self.assertEqual(code, 0)
        self.assertIn("no change", out)


if __name__ == "__main__":
    unittest.main()
