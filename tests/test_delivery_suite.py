"""Tests for the harness of the delivery suite (evals/delivery). They need no fixture and no network."""
import json
import os
import pathlib
import subprocess
import sys
import tempfile
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[1]
SUITE = ROOT / "evals" / "delivery"
sys.path.insert(0, str(SUITE))
import suite  # noqa: E402


class StoryFolderTests(unittest.TestCase):
    def test_each_story_has_the_files_a_run_and_a_verify_need(self):
        for story in suite.all_stories():
            folder = suite.STORIES / story
            with self.subTest(story=story):
                for name in ("story.md", "answers.md", "meta.json"):
                    self.assertTrue((folder / name).is_file(), name)
                meta = json.loads((folder / "meta.json").read_text(encoding="utf-8"))
                self.assertIn(meta["fixture"], suite.FIXTURES)
                for key in ("size", "risk", "difficulty", "kinds", "role"):
                    self.assertIn(key, meta)

    def test_the_story_set_spans_easy_to_very_hard(self):
        levels = {json.loads((suite.STORIES / s / "meta.json").read_text())["difficulty"] for s in suite.all_stories()}
        self.assertTrue({"easy", "medium", "hard", "very hard"} <= levels, levels)

    def test_the_text_a_run_sees_never_names_the_hidden_cases(self):
        for story in suite.all_stories():
            for name in ("story.md", "answers.md"):
                text = (suite.STORIES / story / name).read_text(encoding="utf-8")
                for phrase in ("hidden test", "hidden case", "/hidden"):
                    self.assertNotIn(phrase, text.lower(), f"{story}/{name}")


class RunCasesTests(unittest.TestCase):
    def test_outcomes_of_each_case_are_collected(self):
        with tempfile.TemporaryDirectory() as tmp:
            folder = pathlib.Path(tmp)
            (folder / "test_sample.py").write_text(
                "import os, unittest\n"
                "class Sample(unittest.TestCase):\n"
                "    def test_passes(self):\n        self.assertTrue(os.environ['SUT_URL'])\n"
                "    def test_fails(self):\n        self.assertEqual(1, 2)\n"
                "    def test_errors(self):\n        raise RuntimeError('boom')\n")
            env = dict(os.environ, SUT_URL="http://127.0.0.1:1")
            out = subprocess.run([sys.executable, "-I", str(SUITE / "acceptance" / "run_cases.py"), str(folder)],
                                 env=env, capture_output=True, text=True, check=True).stdout
            outcomes = json.loads(out.strip().splitlines()[-1])
        self.assertEqual(outcomes["Sample.test_passes"][0], "Passed")
        self.assertEqual(outcomes["Sample.test_fails"][0], "Failed")
        self.assertEqual(outcomes["Sample.test_errors"][0], "Error")


class StreamFactsTests(unittest.TestCase):
    def test_cost_skills_agents_and_a_look_at_the_suite_are_read(self):
        lines = [
            {"type": "assistant", "message": {"content": [{"type": "tool_use", "name": "Skill", "input": {"skill": "dev-flow:plan-and-build"}}]}},
            {"type": "assistant", "message": {"content": [{"type": "tool_use", "name": "Agent", "input": {"subagent_type": "dev-team:senior"}}]}},
            {"type": "assistant", "message": {"content": [{"type": "tool_use", "name": "Read", "input": {"file_path": "/x/evals/delivery/stories/a/hidden/t.py"}}]}},
            {"type": "result", "total_cost_usd": 1.25, "duration_ms": 90000},
        ]
        with tempfile.TemporaryDirectory() as tmp:
            stream = pathlib.Path(tmp) / "stream.jsonl"
            stream.write_text("\n".join(json.dumps(line) for line in lines))
            facts = suite.stream_facts(stream)
        self.assertEqual(facts["cost"], 1.25)
        self.assertEqual(facts["minutes"], 1.5)
        self.assertEqual(facts["skills"], ["dev-flow:plan-and-build"])
        self.assertEqual(facts["agents"], ["dev-team:senior"])
        self.assertTrue(facts["peeked"])


if __name__ == "__main__":
    unittest.main()
