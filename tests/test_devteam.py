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

PLUGIN = ROOT / "plugins" / "dev-team"
HOOK = PLUGIN / "hooks" / "log-command.py"

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

    def test_claimed_check_without_command_log_is_hook_finding(self):
        with tempfile.TemporaryDirectory() as tmp:
            run = make_run(pathlib.Path(tmp), commands=False)
            self.assertEqual(rules(runcheck.check(run, "g2")), ["RUN-HOOK"])

    def test_check_command_by_another_agent_does_not_count(self):
        with tempfile.TemporaryDirectory() as tmp:
            run = make_run(pathlib.Path(tmp))
            write(run / "commands.jsonl", json.dumps({"command": "dotnet test", "agent_type": "dev-team:senior"}) + "\n")
            self.assertEqual(rules(runcheck.check(run, "g2")), ["RUN-EVIDENCE"])

    def test_light_run_needs_no_reflection_or_security_review(self):
        with tempfile.TemporaryDirectory() as tmp:
            run = make_run(pathlib.Path(tmp), security=0)
            (run / "reflection.md").unlink()
            (run / "security-review.md").unlink()
            self.assertIn("RUN-FILE", rules(runcheck.check(run, "g2")))
            text = (run / "run.md").read_text().replace("| Status | G2 |", "| Status | G2 |\n| Mode | light |")
            write(run / "run.md", text)
            self.assertEqual(runcheck.check(run, "g2"), [])

    def test_verdict_under_a_heading_counts(self):
        with tempfile.TemporaryDirectory() as tmp:
            run = make_run(pathlib.Path(tmp))
            write(run / "security-review.md", "# Security review\n\n## Verdict\nPASS. No new hole. Critical 0, High 0.\n")
            self.assertEqual(runcheck.check(run, "g2"), [])
            write(run / "security-review.md", "# Security review\n\n## Verdict\nGOOD. No new hole.\n")
            self.assertEqual(rules(runcheck.check(run, "g2")), ["RUN-STATUS"])

    def test_reflector_row_counts_commands_of_the_senior_agent(self):
        with tempfile.TemporaryDirectory() as tmp:
            row = "| 10:40 | reflector | review-core | APPROVED | core-review.md | - | build pass, test pass | - | - | reviewed |\n"
            run = make_run(pathlib.Path(tmp), extra=row)
            with open(run / "commands.jsonl", "a", encoding="utf-8") as f:
                f.write(json.dumps({"command": "python3 checks.py", "agent_type": "dev-team:senior"}) + "\n")
            self.assertEqual(runcheck.check(run, "g2"), [])

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

    def test_tests_run_files_and_rule_excludes_are_not_product_code(self):
        root = self.repo([], {"tests/Api.Tests/Interceptor.cs": "class X : SaveChangesInterceptor {}\n",
                              "docs/work/demo/design.md": "## Threat model\n",
                              "src/Api/Generated/X.cs": "class Y : SaveChangesInterceptor {}\n",
                              "src/Api/Audit.cs": "class Z : SaveChangesInterceptor {}\n"})
        rf = root / "rules.json"
        rf.write_text(json.dumps({"rules": [
            {"id": "DET-AUDIT", "kind": "detect", "decision": "DATA-14", "files": ["**/*.cs"],
             "exclude": ["src/Api/Generated/**"], "pattern": "SaveChangesInterceptor"},
            {"id": "DET-TM", "kind": "detect", "decision": "SEC-18", "files": ["**/*.md"], "pattern": "(?i)threat model"}]}))
        found = decisioncheck.check(root, [str(rf)], None)
        self.assertEqual([(f, r) for f, r, _ in found], [("src/Api/Audit.cs", "DEC-USED")])


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


HEADER = "| ID | Question | Answer | Status | By | ADR | Check | Date |\n|---|---|---|---|---|---|---|---|\n"


def git(root, *args):
    subprocess.run(["git", "-c", "user.name=T", "-c", "user.email=t@example.com", "-c", "commit.gpgsign=false",
                    "-C", str(root), *args], check=True, capture_output=True)


def adr_with_id(status, decision_id):
    return f"---\nstatus: {status}\ndate: 2026-10-28\n---\n\n# Title\n\n## More Information\nDecision ID: {decision_id}.\n"


