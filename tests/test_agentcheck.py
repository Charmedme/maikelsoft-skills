"""Tests for agentcheck.py of the build-agent skill. Run: python3 -m unittest discover -s tests"""
import pathlib
import sys
import tempfile
import unittest

SCRIPTS = pathlib.Path(__file__).resolve().parents[1] / "plugins" / "dev-build" / "skills" / "build-agent" / "scripts"
sys.path.insert(0, str(SCRIPTS))
import agentcheck  # noqa: E402

GOOD = "---\nname: code-reviewer\ndescription: Reviews code for security issues. Use when code changed.\ntools: Read, Grep, Glob\nmodel: sonnet\n---\n\nYou review code. Report findings as a list.\n"


def run(files, runtime="claude-code", sub="a.md"):
    with tempfile.TemporaryDirectory() as tmp:
        root = pathlib.Path(tmp)
        for rel, content in files.items():
            p = root / rel
            p.parent.mkdir(parents=True, exist_ok=True)
            p.write_text(content, encoding="utf-8")
        out = []
        if runtime == "openclaw":
            agentcheck.check_openclaw(root, out)
        else:
            agentcheck.check_md(root / sub, runtime, out)
        return {(r["rule"], r["severity"]) for r in out}


class AgentCheck(unittest.TestCase):
    def test_good(self):
        self.assertEqual(run({"a.md": GOOD}), set())

    def test_missing_frontmatter(self):
        self.assertIn(("AG-FM", "error"), run({"a.md": "You review code. Report.\n"}))

    def test_name_rules(self):
        self.assertIn(("AG-NAME", "error"), run({"a.md": GOOD.replace("code-reviewer", "my:agent")}))
        self.assertIn(("AG-NAME", "error"), run({"a.md": GOOD.replace("name: code-reviewer\n", "")}))

    def test_missing_description(self):
        self.assertIn(("AG-DESC", "error"), run({"a.md": GOOD.replace("description: Reviews code for security issues. Use when code changed.\n", "")}))

    def test_snake_case_field_and_unknown(self):
        r = run({"a.md": GOOD.replace("model: sonnet", "model: sonnet\ndisallowed_tools: Bash\nfoo: 1")})
        self.assertIn(("AG-FIELD", "warning"), r)

    def test_bad_values(self):
        r = run({"a.md": GOOD.replace("model: sonnet", "model: sonnet\npermissionMode: wild\nmaxTurns: ten\neffort: huge")})
        self.assertIn(("AG-PERM", "error"), r)
        self.assertIn(("AG-TURNS", "error"), r)
        self.assertIn(("AG-EFFORT", "error"), r)

    def test_unknown_tool_and_skill_tool(self):
        r = run({"a.md": GOOD.replace("Read, Grep, Glob", "Read, Skill, Reed")})
        self.assertIn(("AG-TOOL", "warning"), r)

    def test_no_tools_warns(self):
        self.assertIn(("AG-TOOLS", "warning"), run({"a.md": GOOD.replace("tools: Read, Grep, Glob\n", "")}))

    def test_plugin_ignored_fields(self):
        r = run({"plugins/p/agents/a.md": GOOD.replace("model: sonnet", "model: sonnet\npermissionMode: plan")}, sub="plugins/p/agents/a.md")
        self.assertIn(("AG-PLUGIN", "warning"), r)

    def test_empty_body_and_output(self):
        self.assertIn(("AG-BODY", "error"), run({"a.md": GOOD.split("---\n\n")[0] + "---\n"}))
        self.assertIn(("AG-OUT", "warning"), run({"a.md": GOOD.replace("Report findings as a list.", "Be nice.")}))

    def test_dash_and_caps(self):
        r = run({"a.md": GOOD + "You MUST obey — now.\n"})
        self.assertIn(("HOUSE-DASH", "error"), r)
        self.assertIn(("AG-CAPS", "warning"), r)

    def test_managed(self):
        md = "---\nname: Coding Assistant\nmodel: claude-opus-5-5\ntools:\n  - type: agent_toolset_20260401\n---\n\nYou write code. Return a summary.\n"
        self.assertEqual(run({"a.md": md}, runtime="managed"), set())
        self.assertIn(("AG-MODEL", "error"), run({"a.md": md.replace("model: claude-opus-5-5\n", "")}, runtime="managed"))

    def test_openclaw(self):
        r = run({"AGENTS.md": "# Agent\n## Memory\nx\n", "SOUL.md": "Tone."}, runtime="openclaw")
        self.assertEqual(r, set())
        r = run({"AGENTS.md": "# A\n", "USER.md": "x" * 4100, "BOOTSTRAP.md": "x"}, runtime="openclaw")
        self.assertIn(("OC-FILE", "error"), r)
        self.assertIn(("OC-SIZE", "error"), r)
        self.assertIn(("OC-BOOT", "warning"), r)


if __name__ == "__main__":
    unittest.main()
