"""Tests for the prototype shell, the review mode of uxcheck.py, the ux: source of
examplecheck.py, and the UX counter of runcheck.py. Run: python3 -m unittest discover -s tests"""
import functools
import http.server
import importlib.util
import json
import pathlib
import subprocess
import sys
import tempfile
import threading
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[1]
SHELL = ROOT / "shared" / "proto-shell"
SKILL = ROOT / "plugins" / "ux-design" / "skills" / "ux-prototype"
UXCHECK = ROOT / "plugins" / "ux-design" / "skills" / "ux-practice" / "scripts"


def load(name, path):
    spec = importlib.util.spec_from_file_location(name, path)
    mod = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(mod)
    return mod


CONTENT = """<!-- PROTO-CONTENT START -->
<script type="application/json" id="proto-manifest">{
  "standard": "proto-shell/1", "title": "Assign work", "question": "Can a planner assign 5 orders in one step?",
  "kind": "throwaway", "scope": "The work order list and the assign panel.", "lang": "en",
  "start": {"screen": "list", "state": "default"},
  "screens": [{"id": "list", "name": "List", "states": ["default", "empty"]}],
  "variants": [{"id": "A", "name": "Panel"}], "themes": ["light"]
}</script>
<style id="proto-css">:root { --c: #1a1a1a; } p { color: var(--c); }</style>
<template data-screen="list" data-variant="*"><p data-hide-state="empty">Work order 4711</p><p data-show-state="empty">No work orders</p>BODY</template>
<!-- PROTO-CONTENT END -->
"""


def protocheck(text):
    with tempfile.TemporaryDirectory() as tmp:
        f = pathlib.Path(tmp) / "content.html"
        f.write_text(text, encoding="utf-8")
        out = subprocess.run([sys.executable, str(SKILL / "scripts" / "protocheck.py"), str(f)],
                             capture_output=True, text=True)
        return out.returncode, out.stdout


class ProtoShellTests(unittest.TestCase):
    def test_clean_content_passes(self):
        code, out = protocheck(CONTENT.replace("BODY", ""))
        self.assertEqual(code, 0, out)

    def test_network_call_fails(self):
        code, out = protocheck(CONTENT.replace("BODY", "<script>fetch('/api')</script>"))
        self.assertEqual(code, 1)
        self.assertIn("offline", out)

    def test_external_file_fails(self):
        code, out = protocheck(CONTENT.replace("BODY", '<img src="https://example.com/a.png" alt="">'))
        self.assertEqual(code, 1)
        self.assertIn("External file", out)

    def test_build_from_skill_copy(self):
        # protobuild.py in a skill finds the shell in references/proto-shell/.
        with tempfile.TemporaryDirectory() as tmp:
            src, out = pathlib.Path(tmp) / "c.html", pathlib.Path(tmp) / "p.html"
            src.write_text(CONTENT.replace("BODY", ""), encoding="utf-8")
            r = subprocess.run([sys.executable, str(SKILL / "scripts" / "protobuild.py"), str(src), str(out)],
                               capture_output=True, text=True)
            self.assertEqual(r.returncode, 0, r.stderr)
            page = out.read_text(encoding="utf-8")
            self.assertIn('id="shell-js"', page)
            self.assertIn('generator" content="proto-shell/1"', page)
            code, msg = protocheck(page)
            self.assertEqual(code, 0, msg)

    def test_skill_copies_match_the_source(self):
        for skill in ("plugins/ux-design/skills/ux-prototype", "plugins/product-flow/skills/product-prototype"):
            for src in SHELL.iterdir():
                target = (ROOT / skill / "scripts" / src.name if src.suffix == ".py"
                          else ROOT / skill / "references" / "proto-shell" / src.name)
                self.assertEqual(target.read_text(encoding="utf-8"), src.read_text(encoding="utf-8"), target)


DESIGN = """# UX design

### States: list

| State | User sees | User can do | Recovery |
|-------|-----------|-------------|----------|
| default | rows | select | n/a |
| error-save | "Not saved" | try again | retry |
"""

EXAMPLES = """# Examples: FL-31

## Rules

- AC1 R1: The save can fail.

## Examples

| Rule | Case | Given | When | Then | Source |
|------|------|-------|------|------|--------|
| AC1 R1 | error-save | 2 selected, server 500 | Assign | "Not saved" | ux: list/STATE |

## Words with more than one meaning

None.

## Answers from decisions

None.

## Open questions

None.
"""


