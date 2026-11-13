"""Tests for the command names of ux-design 1.0.0 and dev-docs 2.0.0, and the forwarder of the old name.
Run: python3 -m unittest discover -s tests"""
import pathlib
import re
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[1]
BUILT_IN = {"bug", "goal", "review", "plan", "init", "code-review", "security-review", "pr-comments", "design",
            "update", "run", "help", "clear", "compact", "config", "memory", "model", "agents", "hooks", "mcp",
            "plugin", "resume", "status", "setup"}
COMMANDS = {"ux-design": {"ux-design", "ux-review"}, "dev-docs": {"update-docs"}}
OUT_OF_MENU = {"ux-design": {"ux-setup", "ux-practice"}}
FORWARDERS = {"dev-docs": {"doc-plan": "update-docs"}}


def front(plugin, name):
    text = (ROOT / "plugins" / plugin / "skills" / name / "SKILL.md").read_text(encoding="utf-8")
    head = text.split("\n---", 1)[0]
    fields = dict(re.findall(r"^([a-z-]+):\s*(.*)$", head, re.M))
    return fields, text


class V7Names(unittest.TestCase):
    def test_each_command_exists_and_is_in_the_menu(self):
        for plugin, names in COMMANDS.items():
            for name in names:
                fields, text = front(plugin, name)
                self.assertEqual(fields["name"], name)
                self.assertNotEqual(fields.get("user-invocable"), "false", name)
                self.assertNotIn(name, BUILT_IN)
                self.assertIn("Next:", text, name)

    def test_the_setup_step_and_the_method_are_out_of_the_menu(self):
        for plugin, names in OUT_OF_MENU.items():
            for name in names:
                fields, _ = front(plugin, name)
                self.assertEqual(fields.get("user-invocable"), "false", name)

    def test_each_forwarder_points_to_one_command(self):
        for plugin, pairs in FORWARDERS.items():
            for old, new in pairs.items():
                fields, text = front(plugin, old)
                self.assertEqual(fields.get("user-invocable"), "false", old)
                self.assertEqual(set(re.findall(rf"{plugin}:([a-z-]+)", text)), {new}, old)
                self.assertIn("$ARGUMENTS", text, old)
                self.assertIn(new, COMMANDS[plugin])
                folder = ROOT / "plugins" / plugin / "skills" / old
                self.assertEqual(sorted(p.name for p in folder.iterdir()), ["SKILL.md"], old)

    def test_no_file_hands_over_to_a_forwarder(self):
        for plugin, pairs in FORWARDERS.items():
            for path in (ROOT / "plugins").rglob("*.md"):
                if path.name in ("CHANGELOG.md", "README.md") or path.parent.name in pairs:
                    continue
                text = path.read_text(encoding="utf-8")
                for old in pairs:
                    self.assertNotIn(f"{plugin}:{old}", text, path)
                    self.assertNotIn(f"`{old}`", text, path)

    def test_the_commands_read_the_design_facts_in_delivery_md(self):
        for name in ("ux-design", "ux-review", "ux-setup"):
            _, text = front("ux-design", name)
            self.assertIn(".claude/rules/delivery.md", text, name)
        agent = (ROOT / "plugins" / "ux-design" / "agents" / "ux-designer.md").read_text(encoding="utf-8")
        self.assertIn(".claude/rules/delivery.md", agent)


if __name__ == "__main__":
    unittest.main()
