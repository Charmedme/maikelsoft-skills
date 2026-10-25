"""Tests for brandcheck.py, html2dc.py, figmamap.py, setupscan.py, and the focus and motion checks
of uxcheck.py review. Run: python3 -m unittest discover -s tests"""
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
UX = ROOT / "plugins" / "ux-design"
BRAND = UX / "shared" / "brandcheck.py"
HTML2DC = UX / "skills" / "claude-design" / "scripts" / "html2dc.py"
FIGMAMAP = UX / "skills" / "figma" / "scripts" / "figmamap.py"
SETUPSCAN = UX / "skills" / "ux-setup" / "scripts" / "setupscan.py"
UXCHECK = UX / "skills" / "ux-practice" / "scripts" / "uxcheck.py"


def run(script, *args, cwd=None):
    out = subprocess.run([sys.executable, str(script), *map(str, args)], capture_output=True, text=True, cwd=cwd)
    return out.returncode, out.stdout + out.stderr


def load(name, path):
    spec = importlib.util.spec_from_file_location(name, path)
    mod = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(mod)
    return mod


def hexc(h):
    return {"colorSpace": "srgb", "components": [round(int(h[i:i + 2], 16) / 255, 4) for i in (1, 3, 5)], "hex": h}


TOKENS = {
    "color": {
        "$type": "color",
        "ink": {"$value": hexc("#1b2430"), "$description": "Body text.",
                "$extensions": {"org.maikelsoft.contrast": {"on": ["{color.bg}"], "min": 4.5}}},
        "bg": {"$value": hexc("#ffffff"), "$description": "Page background."},
        "text": {"$value": "{color.ink}", "$description": "Alias of ink."},
    },
    "space": {"$type": "dimension", "4": {"$value": {"value": 16, "unit": "px"}, "$description": "Card padding."}},
    "radius": {"$type": "dimension", "md": {"$value": {"value": 6, "unit": "px"}, "$description": "Buttons."}},
    "font": {"$type": "fontFamily", "body": {"$value": ["IBM Plex Sans", "sans-serif"], "$description": "UI text."}},
    "typography": {"$type": "typography", "body": {"$value": {"fontFamily": "{font.body}", "fontSize": {"value": 14, "unit": "px"},
                                                              "fontWeight": 400, "lineHeight": 1.4}, "$description": "Body."}},
}