class ExampleUxSourceTests(unittest.TestCase):
    ec = load("examplecheck", ROOT / "shared" / "examplecheck.py")

    def test_known_state_passes(self):
        errors, *_ = self.ec.check(EXAMPLES.replace("STATE", "error-save"), DESIGN)
        self.assertEqual(errors, [])

    def test_unknown_state_fails(self):
        errors, *_ = self.ec.check(EXAMPLES.replace("STATE", "error-undo"), DESIGN)
        self.assertTrue(any("not in the states of list" in e for e in errors), errors)

    def test_without_design_no_check(self):
        errors, *_ = self.ec.check(EXAMPLES.replace("STATE", "error-undo"))
        self.assertEqual(errors, [])


class RunCheckUxTests(unittest.TestCase):
    def test_ux_design_needs_ux_review(self):
        sys.path.insert(0, str(ROOT / "tests"))
        import test_devteam as td
        with tempfile.TemporaryDirectory() as tmp:
            run = td.make_run(pathlib.Path(tmp))
            (run / "ux-design.md").write_text("# UX design\n", encoding="utf-8")
            found = {r for _, r, m in td.runcheck.check(run, "g2") if "ux" in m}
            self.assertEqual(found, {"RUN-COUNTER", "RUN-FILE"})


APP = """<!doctype html><html lang="en"><head><meta charset="utf-8">
<style>:root { --text: #1a1a1a; --bg: #ffffff; --s: 16px; } body { color: var(--text); background: var(--bg); font-size: var(--s); }</style>
</head><body><h1>Orders</h1><div id="out">Loading</div>
<script>fetch('/api/items').then(r => { if (!r.ok) throw 0; return r.json(); })
  .then(d => { document.getElementById('out').textContent = d.length + ' orders'; })
  .catch(() => { document.getElementById('out').textContent = 'Not loaded. Try again.'; });</script>
</body></html>"""


class QuietHandler(http.server.SimpleHTTPRequestHandler):
    def log_message(self, *args):
        pass


@unittest.skipUnless(importlib.util.find_spec("playwright"), "Playwright is not installed")
class ReviewModeTests(unittest.TestCase):
    def test_walk_makes_each_state(self):
        ux = load("uxcheck_rv", UXCHECK / "uxcheck.py")
        with tempfile.TemporaryDirectory() as tmp:
            tmp = pathlib.Path(tmp)
            (tmp / "index.html").write_text(APP, encoding="utf-8")
            (tmp / "api").mkdir()
            (tmp / "api" / "items").write_text("[1, 2, 3]", encoding="utf-8")
            handler = functools.partial(QuietHandler, directory=str(tmp))
            srv = http.server.ThreadingHTTPServer(("127.0.0.1", 0), handler)
            threading.Thread(target=srv.serve_forever, daemon=True).start()
            try:
                walk = {"base_url": f"http://127.0.0.1:{srv.server_port}", "flows": [{"id": "orders", "states": [
                    {"state": "default", "steps": [{"goto": "/"}, {"expect_text": ["#out", "3 orders"]}]},
                    {"state": "error-load", "routes": [{"url": "**/api/items", "status": 500}],
                     "steps": [{"goto": "/"}, {"expect_text": ["#out", "Not loaded"]}]},
                    {"state": "loading", "routes": [{"url": "**/api/items", "hold": True}],
                     "steps": [{"goto": "/"}, {"expect_text": ["#out", "Loading"]}]},
                    {"state": "wrong", "steps": [{"goto": "/"}, {"expect_text": ["#out", "Saved"]}]},
                ]}]}
                wf = tmp / "walk.json"
                wf.write_text(json.dumps(walk), encoding="utf-8")
                r = ux.Report()
                code, stats = ux.check_review(str(wf), str(tmp / "out"), None, ux.PRIMARY, r)
            finally:
                srv.shutdown()
            self.assertIsNone(code)
            self.assertEqual(stats["shots"], 12)
            steps = [i["message"] for i in r.items if i["rule"] == "RV-STEP"]
            self.assertEqual(len(steps), 3, steps)  # only the "wrong" state fails, at each width
            self.assertTrue(all("orders/wrong" in m for m in steps))
            self.assertTrue((tmp / "out" / "orders__error-load__390.png").exists())
            self.assertTrue((tmp / "out" / "review.json").exists())


if __name__ == "__main__":
    unittest.main()
