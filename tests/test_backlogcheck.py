import importlib.util
import pathlib
import tempfile
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[1]
SCRIPT = ROOT / "plugins/product-flow/shared/backlogcheck.py"
spec = importlib.util.spec_from_file_location("backlogcheck", SCRIPT)
bc = importlib.util.module_from_spec(spec)
spec.loader.exec_module(bc)

GOOD = """## Find an order by its number

**Type:** User Story
**Parent:** Order search

As a customer service agent, I want to find an order by its number, so that I can answer at once.

### Acceptance criteria

- [ ] A full order number shows the order.
- [ ] When no order matches, the agent sees "No orders found".
- [ ] Performance: the result shows in less than 2 seconds.
"""

BAD = """## Build the search API endpoint

**Type:** User Story

As a user, I want search and filters, so that I can search.

### Acceptance criteria

- Given an order, when I search, then I see it
- [ ] The search is fast.
- [ ] The user clicks the Search button.
"""

DUTCH = """## Een order vinden op nummer

**Type:** User Story
**Parent:** Zoeken

Als klantenservicemedewerker wil ik een order op nummer vinden, zodat ik de klant direct help.

### Acceptatiecriteria

- [ ] Als geen order past, ziet de medewerker "Geen orders gevonden".
"""


def run(text):
    with tempfile.NamedTemporaryFile("w", suffix=".md", delete=False, encoding="utf-8") as f:
        f.write(text)
    items, findings = bc.check_file(pathlib.Path(f.name))
    return items, {(x.severity, x.rule) for x in findings}


