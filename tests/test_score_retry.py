"""Tests for the retry of a test container that did not start, in evals/dev-stack/score.py."""
import pathlib
import sys
import tempfile
import unittest
from unittest import mock

sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent.parent / "evals" / "dev-stack"))
import score

CRASH = "ContainerNotRunningException: Container 4884e462 is not running"


class ContainerStartFailedTests(unittest.TestCase):
    def test_marker_in_output(self):
        self.assertTrue(score.container_start_failed(CRASH, {}))

    def test_marker_in_test_message(self):
        self.assertTrue(score.container_start_failed("", {"T": ("Failed", CRASH)}))

    def test_real_test_failure_is_not_a_start_failure(self):
        self.assertFalse(score.container_start_failed("failed: 1", {"T": ("Failed", "Expected 2 but found 3")}))


class RunTestsRetryTests(unittest.TestCase):
    def run_with(self, outputs):
        calls = iter(outputs)
        with tempfile.TemporaryDirectory() as tmp, \
                mock.patch.object(score, "run", side_effect=lambda *a, **k: next(calls)) as run, \
                mock.patch.object(score, "parse_trx", return_value={}):
            code, out, _ = score.run_tests(tmp, pathlib.Path(tmp) / "trx")
        return code, out, run.call_count

    def test_retries_after_a_start_crash_and_stops_when_it_works(self):
        code, _, calls = self.run_with([(1, CRASH), (0, "passed")])
        self.assertEqual((code, calls), (0, 2))

    def test_gives_up_after_the_attempts(self):
        code, _, calls = self.run_with([(1, CRASH)] * score.TEST_START_ATTEMPTS)
        self.assertEqual((code, calls), (1, score.TEST_START_ATTEMPTS))

    def test_does_not_retry_a_real_failure(self):
        _, _, calls = self.run_with([(1, "Assert failed")])
        self.assertEqual(calls, 1)


if __name__ == "__main__":
    unittest.main()
