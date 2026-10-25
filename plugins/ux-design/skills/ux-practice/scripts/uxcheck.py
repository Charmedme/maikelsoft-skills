#!/usr/bin/env python3
"""Check UX design files and HTML mockups.

Usage:
    python3 uxcheck.py design <ux-design.md> [--patterns <patterns.md>] [--decisions <decisions.md>] [--json]
    python3 uxcheck.py mockup <file.html>... [--design <ux-design.md>] [--primary <css selector>]
                              [--shots <dir>] [--json]
    python3 uxcheck.py system <file.css>... [--json]
    python3 uxcheck.py review <walk.json> --out <dir> [--axe <axe.min.js>] [--primary <css selector>] [--no-keys] [--json]

design  checks the design spec (10 decisions with a source each), the source tags, the
        pattern and decision IDs, the states tables, and the options table.
system  checks a design system: the contrast of text and background token pairs and of each
        component rule with a color and a background, gradients, the focus style, and font sizes
        outside the size tokens.
mockup  renders each HTML mockup with Playwright and checks the tells: off-token colors and
        font sizes, more than one primary action per frame, nested surfaces, gradients, emoji
        icons, centered body text, unnamed icon buttons, unlabeled inputs, font weights, and
        text contrast. With --design it also checks that each state has a frame.

review  walks the built app with Playwright from a walk file. For each state of each flow, it
        sets the routes, runs the steps, and then at 390, 820, and 1440 px it takes a
        screenshot, runs the tell checks and axe-core (when axe.min.js is found), and checks
        horizontal overflow. At 1440 px it also walks the focus with the Tab key: a focus that
        does not show (RV-FOCUS-VISIBLE), a focus trap (RV-FOCUS-TRAP), a focus under a sticky or
        fixed element (RV-FOCUS-OBSCURED), a focus order that jumps back (RV-FOCUS-ORDER), and
        a tabindex above 0 (RV-TABINDEX).
        With prefers-reduced-motion it finds animations that still run (RV-MOTION). --no-keys
        skips these checks. It writes <out>/review.json and <out>/<flow>__<state>__<width>.png.

A mockup frame is an element with data-screen and data-state attributes.
Exit code: 0 no errors, 1 errors, 2 bad usage, 3 Playwright is missing.
"""
import json
import re
import sys
from pathlib import Path

DECISIONS = ["purpose", "main-task", "pattern", "layout", "hierarchy",
             "components", "density", "states", "copy", "motion"]
CORE_STATES = ["default", "empty", "loading", "partial", "error",
               "success", "disabled", "overflow", "offline"]
SOURCE = re.compile(r"^(product|system|pattern|decision):\s*\S")
PATTERN_ID = re.compile(r"PAT-[A-Z]+-\d{2}")
DECISION_ID = re.compile(r"UXD-\d+")
LOREM = re.compile(r"lorem ipsum|dolor sit amet", re.I)
# Default selector for a primary action. A project can pass its own with --primary.
PRIMARY = ('[data-variant="primary"], button[class*="primary"], a[class*="primary"], '
           '[role="button"][class*="primary"], .primary-action')


class Report:
    def __init__(self):
        self.items = []

    def add(self, level, rule, msg):
        self.items.append({"level": level, "rule": rule, "message": msg})

    def error(self, rule, msg):
        self.add("error", rule, msg)

    def warn(self, rule, msg):
        self.add("warning", rule, msg)

    def count(self, level):
        return sum(1 for i in self.items if i["level"] == level)

    def finish(self, as_json, extra=None):
        if as_json:
            out = {"items": self.items, "errors": self.count("error"), "warnings": self.count("warning")}
            if extra:
                out.update(extra)
            print(json.dumps(out, indent=2))
        else:
            for i in self.items:
                print(f'{i["level"]}: {i["rule"]}: {i["message"]}')
            print(f'uxcheck: {self.count("error")} errors, {self.count("warning")} warnings')
        return 1 if self.count("error") else 0


def sections(text, prefix):
    """Return {name: body} for each heading that starts with prefix."""
    out = {}
    pat = re.compile(r"^(#{2,3}) " + re.escape(prefix) + r"[ \t]*(.*?)[ \t]*$", re.M)
    matches = list(pat.finditer(text))
    for i, m in enumerate(matches):
        start = m.end()
        nxt = re.compile(r"^#{2,3} ", re.M).search(text, start)
        out[m.group(2).strip("` ")] = text[start: nxt.start() if nxt else len(text)]
    return out


def table_rows(body):
    rows = []
    for line in body.splitlines():
        line = line.strip()
        if not line.startswith("|") or re.match(r"^\|[\s:|-]+\|$", line):
            continue
        cells = [c.strip() for c in line.strip("|").split("|")]
        rows.append(cells)
    return rows[1:] if rows else []


