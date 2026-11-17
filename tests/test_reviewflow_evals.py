"""Tests of the review-flow comparison tools. They start no model."""
import contextlib
import importlib.util
import json
import pathlib
import sys
import tempfile
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / "evals" / "review-flow"))
sys.path.insert(0, str(ROOT / "shared"))
sys.path.insert(0, str(ROOT / "evals" / "dev-stack"))
import bugcases  # noqa: E402
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

    def test_recur_stays_under_the_round_limit_and_keeps_the_log_line_of_round_02(self):
        with tempfile.TemporaryDirectory() as tmp:
            repo = self.build("recur", tmp)
            found = sorted(p.name for p in repo.glob(f"docs/work/{scenarios.SLUG}/follow-ups/*-comments.md"))
            self.assertEqual(found, ["01-comments.md", "02-comments.md"])
            self.assertIn("cancelled.", (repo / scenarios.ENDPOINTS).read_text(encoding="utf-8"))

    def test_old_run_folder_is_checked_with_an_adopted_header_and_does_not_change(self):
        with tempfile.TemporaryDirectory() as tmp:
            repo = self.build("comments", tmp)
            folder = repo / "docs" / "work" / scenarios.SLUG
            before = (folder / "run.md").read_text(encoding="utf-8")
            self.assertFalse(before.startswith("---"))
            lines = score.runcheck_lines(folder)
            self.assertTrue(lines)
            self.assertFalse([line for line in lines if "RUN-HEADER" in line], lines)
            self.assertEqual((folder / "run.md").read_text(encoding="utf-8"), before)

    def test_planted_code_scores_as_not_fixed(self):
        with tempfile.TemporaryDirectory() as tmp:
            facts = score.code_facts(self.build("review", tmp))
            self.assertFalse(facts["P1 shipped handled"] or facts["P2 window from options"] or facts["older bug changed"])
            self.assertFalse(facts["N1 o renamed"] or facts["N2 comment removed"])


@contextlib.contextmanager
def fake_ticket(tmp):
    """A stand-in for cancel-order.ticket.md, which another change writes."""
    saved = scenarios.TICKET
    ticket = pathlib.Path(tmp) / "cancel-order.ticket.md"
    ticket.write_text("# ORD-17 Cancel an order\n\n## Behaviour spec\n\nRule R1: a placed order can be cancelled.\n", encoding="utf-8")
    scenarios.TICKET = ticket
    try:
        yield ticket
    finally:
        scenarios.TICKET = saved


class Runner(unittest.TestCase):
    def test_a_run_that_changes_the_real_profile_stops_the_batch(self):
        with tempfile.TemporaryDirectory() as tmp:
            real = pathlib.Path(tmp) / "profile.md"
            real.write_text("a")
            before = runner.fingerprint(real)
            real.write_text("b")
            self.assertNotEqual(runner.fingerprint(real), before)
            self.assertIsNone(runner.fingerprint(pathlib.Path(tmp) / "none.md"))

    def test_baseline_has_no_dev_team(self):
        dirs = runner.plugin_dirs("baseline", "/old")
        self.assertEqual(dirs, ["/old/plugins/dev-stack"])

    def test_current_uses_the_old_root_and_new_uses_this_tree(self):
        self.assertTrue(all(d.startswith("/old") for d in runner.plugin_dirs("current", "/old")))
        self.assertTrue(all(d.startswith(str(ROOT)) for d in runner.plugin_dirs("new", "/old")))

    def test_each_arm_has_a_prompt_and_only_team_arms_name_the_skill(self):
        with tempfile.TemporaryDirectory() as tmp, fake_ticket(tmp):
            for name in ("story", "review", "comments", "cap", "recur"):
                folder = pathlib.Path(tmp) / name
                self.assertNotIn("dev-team:", scenarios.prompt_for(name, "baseline", folder))
                self.assertIn("dev-team:", scenarios.prompt_for(name, "current", folder))
                self.assertIn("dev-team:", scenarios.prompt_for(name, "new", folder))
                self.assertNotIn(str(ROOT), scenarios.prompt_for(name, "new", folder))

    def test_new_loads_dev_flow_and_current_keeps_its_two_plugins(self):
        self.assertEqual([pathlib.Path(d).name for d in runner.plugin_dirs("new", "/old")], ["dev-flow", "dev-team", "dev-stack"])
        self.assertEqual(runner.plugin_dirs("current", "/old"), ["/old/plugins/dev-team", "/old/plugins/dev-stack"])

    def test_story_gives_new_the_ticket_and_current_the_old_story(self):
        with tempfile.TemporaryDirectory() as tmp, fake_ticket(tmp) as ticket:
            new, current = pathlib.Path(tmp) / "new", pathlib.Path(tmp) / "current"
            prompt = scenarios.prompt_for("story", "new", new)
            self.assertIn(str(new / "ticket.md"), prompt)
            self.assertIn("The item file is the ticket.", prompt)
            self.assertEqual((new / "ticket.md").read_text(encoding="utf-8"), ticket.read_text(encoding="utf-8"))
            self.assertFalse((new / "story.md").exists())
            old = scenarios.prompt_for("story", "current", current)
            self.assertEqual(old, scenarios.PROMPTS["story"]["team"].replace("{story}", str(current / "story.md"))
                             + scenarios.AFTER.replace("{answers}", str(current / "answers.md")) + scenarios.SUFFIX)
            self.assertEqual((current / "story.md").read_text(encoding="utf-8"), scenarios.STORY.read_text(encoding="utf-8"))

    def test_review_restarts_new_at_step_7_and_current_at_step_6(self):
        with tempfile.TemporaryDirectory() as tmp:
            self.assertIn("step 7", scenarios.prompt_for("review", "new", pathlib.Path(tmp) / "n"))
            self.assertIn("step 6", scenarios.prompt_for("review", "current", pathlib.Path(tmp) / "c"))

    def test_story_for_new_stops_when_the_ticket_is_missing(self):
        with tempfile.TemporaryDirectory() as tmp:
            saved = scenarios.TICKET
            scenarios.TICKET = pathlib.Path(tmp) / "none.ticket.md"
            try:
                with self.assertRaises(FileNotFoundError):
                    scenarios.prompt_for("story", "new", pathlib.Path(tmp) / "in")
            finally:
                scenarios.TICKET = saved

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


