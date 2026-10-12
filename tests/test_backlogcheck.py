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


if __name__ == "__main__":
    unittest.main()