def check_design(path, patterns_file, decisions_file, r):
    text = Path(path).read_text(encoding="utf-8")
    mode = (re.search(r"^Mode:\s*(\w+)", text, re.M) or [None, "full"])[1].lower()
    surface = re.search(r"^Surface:\s*(\w+)", text, re.M)
    if not surface:
        r.error("DS-SURFACE", 'No "Surface:" line (operate, read, or persuade).')
    known_patterns = set()
    if patterns_file and Path(patterns_file).exists():
        known_patterns = set(PATTERN_ID.findall(Path(patterns_file).read_text(encoding="utf-8")))
    elif patterns_file:
        r.warn("DS-PATTERNS", f"Patterns file {patterns_file} not found. Pattern IDs not checked.")
    known_decisions = set(DECISION_ID.findall(text))
    if decisions_file and Path(decisions_file).exists():
        known_decisions |= set(DECISION_ID.findall(Path(decisions_file).read_text(encoding="utf-8")))

    frame = sections(text, "Frame")
    if not frame:
        r.error("DS-FRAME", "No Frame section.")

    if mode == "full":
        opts = sections(text, "Options")
        if not opts:
            r.error("DS-OPTIONS", "Full mode needs an Options section with 2 or 3 options.")
        else:
            body = next(iter(opts.values()))
            n = len(table_rows(body))
            if n < 2 or n > 3:
                r.error("DS-OPTIONS", f"The Options table has {n} options. Use 2 or 3.")
            if not re.search(r"^Selected:\s*\S", body, re.M):
                r.error("DS-OPTIONS", 'The Options section has no "Selected:" line.')

    screens = sections(text, "Screen:")
    states = sections(text, "States:")
    if not screens:
        r.error("DS-SCREEN", 'No "## Screen: <id>" section.')
    stats = {"decisions": 0, "sourced": 0}
    for sid, body in screens.items():
        got = {}
        for cells in table_rows(body):
            if len(cells) >= 3 and cells[0].lower() in DECISIONS:
                got[cells[0].lower()] = cells
        for d in DECISIONS:
            stats["decisions"] += 1
            if d not in got:
                r.error("DS-DECISION", f"Screen {sid}: decision '{d}' is missing.")
                continue
            _, value, source = got[d][:3]
            if not value:
                r.error("DS-VALUE", f"Screen {sid}: decision '{d}' has no value.")
            if not SOURCE.match(source):
                r.error("DS-SOURCE", f"Screen {sid}: decision '{d}' has no source tag (product:, system:, pattern:, decision:).")
                continue
            stats["sourced"] += 1
            if source.startswith("pattern:"):
                ids = PATTERN_ID.findall(source)
                if not ids:
                    r.error("DS-PATTERN-ID", f"Screen {sid}: decision '{d}' has a pattern tag without a rule ID.")
                for pid in ids:
                    if known_patterns and pid not in known_patterns:
                        r.error("DS-PATTERN-ID", f"Screen {sid}: rule {pid} does not exist.")
            if source.startswith("decision:"):
                ids = DECISION_ID.findall(source)
                if not ids:
                    r.error("DS-DECISION-ID", f"Screen {sid}: decision '{d}' has a decision tag without an ID.")
                for did in ids:
                    if did not in known_decisions:
                        r.error("DS-DECISION-ID", f"Screen {sid}: decision {did} is not in the log or the Decisions section.")
        st_body = states.get(sid)
        if st_body is None:
            r.error("DS-STATES", f"Screen {sid}: no '### States: {sid}' table.")
            continue
        rows = {c[0].lower(): c for c in table_rows(st_body) if c}
        for s in CORE_STATES:
            if s not in rows:
                r.error("DS-STATES", f"Screen {sid}: state '{s}' is missing.")
                continue
            c = rows[s] + ["", "", ""]
            if c[1].lower().startswith("n/a"):
                if not re.match(r"n/a:\s*\S", c[1], re.I):
                    r.error("DS-STATES", f"Screen {sid}: state '{s}' is n/a without a reason.")
                continue
            if not c[1] or not c[2] or not c[3]:
                r.error("DS-STATES", f"Screen {sid}: state '{s}' does not answer the 3 questions.")
    known_states = set()
    for body in states.values():
        known_states |= {c[0].lower().strip("` ") for c in table_rows(body) if c}
    fp = sections(text, "Failure paths")
    if fp:
        for cells in table_rows(next(iter(fp.values()))):
            if len(cells) >= 3 and cells[2]:
                st = cells[2].lower().strip("` ")
                if st not in known_states:
                    r.error("DS-FAILURE", f"Failure path '{cells[1][:50]}' names state '{cells[2]}', which no states table has.")
    elif mode == "full":
        r.error("DS-FAILURE", "No '### Failure paths' table in the Flow.")
    if LOREM.search(text):
        r.error("DS-LOREM", "Lorem ipsum in the design.")
    return {"mode": mode, "screens": len(screens), **stats}


