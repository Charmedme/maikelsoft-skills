import json
import pathlib
import subprocess
import sys
import tempfile
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[1]
SCRIPT = ROOT / "plugins/product-flow/shared/backlogtree.py"

DRAFT = """## Inloggen in de app

**Type:** Feature
**Parent:** Geen

### Doel

Klanten loggen veilig in.

## Inloggen met mijn account

**Type:** User Story
**Parent:** Inloggen in de app

Als klant wil ik inloggen, zodat ik mijn orders zie.
"""


def run(*args, cwd):
    return subprocess.run([sys.executable, str(SCRIPT), *args], cwd=cwd, capture_output=True, text=True)


class BacklogTreeTest(unittest.TestCase):
    def setUp(self):
        self.dir = pathlib.Path(tempfile.mkdtemp())
        (self.dir / "draft.md").write_text(DRAFT, encoding="utf-8")

    def test_tree_order_and_parent(self):
        out = run("tree", "draft.md", "--json", cwd=self.dir)
        data = json.loads(out.stdout)
        self.assertEqual(data["errors"], [])
        self.assertEqual([i["type"] for i in data["items"]], ["Feature", "User Story"])

    def test_unknown_parent_is_an_error(self):
        (self.dir / "bad.md").write_text(DRAFT.replace("**Parent:** Inloggen in de app", "**Parent:** Iets anders"))
        self.assertEqual(run("tree", "bad.md", cwd=self.dir).returncode, 1)

    def test_similar(self):
        (self.dir / "c.json").write_text(json.dumps([{"id": "7", "title": "Inloggen met een account"}]))
        out = run("similar", "draft.md", "--candidates", "c.json", cwd=self.dir)
        self.assertIn("1 possible duplicates", out.stdout)

    def test_log_resume(self):
        run("log", "init", "log.json", "draft.md", "--system", "test", cwd=self.dir)
        run("log", "set", "log.json", "inloggen-in-de-app", "100", cwd=self.dir)
        out = run("log", "status", "log.json", cwd=self.dir)
        self.assertIn("next: inloggen-met-mijn-account", out.stdout)
        self.assertEqual(run("log", "init", "log.json", "draft.md", cwd=self.dir).returncode, 1)

    def test_export_keeps_parent_text(self):
        run("export", "draft.md", "--format", "md", "--split", "--out", "out", cwd=self.dir)
        first = (self.dir / "out" / "01-inloggen-in-de-app.md").read_text()
        self.assertIn("**Parent:** Geen", first)


if __name__ == "__main__":
    unittest.main()
