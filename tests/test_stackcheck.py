import contextlib
import io
import json
import pathlib
import subprocess
import sys
import tempfile
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / "plugins" / "dev-stack" / "shared"))
import stackcheck  # noqa: E402

RULES = {
    "skill": "demo",
    "rules": [
        {"id": "CS-UTCNOW", "kind": "regex", "files": ["**/*.cs"], "exclude": ["tests/**"],
         "pattern": r"DateTime(Offset)?\.UtcNow", "severity": "error", "message": "Use TimeProvider.", "fix": "Inject TimeProvider."},
        {"id": "CS-PRAGMA", "kind": "regex", "files": ["**/*.cs"], "pattern": r"#pragma warning disable",
         "severity": "warning", "message": "Give the reason."},
        {"id": "PROPS-NULLABLE", "kind": "require", "files": ["Directory.Build.props"],
         "pattern": r"<Nullable>enable</Nullable>", "severity": "error", "message": "Turn on nullable."},
        {"id": "PKG-FA8", "kind": "package", "ecosystem": "nuget", "package": "FluentAssertions",
         "versions": ">=8.0.0", "severity": "error", "message": "FluentAssertions 8 needs a paid license."},
        {"id": "PKG-MEDIATR", "kind": "package", "package": "MediatR*", "severity": "error", "message": "Not allowed."},
        {"id": "PKG-ENUMS", "kind": "package", "ecosystem": "npm", "package": "moment", "severity": "warning", "message": "Use Intl."},
        {"id": "DET-CONTROLLER", "kind": "detect", "decision": "API-01", "files": ["**/*.cs"], "pattern": r": ControllerBase"},
    ],
}