MOCKUP_JS = r"""
([primarySel, fileProps]) => {
  const out = { frames: [], tokens: { colors: 0, sizes: 0 } };
  const probe = document.createElement('div');
  document.body.appendChild(probe);
  const norm = (prop, v) => { probe.style[prop] = ''; probe.style[prop] = v; return getComputedStyle(probe)[prop]; };
  const colorSet = new Set(['rgba(0, 0, 0, 0)']);
  const sizeSet = new Set();
  const weightSet = new Set();
  const props = new Set(fileProps);
  for (const sheet of document.styleSheets) {
    let rules; try { rules = sheet.cssRules; } catch (e) { continue; }
    const walk = (rs) => { for (const rule of rs) {
      if (rule.cssRules) walk(rule.cssRules);
      if (rule.style) for (const p of rule.style) if (p.startsWith('--')) props.add(p);
    } };
    walk(rules);
  }
  const typeNamed = [...props].some(p => /font|size|text|type/i.test(p) && !/space|gap|radius|shadow|height|width/i.test(p));
  const themeEls = [document.documentElement, ...document.querySelectorAll('[data-theme]')];
  for (const el of themeEls) {
    const cs = getComputedStyle(el);
    for (const p of props) {
      const v = cs.getPropertyValue(p).trim();
      if (!v) continue;
      const c = norm('color', v);
      if (c && v !== 'inherit' && /^(#|rgb|hsl|oklch|lab|lch|color\(|[a-z]+$)/i.test(v) && c !== '') colorSet.add(c);
      if (/weight/i.test(p) && /^\d{3}$/.test(v)) weightSet.add(v);
      if (/^-?[\d.]+(px|rem|em)$/.test(v) && (!typeNamed || /font|size|text|type/i.test(p))) { const s = norm('fontSize', v); if (s) sizeSet.add(s); }
    }
  }
  probe.remove();
  out.tokens.colors = colorSet.size; out.tokens.sizes = sizeSet.size;
  const lum = (rgb) => { const m = rgb.match(/[\d.]+/g); if (!m) return 1; const a = m.slice(0, 3).map(Number).map(x => { x /= 255; return x <= 0.03928 ? x / 12.92 : Math.pow((x + 0.055) / 1.055, 2.4); }); return 0.2126 * a[0] + 0.7152 * a[1] + 0.0722 * a[2]; };
  const alpha = (rgb) => { const m = rgb.match(/[\d.]+/g); return m && m.length > 3 ? Number(m[3]) : 1; };
  const bgOf = (el) => { for (let e = el; e; e = e.parentElement) { const cs = getComputedStyle(e); if (/gradient|url\(/.test(cs.backgroundImage)) return null; const b = cs.backgroundColor; if (alpha(b) > 0.5) return b; } return 'rgb(255, 255, 255)'; };
  const ratio = (a, b) => { const x = lum(a), y = lum(b); return (Math.max(x, y) + 0.05) / (Math.min(x, y) + 0.05); };
  const visible = (el) => { const r = el.getBoundingClientRect(); const cs = getComputedStyle(el); return r.width > 1 && r.height > 1 && cs.visibility !== 'hidden' && cs.display !== 'none' && Number(cs.opacity) > 0 && !el.closest('[aria-hidden="true"]'); };
  const ownText = (el) => [...el.childNodes].filter(n => n.nodeType === 3).map(n => n.textContent).join('').trim();
  const emoji = /\p{Extended_Pictographic}/u;
  const frames = [...document.querySelectorAll('[data-screen][data-state]')];
  for (const fr of frames) {
    const f = { screen: fr.dataset.screen, state: fr.dataset.state, issues: [] };
    const add = (rule, msg) => f.issues.push({ rule, msg });
    const els = [fr, ...fr.querySelectorAll('*')].filter(visible);
    const offColors = new Set(), offSizes = new Set(), weights = new Set();
    let primaries = 0;
    for (const el of els) {
      const cs = getComputedStyle(el);
      const txt = ownText(el);
      for (const p of ['color', 'backgroundColor', 'borderTopColor']) {
        if (p === 'borderTopColor' && parseFloat(cs.borderTopWidth) === 0) continue;
        if (p === 'color' && !txt) continue;
        const v = cs[p];
        if (v && !colorSet.has(v) && alpha(v) > 0) offColors.add(v);
      }
      if (txt) {
        if (sizeSet.size && !sizeSet.has(cs.fontSize)) offSizes.add(cs.fontSize);
        weights.add(cs.fontWeight);
        const large = parseFloat(cs.fontSize) >= 24 || (parseFloat(cs.fontSize) >= 18.66 && Number(cs.fontWeight) >= 700);
        const bg = bgOf(el);
        const cr = bg ? ratio(cs.color, bg) : 99;
        if (cr < (large ? 3 : 4.5)) add('TELL-11', `Contrast ${cr.toFixed(2)}:1 for "${txt.slice(0, 30)}"`);
        if (emoji.test(txt)) add('TELL-06', `Emoji in "${txt.slice(0, 30)}"`);
        if (txt.length > 60 && cs.textAlign === 'center' && el.getBoundingClientRect().width > 480 && !/^H[1-6]$/.test(el.tagName)) add('TELL-07', `Centered body text: "${txt.slice(0, 30)}"`);
      }
      if (/gradient/.test(cs.backgroundImage)) add('TELL-05', `Gradient on <${el.tagName.toLowerCase()} class="${el.className}">`);
      if (el.matches && el.matches(primarySel)) primaries++;
      const surface = (e) => { const s = getComputedStyle(e); return s.boxShadow !== 'none' && parseFloat(s.borderTopLeftRadius) > 0; };
      if (el !== fr && surface(el)) { for (let a = el.parentElement; a && a !== fr.parentElement; a = a.parentElement) { if (surface(a)) { add('TELL-04', `Nested surface: <${el.tagName.toLowerCase()} class="${el.className}"> in <${a.tagName.toLowerCase()} class="${a.className}">`); break; } } }
      if ((el.tagName === 'BUTTON' || el.getAttribute('role') === 'button') && !el.textContent.trim() && !el.getAttribute('aria-label') && !el.getAttribute('aria-labelledby') && !el.getAttribute('title')) add('TELL-08', `Icon button without a name: class="${el.className}"`);
      if (['INPUT', 'SELECT', 'TEXTAREA'].includes(el.tagName) && !['hidden', 'submit', 'button', 'checkbox', 'radio'].includes(el.type) && !(el.labels && el.labels.length) && !el.getAttribute('aria-label') && !el.getAttribute('aria-labelledby')) add('TELL-09', `Input without a label: name="${el.name || ''}" placeholder="${el.placeholder || ''}"`);
    }
    if (primaries > 1) add('TELL-01', `${primaries} primary actions in one frame`);
    if (!/^proposed/.test(fr.dataset.state || '')) { const n = fr.querySelectorAll('[data-proposed]').length; if (n) add('MK-PROPOSED', `${n} proposed element(s) outside a proposed-op frame`); }
    for (const c of offColors) add('TELL-03', `Color outside the tokens: ${c}`);
    for (const s of offSizes) add('TELL-02', `Font size outside the tokens: ${s}`);
    if (weightSet.size) { for (const w of weights) if (!weightSet.has(w)) add('TELL-10', `Font weight outside the tokens: ${w}`); }
    else if (weights.size > 3) add('TELL-10', `${weights.size} font weights: ${[...weights].join(', ')}`);
    out.frames.push(f);
  }
  return out;
}
"""


