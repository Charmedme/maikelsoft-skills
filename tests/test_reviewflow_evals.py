"""Tests of the review-flow comparison tools. They start no model."""
import importlib.util
import json
import pathlib
import sys
import tempfile
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / "evals" / "review-flow"))
sys.path.insert(0, str(ROOT / "shared"))
import riskcheck  # noqa: E402
import scenarios  # noqa: E402


def load(name):
    """Other test files also import a module named run or score, so load these two by path."""
    spec = importlib.util.spec_from_file_location(f"reviewflow_{name}", ROOT / "evals" / "review-flow" / f"{name}.py")
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


runner = load("run")
score = load("score")


class Scenarios(unittest.TestCase):
    def build(self, name, tmp):
        repo = pathlib.Path(tmp) / "repo"
        scenarios.prepare_scenario(name, repo)
        return repo

    def test_review_is_high_because_of_the_migration(self):
        with tempfile.TemporaryDirectory() as tmp:
            repo = self.build("review", tmp)
            base = scenarios.git(repo, "rev-parse", "main")
            result = riskcheck.run(str(repo), base, None)
            self.assertEqual(result["level"], "high")
            self.assertEqual({kind for kind, _ in result["reasons"]}, {"migration"})

    def test_older_bug_is_in_the_compare_point(self):
        with tempfile.TemporaryDirectory() as tmp:
            repo = self.build("comments", tmp)
            self.assertIn("GetTotalAsync", scenarios.git(repo, "show", f"main:{scenarios.ENDPOINTS}"))
            self.assertNotIn("CancelAsync", scenarios.git(repo, "show", f"main:{scenarios.ENDPOINTS}"))

    def test_review_is_clean_for_the_build_rules_of_the_fixture(self):
        with tempfile.TemporaryDirectory() as tmp:
            self.assertNotIn("is null) return", (self.build("review", tmp) / scenarios.ENDPOINTS).read_text(encoding="utf-8"))

    def test_cap_has_three_earlier_rounds(self):
        with tempfile.TemporaryDirectory() as tmp:
            repo = self.build("cap", tmp)
            found = sorted(p.name for p in repo.glob(f"docs/work/{scenarios.SLUG}/follow-ups/*-comments.md"))
            self.assertEqual(found, ["01-comments.md", "02-comments.md", "03-comments.md"])

    def test_planted_code_scores_as_not_fixed(self):
        with tempfile.TemporaryDirectory() as tmp:
            facts = score.code_facts(self.build("review", tmp))
            self.assertFalse(facts["P1 shipped handled"] or facts["P2 window from options"] or facts["older bug changed"])
            self.assertFalse(facts["N1 o renamed"] or facts["N2 comment removed"])


class Runner(unittest.TestCase):
    def test_baseline_has_no_dev_team(self):
        dirs = runner.plugin_dirs("baseline", "/old")
        self.assertEqual(dirs, ["/old/plugins/dev-stack"])

    def test_current_uses_the_old_root_and_new_uses_this_tree(self):
        self.assertTrue(all(d.startswith("/old") for d in runner.plugin_dirs("current", "/old")))
        self.assertTrue(all(d.startswith(str(ROOT)) for d in runner.plugin_dirs("new", "/old")))

    def test_each_arm_has_a_prompt_and_only_team_arms_name_the_skill(self):
        for name in ("story", "review", "comments", "cap"):
            with tempfile.TemporaryDirectory() as tmp:
                self.assertNotIn("dev-team:", scenarios.prompt_for(name, "baseline", tmp))
                self.assertIn("dev-team:", scenarios.prompt_for(name, "new", tmp))
                self.assertNotIn(str(ROOT), scenarios.prompt_for(name, "new", tmp))

    def test_pinned_copy_sets_the_model_of_each_agent(self):
        with tempfile.TemporaryDirectory() as tmp:
            target = runner.pinned_copy(ROOT / "plugins" / "dev-team", pathlib.Path(tmp) / "dev-team", "sonnet")
            text = (pathlib.Path(target) / "agents" / "senior.md").read_text(encoding="utf-8")
            self.assertIn("model: sonnet", text)
            self.assertNotIn("model: opus", text)

    def test_stream_facts_read_cost_and_agents(self):
        with tempfile.TemporaryDirectory() as tmp:
            stream = pathlib.Path(tmp) / "stream.jsonl"
            lines = [{"type": "assistant", "message": {"content": [{"type": "tool_use", "name": "Agent", "input": {"subagent_type": "dev-team:senior"}}]}},
                     {"type": "result", "total_cost_usd": 1.5, "duration_ms": 60000}]
            stream.write_text("\n".join(json.dumps(line) for line in lines))
            facts = score.stream_facts(stream)
            self.assertEqual((facts["cost"], facts["minutes"], facts["agents"]), (1.5, 1.0, ["dev-team:senior"]))


if __name__ == "__main__":
    unittest.main()