class BrandcheckTests(unittest.TestCase):
    def write(self, tmp, name, data):
        p = pathlib.Path(tmp) / name
        p.write_text(json.dumps(data), encoding="utf-8")
        return p

    def test_valid_tokens_pass(self):
        with tempfile.TemporaryDirectory() as tmp:
            code, out = run(BRAND, "check", self.write(tmp, "t.json", TOKENS))
            self.assertEqual(code, 0, out)
            self.assertIn("0 errors, 0 warnings", out)

    def test_alias_loop_missing_type_and_bad_value(self):
        bad = json.loads(json.dumps(TOKENS))
        bad["color"]["a"] = {"$value": "{color.b}", "$description": "x"}
        bad["color"]["b"] = {"$value": "{color.a}", "$description": "x"}
        bad["misc"] = {"x": {"$value": 3, "$description": "no type"}}
        bad["space"]["5"] = {"$value": "24", "$description": "string, not a dimension"}
        with tempfile.TemporaryDirectory() as tmp:
            code, out = run(BRAND, "check", self.write(tmp, "t.json", bad))
        self.assertEqual(code, 1)
        self.assertIn("TK-ALIAS", out)
        self.assertIn("loop", out)
        self.assertIn("TK-TYPE: misc.x", out)
        self.assertIn("TK-VALUE: space.5", out)

    def test_contrast_pair_fails_in_dark_theme(self):
        dark = {"color": {"ink": {"$value": hexc("#444444")}, "bg": {"$value": hexc("#222222")}}}
        with tempfile.TemporaryDirectory() as tmp:
            code, out = run(BRAND, "check", self.write(tmp, "t.json", TOKENS), "--theme", f"dark={self.write(tmp, 'd.json', dark)}")
        self.assertEqual(code, 1)
        self.assertIn("TK-CONTRAST: dark: color.ink on color.bg", out)

    def test_to_cd_writes_lists(self):
        dark = {"color": {"bg": {"$value": hexc("#111111")}}}
        with tempfile.TemporaryDirectory() as tmp:
            out_file = pathlib.Path(tmp) / "cd.json"
            code, out = run(BRAND, "to-cd", self.write(tmp, "t.json", TOKENS), "--theme", f"dark={self.write(tmp, 'd.json', dark)}",
                            "--name", "Test", "--out", out_file)
            self.assertEqual(code, 0, out)
            cd = json.loads(out_file.read_text())
        self.assertEqual([t["id"] for t in cd["color"]["themes"]], ["light", "dark"])
        names = {t["name"]: t for t in cd["color"]["tokens"]}
        self.assertEqual(names["color-bg"]["value"], {"light": "#ffffff", "dark": "#111111"})
        self.assertEqual(names["color-text"]["value"]["light"], "{color-ink}")
        self.assertIsInstance(cd["spacing"]["tokens"], list)
        self.assertEqual(cd["radius"]["tokens"][0], {"name": "radius-md", "value": "6px", "usage": "Buttons."})
        self.assertEqual(cd["type"]["groups"][0]["styles"][0]["fontSize"], "14px")

    def test_to_cd_reports_missing_usage(self):
        t = json.loads(json.dumps(TOKENS))
        t["color"]["bg"]["$description"] = ""
        with tempfile.TemporaryDirectory() as tmp:
            code, out = run(BRAND, "to-cd", self.write(tmp, "t.json", t), "--name", "T", "--out", pathlib.Path(tmp) / "o.json")
        self.assertEqual(code, 1)
        self.assertIn("CD-USAGE", out)

    def test_from_css_and_to_css_round_trip(self):
        css = ":root {\n --fl-color-ink: #1b2430;\n --fl-color-text: var(--fl-color-ink);\n --fl-space-4: 16px;\n --fl-font: \"IBM Plex Sans\", sans-serif;\n --fl-shadow-card: 0 1px 2px rgba(0, 0, 0, 0.1);\n}\n"
        with tempfile.TemporaryDirectory() as tmp:
            tmp = pathlib.Path(tmp)
            (tmp / "t.css").write_text(css)
            code, out = run(BRAND, "from-css", tmp / "t.css", "--out", tmp / "t.json")
            self.assertEqual(code, 0, out)
            doc = json.loads((tmp / "t.json").read_text())
            self.assertEqual(doc["color"]["text"]["$value"], "{color.ink}")
            self.assertEqual(doc["font"]["$root"]["$type"], "fontFamily")
            self.assertEqual(doc["shadow"]["card"]["$type"], "shadow")
            code, out = run(BRAND, "to-css", tmp / "t.json", "--prefix", "fl", "--out", tmp / "o.css")
            text = (tmp / "o.css").read_text()
        self.assertIn("--fl-color-text: var(--fl-color-ink);", text)
        self.assertIn("--fl-space-4: 16px;", text)
        self.assertIn("--fl-font: \"IBM Plex Sans\", sans-serif;", text)

    def test_copy_finds_banned_words(self):
        product = "# P\n\n## Words\n\n| Do not use | Use | Why |\n|---|---|---|\n| ticket, job | work order | domain |\n\n## Voice\n"
        with tempfile.TemporaryDirectory() as tmp:
            tmp = pathlib.Path(tmp)
            (tmp / "PRODUCT.md").write_text(product)
            (tmp / "m.html").write_text("<h1>Open tickets</h1><style>.job{}</style><button aria-label='New job'>+</button>")
            code, out = run(BRAND, "copy", "--product", tmp / "PRODUCT.md", tmp / "m.html")
        self.assertEqual(code, 1)
        self.assertEqual(out.count("CP-BANNED"), 2, out)  # "tickets" in the h1 and "job" in the aria-label
        self.assertNotIn(".job", out)


MOCKUP = """<!doctype html><html lang="en"><head><link rel="stylesheet" href="t.css"><style>.x{color:red}</style></head>
<body><section data-screen="list" data-state="default"><h2>Default</h2><p>One<br>two {{x}}<li>a<li>b</section>
<section data-screen="list" data-state="empty"><h2>Empty</h2><input type=checkbox checked><button onclick="go()">Go</button></section></body></html>"""