def css_props(html_file):
    """Custom property names from the linked CSS files and the style blocks.

    Chromium does not let a page read the rules of a file:// stylesheet, so read them here.
    """
    text = Path(html_file).read_text(encoding="utf-8", errors="replace")
    css = " ".join(re.findall(r"<style[^>]*>(.*?)</style>", text, re.S))
    for href in re.findall(r'<link[^>]+rel=["\']?stylesheet["\']?[^>]*href=["\']([^"\']+)', text) + \
            re.findall(r'<link[^>]+href=["\']([^"\']+)["\'][^>]*rel=["\']?stylesheet', text):
        if href.startswith("file://"):
            from urllib.parse import unquote, urlparse
            f = Path(unquote(urlparse(href).path))
        else:
            f = (Path(html_file).parent / href).resolve()
        if f.exists():
            css += " " + f.read_text(encoding="utf-8", errors="replace")
    return sorted(set(re.findall(r"(--[\w-]+)\s*:", css)))


def check_mockups(files, design, primary, shots, r):
    try:
        from playwright.sync_api import sync_playwright
    except ImportError:
        r.error("MK-PLAYWRIGHT", "Playwright for Python is missing: pip install playwright.")
        return 3, {}
    found = {}
    stats = {"frames": 0, "tells": 0}
    with sync_playwright() as p:
        browser = p.chromium.launch()
        page = browser.new_page(viewport={"width": 1440, "height": 900})
        for f in files:
            page.goto(Path(f).resolve().as_uri())
            page.wait_for_timeout(300)
            res = page.evaluate(MOCKUP_JS, [primary, css_props(f)])
            if res["tokens"]["colors"] <= 1:
                r.warn("MK-TOKENS", f"{f}: no color tokens found (CSS custom properties). The color check is weak.")
            if not res["frames"]:
                r.error("MK-FRAME", f"{f}: no frame (an element with data-screen and data-state).")
            seen = set()  # count each issue once per file, not once per frame
            for fr in res["frames"]:
                stats["frames"] += 1
                found.setdefault(fr["screen"], set()).add(fr["state"])
                for i in fr["issues"]:
                    key = (i["rule"], i["msg"])
                    if key in seen:
                        continue
                    seen.add(key)
                    stats["tells"] += 1
                    r.error(i["rule"], f'{Path(f).name} [{fr["screen"]}/{fr["state"]}]: {i["msg"]}')
            if shots:
                # One image per frame, so a reviewer sees each state at full size.
                Path(shots).mkdir(parents=True, exist_ok=True)
                for i, fr in enumerate(res["frames"]):
                    loc = page.locator(f'[data-screen="{fr["screen"]}"][data-state="{fr["state"]}"]').first
                    name = f'{Path(f).stem}__{i + 1:02d}-{re.sub(r"[^a-z0-9-]+", "-", fr["state"].lower())}.png'
                    try:
                        loc.screenshot(path=str(Path(shots) / name))
                    except Exception:  # a frame with no box (hidden) gets no image
                        pass
        browser.close()
    if design:
        text = Path(design).read_text(encoding="utf-8")
        for sid, body in sections(text, "States:").items():
            for cells in table_rows(body):
                if not cells or cells[0].lower() in ("", "state"):
                    continue
                if len(cells) > 1 and cells[1].lower().startswith("n/a"):
                    continue
                if cells[0].lower() not in found.get(sid, set()):
                    r.error("MK-STATE", f"Screen {sid}: state '{cells[0]}' has no frame.")
    return None, stats


WIDTHS = [(390, 844), (820, 1180), (1440, 900)]


