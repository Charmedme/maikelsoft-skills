import contextlib
import io
import json
import os
import pathlib
import subprocess
import sys
import tempfile
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / "plugins" / "dev-team" / "shared"))
import checks  # noqa: E402
import decisioncheck  # noqa: E402
import runcheck  # noqa: E402

HOOK = ROOT / "plugins" / "dev-team" / "hooks" / "log-command.py"

RUN_MD = """# Run: demo

| Field | Value |
|-------|-------|
| Status | G2 |

## Counters

| Counter | Limit | Value |
|---------|-------|-------|
| design-reflection | 1 | 1 |
| core-review | 1 | 1 |
| security-review | 1 | {security} |
| security-fix | 1 | 0 |
| 01 plan-feedback | 1 | 1 |
| 01 review | 1 | 1 |
| 01 fix | 1 | {fix} |
"""

LOG_MD = """# Log

| Time | Agent | Step | Status | Files | Commit | Checks | Decisions | Assumptions | Reason |
|------|-------|------|--------|-------|--------|--------|-----------|-------------|--------|
| 10:00 | main | intake | STARTED | story.md | - | - | - | - | story read |
| 10:05 | senior | design | DESIGN_READY | design.md | - | - | API-01 open | - | design done |
| 10:30 | medior | 01 build | DONE | src/A.cs | abc123 | build pass, test pass | - | - | package built |
{extra}"""


def write(path, text):
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(text, encoding="utf-8")


def make_run(root, security=1, fix=0, review="APPROVED", extra="", commands=True):
    run = root / "docs" / "work" / "demo"
    write(run / "run.md", RUN_MD.format(security=security, fix=fix))
    write(run / "story.md", "# Story\n")
    write(run / "design.md", "# Design\n\nStatus: DESIGN_READY\n")
    write(run / "decision-sheet.md", "# Decisions\n")
    write(run / "reflection.md", "Status: REFLECTION_READY\n")
    write(run / "security-review.md", "## Verdict\n\nVerdict: PASS\n")
    write(run / "log.md", LOG_MD.format(extra=extra))
    pk = run / "packages"
    write(pk / "01-cancel-order.md", "# Package 01\n")
    write(pk / "01-plan.md", "Status: PLAN_READY\n")
    write(pk / "01-plan-feedback.md", "Status: PLAN_APPROVED\n")
    write(pk / "01-report.md", "Status: DONE\n")
    write(pk / "01-review.md", f"Status: {review}\n")
    if commands:
        write(run / "commands.jsonl", json.dumps({"command": "python3 checks.py", "agent_type": "dev-team:medior"}) + "\n")
    return run


def rules(findings):
    return sorted({r for _, r, _ in findings})


class RunCheck(unittest.TestCase):
    def test_complete_run_has_no_findings(self):
        with tempfile.TemporaryDirectory() as tmp:
            run = make_run(pathlib.Path(tmp))
            self.assertEqual(runcheck.check(run, "g2"), [])

    def test_missing_security_review_before_g2(self):
        with tempfile.TemporaryDirectory() as tmp:
            run = make_run(pathlib.Path(tmp), security=0)
            (run / "security-review.md").unlink()
            self.assertEqual(rules(runcheck.check(run, "g2")), ["RUN-COUNTER", "RUN-FILE"])

    def test_g1_stage_does_not_need_later_files(self):
        with tempfile.TemporaryDirectory() as tmp:
            run = make_run(pathlib.Path(tmp), security=0)
            (run / "security-review.md").unlink()
            (run / "reflection.md").unlink()
            self.assertEqual(runcheck.check(run, "g1"), [])

    def test_counter_over_limit(self):
        with tempfile.TemporaryDirectory() as tmp:
            run = make_run(pathlib.Path(tmp), fix=2)
            self.assertIn("RUN-COUNTER", rules(runcheck.check(run, "g2")))

    def test_second_review_file_is_a_round_finding(self):
        with tempfile.TemporaryDirectory() as tmp:
            run = make_run(pathlib.Path(tmp))
            write(run / "packages" / "01-review-2.md", "Status: APPROVED\n")
            self.assertIn("RUN-ROUNDS", rules(runcheck.check(run, "g2")))

    def test_changes_required_needs_a_fix_row(self):
        with tempfile.TemporaryDirectory() as tmp:
            run = make_run(pathlib.Path(tmp), review="CHANGES_REQUIRED")
            self.assertEqual(rules(runcheck.check(run, "g2")), ["RUN-FIX"])
            extra = "| 10:50 | medior | 01 fix | DONE | src/A.cs | def456 | - | - | - | blocking finding fixed |\n"
            run = make_run(pathlib.Path(tmp), review="CHANGES_REQUIRED", extra=extra, fix=1)
            self.assertEqual(runcheck.check(run, "g2"), [])

    def test_invalid_status_word(self):
        with tempfile.TemporaryDirectory() as tmp:
            run = make_run(pathlib.Path(tmp), review="LGTM")
            self.assertEqual(rules(runcheck.check(run, "g2")), ["RUN-STATUS"])

    def test_claimed_check_without_command_is_evidence_finding(self):
        with tempfile.TemporaryDirectory() as tmp:
            run = make_run(pathlib.Path(tmp), commands=False)
            self.assertEqual(rules(runcheck.check(run, "g2")), ["RUN-EVIDENCE"])

    def test_check_command_by_another_agent_does_not_count(self):
        with tempfile.TemporaryDirectory() as tmp:
            run = make_run(pathlib.Path(tmp))
            write(run / "commands.jsonl", json.dumps({"command": "dotnet test", "agent_type": "dev-team:senior"}) + "\n")
            self.assertEqual(rules(runcheck.check(run, "g2")), ["RUN-EVIDENCE"])

    def test_main_exit_codes(self):
        with tempfile.TemporaryDirectory() as tmp:
            run = make_run(pathlib.Path(tmp))
            with contextlib.redirect_stdout(io.StringIO()):
                self.assertEqual(runcheck.main([str(run)]), 0)
                (run / "story.md").unlink()
                self.assertEqual(runcheck.main([str(run)]), 1)


