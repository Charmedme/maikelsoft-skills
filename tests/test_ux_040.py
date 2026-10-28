"""Tests for the checks of ux-design 0.4.0 in uxcheck.py. Run: python3 -m unittest tests.test_ux_040

Each new checker ID has one fixture that triggers it and one that does not. The mockup tests need
Playwright with a Chromium browser (python3 -m playwright install chromium); without one they skip.
"""
import importlib.util
import pathlib
import sys
import tempfile
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[1]
SCRIPTS = ROOT / "plugins" / "ux-design" / "skills" / "ux-practice" / "scripts"
EVIDENCE = ROOT / "evals" / "ux-design" / "fixtures" / "field-2026-10-27"
sys.path.insert(0, str(SCRIPTS))
import uxcheck  # noqa: E402

DECISIONS = ["purpose", "main-task", "pattern", "layout", "hierarchy", "components", "density", "states",
             "copy", "motion"]
STATES = ["default", "empty", "loading", "partial", "error", "success", "disabled", "overflow", "offline"]

INVENTORY = """## Element inventory

| Element | Home | Component | Screens |
|---------|------|-----------|---------|
| music | {home} | IconToggle | lobby, game |
"""

NONTEXT = """## Non-text contrast

| Part | Color | Next to | Ratio | Needs |
|------|-------|---------|-------|-------|
| Input edge | #a08bff | #140830 | {ratio} | 3:1 |
| Spotlight | #42277f | #140830 | 1.6:1 | none |
"""

DIRECTION = """## Style direction

| Name | Color | Type | Shape | Moodboard |
|------|-------|------|-------|-----------|
{rows}

Chosen: Gameshow (owner, 2026-10-27)
"""


def design(mode="full", inventory=INVENTORY.format(home="top-bar"), nontext=NONTEXT.format(ratio="6.9:1"),
           direction=None, difference=True):
    rows = "\n".join(f"| {d} | value | product: AC-1 |" for d in DECISIONS)
    states = "\n".join(f"| {s} | sees | does | recovers |" for s in STATES)
    head = "| Option | Structure | Difference | Total |" if difference else "| Option | Structure | Total |"
    opts = "\n".join(f"| {c} | layout {c} | {d} | 12 |" if difference else f"| {c} | layout {c} | 12 |"
                     for c, d in (("A", 3), ("B", 1)))
    mode_line = f"Mode: {mode}\n" if mode else ""
    return f"""# UX design: test

{mode_line}Surface: operate

## Frame

| Question | Answer | Evidence |
|----------|--------|----------|
| Problem | Slow | checked: story |

### Failure paths

| Step | Failure | State |
|------|---------|-------|
| 1 | Save fails | error |

{direction or ""}
## Options

{head}
|---|---|---|---|
{opts}

Selected: A. Reason: new look.

{inventory}
{nontext}
## Screen: list

| Decision | Value | Source |
|----------|-------|--------|
{rows}

### States: list

| State | User sees | User can do | Recovery |
|-------|-----------|-------------|----------|
{states}
"""


def run_design(text):
    with tempfile.TemporaryDirectory() as tmp:
        path = pathlib.Path(tmp) / "ux-design.md"
        path.write_text(text, encoding="utf-8")
        report = uxcheck.Report()
        uxcheck.check_design(str(path), None, None, report)
        return {i["rule"] for i in report.items if i["level"] == "error"}