class NormIdTests(unittest.TestCase):
    def test_case_space_and_dash_variants_are_one_id(self):
        forms = {decisioncheck.norm_id(x) for x in ("API-15", "api-15", " API 15 ", "API‑15", "API_15")}
        self.assertEqual(forms, {"API-15"})


class DuplicateTests(unittest.TestCase):
    def setUp(self):
        self.tmp = tempfile.TemporaryDirectory()
        self.root = pathlib.Path(self.tmp.name)
        self.addCleanup(self.tmp.cleanup)

    def rules(self, findings):
        return [f[1] for f in findings]

    def test_same_id_in_other_spelling_is_a_duplicate(self):
        write(self.root / "docs/dev-team/decisions.md",
              HEADER + "| API-15 | q | a | assumed | me | | | 2026-10-28 |\n"
                       "| api 15 | q2 | a2 | assumed | me | | | 2026-10-28 |\n")
        self.assertEqual(self.rules(decisioncheck.check(self.root, [], None)), ["DEC-DUPLICATE"])

    def test_two_accepted_adr_files_with_one_decision_id(self):
        folder = self.root / "docs/decisions"
        write(folder / "0001-a.md", adr_with_id("accepted", "API-01"))
        write(folder / "0002-b.md", adr_with_id("accepted", "api-01"))
        write(self.root / "docs/dev-team/decisions.md",
              HEADER + "| API-01 | q | a | accepted | me | ../decisions/0001-a.md | | 2026-10-28 |\n")
        self.assertIn("DEC-ADR-DUPLICATE", self.rules(decisioncheck.check(self.root, [], None)))

    def test_a_superseded_adr_with_the_same_id_is_not_a_duplicate(self):
        folder = self.root / "docs/decisions"
        write(folder / "0001-a.md", adr_with_id("superseded by ADR-0002", "API-01"))
        write(folder / "0002-b.md", adr_with_id("accepted", "API-01"))
        write(self.root / "docs/dev-team/decisions.md",
              HEADER + "| API-01 | q | a | accepted | me | ../decisions/0002-b.md | | 2026-10-28 |\n")
        self.assertNotIn("DEC-ADR-DUPLICATE", self.rules(decisioncheck.check(self.root, [], None)))


class AddedLinesTests(unittest.TestCase):
    """DEC-USED reads only the lines that the branch added, not the old code of a touched file."""

    def setUp(self):
        self.tmp = tempfile.TemporaryDirectory()
        self.root = pathlib.Path(self.tmp.name)
        self.addCleanup(self.tmp.cleanup)
        git(self.root, "init", "-q", "-b", "main")
        write(self.root / "docs/dev-team/decisions.md", HEADER)
        write(self.root / "src/Old.cs", "var x = OldPattern();\n")
        write(self.root / "rules.json", json.dumps({"rules": [
            {"id": "R1", "kind": "detect", "decision": "PAT-01", "pattern": "OldPattern|NewPattern", "files": ["src/**"]}]}))
        git(self.root, "add", "-A")
        git(self.root, "commit", "-q", "-m", "base")
        git(self.root, "checkout", "-q", "-b", "feature")

    def run_check(self):
        changed = decisioncheck.changed_files(self.root, "main")
        return decisioncheck.check(self.root, [str(self.root / "rules.json")], changed, "main")

    def test_old_pattern_in_a_touched_file_is_no_finding(self):
        write(self.root / "src/Old.cs", "var x = OldPattern();\nvar y = 1;\n")
        git(self.root, "commit", "-q", "-am", "touch")
        self.assertEqual(self.run_check(), [])

    def test_new_pattern_in_a_touched_file_is_a_finding(self):
        write(self.root / "src/Old.cs", "var x = OldPattern();\nvar y = NewPattern();\n")
        git(self.root, "commit", "-q", "-am", "add")
        self.assertEqual([f[1] for f in self.run_check()], ["DEC-USED"])

    def test_pattern_in_a_new_file_is_a_finding(self):
        write(self.root / "src/New.cs", "var z = NewPattern();\n")
        git(self.root, "add", "-A")
        git(self.root, "commit", "-q", "-m", "new")
        self.assertEqual([f[1] for f in self.run_check()], ["DEC-USED"])