class Html2dcTests(unittest.TestCase):
    def test_build_writes_artboards_and_index(self):
        with tempfile.TemporaryDirectory() as tmp:
            tmp = pathlib.Path(tmp)
            (tmp / "t.css").write_text(":root{--a:#fff}")
            (tmp / "list.html").write_text(MOCKUP)
            code, out = run(HTML2DC, "css", tmp / "list.html", "--root", tmp / "c")
            self.assertEqual(code, 0, out)
            css = (tmp / "c" / "assets" / "list.css").read_text()
            self.assertIn("--a:#fff", css)
            self.assertIn(".x{color:red}", css)
            code, out = run(HTML2DC, "build", tmp / "list.html", "--root", tmp / "c", "--css", "list=/_blob/abc", "--title", "T")
            self.assertEqual(code, 0, out)
            idx = json.loads((tmp / "c" / "project" / "canvas.json").read_text())
            main = (tmp / "c" / "project" / "Main.dc.html").read_text()
            empty = (tmp / "c" / "project" / "list--empty.dc.html").read_text()
        self.assertEqual(idx["order"], ["Main.dc.html", "list--empty.dc.html"])
        self.assertEqual(idx["boards"]["Main.dc.html"]["expand"], "fill")
        self.assertIn("createdOnFiles", idx)
        self.assertIn('<script src="./support.js"></script>', main)
        self.assertIn('href="/_blob/abc"', main)
        self.assertIn("data-dc-script", main)
        self.assertIn("<li>a</li><li>b</li>", main)
        self.assertIn("{ {x}}", main)
        self.assertIn('type="checkbox"', empty)
        self.assertNotIn("onclick", empty)

    def test_build_rejects_a_css_value_that_is_not_a_blob(self):
        with tempfile.TemporaryDirectory() as tmp:
            tmp = pathlib.Path(tmp)
            (tmp / "list.html").write_text(MOCKUP)
            code, _ = run(HTML2DC, "build", tmp / "list.html", "--root", tmp / "c", "--css", "list=t.css", "--title", "T")
        self.assertEqual(code, 2)


class FigmamapTests(unittest.TestCase):
    def test_map_finds_each_match_kind(self):
        with tempfile.TemporaryDirectory() as tmp:
            tmp = pathlib.Path(tmp)
            (tmp / "t.json").write_text(json.dumps(TOKENS))
            (tmp / "v.json").write_text(json.dumps({"Color/Ink": "#1B2430", "Color/Bg": "#FAFAFA", "Space/4": "16",
                                                    "Color/Promo": "#E0457B"}))
            code, out = run(FIGMAMAP, "map", tmp / "v.json", "--tokens", tmp / "t.json", "--json")
        self.assertEqual(code, 1)  # one conflict
        rows = {r["variable"]: r["match"] for r in json.loads(out)["rows"]}
        self.assertEqual(rows, {"Color/Ink": "exact", "Color/Bg": "conflict", "Space/4": "exact", "Color/Promo": "none"})

    def test_count_stops_at_the_limit_and_skips_write_tools(self):
        with tempfile.TemporaryDirectory() as tmp:
            log = pathlib.Path(tmp) / "calls.log"
            self.assertEqual(run(FIGMAMAP, "count", log, "--limit", "2", "--add", "get_metadata")[0], 0)
            self.assertEqual(run(FIGMAMAP, "count", log, "--limit", "2", "--add", "use_figma")[0], 0)
            self.assertEqual(run(FIGMAMAP, "count", log, "--limit", "2", "--add", "get_design_context")[0], 3)


