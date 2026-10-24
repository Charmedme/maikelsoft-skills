"""Tests for evals/runwatch.py. Run: python3 -m unittest discover -s tests"""
import json
import pathlib
import sys
import tempfile
import unittest

sys.path.insert(0, str(pathlib.Path(__file__).resolve().parents[1] / "evals"))
import runwatch  # noqa: E402

FAST = {"poll_seconds": 0.2}


def script(body):
    return [sys.executable, "-c", "import json,sys,time\n" + body]


def failing_call(i, same=True):
    use = {"type": "assistant", "message": {"content": [{"type": "tool_use", "id": f"t{i}", "name": "Bash",
                                                          "input": {"command": "dotnet test" if same else f"x{i}"}}]}}
    res = {"type": "user", "message": {"content": [{"type": "tool_result", "tool_use_id": f"t{i}", "is_error": True,
                                                    "content": "build failed" if same else f"err {i}"}]}}
    return json.dumps(use) + "\n" + json.dumps(res) + "\n"


class RunWatch(unittest.TestCase):
    def run_it(self, body, limits):
        d = pathlib.Path(tempfile.mkdtemp())
        code = runwatch.run_watched(script(body), d, None, d, dict(FAST, **limits), log=lambda *_: None)
        stop = json.loads((d / "stop.json").read_text()) if (d / "stop.json").exists() else None
        return code, stop

    def test_normal_end_has_no_stop(self):
        code, stop = self.run_it("print('{}')", {})
        self.assertEqual(code, 0)
        self.assertIsNone(stop)

    def test_same_failing_call_stops_the_run(self):
        lines = "".join(failing_call(i) for i in range(5))
        code, stop = self.run_it(f"sys.stdout.write({lines!r}); sys.stdout.flush(); time.sleep(30)", {"max_repeats": 4})
        self.assertIn("same failing tool call", stop["reason"])

    def test_error_streak_stops_the_run(self):
        lines = "".join(failing_call(i, same=False) for i in range(6))
        code, stop = self.run_it(f"sys.stdout.write({lines!r}); sys.stdout.flush(); time.sleep(30)", {"max_errors": 5})
        self.assertIn("tool errors in a row", stop["reason"])

    def test_stall_stops_the_run(self):
        code, stop = self.run_it("print('{}', flush=True); time.sleep(30)", {"stall_minutes": 0.02})
        self.assertIn("no output", stop["reason"])

    def test_wall_time_stops_the_run(self):
        body = "\nfor i in range(300):\n    print('{}', flush=True); time.sleep(0.1)"
        code, stop = self.run_it(body, {"max_minutes": 0.02, "stall_minutes": 5})
        self.assertIn("wall time", stop["reason"])


if __name__ == "__main__":
    unittest.main()
