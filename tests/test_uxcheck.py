"""Tests for the uxcheck.py script of the ux-practice skill. Run: python3 -m unittest discover -s tests"""
import importlib.util
import pathlib
import sys
import tempfile
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[1]
SCRIPTS = ROOT / "plugins" / "ux-design" / "skills" / "ux-practice" / "scripts"
PATTERNS = ROOT / "plugins" / "ux-design" / "skills" / "ux-patterns" / "references" / "patterns.md"
FIXTURES = ROOT / "evals" / "ux-design" / "fixtures"
sys.path.insert(0, str(SCRIPTS))
import uxcheck  # noqa: E402

DECISIONS = """| purpose | Assign work | product: AC-1 |
| main-task | Assign | product: story |
| pattern | Data table | pattern: {pattern} |
| layout | header, filters, table | system: page layout |
| hierarchy | 1 table 2 filters 3 actions | pattern: PAT-ACT-01 |
| components | DataTable | system: components |
| density | compact | decision: UXD-001 |
| states | default, empty | product: AC-5 |
| copy | Assign | product: terms |
| motion | none | system: motion tokens |"""

STATES = ["default", "empty", "loading", "partial", "error", "success", "disabled", "overflow", "offline", "error-undo"]


def design(pattern="PAT-LIST-01", drop_state=None, failure_state="error-undo", options=2, mode="full"):
    rows = "\n".join(f"| {s} | sees | does | recovers |" for s in STATES if s != drop_state)
    opts = "\n".join(f"| {c} | layout {c} | 3 | 3 | 3 | 3 | 3 | 15 |" for c in "ABC"[:options])
    return f"""# UX design: test

Mode: {mode}
Surface: operate

## Frame

| Question | Answer | Evidence |
|----------|--------|----------|
| Problem | Slow planning | checked: story |

## Flow

1. Assign. Screen: list.

### Failure paths

| Step | Failure | State |
|------|---------|-------|
| 1 | The undo fails | {failure_state} |

## Options

| Option | Structure | Task fit | Clarity | Consistency | Risk | Cost | Total |
|--------|-----------|----------|---------|-------------|------|------|-------|
{opts}

Selected: A. Reason: fastest.

## Element inventory

| Element | Home | Component | Screens |
|---------|------|-----------|---------|
| filter bar | top-bar | FilterBar | list |

## Non-text contrast

| Part | Color | Next to | Ratio | Needs |
|------|-------|---------|-------|-------|
| input border | #6b7785 | #ffffff | 4.6 | 3:1 |

## Screen: list

| Decision | Value | Source |
|----------|-------|--------|
{DECISIONS.format(pattern=pattern)}

### States: list

| State | User sees | User can do | Recovery |
|-------|-----------|-------------|----------|
{rows}

## Decisions

| ID | Decision | Status | Reason |
|----|----------|--------|--------|
| UXD-001 | Compact rows | proposed | Daily use |
"""


def run_design(text):
    with tempfile.TemporaryDirectory() as tmp:
        path = pathlib.Path(tmp) / "ux-design.md"
        path.write_text(text, encoding="utf-8")
        report = uxcheck.Report()
        uxcheck.check_design(str(path), str(PATTERNS), None, report)
        return {i["rule"] for i in report.items if i["level"] == "error"}


class DesignTests(unittest.TestCase):
    def test_valid_design_passes(self):
        self.assertEqual(run_design(design()), set())

    def test_unknown_pattern_id(self):
        self.assertIn("DS-PATTERN-ID", run_design(design(pattern="PAT-XYZ-99")))

    def test_missing_core_state(self):
        self.assertIn("DS-STATES", run_design(design(drop_state="offline")))

    def test_failure_path_without_state(self):
        self.assertIn("DS-FAILURE", run_design(design(failure_state="error-save")))

    def test_one_option_is_not_enough(self):
        self.assertIn("DS-OPTIONS", run_design(design(options=1)))

    def test_light_mode_needs_no_options(self):
        self.assertEqual(run_design(design(options=0, mode="light")), set())

    def test_decision_without_source(self):
        text = design().replace("| copy | Assign | product: terms |", "| copy | Assign | my taste |")
        self.assertIn("DS-SOURCE", run_design(text))


class SystemTests(unittest.TestCase):
    def audit(self, fixture):
        ds = FIXTURES / fixture / "design-system"
        report = uxcheck.Report()
        uxcheck.check_system([str(ds / "tokens.css"), str(ds / "components.css")], report)
        return {i["rule"] for i in report.items if i["level"] == "error"}

    def test_strong_system_has_no_errors(self):
        # The input border (border-strong on the surface, 1.84:1) fails WCAG 1.4.11.
        # The fixture stays as it is, because older eval cases use it.
        self.assertEqual(self.audit("fieldline"), {"SYS-NONTEXT"})

    def test_faulty_system_shows_planted_faults(self):
        self.assertEqual(self.audit("fieldline-faulty"),
                         {"SYS-CONTRAST", "SYS-RULE-CONTRAST", "SYS-GRADIENT", "SYS-FOCUS", "SYS-SCALE", "SYS-NONTEXT"})

    def test_contrast_formula(self):
        self.assertAlmostEqual(uxcheck.contrast((0, 0, 0), (255, 255, 255)), 21.0, places=1)
        self.assertIsNone(uxcheck.parse_color("rgba(0, 0, 0, 0)"))
        self.assertEqual(uxcheck.parse_color("#fff"), (255, 255, 255))


@unittest.skipUnless(importlib.util.find_spec("playwright"), "Playwright is not installed")
class MockupTests(unittest.TestCase):
    def check(self, body):
        with tempfile.TemporaryDirectory() as tmp:
            ds = FIXTURES / "fieldline" / "design-system"
            html = pathlib.Path(tmp) / "m.html"
            html.write_text(f'<!doctype html><html lang="en"><head><meta charset="utf-8">'
                            f'<link rel="stylesheet" href="{(ds / "tokens.css").as_uri()}">'
                            f'<link rel="stylesheet" href="{(ds / "components.css").as_uri()}"></head>'
                            f'<body><section data-screen="list" data-state="default">{body}</section></body></html>',
                            encoding="utf-8")
            report = uxcheck.Report()
            uxcheck.check_mockups([str(html)], None, uxcheck.PRIMARY, None, report)
            return {i["rule"] for i in report.items if i["level"] == "error"}

    def test_clean_frame(self):
        self.assertEqual(self.check('<header class="fl-page-header"><h1>Work orders</h1></header><button class="fl-btn fl-btn--primary">Assign</button>'), set())

    def test_planted_tells(self):
        rules = self.check('<button class="fl-btn fl-btn--primary">A</button><button class="fl-btn fl-btn--primary">B</button>'
                           '<input class="fl-input" placeholder="Search"><p style="color:#bbbbbb">Low contrast text</p>')
        self.assertTrue({"TELL-01", "TELL-09", "TELL-11", "TELL-03"} <= rules)


if __name__ == "__main__":
    unittest.main()
