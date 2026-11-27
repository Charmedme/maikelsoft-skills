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


FAKE_NG = """import json, pathlib, sys
# Stands in for 'npx ng test': checks the probe file and writes a Vitest JSON report.
args = dict(a.split("=", 1) for a in sys.argv[1:] if a.startswith("--") and "=" in a)
probe = pathlib.Path("src/app/eval-probes/p.spec.ts")
assert args["--reporters"] == "json"
if not probe.is_file() or not pathlib.Path("node_modules/.package-lock.json").is_file():
    sys.exit(3)
tests = [{"title": "A passes", "status": "passed", "failureMessages": []},
         {"title": "B fails", "status": "failed", "failureMessages": ["expected true to be false"]}]
report = {"testResults": [{"name": str(probe.resolve()), "status": "failed", "message": "", "assertionResults": tests}]}
pathlib.Path(args["--output-file"]).write_text(json.dumps(report))
sys.exit(1)
"""


class SpecProbe(unittest.TestCase):
    def setUp(self):
        self.tmp = pathlib.Path(tempfile.mkdtemp())
        self.addCleanup(__import__("shutil").rmtree, self.tmp, True)
        probes = self.tmp / "probes"
        write(probes, "p/probe.json", json.dumps({
            "target": "frontend", "folder": "src/app/eval-probes", "kind": "ng_spec", "files": ["p.spec.ts"],
            "run": f"{sys.executable} {self.tmp / 'fake_ng.py'} --include src/app/eval-probes/p.spec.ts"}))
        write(probes, "p/p.spec.ts", "it('A passes', () => {});\n")
        write(self.tmp, "fake_ng.py", FAKE_NG)
        self.repo = self.tmp / "repo"
        write(self.repo, "frontend/package-lock.json", "{}")
        write(self.repo, "frontend/node_modules/.package-lock.json", "{}")
        write(self.repo, "src/Api/bin/Debug/api.dll", "binary")
        for name, value in (("PROBES", probes), ("CACHE", self.tmp / "cache")):
            patcher = mock.patch.object(angular_checks, name, value)
            patcher.start()
            self.addCleanup(patcher.stop)
        for cache in (angular_checks._COPIES, angular_checks._WORK, angular_checks._RESULTS):
            patcher = mock.patch.dict(cache, clear=True)
            patcher.start()
            self.addCleanup(patcher.stop)

    def test_reads_each_test_from_the_json_report(self):
        ok, got = angular_checks.check("ng_spec_probe", self.repo, {}, {"probe": "p", "tests": ["A passes"]}, None)
        self.assertTrue(ok, got)
        ok, got = angular_checks.check("ng_spec_probe", self.repo, {}, {"probe": "p", "tests": ["A passes", "B fails", "C"]}, None)
        self.assertFalse(ok)
        self.assertEqual(got["A passes"], "Passed")
        self.assertEqual(got["B fails"], "Failed: expected true to be false")
        self.assertTrue(got["C"].startswith("Missing"))

    def test_the_run_folder_does_not_change_and_the_probe_runs_once(self):
        with mock.patch.object(angular_checks, "run", wraps=angular_checks.run) as spy:
            angular_checks.check_spec_probe(self.repo, {"probe": "p", "tests": ["A passes"]})
            angular_checks.check_spec_probe(self.repo, {"probe": "p", "tests": ["B fails"]})
        self.assertEqual(sum(1 for c in spy.call_args_list if "fake_ng.py" in " ".join(map(str, c.args[0]))), 1)
        self.assertFalse((self.repo / "frontend" / "src").exists())
        copy, _ = angular_checks.workspace(self.repo, "frontend", "probe-p")
        self.assertTrue((copy / "src/app/eval-probes/p.spec.ts").is_file())
        self.assertFalse((copy.parent / "src" / "Api" / "bin").exists())

    def test_no_report_means_the_probe_did_not_run(self):
        write(self.tmp, "fake_ng.py", "import sys\nprint('error TS2307: Cannot find module')\nsys.exit(1)\n")
        ok, got = angular_checks.check_spec_probe(self.repo, {"probe": "p", "tests": ["A passes"]})
        self.assertFalse(ok)
        self.assertIn("TS2307", got)

    def test_a_file_that_does_not_compile_gives_its_message(self):
        report = self.tmp / "report.json"
        report.write_text(json.dumps({"testResults": [{"status": "failed", "message": "Transform failed\nline 2",
                                                       "assertionResults": []}]}))
        results, errors = angular_checks.parse_vitest_json(report)
        self.assertEqual(results, {})
        self.assertEqual(errors, ["Transform failed"])


class AppFolder(unittest.TestCase):
    def test_ng_build_and_ng_test_take_the_dir_of_the_assertion(self):
        with mock.patch.object(angular_checks, "ng", return_value=(0, "ok")) as ng:
            angular_checks.check("ng_build", "/r", {}, {"dir": "frontend"}, None)
            angular_checks.check("ng_test", "/r", {}, {}, None)
        self.assertEqual([c.args for c in ng.call_args_list], [("/r", "build", "frontend"), ("/r", "test", "")])

    def test_tests_added_min_is_an_angular_count_for_a_case_with_node_dir(self):
        self.assertTrue(angular_checks.handles("tests_added_min", {"stack": "kiosk", "node_dir": "frontend"}))
        self.assertTrue(angular_checks.handles("ng_spec_probe", {}))


class CaseFiles(unittest.TestCase):
    def test_each_spec_probe_test_of_the_cases_exists(self):
        for path in sorted((DEV_STACK / "cases").glob("*.json")):
            case = json.loads(path.read_text(encoding="utf-8"))
            for a in case["assertions"]:
                if a.get("check") != "ng_spec_probe":
                    continue
                meta = json.loads((DEV_STACK / "probes" / a["probe"] / "probe.json").read_text(encoding="utf-8"))
                text = "".join((DEV_STACK / "probes" / a["probe"] / f).read_text(encoding="utf-8") for f in meta["files"])
                for test in a["tests"]:
                    self.assertIn(f"it('{test}'", text, f"{path.name} {a['id']}")

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