class DesignTests(unittest.TestCase):
    def test_full_design_with_new_sections_passes(self):
        self.assertEqual(run_design(design()), set())

    def test_ds_mode(self):
        self.assertIn("DS-MODE", run_design(design(mode="")))
        self.assertIn("DS-MODE", run_design(design(mode="fancy")))
        self.assertNotIn("DS-MODE", run_design(design(mode="light")))

    def test_ds_direction(self):
        three = "\n".join(f"| D{i} | c | t | s | d{i}.html |" for i in range(3))
        good = DIRECTION.format(rows=three)
        self.assertNotIn("DS-DIRECTION", run_design(design(mode="new-direction", direction=good)))
        self.assertIn("DS-DIRECTION", run_design(design(mode="new-direction")))
        self.assertIn("DS-DIRECTION", run_design(design(mode="new-direction", direction=good, difference=False)))
        two = DIRECTION.format(rows="| D1 | c | t | s | d.html |\n| D2 | c | t | s | e.html |")
        self.assertIn("DS-DIRECTION", run_design(design(mode="new-direction", direction=two)))
        self.assertIn("DS-DIRECTION", run_design(design(mode="new-direction", direction=good.replace("Chosen:", "Maybe:"))))

    def test_ds_inventory(self):
        self.assertIn("DS-INVENTORY", run_design(design(inventory="")))
        self.assertIn("DS-INVENTORY", run_design(design(inventory=INVENTORY.format(home="top-bar, bottom-bar"))))
        self.assertNotIn("DS-INVENTORY", run_design(design(mode="light", inventory="")))

    def test_ds_nontext(self):
        self.assertIn("DS-NONTEXT", run_design(design(nontext="")))
        self.assertIn("DS-NONTEXT", run_design(design(nontext=NONTEXT.format(ratio="2.1:1"))))
        self.assertNotIn("DS-NONTEXT", run_design(design(nontext=NONTEXT.format(ratio="3.2:1"))))


def run_system(css_files):
    report = uxcheck.Report()
    uxcheck.check_system([str(f) for f in css_files], report)
    return report.items


class SystemTests(unittest.TestCase):
    def test_field_tokens_before_fix(self):
        """Regression: the raised surface #2e1a63 on the stage #1a0b3b of the field report."""
        items = run_system([EVIDENCE / "gameshow-tokens-before-fix.css"])
        surface = [i for i in items if i["rule"] == "SYS-SURFACE"]
        self.assertTrue(any("--color-surface on --color-bg" in i["message"] and i["level"] == "warning" for i in surface))
        self.assertTrue(any(i["rule"] == "SYS-NONTEXT" and i["level"] == "error" for i in items))

    def test_field_tokens_after_fix(self):
        items = run_system([EVIDENCE / "gameshow-tokens-after-fix-in-progress.css"])
        self.assertEqual([i for i in items if i["rule"] in ("SYS-SURFACE", "SYS-NONTEXT")], [])

    def write(self, css):
        tmp = tempfile.TemporaryDirectory()
        self.addCleanup(tmp.cleanup)
        path = pathlib.Path(tmp.name) / "t.css"
        path.write_text(css, encoding="utf-8")
        return [path]

    def test_border_of_a_control_is_checked(self):
        root = ":root { --c-bg: #ffffff; --c-text: #111111; --c-border: #dddddd; }\n"
        bad = run_system(self.write(root + ".input { border: 1px solid var(--c-border); }"))
        self.assertTrue(any(i["rule"] == "SYS-NONTEXT" for i in bad))
        card = run_system(self.write(root + ".card { border: 1px solid var(--c-border); }"))
        self.assertFalse(any(i["rule"] == "SYS-NONTEXT" for i in card))


def browser_ok():
    if not importlib.util.find_spec("playwright"):
        return False
    try:
        from playwright.sync_api import sync_playwright
        with sync_playwright() as p:
            p.chromium.launch().close()
        return True
    except Exception:
        return False


BROWSER = browser_ok()
CSS = """<style>
:root { --bg: #ffffff; --ink: #1b2430; --line: #767676; --accent: #0b6e66; --faint: #f4f4f4; --pale: #dddddd; }
body { margin: 0; background: var(--bg); color: var(--ink); font: 16px sans-serif; }
button { background: transparent; border: 0; padding: 0; width: 44px; height: 44px; color: var(--ink); font: inherit; }
button.wide { width: auto; padding: 0 12px; }
</style>"""
SPEAKER = '<svg width="24" height="24" viewBox="0 0 24 24" fill="currentColor" aria-hidden="true"><path d="M3 9v6h4l5 5V4L7 9H3z"/></svg>'
SLASHED = '<svg width="24" height="24" viewBox="0 0 24 24" fill="currentColor" aria-hidden="true"><path d="M3 9v6h4l5 5V4L7 9H3z M2 2l20 20"/></svg>'


def frame(body, screen="s", state="default"):
    return f'<section data-screen="{screen}" data-state="{state}">{body}</section>'


