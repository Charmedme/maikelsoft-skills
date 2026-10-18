import json
import pathlib
import re
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[1]
RULE_FILES = sorted(ROOT.glob("plugins/dev-stack/skills/*/rules.json"))
KINDS = {"regex", "require", "package", "detect"}


class StackRulesTest(unittest.TestCase):
    def test_each_rules_file(self):
        ids = {}
        for path in RULE_FILES:
            data = json.loads(path.read_text(encoding="utf-8"))
            self.assertEqual(data.get("skill"), path.parent.name, f"{path}: skill must equal the folder name")
            for rule in data["rules"]:
                with self.subTest(file=path.parent.name, rule=rule.get("id")):
                    self.assertRegex(rule["id"], r"^[A-Z][A-Z0-9]*(-[A-Z0-9]+)+$")
                    self.assertNotIn(rule["id"], ids, f"duplicate id, also in {ids.get(rule['id'])}")
                    ids[rule["id"]] = path.parent.name
                    self.assertIn(rule.get("kind", "regex"), KINDS)
                    if rule.get("kind", "regex") != "detect":
                        self.assertTrue(rule.get("message"))
                    if rule.get("kind", "regex") in ("regex", "detect"):
                        pattern = re.compile(rule["pattern"], re.MULTILINE)
                        self.assertTrue(rule.get("match"), "needs match examples")
                        self.assertTrue(rule.get("nomatch"), "needs nomatch examples")
                        for line in rule["match"]:
                            self.assertRegex(line, pattern)
                        for line in rule["nomatch"]:
                            self.assertNotRegex(line, pattern)
                    if rule.get("kind") == "detect":
                        self.assertRegex(rule.get("decision", ""), r"^[A-Z]+-\d{2}$")


if __name__ == "__main__":
    unittest.main()