INDEX = """# Decisions

| ID | Question | Answer | Status | By | ADR | Check | Date |
|----|----------|--------|--------|----|-----|-------|------|
{rows}
"""


def adr(status, extra=""):
    return f"---\nstatus: {status}\ndate: 2026-10-18\n---\n\n# Decision\n{extra}\n"


class DecisionCheck(unittest.TestCase):
    def repo(self, rows, files=None):
        tmp = tempfile.TemporaryDirectory()
        root = pathlib.Path(tmp.name)
        write(root / "docs/dev-team/decisions.md", INDEX.format(rows="\n".join(rows)))
        for rel, text in (files or {}).items():
            write(root / rel, text)
        self.addCleanup(tmp.cleanup)
        return root

    def test_valid_index(self):
        root = self.repo(["| API-01 | Endpoint style | Minimal API | accepted | human | [ADR](../decisions/0001-api.md) | judge | 2026-10-18 |"],
                         {"docs/decisions/0001-api.md": adr("accepted")})
        self.assertEqual(decisioncheck.check(root, [], None), [])

    def test_missing_index(self):
        root = pathlib.Path(tempfile.mkdtemp())
        self.assertEqual(rules(decisioncheck.check(root, [], None)), ["DEC-INDEX"])

    def test_missing_adr_and_wrong_status(self):
        root = self.repo(["| API-01 | q | a | accepted | human | [ADR](../decisions/0001-api.md) | judge | d |",
                          "| API-02 | q | a | accepted | human | [ADR](../decisions/0002-x.md) | judge | d |",
                          "| API-03 | q | a | maybe | human | - | judge | d |"],
                         {"docs/decisions/0002-x.md": adr("proposed")})
        self.assertEqual(rules(decisioncheck.check(root, [], None)), ["DEC-ADR", "DEC-ADR-STATUS", "DEC-STATUS"])

    def test_duplicate_active_rows(self):
        root = self.repo(["| API-01 | q | a | assumed | agent | - | judge | d |",
                          "| API-01 | q | b | assumed | agent | - | judge | d |"])
        self.assertEqual(rules(decisioncheck.check(root, [], None)), ["DEC-DUPLICATE"])

    def test_supersede_needs_links_both_ways(self):
        rows = ["| API-01 | q | a | superseded | human | [ADR](../decisions/0001-a.md) | judge | d |",
                "| API-01 | q | b | accepted | human | [ADR](../decisions/0002-b.md) | judge | d |"]
        root = self.repo(rows, {"docs/decisions/0001-a.md": adr("superseded by 0002", "Superseded by [0002](0002-b.md)"),
                                "docs/decisions/0002-b.md": adr("accepted")})
        self.assertEqual(rules(decisioncheck.check(root, [], None)), ["DEC-SUPERSEDE"])
        write(root / "docs/decisions/0002-b.md", adr("accepted", "Supersedes [0001](0001-a.md)"))
        self.assertEqual(decisioncheck.check(root, [], None), [])

    def test_supersede_link_in_plain_text_and_backtick_index_link(self):
        rows = ["| API-01 | q | a | superseded | human | `../decisions/0001-a.md` | judge | d |",
                "| API-01 | q | b | accepted | human | `../decisions/0002-controllers-for-odata.md` | judge | d |"]
        root = self.repo(rows, {
            "docs/decisions/0001-a.md": adr("superseded by ADR-0002", "Superseded by ADR-0002 (0002-controllers-for-odata.md)."),
            "docs/decisions/0002-controllers-for-odata.md": adr("accepted", "Supersedes 0001-a.md."),
        })
        self.assertEqual(decisioncheck.check(root, [], None), [])

    def test_code_that_uses_an_open_decision(self):
        root = self.repo(["| API-01 | q | a | accepted | human | [ADR](../decisions/0001-api.md) | judge | d |"],
                         {"docs/decisions/0001-api.md": adr("accepted"),
                          "src/Api/OrdersController.cs": "public class OrdersController : ControllerBase {}\n",
                          "src/Api/Repo.cs": "public class Repo : IRepository<Order> {}\n"})
        rf = root / "rules.json"
        rf.write_text(json.dumps({"rules": [
            {"id": "DET-CTRL", "kind": "detect", "decision": "API-01", "files": ["**/*.cs"], "pattern": ": ControllerBase"},
            {"id": "DET-REPO", "kind": "detect", "decision": "DATA-05", "files": ["**/*.cs"], "pattern": r"IRepository<"}]}))
        found = decisioncheck.check(root, [str(rf)], None)
        self.assertEqual([(f, r) for f, r, _ in found], [("src/Api/Repo.cs", "DEC-USED")])
        self.assertEqual(decisioncheck.check(root, [str(rf)], {"src/Api/OrdersController.cs"}), [])