def find_axe(path):
    """axe.min.js from --axe, or from node_modules in the current folder."""
    for c in [path, "node_modules/axe-core/axe.min.js"]:
        if c and Path(c).exists():
            return Path(c).read_text(encoding="utf-8")
    return None


def run_steps(page, steps, base):
    """Run the steps of one state. Return None, or the text of the step that failed."""
    for n, st in enumerate(steps, 1):
        try:
            if "goto" in st:
                page.goto(base.rstrip("/") + "/" + st["goto"].lstrip("/"))
            elif "click" in st:
                page.click(st["click"], timeout=5000, no_wait_after=bool(st.get("no_wait")))
            elif "fill" in st:
                page.fill(st["fill"][0], st["fill"][1], timeout=5000)
            elif "select" in st:
                page.select_option(st["select"][0], st["select"][1], timeout=5000)
            elif "check" in st:
                page.check(st["check"], timeout=5000)
            elif "press" in st:
                page.press(st["press"][0], st["press"][1], timeout=5000)
            elif "wait" in st:
                page.wait_for_selector(st["wait"], timeout=st.get("timeout", 5000))
            elif "wait_ms" in st:
                page.wait_for_timeout(st["wait_ms"])
            elif "expect_text" in st:
                sel, want = st["expect_text"]
                got = page.inner_text(sel, timeout=5000)
                if want not in got:
                    return f"step {n}: {sel} shows {got[:80]!r}, expected {want!r}"
            else:
                return f"step {n}: unknown step {st}"
        except Exception as e:  # a failed step is a finding, not a crash
            return f"step {n} {json.dumps(st)}: {str(e).splitlines()[0][:160]}"
    return None


def add_routes(page, routes, held):
    """Fake answers of the server, to make a state: an error status, a body, or a request that never ends."""
    def make(rt):
        def handler(route, request=None):
            if rt.get("method") and route.request.method != rt["method"].upper():
                return route.continue_()
            if rt.get("hold"):
                held.append(route)  # never answered: the page stays in its loading state
                return None
            if rt.get("abort"):
                return route.abort()
            return route.fulfill(status=rt.get("status", 200), body=rt.get("body", ""),
                                 content_type=rt.get("content_type", "application/json"))
        return handler
    for rt in routes:
        page.route(rt["url"], make(rt))


FOCUS_JS = """() => {
  const el = document.activeElement;
  if (!el || el === document.body || el === document.documentElement) return null;
  const sel = (n) => {
    const parts = [];
    while (n && n.nodeType === 1 && parts.length < 4) {
      let p = n.tagName.toLowerCase();
      if (n.id) { parts.unshift(p + '#' + n.id); break; }
      const t = (n.getAttribute('aria-label') || n.textContent || '').trim().slice(0, 24);
      if (parts.length === 0 && t) p += '[' + t.replace(/\\s+/g, ' ') + ']';
      parts.unshift(p);
      n = n.parentElement;
    }
    return parts.join(' > ');
  };
  const sig = (e) => { const c = getComputedStyle(e); return [c.outlineStyle, c.outlineWidth, c.outlineColor, c.boxShadow,
    c.borderTopColor, c.borderBottomColor, c.backgroundColor, c.color, c.textDecorationLine].join('|'); };
  const r = el.getBoundingClientRect();
  const focused = sig(el);
  el.blur();
  const plain = sig(el);
  el.focus({preventScroll: true});
  let obscured = '';
  const cx = r.left + r.width / 2, cy = r.top + Math.min(r.height / 2, 12);
  if (cy >= 0 && cy < innerHeight && cx >= 0 && cx < innerWidth) {
    const top = document.elementFromPoint(cx, cy);
    if (top && !el.contains(top) && !top.contains(el)) {
      for (let n = top; n && n !== document.body; n = n.parentElement) {
        const pos = getComputedStyle(n).position;
        if (pos === 'fixed' || pos === 'sticky') { obscured = sel(n); break; }
      }
    }
  }
  return {sel: sel(el), top: Math.round(r.top + scrollY), left: Math.round(r.left + scrollX),
          visible: focused !== plain, obscured, modal: !!el.closest('[aria-modal="true"], dialog[open]')};
}"""

TABBABLE_JS = """() => [...document.querySelectorAll('a[href], button:not([disabled]), input:not([disabled]):not([type=hidden]), select:not([disabled]), textarea:not([disabled]), summary, [tabindex]:not([tabindex="-1"])')]
  .filter(e => { const r = e.getBoundingClientRect(), c = getComputedStyle(e);
    return r.width > 1 && r.height > 1 && c.visibility !== 'hidden' && !e.closest('[inert], [aria-hidden="true"]'); }).length"""

TABINDEX_JS = """() => [...document.querySelectorAll('[tabindex]')].filter(e => e.tabIndex > 0)
  .map(e => e.tagName.toLowerCase() + (e.id ? '#' + e.id : '') + '[tabindex=' + e.getAttribute('tabindex') + '] ' +
       (e.getAttribute('aria-label') || e.textContent || '').trim().slice(0, 24))"""