class HardComparison(unittest.TestCase):
    def test_each_story_has_an_answer_sheet_a_probe_and_a_reference(self):
        hard = load("hard")
        for story in hard.STORIES:
            self.assertTrue((hard.HARD / "stories" / f"{story}.answers.md").is_file(), story)
            self.assertTrue(list((hard.HARD / "probes" / story).glob("*.cs")), story)
            self.assertGreater((hard.HARD / "references" / f"{story}.patch").stat().st_size, 0, story)

    def test_stories_do_not_give_the_hidden_rules(self):
        hard = load("hard")
        for story in hard.STORIES:
            text = (hard.HARD / "stories" / f"{story}.md").read_text(encoding="utf-8").lower()
            for word in ("away from zero", "event", "at the same time"):
                self.assertNotIn(word, text, story)

    def test_only_the_flow_differs_between_the_arms(self):
        hard = load("hard")
        for plugins in hard.ARMS.values():
            self.assertIn("dev-stack", plugins)
        self.assertEqual(hard.PROMPTS["both"], hard.PROMPTS["baseline"])


class BugCases(unittest.TestCase):
    def test_each_case_has_a_key_and_a_branch_with_a_diff(self):
        self.assertEqual(set(bugcases.CASES), set(bugcases.KEY))
        for name in bugcases.CASES:
            with tempfile.TemporaryDirectory() as tmp:
                repo = bugcases.prepare_case(name, pathlib.Path(tmp) / "repo")
                self.assertIn("CaseTests.cs", scenarios.git(repo, "diff", "--name-only", "main...HEAD"), name)

    def test_collect_names_the_case_and_no_version(self):
        bugfind = load("bugfind")
        with tempfile.TemporaryDirectory() as tmp:
            runs, out = pathlib.Path(tmp) / "runs", pathlib.Path(tmp) / "out"
            review = runs / "c1-total" / "bundle" / "1" / "repo" / "docs" / "work" / bugcases.SLUG / "core-review.md"
            review.parent.mkdir(parents=True)
            review.write_text("Status: APPROVED\n")
            self.assertEqual(bugfind.collect(runs, out), 1)
            self.assertEqual(json.loads((out / "map.json").read_text())["R01"], {"case": "c1-total", "version": "bundle", "run": "1"})
            text = (out / "R01.md").read_text()
            self.assertIn("Case: c1-total", text)
            self.assertNotIn("bundle", text)

    def test_strip_origin_removes_the_column(self):
        bugfind = load("bugfind")
        text = "| # | Label | Origin | What |\n|---|---|---|---|\n| 1 | issue | exposed | x |\n\nNext\n"
        self.assertEqual(bugfind.strip_origin(text), "| # | Label | What |\n|---|---|---|\n| 1 | issue | x |\n\nNext\n")


if __name__ == "__main__":
    unittest.main()
