"""Tests for evals/dev-stack/run.py: the final message of a run."""
import json
import pathlib
import sys
import tempfile
import unittest

sys.path.insert(0, str(pathlib.Path(__file__).resolve().parents[1] / "evals" / "dev-stack"))
sys.path.insert(0, str(pathlib.Path(__file__).resolve().parents[1] / "evals"))

import run  # noqa: E402


def stream(*events):
    folder = pathlib.Path(tempfile.mkdtemp())
    path = folder / "stream.jsonl"
    path.write_text("\n".join(json.dumps(e) for e in events) + "\n", encoding="utf-8")
    return path


class FinalMessageTests(unittest.TestCase):
    def test_one_result_is_the_final_message(self):
        path = stream({"type": "assistant"}, {"type": "result", "result": "## Findings\n1. P1"})
        self.assertEqual(run.final_message(path), "## Findings\n1. P1")

    def test_short_note_after_a_task_notification_does_not_replace_the_report(self):
        report = "## Findings\n" + "a finding. " * 50
        path = stream({"type": "result", "result": report},
                      {"type": "system", "subtype": "task_notification"},
                      {"type": "result", "result": "That notification is only the background watcher."})
        self.assertEqual(run.final_message(path), report)

    def test_no_result_gives_an_empty_message(self):
        self.assertEqual(run.final_message(stream({"type": "assistant"})), "")

    def test_broken_line_is_skipped(self):
        path = stream({"type": "result", "result": "report"})
        path.write_text(path.read_text() + '{"type":"result", broken\n', encoding="utf-8")
        self.assertEqual(run.final_message(path), "report")


if __name__ == "__main__":
    unittest.main()