MOTION_JS = """() => {
  const out = [];
  for (const e of document.querySelectorAll('*')) {
    const c = getComputedStyle(e);
    if (c.animationName && c.animationName !== 'none') {
      const d = Math.max(...c.animationDuration.split(',').map(x => parseFloat(x) * (x.trim().endsWith('ms') ? 0.001 : 1)));
      if (d > 0.01 && c.animationPlayState !== 'paused') out.push((e.className && String(e.className).split(' ')[0]) || e.tagName.toLowerCase());
    }
  }
  return [...new Set(out)].slice(0, 5);
}"""


def check_keys(page, where, rec, r, stats):
    """Walk the focus with Tab. Report focus that does not show, traps, obscured focus, and order jumps."""
    positive = page.evaluate(TABINDEX_JS)
    if positive:
        rec["issues"].append({"rule": "RV-TABINDEX", "msg": f"{len(positive)} elements with tabindex above 0: {'; '.join(positive[:4])}"})
        r.error("RV-TABINDEX", f"{where}: {len(positive)} elements with a tabindex above 0 get the focus before all other "
                f"elements (WCAG 2.4.3): {'; '.join(positive[:4])}.")
    total = page.evaluate(TABBABLE_JS)
    if not total:
        return
    # Start the walk at the top of the document.
    page.evaluate("""() => { const s = document.createElement('span'); s.id = '__ux_start'; s.tabIndex = -1;
      document.body.prepend(s); s.focus(); window.scrollTo(0, 0); }""")
    seq, stuck, looped = [], 0, False
    for _ in range(min(80, total * 2 + 4)):
        page.keyboard.press("Tab")
        f = page.evaluate(FOCUS_JS)
        if f is None:
            continue
        if seq and f["sel"] == seq[-1]["sel"]:
            stuck += 1
            if stuck >= 2:
                break
            continue
        if any(f["sel"] == x["sel"] for x in seq):
            looped = True
            break
        seq.append(f)
    page.evaluate("() => { const s = document.getElementById('__ux_start'); if (s) s.remove(); }")
    stats["focus_stops"] = stats.get("focus_stops", 0) + len(seq)
    uniq = {f["sel"] for f in seq}
    modal = any(f["modal"] for f in seq)

    def add(rule, msg, level="error"):
        rec["issues"].append({"rule": rule, "msg": msg})
        (r.error if level == "error" else r.warn)(rule, f"{where}: {msg}")
    if not modal and (stuck >= 2 or (looped and len(uniq) < max(2, total * 0.5))):
        add("RV-FOCUS-TRAP", f"Tab reaches {len(uniq)} of {total} focusable elements, then stays in a loop "
            f"({' -> '.join(f['sel'] for f in seq[-3:])}).")
    hidden = [f["sel"] for f in seq if not f["visible"]]
    if hidden:
        add("RV-FOCUS-VISIBLE", f"no visible focus on {len(hidden)} of {len(seq)} focus stops (outline, shadow, border, "
            f"and color do not change): {'; '.join(hidden[:4])}{' ...' if len(hidden) > 4 else ''}.")
    for f in seq:
        if f["obscured"]:
            add("RV-FOCUS-OBSCURED", f"the focus on {f['sel']} is under {f['obscured']} (WCAG 2.4.11).")
    back = [(a, b) for a, b in zip(seq, seq[1:]) if b["top"] < a["top"] - 40 and b["left"] <= a["left"] + 100]
    if len(back) >= 2 or any(a["top"] - b["top"] > 200 for a, b in back):
        a, b = back[0]
        add("RV-FOCUS-ORDER", f"the focus jumps back up from {a['sel']} to {b['sel']} ({len(back)} jumps). "
            "Check that the focus order follows the reading order.", "warn")


def check_motion(page, where, rec, r):
    page.emulate_media(reduced_motion="reduce")
    page.wait_for_timeout(100)
    moving = page.evaluate(MOTION_JS)
    page.emulate_media(reduced_motion="no-preference")
    if moving:
        rec["issues"].append({"rule": "RV-MOTION", "msg": f"animations run with prefers-reduced-motion: {', '.join(moving)}"})
        r.error("RV-MOTION", f"{where}: animations still run when the user asks for reduced motion ({', '.join(moving)}).")


