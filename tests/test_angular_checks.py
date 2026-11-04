"""Tests for evals/dev-stack/angular_checks.py (no Node, no browser). Run: python3 -m unittest discover -s tests"""
import json
import pathlib
import sys
import tempfile
import unittest
from unittest import mock

DEV_STACK = pathlib.Path(__file__).resolve().parents[1] / "evals" / "dev-stack"
sys.path.insert(0, str(DEV_STACK))
import angular_checks  # noqa: E402
import score  # noqa: E402

SPEC = """import { TestBed } from '@angular/core/testing';

describe('ProductList', () => {
  it('shows one row for each product', () => {});
  test('works with test()', () => {});
  it.each([1, 2])('runs for %s', (n) => {});
  it.only('focused', () => {});
  // it('a comment is not a test', () => {});
  /* it('a block comment is not a test either', () => {}); */
  xit('a skipped test is not counted', () => {});
  const submit = () => {};
  submit();
});
"""


def write(root, rel, text):
    path = pathlib.Path(root, rel)
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(text, encoding="utf-8")


class CountSpecTests(unittest.TestCase):
    def test_counts_it_and_test_calls_without_comments(self):
        with tempfile.TemporaryDirectory() as tmp:
            write(tmp, "src/app/product-list.spec.ts", SPEC)
            self.assertEqual(angular_checks.count_spec_tests(tmp), 4)

    def test_skips_node_modules_dist_and_other_files(self):
        with tempfile.TemporaryDirectory() as tmp:
            write(tmp, "node_modules/pkg/a.spec.ts", "it('x', () => {});\n")
            write(tmp, "dist/a.spec.ts", "it('x', () => {});\n")
            write(tmp, "src/app/a.ts", "it('not a spec file', () => {});\n")
            write(tmp, "src/app/b.spec.ts", "it('counted', () => {});\n")
            self.assertEqual(angular_checks.count_spec_tests(tmp), 1)


class Dispatch(unittest.TestCase):
    def test_handles_only_angular_kinds_and_angular_test_count(self):
        self.assertTrue(angular_checks.handles("ng_build", {}))
        self.assertTrue(angular_checks.handles("ui_probe", {"stack": "angular"}))
        self.assertTrue(angular_checks.handles("tests_added_min", {"stack": "angular"}))
        self.assertFalse(angular_checks.handles("tests_added_min", {}))
        self.assertFalse(angular_checks.handles("grep_absent", {"stack": "angular"}))

    def test_tests_added_min_compares_with_the_fixture(self):
        with tempfile.TemporaryDirectory() as fixture, tempfile.TemporaryDirectory() as repo:
            write(fixture, "src/a.spec.ts", "it('old', () => {});\n")
            write(repo, "src/a.spec.ts", "it('old', () => {});\nit('new 1', () => {});\ntest('new 2', () => {});\n")
            ok, got = angular_checks.check("tests_added_min", pathlib.Path(repo), {"stack": "angular"},
                                           {"value": 2}, pathlib.Path(fixture))
            self.assertTrue(ok)
            self.assertEqual(got, 2)

    def test_score_run_sends_angular_kinds_to_the_module(self):
        ev = {"name": "x", "fixture": "angular-products", "stack": "angular",
              "assertions": [{"id": "C1", "check": "ng_build"}, {"id": "C3", "check": "tests_added_min", "value": 1}]}
        calls = []

        def fake(kind, repo, ev_, a, fixture):
            calls.append(kind)
            return True, "fake"

        with mock.patch.object(angular_checks, "check", side_effect=fake):
            results = score.score_run(ev, pathlib.Path("/nonexistent"))
        self.assertEqual(calls, ["ng_build", "tests_added_min"])
        self.assertEqual([r["ok"] for r in results], [True, True])


class UiProbe(unittest.TestCase):
    def test_reports_each_named_test(self):
        results = {"a": {"outcome": "Passed", "message": ""}, "b": {"outcome": "Failed", "message": "focus on body"}}
        with mock.patch.object(angular_checks, "probe_results", return_value=(results, "log")):
            ok, got = angular_checks.check_ui_probe("/r", {}, {"probe": "p", "tests": ["a", "b", "c"]})
        self.assertFalse(ok)
        self.assertEqual(got["a"], "Passed")
        self.assertEqual(got["b"], "Failed: focus on body")
        self.assertTrue(got["c"].startswith("Missing"))

    def test_passes_when_all_named_tests_pass(self):
        results = {"a": {"outcome": "Passed", "message": ""}, "b": {"outcome": "Failed", "message": "x"}}
        with mock.patch.object(angular_checks, "probe_results", return_value=(results, "log")):
            ok, _ = angular_checks.check_ui_probe("/r", {}, {"probe": "p", "tests": ["a"]})
        self.assertTrue(ok)

    def test_fails_when_the_probe_did_not_run(self):
        with mock.patch.object(angular_checks, "probe_results", return_value=(None, "build failed")):
            ok, got = angular_checks.check_ui_probe("/r", {}, {"probe": "p", "tests": ["a"]})
        self.assertFalse(ok)
        self.assertIn("build failed", got)

    def test_mock_server_comes_from_the_fixture(self):
        server = angular_checks.mock_server({"fixture": "angular-products"})
        self.assertEqual(server, DEV_STACK / "fixtures" / "angular-products" / "mock-api" / "server.py")
        self.assertIsNone(angular_checks.mock_server({"fixture": "orders-api"}))


class CaseFiles(unittest.TestCase):
    def test_each_probe_test_of_the_cases_exists(self):
        for name in ("angular-delete", "angular-edit-hard"):
            case = json.loads((DEV_STACK / "cases" / f"{name}.json").read_text(encoding="utf-8"))
            for a in case["assertions"]:
                if a.get("check") != "ui_probe":
                    continue
                text = (DEV_STACK / "probes" / a["probe"] / "probe.py").read_text(encoding="utf-8")
                for test in a["tests"]:
                    self.assertIn(f"def {test}(p):", text, f"{name} {a['id']}")
                    self.assertIn(f'"{test}"', text, f"{name} {a['id']} not in TESTS")

    def test_answer_sheet_lines_exist(self):
        sheet = json.loads((DEV_STACK / "planted" / "review-angular-hard.json").read_text(encoding="utf-8"))
        root = DEV_STACK / "fixtures" / "review-angular-hard"
        for item in sheet["planted"] + sheet["decoys"]:
            lines = (root / item["file"]).read_text(encoding="utf-8").splitlines()
            self.assertLessEqual(item["line"], len(lines), item["id"])
            self.assertTrue(lines[item["line"] - 1].strip(), f"{item['id']} points at an empty line")


if __name__ == "__main__":
    unittest.main()
