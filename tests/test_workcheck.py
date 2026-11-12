"""Tests for the workcheck.py script of the build skill. Run: python3 -m unittest tests.test_workcheck"""
import pathlib
import subprocess
import sys
import tempfile
import unittest

SCRIPTS = pathlib.Path(__file__).resolve().parents[1] / "plugins" / "dev-flow" / "skills" / "build" / "scripts"
sys.path.insert(0, str(SCRIPTS))
import workcheck  # noqa: E402

RUN = "---\nflow: solo\nitem: ORD-1\nsize: {size}\nstep: plan gate\nbaseline: set\n---\n\n# Run\n"

SPEC = """# Spec: ORD-1

<!-- BEGIN behaviour spec: ORD-1 -->
```gherkin
  Rule: R1 A return within 30 days is free (AC1)
    Scenario: at the limit
      When the customer returns the order on day 30
      Then the return is free
  Rule: R2 A return after 30 days costs a fee (AC2)
    Scenario: above the limit
      When the customer returns the order on day 31
      Then the return costs 5 euro
```
<!-- END behaviour spec -->

## Checks

| Part | Check |
|------|-------|
{rows}
"""

ROWS = "| R1 Free return | `tests/Specs/R1FreeReturn.cs` |\n| R2 Fee | `tests/Specs/R2Fee.cs` |"

PLAN = "# Plan: ORD-1\n\n## Scenarios\n\n{lines}\n\n## Files\n\n- src/Returns.cs\n"


class WorkCheck(unittest.TestCase):
    def setUp(self):
        tmp = tempfile.TemporaryDirectory()
        self.addCleanup(tmp.cleanup)
        self.repo = pathlib.Path(tmp.name)
        subprocess.run(["git", "init", "-q", str(self.repo)], check=True)
        self.folder = self.repo / "docs" / "work" / "ord-1"
        self.folder.mkdir(parents=True)

    def write(self, size="small", rows=ROWS, plan=None, spec=SPEC):
        (self.folder / "run.md").write_text(RUN.format(size=size), encoding="utf-8")
        (self.folder / "spec.md").write_text(spec.format(rows=rows), encoding="utf-8")
        if plan is not None:
            (self.folder / "plan.md").write_text(PLAN.format(lines=plan), encoding="utf-8")

    def codes(self, final=False):
        return [code for code, _ in workcheck.check(self.folder, final)]

    def test_a_small_folder_with_a_check_for_each_rule_passes(self):
        self.write()
        self.assertEqual(self.codes(), [])

    def test_a_rule_without_a_row_fails(self):
        self.write(rows="| R1 Free return | `tests/Specs/R1FreeReturn.cs` |")
        self.assertEqual(self.codes(), ["CHECK-RULE"])

    def test_a_row_without_a_check_fails(self):
        self.write(rows="| R1 Free return | `tests/Specs/R1FreeReturn.cs` |\n| R2 Fee |  |")
        self.assertIn("CHECK-EMPTY", self.codes())

    def test_review_only_is_a_check(self):
        self.write(rows="| R1 Free return | `tests/Specs/R1FreeReturn.cs` |\n| R2 Fee | review only |")
        self.assertEqual(self.codes(), [])

    def test_a_spec_without_a_checks_table_fails(self):
        self.write(spec=SPEC.split("## Checks")[0])
        self.assertIn("CHECKS", self.codes())

    def test_a_missing_spec_fails(self):
        (self.folder / "run.md").write_text(RUN.format(size="small"), encoding="utf-8")
        self.assertEqual(self.codes(), ["SPEC-FILE"])

    def test_a_missing_header_fails(self):
        self.write()
        (self.folder / "run.md").write_text("# Run\n", encoding="utf-8")
        self.assertIn("RUN-HEADER", self.codes())

    def test_a_chore_without_a_spec_block_needs_a_row(self):
        chore = "# Spec: ORD-1\n\n## Checks\n\n| Part | Check |\n|------|-------|\n{rows}\n"
        self.write(spec=chore, rows="| Update the package | `tests/BuildTests.cs` |")
        self.assertEqual(self.codes(), [])
        self.write(spec=chore, rows="")
        self.assertIn("CHECKS", self.codes())

    def test_medium_needs_the_scenarios_in_the_plan(self):
        self.write(size="medium")
        self.assertIn("PLAN", self.codes())
        self.write(size="medium", plan="1. R1 Free return\n2. R2 Fee")
        self.assertEqual(self.codes(), [])

    def test_medium_fails_when_a_tested_rule_is_not_in_the_plan(self):
        self.write(size="medium", plan="1. R1 Free return")
        self.assertEqual(self.codes(), ["PLAN-RULE"])

    def test_final_needs_each_test_file(self):
        self.write()
        self.assertEqual(self.codes(final=True).count("CHECK-FILE"), 2)
        for name in ("R1FreeReturn.cs", "R2Fee.cs"):
            (self.repo / "tests" / "Specs").mkdir(parents=True, exist_ok=True)
            (self.repo / "tests" / "Specs" / name).write_text("// test\n", encoding="utf-8")
        self.assertEqual(self.codes(final=True), [])

    def test_the_command_line_exits_1_on_an_error(self):
        self.write(rows="")
        p = subprocess.run([sys.executable, str(SCRIPTS / "workcheck.py"), str(self.folder)],
                           capture_output=True, text=True)
        self.assertEqual(p.returncode, 1)
        self.assertIn("ERROR", p.stdout)


if __name__ == "__main__":
    unittest.main()