def check_review(walk_file, out, axe_path, primary, r, keys=True):
    try:
        from playwright.sync_api import sync_playwright
    except ImportError:
        r.error("RV-PLAYWRIGHT", "Playwright for Python is missing: pip install playwright.")
        return 3, {}
    walk = json.loads(Path(walk_file).read_text(encoding="utf-8"))
    base = walk.get("base_url", "")
    out = Path(out)
    out.mkdir(parents=True, exist_ok=True)
    axe = find_axe(axe_path)
    if not axe:
        r.warn("RV-AXE", "axe-core not found (npm install axe-core, or --axe <axe.min.js>). The accessibility check is not complete.")
    results, stats = [], {"states": 0, "shots": 0, "tells": 0, "axe": 0, "steps_failed": 0}
    with sync_playwright() as p:
        browser = p.chromium.launch()
        for flow in walk.get("flows", []):
            fid = flow["id"]
            for stt in flow.get("states", []):
                state = stt["state"]
                stats["states"] += 1
                for w, h in WIDTHS:
                    ctx = browser.new_context(viewport={"width": w, "height": h})
                    page = ctx.new_page()
                    console = []
                    page.on("pageerror", lambda e, c=console: c.append(str(e)))
                    held = []
                    add_routes(page, flow.get("routes", []) + stt.get("routes", []), held)
                    fail = run_steps(page, stt.get("steps", []), base)
                    page.wait_for_timeout(stt.get("settle_ms", 300))
                    rec = {"flow": fid, "state": state, "width": w, "issues": []}
                    where = f"{fid}/{state} @{w}"
                    if fail:
                        stats["steps_failed"] += 1
                        r.error("RV-STEP", f"{where}: {fail}")
                        rec["issues"].append({"rule": "RV-STEP", "msg": fail})
                    shot = out / f"{fid}__{re.sub(r'[^a-z0-9-]+', '-', state.lower())}__{w}.png"
                    page.screenshot(path=str(shot), full_page=True)
                    stats["shots"] += 1
                    rec["shot"] = shot.name
                    page.evaluate("([s, f]) => { document.body.dataset.screen = f; document.body.dataset.state = s; }", [state, fid])
                    res = page.evaluate(MOCKUP_JS, [primary, []])
                    for fr in res["frames"][:1]:
                        for i in fr["issues"]:
                            if i["rule"] == "MK-PROPOSED":
                                continue
                            stats["tells"] += 1
                            rec["issues"].append(i)
                            r.error(i["rule"], f"{where}: {i['msg']}")
                    over = page.evaluate("() => document.documentElement.scrollWidth - document.documentElement.clientWidth")
                    if over > 1:
                        rec["issues"].append({"rule": "RV-OVERFLOW", "msg": f"{over}px horizontal scroll"})
                        r.error("RV-OVERFLOW", f"{where}: the page scrolls {over}px to the side.")
                    if axe:
                        page.add_script_tag(content=axe)
                        v = page.evaluate("async () => (await axe.run(document, {runOnly: ['wcag2a', 'wcag2aa', 'wcag21aa', 'wcag22aa']})).violations.map(x => ({id: x.id, impact: x.impact, help: x.help, n: x.nodes.length, target: x.nodes[0] && x.nodes[0].target.join(' ')}))")
                        for x in v:
                            stats["axe"] += 1
                            rec["issues"].append({"rule": f"RV-AXE-{x['id']}", "msg": f"{x['impact']}: {x['help']} ({x['n']} nodes, {x['target']})"})
                            r.error(f"RV-AXE-{x['id']}", f"{where}: {x['impact']}: {x['help']} ({x['n']} nodes, first {x['target']})")
                    if keys and w == WIDTHS[-1][0] and not fail:
                        check_motion(page, where, rec, r)
                        check_keys(page, where, rec, r, stats)
                    for c in console:
                        rec["issues"].append({"rule": "RV-JS", "msg": c[:160]})
                        r.error("RV-JS", f"{where}: script error: {c[:160]}")
                    results.append(rec)
                    for route in held:
                        try:
                            route.abort()
                        except Exception:
                            pass
                    ctx.close()
        browser.close()
    (out / "review.json").write_text(json.dumps({"stats": stats, "results": results}, indent=2), encoding="utf-8")
    return None, stats


NAMED = {"black": "#000000", "white": "#ffffff", "transparent": None}


def parse_color(v):
    """Return (r, g, b) for a hex or rgb() color, None for anything else or a transparent color."""
    v = v.strip().lower()
    v = NAMED.get(v, v) if v in NAMED else v
    if not v:
        return None
    m = re.fullmatch(r"#([0-9a-f]{3}|[0-9a-f]{6})", v)
    if m:
        h = m.group(1)
        if len(h) == 3:
            h = "".join(c * 2 for c in h)
        return tuple(int(h[i:i + 2], 16) for i in (0, 2, 4))
    m = re.fullmatch(r"rgba?\(([^)]*)\)", v)
    if m:
        parts = [p for p in re.split(r"[\s,/]+", m.group(1)) if p]
        if len(parts) >= 4 and float(parts[3].rstrip("%")) < 0.5:
            return None
        return tuple(int(float(x)) for x in parts[:3])
    return None


def contrast(a, b):
    def lum(c):
        out = []
        for x in c:
            x /= 255
            out.append(x / 12.92 if x <= 0.03928 else ((x + 0.055) / 1.055) ** 2.4)
        return 0.2126 * out[0] + 0.7152 * out[1] + 0.0722 * out[2]
    la, lb = lum(a), lum(b)
    return (max(la, lb) + 0.05) / (min(la, lb) + 0.05)