class BacklogCheckTest(unittest.TestCase):
    def test_good_story_has_no_findings(self):
        items, found = run(GOOD)
        self.assertEqual(len(items), 1)
        self.assertEqual(found, set())

    def test_as_the_role(self):
        items, found = run(GOOD.replace("As a", "As the", 1).replace("As an", "As the", 1))
        self.assertNotIn(("error", "S1"), found)

    def test_section_heading_is_not_an_item(self):
        nested = "## Backlog\n\nThe items.\n\n" + GOOD.replace("## ", "### ")
        items, found = run(nested)
        self.assertEqual(len(items), 1)

    def test_copied_feature_goal(self):
        def feature(title, goal):
            return (f"## {title}\n\n**Type:** Feature\n**Parent:** None\n\n### Goal\n\n{goal}\n"
                    "Measure: open question\nSupports: Customers order without help.\n\n### Scope\n\n- In: a\n- Out: b\n\n"
                    "### Acceptance criteria\n\n- [ ] The page shows the result.\n\n")
        same = feature("Product search", "Customers order without help.") + feature("Order history", "Customers order without help.")
        _, found = run(same)
        self.assertIn(("warning", "F2b"), found)
        _, found = run(feature("Product search", "Customers order without help."))
        self.assertIn(("warning", "F2b"), found)
        own = feature("Product search", "Customers find a product by name.") + feature("Order history", "Customers see what they ordered before.")
        _, found = run(own)
        self.assertNotIn(("warning", "F2b"), found)

    def test_open_question_in_criteria(self):
        text = GOOD.replace("### Acceptance criteria\n\n", "### Acceptance criteria\n\n- [ ] Open question: which statuses count?\n", 1)
        _, found = run(text)
        self.assertIn(("error", "A8"), found)

    def test_ui_word_in_title(self):
        _, found = run(GOOD.replace("## Find an order by its number", "## Order search wizard", 1))
        self.assertIn(("warning", "T3"), found)

    def test_open_question_needs_number_with_section(self):
        text = GOOD + "\n### Notes\n\n- Open question: which statuses count?\n\n## Open questions\n\n| No | Question |\n"
        _, found = run(text)
        self.assertIn(("warning", "OQ"), found)
        _, found = run(text.replace("- Open question: which", "- Open question: O1 which"))
        self.assertNotIn(("warning", "OQ"), found)

    def test_profile_switches_rules(self):
        text = GOOD.replace("### Acceptance criteria\n\n", "### Acceptance criteria\n\n- [ ] Given an order, when I search, then I see it.\n", 1)
        with tempfile.NamedTemporaryFile("w", suffix=".md", delete=False, encoding="utf-8") as f:
            f.write(text)
        prof = pathlib.Path(f.name).with_suffix(".profile.md")
        prof.write_text("# Team profile\n\n## Checker\ncriteria: scenarios\ntitle-pattern: ^Orders - .+\n", encoding="utf-8")
        _, plain = bc.check_file(pathlib.Path(f.name))
        _, profiled = bc.check_file(pathlib.Path(f.name), bc.read_profile(prof))
        self.assertIn("A5", {x.rule for x in plain})
        self.assertNotIn("A5", {x.rule for x in profiled})
        self.assertIn("TP", {x.rule for x in profiled})

    def test_bad_story(self):
        _, found = run(BAD)
        for expected in [("error", "S1"), ("error", "A5"), ("warning", "T2"), ("warning", "S2"),
                         ("warning", "A3"), ("warning", "A4"), ("warning", "PARENT")]:
            self.assertIn(expected, found)

    def test_dutch_condition_is_not_gherkin(self):
        _, found = run(DUTCH)
        self.assertEqual(found, set())

    def test_missing_criteria(self):
        _, found = run(GOOD.split("### Acceptance")[0])
        self.assertIn(("error", "A1"), found)

    def test_spike_needs_no_sentence(self):
        text = "## Select a payment provider\n\n**Type:** Spike\n**Parent:** None\n\n### Acceptance criteria\n\n- [ ] The team has a decision.\n"
        _, found = run(text)
        self.assertEqual(found, set())

    def test_qualified_user_is_warning(self):
        _, found = run(GOOD.replace("customer service agent", "signed-in user"))
        self.assertIn(("warning", "S1"), found)

    def test_english_of_is_not_dutch_or(self):
        _, found = run(GOOD.replace("find an order by its number", "find the products of a brand"))
        self.assertNotIn(("warning", "S2"), found)

    def test_short_is_vague(self):
        _, found = run(GOOD.replace("- [ ] A full order", "- [ ] A short message shows. A full order"))
        self.assertIn(("warning", "A3"), found)

    def test_good_feature(self):
        text = open(ROOT / "plugins/product-flow/skills/backlog-feature/references/feature-rules.md").read()
        _, found = run(text.split("```markdown")[1].split("```")[0])
        self.assertEqual(found, set())

    def test_feature_without_goal_and_scope(self):
        text = "## Product search\n\n**Type:** Feature\n**Parent:** None\n\n### Acceptance criteria\n\n- [ ] Search is fast.\n"
        _, found = run(text)
        for expected in [("error", "F2"), ("error", "F4"), ("warning", "A3")]:
            self.assertIn(expected, found)

    def test_story_without_edge_case(self):
        text = GOOD.replace('- [ ] When no order matches, the agent sees "No orders found".\n', "")
        _, found = run(text)
        self.assertIn(("warning", "A6"), found)

    def test_epic(self):
        good = ("## Customers manage their own account\n\n**Type:** Epic\n**Parent:** None\n\n### Outcome\n\n"
                "Fewer calls.\nMeasure: 20% fewer calls.\n\n### Scope\n\n- In: addresses\n- Out: invoices\n\n"
                "### Features\n\n1. Manage addresses\n2. Manage users\n")
        self.assertEqual(run(good)[1], set())
        self.assertIn(("warning", "E3"), run(good.replace("2. Manage users\n", ""))[1])

    def test_goals(self):
        pg = ("## More customers order without help\n\n**Type:** Product Goal\n**Time frame:** Q1 2027\n\n"
              "Measure: from 60% to 75%\n\n### Supports\n\n- Product search\n")
        self.assertEqual(run(pg)[1], set())
        sg = pg.replace("Product Goal", "Sprint Goal").replace("Measure:", "Check:")
        self.assertEqual(run(sg)[1], set())
        self.assertIn(("error", "SG2"), run(pg.replace("Product Goal", "Sprint Goal"))[1])


if __name__ == "__main__":
    unittest.main()
