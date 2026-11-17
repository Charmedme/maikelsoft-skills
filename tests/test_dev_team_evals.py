"""Tests of the dev-team eval harness (evals/dev-team/run.py and score.py). They start no model, no Docker, and no dotnet."""
import importlib.util
import json
import pathlib
import subprocess
import tempfile
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[1]
EVALS = ROOT / "evals" / "dev-team"


def load(name):
    """Other test files also import a module named run or score, so load these two by path."""
    spec = importlib.util.spec_from_file_location(f"devteam_{name}", EVALS / f"{name}.py")
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


runner = load("run")
score = load("score")
CFG = json.loads((EVALS / "evals.json").read_text(encoding="utf-8"))


def git(repo, *args):
    cmd = ["git", "-c", "user.name=Dev", "-c", "user.email=dev@example.com", "-c", "commit.gpgsign=false", "-C", str(repo), *args]
    subprocess.run(cmd, check=True, capture_output=True, text=True)


class Config(unittest.TestCase):
    def test_each_eval_names_its_ticket(self):
        for ev in CFG["evals"]:
            self.assertEqual(ev["ticket"], f"stories/{ev['name']}.ticket.md", ev["name"])
            self.assertTrue((EVALS / ev["story"]).is_file(), ev["name"])

    def test_the_team_prompt_asks_the_team_for_the_ticket(self):
        prompt = CFG["prompt"]["team"]
        self.assertIn("dev-team:deliver-story", prompt)
        self.assertIn("The item file is the ticket.", prompt)
        self.assertIn("Take each answer of the human from that sheet.", prompt)


class Runner(unittest.TestCase):
    def test_the_team_arm_loads_dev_flow_dev_team_and_dev_stack(self):
        self.assertEqual([pathlib.Path(d).name for d in runner.plugin_dirs("team")], ["dev-flow", "dev-team", "dev-stack"])
        self.assertEqual([pathlib.Path(d).name for d in runner.plugin_dirs("baseline")], ["dev-stack"])
        for arm in ("flow", "flowspec"):
            self.assertEqual([pathlib.Path(d).name for d in runner.plugin_dirs(arm)], ["dev-flow", "dev-team", "dev-stack"])

    def test_the_team_arm_reads_the_ticket_and_the_other_arms_the_story(self):
        with tempfile.TemporaryDirectory() as tmp:
            ticket = pathlib.Path(tmp) / "x.ticket.md"
            ticket.write_text("# X\n", encoding="utf-8")
            ev = {"name": "x", "story": "stories/get-order.md", "ticket": str(ticket), "answers": "stories/get-order.answers.md"}
            self.assertEqual(runner.item_file(ev, "team"), ticket)
            for arm in ("baseline", "flow", "flowspec"):
                self.assertEqual(runner.item_file(ev, arm), EVALS / "stories" / "get-order.md")
            prompt = runner.prompt_for(ev, "team", CFG)
            self.assertIn(str(ticket), prompt)
            self.assertTrue(prompt.endswith(CFG["prompt"]["suffix"]))

    def test_a_missing_ticket_stops_the_team_arm_only(self):
        ev = {"name": "x", "story": "stories/get-order.md", "ticket": "stories/none.ticket.md", "answers": "stories/get-order.answers.md"}
        with self.assertRaises(FileNotFoundError):
            runner.item_file(ev, "team")
        self.assertEqual(runner.item_file(ev, "baseline"), EVALS / "stories" / "get-order.md")


class WorkCommitted(unittest.TestCase):
    def repo(self, tmp, branch):
        repo = pathlib.Path(tmp) / "repo"
        repo.mkdir()
        git(repo, "init", "-q", "-b", "main")
        (repo / "a.txt").write_text("a")
        git(repo, "add", "-A")
        git(repo, "commit", "-qm", "a")
        git(repo, "switch", "-qc", branch)
        (repo / "b.txt").write_text("b")
        git(repo, "add", "-A")
        git(repo, "commit", "-qm", "b")
        return repo

    def test_team_accepts_the_branch_names_of_1_0_and_0_6(self):
        for branch, ok in (("feat/ORD-17", True), ("fix/ORD-18", True), ("dev-team/ord-17-cancel", True), ("work/ord-17", False)):
            with tempfile.TemporaryDirectory() as tmp:
                self.assertEqual(score.check_work_committed(self.repo(tmp, branch), "team")[0], ok, branch)

    def test_baseline_accepts_any_branch(self):
        with tempfile.TemporaryDirectory() as tmp:
            self.assertTrue(score.check_work_committed(self.repo(tmp, "work/ord-17"), "baseline")[0])


class Layout(unittest.TestCase):
    def test_the_speclock_and_runheader_scripts_exist(self):
        self.assertTrue(score.SPECLOCK.is_file())
        self.assertTrue(score.RUNHEADER.is_file())
        self.assertTrue((score.SHARED / "runcheck.py").is_file())

    def test_an_old_run_folder_is_checked_with_an_adopted_header(self):
        with tempfile.TemporaryDirectory() as tmp:
            folder = pathlib.Path(tmp) / "docs" / "work" / "ord-17"
            folder.mkdir(parents=True)
            (folder / "run.md").write_text("# Run: ord-17\n\n| Field | Value |\n|---|---|\n| Mode | light |\n", encoding="utf-8")
            (folder / "story.md").write_text("# Cancel an order\n", encoding="utf-8")
            findings = json.loads(score.runcheck_json(folder).stdout)
            self.assertFalse([f for f in findings if f["rule"] == "RUN-HEADER"], findings)
            self.assertFalse((folder / "run.md").read_text(encoding="utf-8").startswith("---"))

    def test_a_new_run_folder_without_files_has_findings(self):
        with tempfile.TemporaryDirectory() as tmp:
            folder = pathlib.Path(tmp) / "ORD-17"
            folder.mkdir()
            (folder / "run.md").write_text("---\nflow: team\nitem: ORD-17\nsize: large\nstep: 1 start\nbaseline: set\n---\n\n# Run\n", encoding="utf-8")
            findings = json.loads(score.runcheck_json(folder).stdout)
            self.assertTrue([f for f in findings if f["rule"] == "RUN-FILE"], findings)

    def test_the_tier_comes_from_the_run_md_header_when_there_is_no_plan_md(self):
        with tempfile.TemporaryDirectory() as tmp:
            run = pathlib.Path(tmp)
            folder = run / "repo" / "docs" / "work" / "ORD-17"
            folder.mkdir(parents=True)
            (folder / "run.md").write_text("---\nflow: solo\nitem: ORD-17\nsize: medium\nstep: 3\nbaseline: set\n---\n\n# Run\n", encoding="utf-8")
            self.assertEqual(score.check_tier(run, ["medium", "large"]), (True, "medium"))
            (folder / "plan.md").write_text("Tier: small\n", encoding="utf-8")
            self.assertEqual(score.check_tier(run, ["medium"]), (False, "small"))


if __name__ == "__main__":
    unittest.main()
