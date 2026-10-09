"""Tests for the workcheck.py script of the plan-and-build skill. Run: python3 -m unittest discover -s tests"""
import pathlib
import sys
import tempfile
import unittest

SCRIPTS = pathlib.Path(__file__).resolve().parents[1] / "plugins" / "dev-flow" / "skills" / "plan-and-build" / "scripts"
sys.path.insert(0, str(SCRIPTS))
import workcheck  # noqa: E402

PLAN = """# Plan: Test

## Documents

- [Spec](specs/main.md)

## Tickets

| No | Ticket | Blocked by | Status |
|----|--------|------------|--------|
{rows}

## Gates

- G1 Shared understanding: {g1}
- G2 Prototype validated: {g2}
- G3 Tickets approved: {g3}
- G4 Done: open
"""

TICKET = """# {n}: Ticket {n}

**Spec:** [Main](../specs/main.md)
**Blocked by:** {blocked}
**Status:** {status}

## Acceptance criteria

- [{mark}] It works.
"""


def make(folder, tickets, gates=("approved", "approved", "approved")):
    """tickets: list of (number, blocked_by, status)."""
    root = pathlib.Path(folder)
    (root / "specs").mkdir()
    (root / "specs" / "main.md").write_text("# Spec: Main\n", encoding="utf-8")
    (root / "tickets").mkdir()
    rows = []
    for n, blocked, status in tickets:
        mark = "x" if status == "done" else " "
        (root / "tickets" / f"{n}-t.md").write_text(
            TICKET.format(n=n, blocked=blocked, status=status, mark=mark), encoding="utf-8")
        rows.append(f"| {n} | [Ticket {n}](tickets/{n}-t.md) | {blocked} | {status} |")
    (root / "plan.md").write_text(
        PLAN.format(rows="\n".join(rows), g1=gates[0], g2=gates[1], g3=gates[2]), encoding="utf-8")
    return root


def messages(root):
    findings, tickets, _ = workcheck.check(root)
    return [f.message for f in findings if f.severity == "error"], tickets


class Valid(unittest.TestCase):
    def test_valid_folder_has_no_errors(self):
        with tempfile.TemporaryDirectory() as d:
            root = make(d, [("01", "None", "done"), ("02", "01", "todo")])
            errors, _ = messages(root)
            self.assertEqual(errors, [])

    def test_next_gives_unblocked_todo_tickets(self):
        with tempfile.TemporaryDirectory() as d:
            root = make(d, [("01", "None", "done"), ("02", "01", "todo"), ("03", "02", "todo")])
            _, tickets = messages(root)
            self.assertEqual([t.number for t in workcheck.frontier(tickets)], ["02"])


class Errors(unittest.TestCase):
    def test_missing_blocker(self):
        with tempfile.TemporaryDirectory() as d:
            errors, _ = messages(make(d, [("01", "05", "draft")], ("open", "open", "open")))
            self.assertTrue(any("Blocker 05 does not exist" in e for e in errors))

    def test_cycle(self):
        with tempfile.TemporaryDirectory() as d:
            errors, _ = messages(make(d, [("01", "02", "draft"), ("02", "01", "draft")], ("open", "open", "open")))
            self.assertTrue(any("cycle" in e for e in errors))

    def test_build_before_g3(self):
        with tempfile.TemporaryDirectory() as d:
            errors, _ = messages(make(d, [("01", "None", "todo")], ("approved", "approved", "open")))
            self.assertTrue(any("G3 is not passed" in e for e in errors))

    def test_g2_cannot_be_assumed(self):
        with tempfile.TemporaryDirectory() as d:
            errors, _ = messages(make(d, [("01", "None", "draft")], ("assumed", "assumed", "open")))
            self.assertTrue(any("only a human" in e for e in errors))

    def test_done_with_open_blocker(self):
        with tempfile.TemporaryDirectory() as d:
            errors, _ = messages(make(d, [("01", "None", "todo"), ("02", "01", "done")]))
            self.assertTrue(any("blocker 01 is todo" in e for e in errors))

    def test_plan_table_differs(self):
        with tempfile.TemporaryDirectory() as d:
            root = make(d, [("01", "None", "draft")], ("open", "open", "open"))
            plan = root / "plan.md"
            plan.write_text(plan.read_text().replace("| None | draft |", "| None | todo |"), encoding="utf-8")
            errors, _ = messages(root)
            self.assertTrue(any("differs from the ticket file" in e for e in errors))

    def test_done_needs_all_criteria_checked(self):
        with tempfile.TemporaryDirectory() as d:
            root = make(d, [("01", "None", "done")])
            t = root / "tickets" / "01-t.md"
            t.write_text(t.read_text() + "- [ ] Another check.\n", encoding="utf-8")
            errors, _ = messages(root)
            self.assertTrue(any("criteria are not checked" in e for e in errors))

    def test_bad_file_name(self):
        with tempfile.TemporaryDirectory() as d:
            root = make(d, [], ("open", "open", "open"))
            (root / "tickets" / "first.md").write_text("# 01: X\n", encoding="utf-8")
            errors, _ = messages(root)
            self.assertTrue(any("NN-slug.md" in e for e in errors))

    def test_no_plan(self):
        with tempfile.TemporaryDirectory() as d:
            errors, _ = messages(pathlib.Path(d))
            self.assertEqual(errors, ["plan.md does not exist"])


if __name__ == "__main__":
    unittest.main()
