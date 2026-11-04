"""Each plugin README has a section "Commands" that lists the command of each skill and the name of each agent."""
import pathlib
import re
import unittest

ROOT = pathlib.Path(__file__).resolve().parent.parent
PLUGINS = sorted(p for p in (ROOT / "plugins").iterdir() if (p / ".claude-plugin" / "plugin.json").exists())


def commands_section(readme: str) -> str:
    match = re.search(r"^## Commands\n(.*?)(?=^## |\Z)", readme, re.M | re.S)
    return match.group(1) if match else ""


class ReadmeCommandsTest(unittest.TestCase):
    def test_each_skill_and_agent_is_in_the_commands_section(self):
        for plugin in PLUGINS:
            with self.subTest(plugin=plugin.name):
                section = commands_section((plugin / "README.md").read_text(encoding="utf-8"))
                self.assertTrue(section, f"{plugin.name}/README.md has no section '## Commands'")
                for skill in sorted((plugin / "skills").glob("*/SKILL.md")):
                    command = f"`/{plugin.name}:{skill.parent.name}`"
                    self.assertIn(command, section, f"{command} is not in {plugin.name}/README.md")
                for agent in sorted((plugin / "agents").glob("*.md")):
                    name = f"`{plugin.name}:{agent.stem}`"
                    self.assertIn(name, section, f"agent {name} is not in {plugin.name}/README.md")

    def test_no_command_for_a_skill_that_does_not_exist(self):
        for plugin in PLUGINS:
            with self.subTest(plugin=plugin.name):
                section = commands_section((plugin / "README.md").read_text(encoding="utf-8"))
                skills = {s.parent.name for s in (plugin / "skills").glob("*/SKILL.md")}
                for name in re.findall(rf"`/{re.escape(plugin.name)}:([a-z0-9-]+)`", section):
                    self.assertIn(name, skills, f"/{plugin.name}:{name} has no skill folder")


if __name__ == "__main__":
    unittest.main()
