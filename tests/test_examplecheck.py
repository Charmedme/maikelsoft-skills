"""Tests for shared/examplecheck.py. Run: python3 -m unittest discover -s tests"""
import importlib.util
import pathlib
import unittest

SCRIPT = pathlib.Path(__file__).resolve().parents[1] / "shared" / "examplecheck.py"
spec = importlib.util.spec_from_file_location("examplecheck", SCRIPT)
ec = importlib.util.module_from_spec(spec)
spec.loader.exec_module(ec)

GOOD = """# Examples: ZK-36 Tijdslimiet

## Rules

- AC1 R1: De host zet de tijdslimiet aan of uit.
- AC2 R1 (limit: 2 minuten): Na de tijd gaat de ronde door.

## Examples

| Rule | Case | Given | When | Then | Source |
|------|------|-------|------|------|--------|
| AC1 R1 | normal | wachtkamer | host zet aan | timer 2 | item |
| AC2 R1 | below | 1 antwoord | 1:59 | ronde wacht | item |
| AC2 R1 | at | 1 antwoord | 2:00 | ronde gaat door | open: P1 |
| AC2 R1 | above | 1 antwoord | 2:01 | ronde gaat door | item |

## Words with more than one meaning

- "na 2 minuten" (AC2): op 2:00 of na 2:00. Answer: open: P1

## Answers from decisions

- Q40: "standaard 2 minuten voor het kiezen" (rule AC2 R1)

## Open questions

- P1 (AC2 R1): Gaat de ronde door op precies 2:00? Recommended: ja.
"""


class ExampleCheck(unittest.TestCase):
    def test_good_file(self):
        errors, waiting, rules, questions = ec.check(GOOD)
        self.assertEqual(errors, [])
        self.assertEqual(waiting, [("AC2 R1", "P1")])
        self.assertEqual(rules, 2)

    def test_limit_needs_at(self):
        errors, *_ = ec.check(GOOD.replace("| AC2 R1 | at |", "| AC2 R1 | normal |"))
        self.assertTrue(any("no example for: at" in e for e in errors))

    def test_rule_without_example(self):
        errors, *_ = ec.check(GOOD.replace("- AC1 R1:", "- AC1 R2: extra.\n- AC1 R1:"))
        self.assertTrue(any("AC1 R2 has no example" in e for e in errors))

    def test_missing_section(self):
        errors, *_ = ec.check(GOOD.replace("## Words with more than one meaning", "## Words"))
        self.assertTrue(any("Words with more than one meaning" in e for e in errors))

    def test_open_source_needs_question(self):
        errors, *_ = ec.check(GOOD.replace("- P1 (AC2 R1)", "- (AC2 R1)"))
        self.assertTrue(any("P1 is not under Open questions" in e for e in errors))

    def test_unknown_rule_in_example(self):
        errors, *_ = ec.check(GOOD.replace("| AC1 R1 | normal |", "| AC9 R1 | normal |"))
        self.assertTrue(any("does not exist" in e for e in errors))


if __name__ == "__main__":
    unittest.main()