def check_system(files, r):
    css = ""
    for f in files:
        css += "\n" + Path(f).read_text(encoding="utf-8", errors="replace")
    css = re.sub(r"/\*.*?\*/", "", css, flags=re.S)
    tokens = dict(re.findall(r"(--[\w-]+)\s*:\s*([^;}]+)", css))

    def resolve(v, depth=0):
        v = v.strip()
        m = re.fullmatch(r"var\(\s*(--[\w-]+)\s*(?:,\s*([^)]+))?\)", v)
        if m and depth < 5:
            if m.group(1) in tokens:
                return resolve(tokens[m.group(1)], depth + 1)
            return resolve(m.group(2) or "", depth + 1)
        return v

    colors = {k: parse_color(resolve(v)) for k, v in tokens.items()}
    colors = {k: c for k, c in colors.items() if c}
    if not colors:
        r.error("SYS-TOKENS", "No color tokens found.")
    text_like = [k for k in colors if re.search(r"text|ink|fg|foreground|muted", k) and not re.search(r"inverse|on-|soft|bg", k)]
    bg_like = [k for k in colors if re.search(r"(^|-)(bg|background|surface|canvas)", k)]
    for t in text_like:
        for b in bg_like:
            c = contrast(colors[t], colors[b])
            if c < 4.5:
                r.error("SYS-CONTRAST", f"{t} on {b}: {c:.2f}:1 (needs 4.5:1 for text).")
    for name in ("success", "warning", "danger", "error", "info"):
        fg = [k for k in colors if re.search(rf"-{name}$", k)]
        soft = [k for k in colors if re.search(rf"-{name}-(soft|bg|subtle|light)$", k)]
        for f_ in fg:
            for b in soft + bg_like[:1]:
                c = contrast(colors[f_], colors[b])
                if c < 4.5:
                    r.error("SYS-CONTRAST", f"{f_} on {b}: {c:.2f}:1 (status text needs 4.5:1).")
    # Each rule with both a text color and a solid background color.
    for sel, body in re.findall(r"([^{}]+)\{([^{}]*)\}", css):
        decl = dict((k.strip().lower(), v.strip()) for k, v in re.findall(r"([\w-]+)\s*:\s*([^;]+)", body))
        sel = " ".join(sel.split())
        if sel.startswith(":root") or sel.startswith("@"):
            continue
        bg = decl.get("background-color") or decl.get("background")
        fg = decl.get("color")
        if bg and "gradient" in bg:
            r.error("SYS-GRADIENT", f"{sel}: a gradient as a background.")
        if bg and fg:
            cb, cf = parse_color(resolve(bg)), parse_color(resolve(fg))
            if cb and cf and contrast(cf, cb) < 4.5:
                r.error("SYS-RULE-CONTRAST", f"{sel}: text {contrast(cf, cb):.2f}:1 on its background (needs 4.5:1).")
    # Focus: an outline removed without a focus style.
    removes = re.search(r"outline\s*:\s*(none|0)\b", css)
    has_focus = re.search(r":focus(-visible)?[^{]*\{[^}]*(outline\s*:\s*(?!none|0)[^;]+|box-shadow)", css)
    if removes and not has_focus:
        r.error("SYS-FOCUS", "The outline is removed and no :focus or :focus-visible rule shows the focus.")
    elif not has_focus:
        r.warn("SYS-FOCUS", "No :focus-visible rule. The browser default focus is used.")
    sizes = {resolve(v) for k, v in tokens.items() if re.search(r"size|font-?size|text-", k) and re.fullmatch(r"[\d.]+px", resolve(v))}
    if sizes:
        for sel, body in re.findall(r"([^{}]+)\{([^{}]*)\}", css):
            for v in re.findall(r"font-size\s*:\s*([\d.]+px)", body):
                if v not in sizes:
                    r.error("SYS-SCALE", f"{' '.join(sel.split())}: font-size {v} is not a size token.")
        if len(sizes) > 9:
            r.warn("SYS-SCALE", f"{len(sizes)} font sizes in the scale. A scale of 6 to 8 sizes is easier to use.")
    return {"color_tokens": len(colors), "size_tokens": len(sizes)}


def main() -> int:
    argv = sys.argv[1:]
    as_json = "--json" in argv
    no_keys = "--no-keys" in argv
    argv = [a for a in argv if a not in ("--json", "--no-keys")]

    def opt(name):
        if name in argv:
            i = argv.index(name)
            val = argv[i + 1] if i + 1 < len(argv) else None
            del argv[i:i + 2]
            return val
        return None

    r = Report()
    if not argv or argv[0] not in ("design", "mockup", "system", "review"):
        print(__doc__, file=sys.stderr)
        return 2
    cmd = argv.pop(0)
    if cmd == "system":
        if not argv:
            print(__doc__, file=sys.stderr)
            return 2
        extra = check_system(argv, r)
        return r.finish(as_json, extra)
    if cmd == "design":
        patterns, decisions = opt("--patterns"), opt("--decisions")
        if decisions is None and Path("docs/ux/decisions.md").exists():
            decisions = "docs/ux/decisions.md"
        if len(argv) != 1:
            print(__doc__, file=sys.stderr)
            return 2
        extra = check_design(argv[0], patterns, decisions, r)
        return r.finish(as_json, extra)
    if cmd == "review":
        out, axe, primary = opt("--out"), opt("--axe"), opt("--primary") or PRIMARY
        if len(argv) != 1 or not out:
            print(__doc__, file=sys.stderr)
            return 2
        code, extra = check_review(argv[0], out, axe, primary, r, keys=not no_keys)
        res = r.finish(as_json, extra)
        return code if code is not None else res
    design, primary, shots = opt("--design"), opt("--primary") or PRIMARY, opt("--shots")
    if not argv:
        print(__doc__, file=sys.stderr)
        return 2
    code, extra = check_mockups(argv, design, primary, shots, r)
    res = r.finish(as_json, extra)
    return code if code is not None else res


if __name__ == "__main__":
    sys.exit(main())