class SetupscanTests(unittest.TestCase):
    def test_finds_the_design_facts(self):
        with tempfile.TemporaryDirectory() as tmp:
            tmp = pathlib.Path(tmp)
            (tmp / "DESIGN.md").write_text("# D\n")
            (tmp / "src").mkdir()
            (tmp / "src" / "tokens.css").write_text(":root{" + "".join(f"--c{i}:#fff;" for i in range(12)) + "}")
            (tmp / "src" / "index.html").write_text('<html lang="nl"><a href="https://www.figma.com/design/AbC123/App?node-id=1">x</a></html>')
            (tmp / "package.json").write_text(json.dumps({"scripts": {"dev": "vite"}, "dependencies": {"react": "^19.0.0"}}))
            code, out = run(SETUPSCAN, tmp, "--json")
        self.assertEqual(code, 0, out)
        facts = json.loads(out)
        self.assertEqual(facts["brand_files"], {"DESIGN.md": ["DESIGN.md"]})
        self.assertEqual(facts["tokens"][0]["kind"], "css-custom-properties")
        self.assertEqual(facts["figma_links"], ["https://www.figma.com/design/AbC123/App"])
        self.assertIn("nl", facts["languages"])
        self.assertIn("react ^19.0.0", facts["ui_stack"])
        self.assertIn(".: npm run dev", facts["run"])


PAGE = """<!doctype html><html lang="en"><head><title>T</title><style>
button:focus-visible, a:focus-visible { outline: 2px solid #0b6e66; }
BAD
</style></head><body><nav><a href="#a">A</a> <a href="#b">B</a></nav><main><h1>T</h1>
<button id="one">One</button><button id="two">Two</button>
<aside id="panel"><button id="p1">P1</button><button id="p2">P2</button></aside>
<div class="spin">x</div><footer id="bar">Bar</footer></main>
<script>TRAP</script></body></html>"""

TRAP_JS = """document.addEventListener('keydown', e => { if (e.key !== 'Tab') return;
 const p = [...document.querySelectorAll('#panel button')]; const i = p.indexOf(document.activeElement);
 e.preventDefault(); p[(i + 1) % p.length].focus(); });"""


class QuietHandler(http.server.SimpleHTTPRequestHandler):
    def log_message(self, *args):
        pass


@unittest.skipUnless(importlib.util.find_spec("playwright"), "Playwright is not installed")
class FocusAndMotionTests(unittest.TestCase):
    def review(self, bad_css, trap):
        ux = load("uxcheck_keys", UXCHECK)
        with tempfile.TemporaryDirectory() as tmp:
            tmp = pathlib.Path(tmp)
            (tmp / "index.html").write_text(PAGE.replace("BAD", bad_css).replace("TRAP", TRAP_JS if trap else ""))
            srv = http.server.ThreadingHTTPServer(("127.0.0.1", 0), functools.partial(QuietHandler, directory=str(tmp)))
            threading.Thread(target=srv.serve_forever, daemon=True).start()
            try:
                walk = {"base_url": f"http://127.0.0.1:{srv.server_port}",
                        "flows": [{"id": "f", "states": [{"state": "default", "steps": [{"goto": "/"}]}]}]}
                (tmp / "walk.json").write_text(json.dumps(walk))
                r = ux.Report()
                ux.check_review(str(tmp / "walk.json"), str(tmp / "out"), None, ux.PRIMARY, r)
            finally:
                srv.shutdown()
        return {i["rule"] for i in r.items}

    def test_clean_page_has_no_focus_or_motion_finding(self):
        rules = self.review("", False)
        self.assertFalse({x for x in rules if x.startswith(("RV-FOCUS", "RV-MOTION"))}, rules)

    def test_planted_faults_are_found(self):
        bad = ("#two:focus-visible { outline: none; }\n"
               "#bar { position: fixed; left: 0; right: 0; top: 0; height: 900px; background: #fff; }\n"
               ".spin { animation: s 1s linear infinite; } @keyframes s { to { opacity: .5; } }")
        rules = self.review(bad, False)
        self.assertIn("RV-FOCUS-VISIBLE", rules)
        self.assertIn("RV-FOCUS-OBSCURED", rules)
        self.assertIn("RV-MOTION", rules)

    def test_positive_tabindex_is_found(self):
        ux = load("uxcheck_keys2", UXCHECK)
        global PAGE
        old = PAGE
        PAGE = PAGE.replace('<button id="two">', '<button id="two" tabindex="2">')
        try:
            rules = self.review("", False)
        finally:
            PAGE = old
        self.assertIn("RV-TABINDEX", rules)

    def test_trap_is_found(self):
        self.assertIn("RV-FOCUS-TRAP", self.review("", True))


if __name__ == "__main__":
    unittest.main()