@unittest.skipUnless(BROWSER, "Playwright with Chromium is not available (python3 -m playwright install chromium)")
class MockupTests(unittest.TestCase):
    def run_mockups(self, *bodies, shots=None):
        """Each body is the frames of one HTML file. Return {rule: set of levels}."""
        with tempfile.TemporaryDirectory() as tmp:
            files = []
            for n, body in enumerate(bodies):
                f = pathlib.Path(tmp) / f"m{n}.html"
                f.write_text(f'<!doctype html><html lang="en"><head><meta charset="utf-8">{CSS}</head>'
                             f"<body>{body}</body></html>", encoding="utf-8")
                files.append(str(f))
            report = uxcheck.Report()
            uxcheck.check_mockups(files, None, uxcheck.PRIMARY, shots, report)
        out = {}
        for i in report.items:
            out.setdefault(i["rule"], set()).add(i["level"])
        return out

    def assertRule(self, rule, level, body, *more):
        got = self.run_mockups(body, *more)
        self.assertIn(rule, got, got)
        self.assertIn(level, got[rule])

    def assertNoRule(self, rule, body, *more):
        got = self.run_mockups(body, *more)
        self.assertNotIn(rule, got, got)

    def test_tell08_emoji_and_ligature_are_no_name(self):
        self.assertRule("TELL-08", "error", frame("<button>\U0001F507</button>"))
        self.assertRule("TELL-08", "error", frame('<button><span class="material-icons">volume_off</span></button>'))
        self.assertNoRule("TELL-08", frame(f'<button aria-label="Mute">{SPEAKER}</button>'))

    def test_tell21_name_with_next_action(self):
        self.assertRule("TELL-21", "error", frame(f'<button aria-label="Unmute">{SPEAKER}</button>'))
        self.assertNoRule("TELL-21", frame(f'<button aria-label="Mute">{SPEAKER}</button>'
                                           f'<button aria-label="Undo">{SPEAKER}</button>'))

    def test_tell21_name_changes_between_frames(self):
        on = frame(f'<button id="music" aria-pressed="true" aria-label="Music playing">{SPEAKER}</button>', state="on")
        off = frame(f'<button id="music" aria-pressed="false" aria-label="Music stopped">{SLASHED}</button>', state="off")
        self.assertRule("TELL-21", "error", on + off)
        fixed_off = off.replace("Music stopped", "Music playing")
        self.assertNoRule("TELL-21", on + fixed_off)

    def test_tell22_icon_changes_without_state(self):
        a = frame(f'<button id="music" aria-label="Music">{SPEAKER}</button>', state="a")
        b = frame(f'<button id="music" aria-label="Music">{SLASHED}</button>', state="b")
        self.assertRule("TELL-22", "error", a + b)
        self.assertNoRule("TELL-22", a.replace("<button ", '<button aria-pressed="true" ')
                          + b.replace("<button ", '<button aria-pressed="false" '))

    def test_tell23_toggle_by_color_only(self):
        on = frame(f'<button id="mic" aria-pressed="true" aria-label="Microphone" style="color:#0b6e66">{SPEAKER}</button>', state="on")
        off = frame(f'<button id="mic" aria-pressed="false" aria-label="Microphone" style="color:#1b2430">{SPEAKER}</button>', state="off")
        self.assertRule("TELL-23", "error", on + off)
        filled = on.replace('style="color:#0b6e66"', 'style="color:#ffffff;background:#0b6e66"')
        self.assertNoRule("TELL-23", filled + off)

    def test_tell24_icon_contrast(self):
        self.assertRule("TELL-24", "error", frame(f'<button aria-label="Mute" style="color:#dddddd">{SPEAKER}</button>'))
        self.assertNoRule("TELL-24", frame(f'<button aria-label="Mute">{SPEAKER}</button>'))

    def test_tell25_icon_only_primary(self):
        self.assertRule("TELL-25", "error", frame(f'<button class="primary-action" aria-label="Start">{SPEAKER}</button>'))
        self.assertNoRule("TELL-25", frame(f'<button class="primary-action wide">{SPEAKER} Start</button>'))

    def test_tell26_icon_only_status(self):
        self.assertRule("TELL-26", "error", frame(f'<div role="status">{SPEAKER}</div>'))
        self.assertNoRule("TELL-26", frame(f'<div role="status">{SPEAKER} Ready</div>'))

    def test_tell27_title_only(self):
        self.assertRule("TELL-27", "warning", frame(f'<button title="Settings">{SPEAKER}</button>'))
        self.assertNoRule("TELL-27", frame(f'<button title="Settings" aria-label="Settings">{SPEAKER}</button>'))

    def test_tell28_small_targets(self):
        small = '<button aria-label="A" style="width:16px;height:16px">a</button>'
        self.assertRule("TELL-28", "warning", frame(f"<div>{small}{small.replace('A', 'B')}</div>"))
        spaced = f'<div style="display:flex;gap:40px">{small}{small.replace("A", "B")}</div>'
        self.assertNoRule("TELL-28", frame(spaced))

    def test_tell29_icon_only_navigation(self):
        self.assertRule("TELL-29", "warning", frame(f'<nav><a href="#" aria-label="Home">{SPEAKER}</a></nav>'))
        self.assertRule("TELL-29", "warning", frame(f'<button aria-expanded="false" aria-label="Menu">{SPEAKER}</button>'))
        self.assertNoRule("TELL-29", frame(f'<nav><a href="#">{SPEAKER} Home</a></nav>'))

    def test_tell30_one_icon_two_names(self):
        a = frame(f'<button aria-label="Sound">{SPEAKER}</button>', screen="lobby")
        b = frame(f'<button aria-label="Announce">{SPEAKER}</button>', screen="game")
        self.assertRule("TELL-30", "warning", a, b)
        self.assertNoRule("TELL-30", a, b.replace("Announce", "Sound"))

    def test_mk_nontext_input_and_segment(self):
        self.assertRule("MK-NONTEXT", "error", frame('<label>Name <input style="border:1px solid #dddddd;background:#fff"></label>'))
        self.assertNoRule("MK-NONTEXT", frame('<label>Name <input style="border:1px solid #767676;background:#fff"></label>'))
        seg = ('<div role="group" style="background:#fff"><button class="wide" aria-pressed="true" style="background:{}">Easy</button>'
               '<button class="wide" aria-pressed="false">Hard</button></div>')
        self.assertRule("MK-NONTEXT", "error", frame(seg.format("#f4f4f4")))
        self.assertNoRule("MK-NONTEXT", frame(seg.format("#0b6e66;color:#fff")))

    def test_mk_surface_field_chip(self):
        stage = '<div style="background:#1a0b3b;color:#fff;padding:16px">{}</div>'
        chip = '<button class="chip wide" style="background:#2e1a63;color:#fff{}">Alex</button>'
        self.assertRule("MK-SURFACE", "warning", frame(stage.format(chip.format(""))))
        self.assertNoRule("MK-SURFACE", frame(stage.format(chip.format(";border:2px solid #a08bff"))))

    def test_mk_region(self):
        lobby = frame('<header data-region="top-bar"><button data-element="music" aria-label="Music">m</button></header>', "lobby")
        game = frame('<footer data-region="bottom-bar"><button data-element="music" aria-label="Music">m</button></footer>', "game")
        self.assertRule("MK-REGION", "error", lobby + game)
        self.assertNoRule("MK-REGION", lobby + game.replace("bottom-bar", "top-bar"))

    def test_mk_region_missing(self):
        self.assertRule("MK-REGION-MISSING", "warning", frame('<button data-element="music" aria-label="Music">m</button>'))
        self.assertNoRule("MK-REGION-MISSING", frame('<div data-region="top-bar"><button data-element="music" aria-label="Music">m</button></div>'))

    def test_mk_width_and_shots(self):
        self.assertRule("MK-WIDTH", "error", frame('<div style="width:600px">Wide</div>'))
        with tempfile.TemporaryDirectory() as tmp:
            got = self.run_mockups(frame("<p>Fits</p>"), shots=tmp)
            self.assertNotIn("MK-WIDTH", got)
            names = sorted(p.name for p in pathlib.Path(tmp).iterdir())
        self.assertEqual(names, ["m0__01-default__1440.png", "m0__01-default__390.png", "m0__01-default__820.png"])


