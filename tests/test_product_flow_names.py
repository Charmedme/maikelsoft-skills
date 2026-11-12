"""Tests for the command names of product-flow 2.0.0 and the forwarders of the old names.
Run: python3 -m unittest discover -s tests"""
import pathlib
import re
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[1]
SKILLS = ROOT / "plugins" / "product-flow" / "skills"
BUILT_IN = {"bug", "goal", "review", "plan", "init", "code-review", "security-review", "pr-comments", "design",
            "update", "run", "help", "clear", "compact", "config", "memory", "model", "agents", "hooks", "mcp",
            "plugin", "resume", "status", "setup"}
COMMANDS = {"to-backlog", "epic", "feature", "user-story", "bug-item", "refine", "prototype", "product-goal"}
FORWARDERS = {"idea-to-backlog": "to-backlog", "backlog-epic": "epic", "backlog-feature": "feature",
              "backlog-story": "user-story", "product-prototype": "prototype", "backlog-goal": "product-goal"}


def front(name):
    text = (SKILLS / name / "SKILL.md").read_text(encoding="utf-8")
    head = text.split("\n---", 1)[0]
    fields = dict(re.findall(r"^([a-z-]+):\s*(.*)$", head, re.M))
    return fields, text


class ProductFlowNames(unittest.TestCase):
    def test_each_command_exists_and_is_in_the_menu(self):
        for name in COMMANDS:
            fields, _ = front(name)
            self.assertEqual(fields["name"], name)
            self.assertNotEqual(fields.get("user-invocable"), "false", name)
            self.assertNotIn(name, BUILT_IN)

    def test_each_forwarder_points_to_one_command(self):
        for old, new in FORWARDERS.items():
            fields, text = front(old)
            self.assertEqual(fields.get("user-invocable"), "false", old)
            self.assertEqual(set(re.findall(r"product-flow:([a-z-]+)", text)), {new}, old)
            self.assertIn("$ARGUMENTS", text, old)
            self.assertIn(new, COMMANDS)
            self.assertEqual(sorted(p.name for p in (SKILLS / old).iterdir()), ["SKILL.md"], old)

    def test_no_skill_hands_over_to_a_forwarder(self):
        for skill in SKILLS.iterdir():
            if skill.name in FORWARDERS:
                continue
            for f in skill.rglob("*.md"):
                text = f.read_text(encoding="utf-8")
                for old in FORWARDERS:
                    self.assertNotIn(f"product-flow:{old}", text, f)
                    self.assertNotIn(f"`{old}`", text, f)

    def test_the_helpers_are_out_of_the_menu(self):
        for name in ("backlog-push", "example-mapping"):
            fields, _ = front(name)
            self.assertEqual(fields.get("user-invocable"), "false", name)


if __name__ == "__main__":
    unittest.main()