class OutputTests(unittest.TestCase):
    def test_findings_are_sorted_by_rule_and_counted(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = pathlib.Path(tmp)
            write(root / "docs/dev-team/decisions.md",
                  HEADER + "| A-1 | q | a | assumed | me | | | d |\n| A-1 | q | a | assumed | me | | | d |\n"
                           "| B-1 | q | a | maybe | me | | | d |\n")
            out = io.StringIO()
            with contextlib.redirect_stdout(out):
                code = decisioncheck.main(["--root", str(root)])
        lines = out.getvalue().strip().splitlines()
        self.assertEqual(code, 1)
        self.assertIn("DEC-STATUS", lines[0])
        self.assertIn("DEC-DUPLICATE", lines[1])
        self.assertEqual(lines[-1], "2 findings (DEC-STATUS 1, DEC-DUPLICATE 1)")


HOOK_LOG = ("# Log: s\n\n| Time | Agent | Step | Status | Files | Commit | Checks | Decisions | Assumptions | Reason |\n"
       "|------|-------|------|--------|-------|--------|--------|-----------|-------------|--------|\n"
       "| 10:00 | senior | build-core | CORE_DONE | a | abc | build pass, test pass | - | - | done |\n"
       "| 10:10 | senior | fix | FIX_DONE | a | abd | build pass | - | - | done |\n")


class RunHookTests(unittest.TestCase):
    def findings(self, commands):
        with tempfile.TemporaryDirectory() as tmp:
            run = pathlib.Path(tmp)
            write(run / "log.md", HOOK_LOG)
            if commands is not None:
                write(run / "commands.jsonl", commands)
            return [f[1] for f in runcheck.check(run, "g2")]

    def test_missing_commands_file_gives_one_finding_with_the_cause(self):
        rules = self.findings(None)
        self.assertEqual(rules.count("RUN-HOOK"), 1)
        self.assertNotIn("RUN-EVIDENCE", rules)

    def test_empty_commands_file_gives_one_finding_with_the_cause(self):
        rules = self.findings("")
        self.assertEqual(rules.count("RUN-HOOK"), 1)
        self.assertNotIn("RUN-EVIDENCE", rules)

    def test_a_row_without_a_check_command_is_still_run_evidence(self):
        line = json.dumps({"agent_type": None, "command": "ls", "exit_code": 0}) + "\n"
        rules = self.findings(line)
        self.assertNotIn("RUN-HOOK", rules)
        self.assertEqual(rules.count("RUN-EVIDENCE"), 2)


UX_RUN_MD = ("# Run: s\n\n| Field | Value |\n|---|---|\n| Step | 9 UX review |\n| Mode | light |\n\n## Counters\n\n"
          "| Counter | Limit | Value |\n|---|---|---|\n| ux-review | 1 | 0 |\n")


class MockupTests(unittest.TestCase):
    """A story with a mock-up needs a UX review at G2, also without ux-design.md."""

    def rules(self, story, ux_design=False):
        with tempfile.TemporaryDirectory() as tmp:
            run = pathlib.Path(tmp)
            write(run / "run.md", UX_RUN_MD)
            write(run / "log.md", HOOK_LOG.split("| 10:00")[0])
            write(run / "story.md", story)
            if ux_design:
                write(run / "ux-design.md", "# Design\n")
            return [(f[1], f[2]) for f in runcheck.check(run, "g2") if "ux-review" in f[2] or f[0].endswith("ux-review.md")]

    def test_mockup_line_without_ux_design_needs_the_review(self):
        found = self.rules("# Story\n\n## Context\nMock-up: https://example.com/design/1\n")
        self.assertEqual({r for r, _ in found}, {"RUN-COUNTER", "RUN-FILE"})
        self.assertTrue(all("mock-up in story.md" in m for _, m in found))

    def test_no_mockup_and_no_ux_design_needs_no_review(self):
        self.assertEqual(self.rules("# Story\n\nNo design here.\n"), [])

    def test_ux_design_still_needs_the_review(self):
        found = self.rules("# Story\n", ux_design=True)
        self.assertEqual({r for r, _ in found}, {"RUN-COUNTER", "RUN-FILE"})


class FollowUpStatusTests(unittest.TestCase):
    def test_followup_words_are_valid_in_the_log(self):
        row = "| 11:00 | main | follow-up 01 | FOLLOWUP_DONE | - | - | - | - | - | done |\n"
        with tempfile.TemporaryDirectory() as tmp:
            run = pathlib.Path(tmp)
            write(run / "log.md", HOOK_LOG.split("| 10:00")[0] + row)
            rules = [f[1] for f in runcheck.check(run, "g2")]
        self.assertNotIn("RUN-LOG", rules)


class CiChecksTests(unittest.TestCase):
    def conventions(self, text):
        tmp = tempfile.TemporaryDirectory()
        self.addCleanup(tmp.cleanup)
        root = pathlib.Path(tmp.name)
        if text is not None:
            write(root / "docs/dev-team/conventions.md", text)
        return root

    def test_no_conventions_file_means_not_declared(self):
        self.assertIsNone(checks.ci_commands(self.conventions(None)))

    def test_no_section_means_not_declared(self):
        self.assertIsNone(checks.ci_commands(self.conventions("# Conventions\n\n## Build\n- `dotnet build`\n")))

    def test_section_commands_are_read_and_other_sections_are_not(self):
        text = "# C\n\n## CI checks\n- `dotnet build -warnaserror`\n* `npm run lint -- --max-warnings 0`\n\n## Other\n- `rm -rf x`\n"
        self.assertEqual(checks.ci_commands(self.conventions(text)),
                         [["dotnet", "build", "-warnaserror"], ["npm", "run", "lint", "--", "--max-warnings", "0"]])

    def test_empty_section_is_declared_with_no_commands(self):
        self.assertEqual(checks.ci_commands(self.conventions("## CI checks\n\nNone.\n")), [])

    def test_plan_adds_each_ci_command_as_a_check(self):
        root = self.conventions("## CI checks\n- `echo one`\n- `echo two`\n")
        names = [c[0] for c in checks.plan(root)]
        self.assertEqual(names, ["ci-1", "ci-2"])

    def test_report_says_when_parity_is_not_known(self):
        with tempfile.TemporaryDirectory() as tmp:
            out = pathlib.Path(tmp) / "checks.md"
            checks.write_report(out, pathlib.Path(tmp), [], ci_declared=False)
            self.assertIn("CI parity: not known", out.read_text(encoding="utf-8"))
            checks.write_report(out, pathlib.Path(tmp), [], ci_declared=True)
            self.assertNotIn("CI parity", out.read_text(encoding="utf-8"))


class HookTests(unittest.TestCase):
    def run_hook(self, script, stdin, **kwargs):
        return subprocess.run([sys.executable, str(PLUGIN / "hooks" / script)], input=stdin, capture_output=True,
                              text=True, **kwargs)

    def test_session_start_is_silent_outside_a_dev_team_project(self):
        with tempfile.TemporaryDirectory() as tmp:
            result = self.run_hook("session-start.py", json.dumps({"cwd": tmp}))
        self.assertEqual((result.returncode, result.stdout), (0, ""))

    def test_session_start_names_the_entry_skills_in_a_dev_team_project(self):
        with tempfile.TemporaryDirectory() as tmp:
            write(pathlib.Path(tmp) / "docs/dev-team/conventions.md", "# C\n")
            result = self.run_hook("session-start.py", json.dumps({"cwd": tmp}))
        context = json.loads(result.stdout)["hookSpecificOutput"]
        self.assertEqual(context["hookEventName"], "SessionStart")
        for skill in ("dev-team:deliver-story", "dev-flow:plan-and-build", "dev-team:process-review"):
            self.assertIn(skill, context["additionalContext"])

    def test_session_start_survives_bad_input(self):
        result = self.run_hook("session-start.py", "not json")
        self.assertEqual(result.returncode, 0)

    def test_each_hook_command_falls_back_from_python3_to_python_to_py(self):
        hooks = json.loads((PLUGIN / "hooks" / "hooks.json").read_text(encoding="utf-8"))["hooks"]
        commands = [h["command"] for event in hooks.values() for entry in event for h in entry["hooks"]]
        self.assertEqual(len(commands), 2)
        for command in commands:
            parts = [p.strip() for p in command.split("||")]
            self.assertEqual([p.split()[0] for p in parts], ["python3", "python", "py"])
            self.assertTrue(parts[2].startswith("py -3 "))

    def test_the_fallback_chain_runs_the_next_command_when_the_first_is_missing(self):
        shell = ["sh", "-c", "no-such-python3 x || cat"]
        result = subprocess.run(shell, input="payload", capture_output=True, text=True)
        self.assertEqual(result.stdout, "payload")


if __name__ == "__main__":
    unittest.main()