class CrossFrameTests(unittest.TestCase):
    """cross_frame without a browser, from the data that MOCKUP_JS returns."""

    def ctl(self, **kw):
        base = {"key": "music", "name": "Music", "src": "label", "iconOnly": True, "pressed": None, "checked": None,
                "role": "", "text": "", "iconKey": "svg:a", "markup": "<svg>a</svg>", "color": "rgb(0, 0, 0)",
                "bg": "rgba(0, 0, 0, 0)", "bgOn": False, "border": "rgb(0, 0, 0)", "borderW": "0px", "desc": "<button>"}
        base.update(kw)
        return base

    def rules(self, *frames):
        report = uxcheck.Report()
        data = [{"file": "m.html", "width": 390, "screen": s, "state": st, "controls": c, "elements": e}
                for s, st, c, e in frames]
        uxcheck.cross_frame(data, report)
        return {i["rule"] for i in report.items}

    def test_toggle_with_fixed_name_and_shape_passes(self):
        got = self.rules(("s", "on", [self.ctl(pressed="true", iconKey="svg:b", markup="<svg>b</svg>")], []),
                         ("s", "off", [self.ctl(pressed="false")], []))
        self.assertEqual(got, set())

    def test_color_only_toggle(self):
        got = self.rules(("s", "on", [self.ctl(pressed="true", color="rgb(1, 2, 3)")], []),
                         ("s", "off", [self.ctl(pressed="false")], []))
        self.assertEqual(got, {"TELL-23"})


