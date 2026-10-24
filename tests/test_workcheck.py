"""Tests for the workcheck.py script of the plan-and-build skill. Run: python3 -m unittest discover -s tests"""
import pathlib
import sys
import tempfile
import unittest

SCRIPTS = pathlib.Path(__file__).resolve().parents[1] / "plugins" / "dev-flow" / "skills" / "plan-and-build" / "scripts"
sys.path.insert(0, str(SCRIPTS))
import workcheck  # noqa: E402

PLAN = """# Plan: ORD-1 Test

**Item:** ORD-1
**Tier:** {tier} feature. Test.

## Item

- AC1: It works.

## Plan

| No | Ticket | AC | Blocked by | Status |
|----|--------|----|------------|--------|
{rows}

## Waiting for the PO

None.

## Gates

{gate_lines}
"""

TICKET = """# {n}: Ticket {n}

**Item:** [ORD-1](../plan.md#item), AC1
**Blocked by:** {blocked}
**Status:** {status}

## Acceptance criteria

- [{mark}] It works.
"""


def make(folder, tickets, gates=("approved", "open"), tier="medium"):
    """tickets: list of (number, blocked_by, status). gates: the states of the gates of the tier, in order."""
    root = pathlib.Path(folder)
    (root / "tickets").mkdir()
    rows = []
    for n, blocked, status in tickets:
        mark = "x" if status == "done" else " "
        (root / "tickets" / f"{n}-t.md").write_text(
            TICKET.format(n=n, blocked=blocked, status=status, mark=mark), encoding="utf-8")
        rows.append(f"| {n} | [Ticket {n}](tickets/{n}-t.md) | AC1 | {blocked} | {status} |")
    names = workcheck.TIERS.get(tier, ("Spec", "Done"))
    gate_lines = "\n".join(f"- {g}: {s}" for g, s in zip(names, gates))
    (root / "plan.md").write_text(PLAN.format(rows="\n".join(rows), tier=tier, gate_lines=gate_lines), encoding="utf-8")
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


class Tiers(unittest.TestCase):
    def test_small_needs_spec_and_done(self):
        with tempfile.TemporaryDirectory() as d:
            errors, _ = messages(make(d, [], ("open", "open"), tier="small"))
            self.assertEqual(errors, [])


class Errors(unittest.TestCase):
    def test_missing_blocker(self):
        with tempfile.TemporaryDirectory() as d:
            errors, _ = messages(make(d, [("01", "05", "draft")], ("open", "open")))
            self.assertTrue(any("Blocker 05 does not exist" in e for e in errors))

    def test_cycle(self):
        with tempfile.TemporaryDirectory() as d:
            errors, _ = messages(make(d, [("01", "02", "draft"), ("02", "01", "draft")], ("open", "open")))
            self.assertTrue(any("cycle" in e for e in errors))

    def test_build_before_plan_gate(self):
        with tempfile.TemporaryDirectory() as d:
            errors, _ = messages(make(d, [("01", "None", "todo")], ("open", "open")))
            self.assertTrue(any("gate Spec is not passed" in e for e in errors))

    def test_done_cannot_be_assumed(self):
        with tempfile.TemporaryDirectory() as d:
            errors, _ = messages(make(d, [("01", "None", "done")], ("approved", "assumed")))
            self.assertTrue(any("only a human" in e for e in errors))

    def test_large_needs_design_before_plan(self):
        with tempfile.TemporaryDirectory() as d:
            errors, _ = messages(make(d, [("01", "None", "draft")], ("open", "approved", "open"), tier="large"))
            self.assertTrue(any("Design is not" in e for e in errors))

    def test_plan_not_assumed_while_po_waits(self):
        with tempfile.TemporaryDirectory() as d:
            root = make(d, [("01", "None", "draft")], ("assumed", "open"))
            plan = root / "plan.md"
            plan.write_text(plan.read_text().replace("## Waiting for the PO\n\nNone.", "## Waiting for the PO\n\n- P1: Where does the mail address come from?"), encoding="utf-8")
            errors, _ = messages(root)
            self.assertTrue(any("waits for the PO" in e for e in errors))

    def test_bad_tier(self):
        with tempfile.TemporaryDirectory() as d:
            errors, _ = messages(make(d, [("01", "None", "draft")], tier="huge"))
            self.assertTrue(any("Tier" in e for e in errors))

    def test_ac_without_ticket(self):
        with tempfile.TemporaryDirectory() as d:
            root = make(d, [("01", "None", "draft")], ("open", "open"))
            plan = root / "plan.md"
            plan.write_text(plan.read_text().replace("- AC1: It works.", "- AC1: It works.\n- AC2: It also fails well."), encoding="utf-8")
            errors, _ = messages(root)
            self.assertTrue(any("AC2 of the item is in no ticket" in e for e in errors))

    def test_done_with_open_blocker(self):
        with tempfile.TemporaryDirectory() as d:
            errors, _ = messages(make(d, [("01", "None", "todo"), ("02", "01", "done")]))
            self.assertTrue(any("blocker 01 is todo" in e for e in errors))

    def test_plan_table_differs(self):
        with tempfile.TemporaryDirectory() as d:
            root = make(d, [("01", "None", "draft")], ("open", "open"))
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
            root = make(d, [], ("open", "open"))
            (root / "tickets" / "first.md").write_text("# 01: X\n", encoding="utf-8")
            errors, _ = messages(root)
            self.assertTrue(any("NN-slug.md" in e for e in errors))

    def test_no_plan(self):
        with tempfile.TemporaryDirectory() as d:
            errors, _ = messages(pathlib.Path(d))
            self.assertEqual(errors, ["plan.md does not exist"])


if __name__ == "__main__":
    unittest.main()