class StackcheckTest(unittest.TestCase):
    def setUp(self):
        self.tmp = tempfile.TemporaryDirectory()
        self.root = pathlib.Path(self.tmp.name)
        self.rules = self.root / "rules.json"
        self.rules.write_text(json.dumps(RULES), encoding="utf-8")

    def tearDown(self):
        self.tmp.cleanup()

    def write(self, rel, text):
        path = self.root / "repo" / rel
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_text(text, encoding="utf-8")

    def run_check(self, *extra):
        out = io.StringIO()
        with contextlib.redirect_stdout(out):
            code = stackcheck.main(["--root", str(self.root / "repo"), "--rules", str(self.rules), *extra])
        return code, out.getvalue()

    def test_regex_finding_has_path_line_and_fix(self):
        self.write("src/Clock.cs", "class C\n{\n    var now = DateTime.UtcNow;\n}\n")
        self.write("Directory.Build.props", "<Project><PropertyGroup><Nullable>enable</Nullable></PropertyGroup></Project>")
        code, out = self.run_check()
        self.assertEqual(code, 1)
        self.assertIn("src/Clock.cs:3 CS-UTCNOW error Use TimeProvider. Fix: Inject TimeProvider.", out)

    def test_exclude_glob_skips_tests(self):
        self.write("tests/ClockTests.cs", "var now = DateTimeOffset.UtcNow;\n")
        self.write("Directory.Build.props", "<Project><PropertyGroup><Nullable>enable</Nullable></PropertyGroup></Project>")
        code, out = self.run_check()
        self.assertEqual(code, 0)
        self.assertIn("0 errors", out)

    def test_suppression_needs_a_reason(self):
        self.write("src/A.cs", "// stackcheck: ignore CS-UTCNOW audit field must use wall clock\nvar a = DateTime.UtcNow;\n")
        self.write("src/B.cs", "var b = DateTime.UtcNow; // stackcheck: ignore CS-UTCNOW\n")
        self.write("Directory.Build.props", "<Project><PropertyGroup><Nullable>enable</Nullable></PropertyGroup></Project>")
        code, out = self.run_check()
        self.assertIn("src/B.cs:1 CS-UTCNOW", out)
        self.assertNotIn("src/A.cs", out)
        self.assertIn("1 suppressed", out)

    def test_require_rule(self):
        self.write("Directory.Build.props", "<Project><PropertyGroup></PropertyGroup></Project>")
        code, out = self.run_check()
        self.assertEqual(code, 1)
        self.assertIn("Directory.Build.props:1 PROPS-NULLABLE error", out)

    def test_package_rules_nuget_and_central_versions(self):
        self.write("Directory.Build.props", "<Project><PropertyGroup><Nullable>enable</Nullable></PropertyGroup></Project>")
        self.write("Directory.Packages.props",
                   '<Project><ItemGroup>\n<PackageVersion Include="FluentAssertions" Version="8.1.0" />\n'
                   '<PackageVersion Include="MediatR.Contracts" Version="2.0.1" />\n</ItemGroup></Project>')
        self.write("tests/T.csproj", '<Project><ItemGroup><PackageReference Include="FluentAssertions" Version="[7.2.2,8.0.0)" /></ItemGroup></Project>')
        code, out = self.run_check()
        self.assertEqual(code, 1)
        self.assertIn("Directory.Packages.props:2 PKG-FA8 error", out)
        self.assertIn("Directory.Packages.props:3 PKG-MEDIATR error", out)
        self.assertNotIn("tests/T.csproj", out)

    def test_versionless_reference_under_cpm_is_checked_at_the_package_version(self):
        self.write("Directory.Build.props", "<Project><PropertyGroup><Nullable>enable</Nullable></PropertyGroup></Project>")
        self.write("Directory.Packages.props",
                   '<Project><ItemGroup>\n<PackageVersion Include="FluentAssertions" Version="7.2.2" />\n</ItemGroup></Project>')
        self.write("tests/T.csproj", '<Project><ItemGroup><PackageReference Include="FluentAssertions" />\n'
                   '<PackageReference Include="MediatR" /></ItemGroup></Project>')
        code, out = self.run_check()
        self.assertNotIn("PKG-FA8", out)
        self.assertIn("tests/T.csproj:2 PKG-MEDIATR error", out)

    def test_npm_package_rule(self):
        self.write("Directory.Build.props", "<Project><PropertyGroup><Nullable>enable</Nullable></PropertyGroup></Project>")
        self.write("web/package.json", json.dumps({"dependencies": {"moment": "^2.30.1"}}, indent=2))
        code, out = self.run_check()
        self.assertEqual(code, 0)
        self.assertIn("web/package.json:3 PKG-ENUMS warning", out)

    def test_detect_rules_only_with_flag(self):
        self.write("Directory.Build.props", "<Project><PropertyGroup><Nullable>enable</Nullable></PropertyGroup></Project>")
        self.write("src/OrdersController.cs", "public class OrdersController : ControllerBase { }\n")
        _, out = self.run_check()
        self.assertNotIn("DET-CONTROLLER", out)
        _, out = self.run_check("--detect")
        self.assertIn("src/OrdersController.cs:1 DET-CONTROLLER info uses decision API-01", out)

    def test_skip_dirs_and_json_format(self):
        self.write("Directory.Build.props", "<Project><PropertyGroup><Nullable>enable</Nullable></PropertyGroup></Project>")
        self.write("src/obj/Gen.cs", "var x = DateTime.UtcNow;\n")
        code, out = self.run_check("--format", "json")
        data = json.loads(out)
        self.assertEqual(code, 0)
        self.assertEqual(data["errors"], 0)
        self.assertEqual(data["findings"], [])

    def test_changed_only(self):
        repo = self.root / "repo"
        self.write("Directory.Build.props", "<Project><PropertyGroup><Nullable>enable</Nullable></PropertyGroup></Project>")
        self.write("src/Old.cs", "var x = DateTime.UtcNow;\n")
        git = lambda *a: subprocess.run(["git", "-C", str(repo), *a], check=True, capture_output=True)
        git("init", "-q", "-b", "main")
        git("-c", "user.email=t@t", "-c", "user.name=t", "add", ".")
        git("-c", "user.email=t@t", "-c", "user.name=t", "commit", "-qm", "base")
        self.write("src/New.cs", "var y = DateTime.UtcNow;\n")
        code, out = self.run_check("--changed", "main")
        self.assertIn("src/New.cs:1 CS-UTCNOW", out)
        self.assertNotIn("src/Old.cs", out)

    def test_version_constraints(self):
        self.assertTrue(stackcheck.version_matches((8, 0, 0), ">=8.0.0"))
        self.assertFalse(stackcheck.version_matches((7, 2, 2), ">=8.0.0"))
        self.assertTrue(stackcheck.version_matches((4, 20, 1), ">=4.20.0,<4.21.0"))
        self.assertEqual(stackcheck.parse_version("[7.2.2,8.0.0)"), (7, 2, 2))
        self.assertEqual(stackcheck.parse_version("^19.1.0"), (19, 1, 0))


if __name__ == "__main__":
    unittest.main()