FAULTS = ROOT / "evals" / "ux-design" / "fixtures" / "party-review-faults" / "design-system" / "gameshow"
PLANTED = ROOT / "evals" / "ux-design" / "fixtures" / "party-planted" / "mockups"


@unittest.skipUnless(BROWSER, "Playwright with Chromium is not available (python3 -m playwright install chromium)")
class FixRoundTests(unittest.TestCase):
    """The fix round after the 0.4.0 eval: gradients, tiles without English class names, the segment track,
    a missing file, and the control edges of the field tokens."""

    def check(self, html, files=()):
        """Write html to a temp file and run the mockup check. Return [(rule, level, message)]."""
        with tempfile.TemporaryDirectory() as tmp:
            f = pathlib.Path(tmp) / "m.html"
            f.write_text(html, encoding="utf-8")
            report = uxcheck.Report()
            uxcheck.check_mockups([str(f), *files], None, uxcheck.PRIMARY, None, report)
        return [(i["rule"], i["level"], i["message"]) for i in report.items]

    def page(self, body, head=CSS):
        return f'<!doctype html><html lang="nl"><head><meta charset="utf-8">{head}</head><body>{body}</body></html>'

    def rules(self, items, rule):
        return [m for r, _, m in items if r == rule]

    def test_gradient_stage_gives_a_background(self):
        stage = ('<div style="background:radial-gradient(circle at 50% 0%, #42277f, #1a0b3b 60%);color:#fff;'
                 'padding:16px"><label>Naam <input value="Anna" style="background:#0d0620;color:#fff;'
                 'border:2px solid {}"></label></div>')
        bad = self.check(self.page(frame(stage.format("#42277f"))))
        self.assertTrue(self.rules(bad, "MK-NONTEXT"), bad)
        good = self.check(self.page(frame(stage.format("#a08bff"))))
        self.assertFalse(self.rules(good, "MK-NONTEXT"), good)

    def test_tile_without_an_english_class_name(self):
        stage = '<div style="background:#1a0b3b;color:#fff;padding:16px"><ul style="list-style:none;margin:0;padding:0">{}</ul></div>'
        tile = ('<li class="gs-plek" style="background:#2e1a63;padding:12px{}"><button class="wide" '
                'style="background:transparent;color:#fff" aria-label="Bram verwijderen">Bram</button></li>')
        bad = self.check(self.page(frame(stage.format(tile.format("")))))
        self.assertTrue(any("gs-plek" in m for m in self.rules(bad, "MK-SURFACE")), bad)
        good = self.check(self.page(frame(stage.format(tile.format(";border:2px solid #a08bff")))))
        self.assertFalse(self.rules(good, "MK-SURFACE"), good)
        # A tile without a control is decoration, and the stage itself is not a tile.
        plain = self.check(self.page(frame(stage.format('<li style="background:#2e1a63;padding:12px">Anna</li>'))))
        self.assertFalse(self.rules(plain, "MK-SURFACE"), plain)

    def test_segment_track_is_checked(self):
        seg = ('<div style="background:#1a0b3b;color:#fff;padding:16px"><div role="group" aria-label="Doelscore" '
               'style="display:inline-flex;background:#2e1a63;padding:4px{}"><button class="wide" aria-pressed="false" '
               'style="color:#fff">5</button><button class="wide" aria-pressed="true" style="background:#ffd23f;'
               'color:#1a0b3b">7</button></div></div>')
        bad = self.check(self.page(frame(seg.format(""))))
        self.assertTrue(any(m.startswith("m.html") and "Track" in m for m in self.rules(bad, "MK-NONTEXT")), bad)
        good = self.check(self.page(frame(seg.format(";border:2px solid #a08bff"))))
        self.assertFalse(self.rules(good, "MK-NONTEXT"), good)

    def test_planted_field_mockup(self):
        """The planted lobby of the eval: the input on the gradient stage, the tiles, and the segment track."""
        report = uxcheck.Report()
        uxcheck.check_mockups([str(PLANTED / "lobby.html")], None, uxcheck.PRIMARY, None, report)
        msgs = [(i["rule"], i["message"]) for i in report.items]
        self.assertTrue(any(r == "MK-NONTEXT" and "gs-invoer" in m for r, m in msgs), msgs)
        self.assertTrue(any(r == "MK-NONTEXT" and "gs-segment" in m for r, m in msgs), msgs)
        self.assertTrue(any(r == "MK-SURFACE" and "gs-tegel" in m for r, m in msgs), msgs)
        # Decoys: the decorative card stack and the text button "Spelregels".
        self.assertFalse(any("stapel" in m or "Spelregels" in m for r, m in msgs if r.startswith("MK-")), msgs)

    def field_controls(self, tokens, fixed):
        """A mockup with the field tokens: a segmented control and a field filled with --color-surface."""
        edge = "border: 2px solid var(--color-border);" if fixed else "border: 0;"
        selected = "var(--color-lamp); color: var(--color-text-inverse)" if fixed else "var(--color-surface-spot)"
        head = (f'<link rel="stylesheet" href="{tokens.resolve().as_uri()}"><style>'
                "body { margin: 0; background: var(--color-bg); color: var(--color-text); font: 18px sans-serif; }"
                f".seg {{ display: inline-flex; background: var(--color-surface); {edge} padding: 4px; }}"
                ".seg button { background: transparent; color: var(--color-text); border: 0; padding: 8px 16px; font: inherit; }"
                f'.seg button[aria-pressed="true"] {{ background: {selected}; }}'
                f".veld {{ background: var(--color-surface); color: var(--color-text); {edge} padding: 12px; font: inherit; }}"
                "</style>")
        body = frame('<div role="group" aria-label="Doelscore" class="seg"><button aria-pressed="false">5</button>'
                     '<button aria-pressed="true">7</button></div><label>Naam <input class="veld" value="Anna"></label>')
        return self.check(self.page(body, head))

    def test_field_tokens_control_edges(self):
        """SYS-NONTEXT sees tokens only. The edge fault of the field is a surface used as a control fill:
        MK-NONTEXT finds it in the mockup."""
        before = self.field_controls(EVIDENCE / "gameshow-tokens-before-fix.css", fixed=False)
        self.assertTrue(self.rules(before, "MK-NONTEXT"), before)
        after = self.field_controls(EVIDENCE / "gameshow-tokens-after-fix-in-progress.css", fixed=True)
        self.assertFalse(self.rules(after, "MK-NONTEXT"), after)


class MissingFileTests(unittest.TestCase):
    def test_missing_file_is_a_usage_error(self):
        import subprocess
        script = SCRIPTS / "uxcheck.py"
        for args in (["mockup", "nope.html"], ["system", "nope.css"], ["design", "nope.md"],
                     ["review", "nope.json", "--out", "x"]):
            res = subprocess.run([sys.executable, str(script), *args], capture_output=True, text=True)
            self.assertEqual(res.returncode, 2, args)
            self.assertIn("file not found: nope", res.stderr)
            self.assertNotIn("Traceback", res.stderr)


if __name__ == "__main__":
    unittest.main()