class Checks(unittest.TestCase):
    def test_plan_for_dotnet_and_npm(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = pathlib.Path(tmp)
            write(root / "Shop.slnx", "<Solution />\n")
            write(root / "src/Api/Api.csproj", "<Project />\n")
            write(root / "web/package.json", "{}")
            write(root / "package.json", json.dumps({"scripts": {"build": "ng build", "lint": "eslint ."}}))
            names = [c[0] for c in checks.plan(root)]
            self.assertEqual(names, ["build", "test", "format", "npm-build", "npm-lint"])
            self.assertIn("Shop.slnx", checks.plan(root)[0][1])

    def test_no_tools_writes_a_skipped_row(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = pathlib.Path(tmp)
            with contextlib.redirect_stdout(io.StringIO()):
                self.assertEqual(checks.main(["--root", str(root)]), 0)
            self.assertIn("skipped", (root / "checks.md").read_text())

    def test_failed_check_is_exit_1_with_tail(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = pathlib.Path(tmp)
            write(root / "package.json", json.dumps({"scripts": {"build": "node -e \"console.log('boom'); process.exit(3)\""}}))
            if subprocess.run(["which", "npm"], capture_output=True).returncode != 0:
                self.skipTest("npm is not installed")
            with contextlib.redirect_stdout(io.StringIO()):
                self.assertEqual(checks.main(["--root", str(root), "--only", "npm"]), 1)
            text = (root / "checks.md").read_text()
            self.assertIn("| npm-build | fail |", text)
            self.assertIn("boom", text)


class Hook(unittest.TestCase):
    def run_hook(self, project, event):
        env = dict(os.environ, CLAUDE_PROJECT_DIR=str(project))
        return subprocess.run([sys.executable, str(HOOK)], input=json.dumps(event), text=True,
                              capture_output=True, env=env)

    def test_no_active_run_writes_nothing(self):
        with tempfile.TemporaryDirectory() as tmp:
            project = pathlib.Path(tmp)
            proc = self.run_hook(project, {"tool_name": "Bash", "tool_input": {"command": "ls"}})
            self.assertEqual(proc.returncode, 0)
            self.assertFalse(list(project.rglob("commands.jsonl")))

    def test_active_run_gets_one_line_per_command(self):
        with tempfile.TemporaryDirectory() as tmp:
            project = pathlib.Path(tmp)
            run = project / "docs/work/demo"
            run.mkdir(parents=True)
            write(project / ".dev-team/active-run", "docs/work/demo\n")
            event = {"tool_name": "Bash", "session_id": "s1", "agent_type": "dev-team:medior", "agent_id": "a1",
                     "tool_input": {"command": "dotnet test"}, "tool_response": {"exit_code": 0}}
            self.run_hook(project, event)
            self.run_hook(project, dict(event, tool_name="Read"))
            lines = (run / "commands.jsonl").read_text().splitlines()
            self.assertEqual(len(lines), 1)
            entry = json.loads(lines[0])
            self.assertEqual((entry["command"], entry["agent_type"], entry["exit_code"]), ("dotnet test", "dev-team:medior", 0))

    def test_bad_input_never_fails(self):
        with tempfile.TemporaryDirectory() as tmp:
            env = dict(os.environ, CLAUDE_PROJECT_DIR=tmp)
            proc = subprocess.run([sys.executable, str(HOOK)], input="not json", text=True, capture_output=True, env=env)
            self.assertEqual((proc.returncode, proc.stdout), (0, ""))


if __name__ == "__main__":
    unittest.main()
