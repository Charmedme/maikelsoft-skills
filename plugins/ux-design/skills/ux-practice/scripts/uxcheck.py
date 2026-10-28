#!/usr/bin/env python3
"""Check UX design files and HTML mockups.

Usage:
    python3 uxcheck.py design <ux-design.md> [--patterns <patterns.md>] [--decisions <decisions.md>] [--json]
    python3 uxcheck.py mockup <file.html>... [--design <ux-design.md>] [--primary <css selector>]
                              [--shots <dir>] [--json]
    python3 uxcheck.py system <file.css>... [--json]
    python3 uxcheck.py review <walk.json> --out <dir> [--axe <axe.min.js>] [--primary <css selector>] [--no-keys] [--json]

design  checks the design spec (10 decisions with a source each), the source tags, the
        pattern and decision IDs, the states tables, and the options table. It also checks the
        Mode line (light, full, or new-direction: DS-MODE), the Style direction section and the
        Difference column in new-direction mode (DS-DIRECTION), and in full and new-direction
        mode the Element inventory with one home for each element (DS-INVENTORY) and the
        Non-text contrast table with 3:1 where it is needed (DS-NONTEXT).
system  checks a design system: the contrast of text and background token pairs and of each
        component rule with a color and a background, gradients, the focus style, and font sizes
        outside the size tokens. Non-text contrast: a border, outline, or focus-ring token for a
        control below 3:1 on its background token (SYS-NONTEXT, error), and a surface or raised
        token below 1.5:1 on the background token (SYS-SURFACE, warning).
mockup  renders each HTML mockup with Playwright at 390x844, 820x1180, and 1440x900 and checks
        the tells: off-token colors and font sizes, more than one primary action per frame,
        nested surfaces, gradients, emoji icons, centered body text, controls without a name
        (an emoji or an icon-font ligature is not a name), unlabeled inputs, font weights, and
        text contrast. It also checks horizontal overflow of each frame at each width
        (MK-WIDTH), the 3:1 edge or state indicator of inputs, check boxes, switches, segmented
        controls (the selected segment and a painted track), and icon-only controls
        (MK-NONTEXT), and interactive tiles below 1.5:1 (MK-SURFACE, warning). An interactive
        tile is a control, or the nearest element around a control with its own fill or border.
        A gradient behind a control counts as the average of its color stops. It also checks
        one region for each data-element on all screens (MK-REGION) and a data-region around each data-element (MK-REGION-MISSING, warning), and the icon
        tells TELL-21 to TELL-30: a toggle name that changes or starts with "un", "turn on/off",
        "enable", or "disable" (21), an icon that changes without a state attribute (22), a
        toggle state shown by color only (23), an icon below 3:1 (24), an icon-only primary
        action (25), an icon-only status (26), and the warnings: a name from title only (27), a
        target below 24 x 24 px (28), icon-only navigation or menu button (29), and one icon
        with 2 or more names (30). TELL-21, 22, 23, and 30 compare the frames: a control is the
        same in 2 frames when it has the same id, data-testid, or data-element. With --design
        it also checks that each state has a frame. With --shots it writes one image for each
        frame at each width: <file>__<nn>-<state>__<width>.png.

review  walks the built app with Playwright from a walk file. For each state of each flow, it
        sets the routes, runs the steps, and then at 390, 820, and 1440 px it takes a
        screenshot, runs the tell checks and axe-core (when axe.min.js is found), and checks
        horizontal overflow. At 1440 px it also walks the focus with the Tab key: a focus that
        does not show (RV-FOCUS-VISIBLE), a focus trap (RV-FOCUS-TRAP), a focus under a sticky or
        fixed element (RV-FOCUS-OBSCURED), a focus order that jumps back (RV-FOCUS-ORDER), and
        a tabindex above 0 (RV-TABINDEX).
        With prefers-reduced-motion it finds animations that still run (RV-MOTION). --no-keys
        skips these checks. It writes <out>/review.json and <out>/<flow>__<state>__<width>.png.

A mockup frame is an element with data-screen and data-state attributes. A shared element has
data-element="<inventory name>", and a region is data-region="<region name>" on an ancestor.
Exit code: 0 no errors, 1 errors, 2 bad usage or a missing input file, 3 Playwright is missing.
"""
import itertools
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


MODES = ("light", "full", "new-direction")


def table_header(body):
    for line in body.splitlines():
        line = line.strip()
        if line.startswith("|"):
            return [c.strip().lower() for c in line.strip("|").split("|")]
    return []


def column(header, name, default):
    return next((i for i, h in enumerate(header) if h.startswith(name)), default)


def check_design_040(text, mode, r):
    """The checks of 0.4.0: style direction, element inventory, and non-text contrast."""
    full = mode in ("full", "new-direction")
    if mode == "new-direction":
        sd = sections(text, "Style direction")
        body = next(iter(sd.values()), None)
        if body is None:
            r.error("DS-DIRECTION", "Mode new-direction needs a '## Style direction' section.")
        else:
            n = len(table_rows(body))
            if n < 3 or n > 4:
                r.error("DS-DIRECTION", f"The Style direction table has {n} directions. Use 3 or 4.")
            if not re.search(r"^Chosen:\s*\S", body, re.M):
                r.error("DS-DIRECTION", 'The Style direction section has no "Chosen:" line.')
        opts = next(iter(sections(text, "Options").values()), "")
        head = table_header(opts)
        col = column(head, "difference", None)
        if col is None:
            r.error("DS-DIRECTION", "Mode new-direction needs a Difference column in the Options table.")
        elif not any(len(c) > col and re.match(r"^3\b", c[col]) for c in table_rows(opts)):
            r.error("DS-DIRECTION", "No option has Difference 3. At least one option must break the visual identity.")
    if not full:
        return
    inv = next(iter(sections(text, "Element inventory").values()), None)
    if inv is None:
        r.error("DS-INVENTORY", "No '## Element inventory' section (Element | Home | Component | Screens).")
    else:
        head = table_header(inv)
        hc = column(head, "home", 1)
        homes = {}
        for cells in table_rows(inv):
            if not cells or not cells[0]:
                continue
            home = cells[hc] if len(cells) > hc else ""
            parts = [x for x in re.split(r"\s*(?:,|/|;|\+|&|\bor\b|\band\b)\s*", home.strip("` ")) if x.strip("` ")]
            if not parts:
                r.error("DS-INVENTORY", f"Element '{cells[0]}' has no home.")
            elif len(parts) > 1:
                r.error("DS-INVENTORY", f"Element '{cells[0]}' has more than one home: {home}. Give it one region.")
            homes.setdefault(cells[0].lower().strip("` "), set()).add(home.lower().strip("` "))
        for el, hs in homes.items():
            if len(hs) > 1:
                r.error("DS-INVENTORY", f"Element '{el}' has rows with different homes: {', '.join(sorted(hs))}.")
    nt = next(iter(sections(text, "Non-text contrast").values()), None)
    if nt is None:
        r.error("DS-NONTEXT", "No '## Non-text contrast' table (Part | Color | Next to | Ratio | Needs).")
        return
    head = table_header(nt)
    rc, nc = column(head, "ratio", 3), column(head, "needs", 4)
    for cells in table_rows(nt):
        if len(cells) <= max(rc, nc) or "3:1" not in cells[nc]:
            continue
        m = re.search(r"\d+(?:\.\d+)?", cells[rc])
        if not m:
            r.error("DS-NONTEXT", f"Part '{cells[0]}' needs 3:1 but has no ratio.")
        elif float(m.group()) < 3:
            r.error("DS-NONTEXT", f"Part '{cells[0]}' has {cells[rc]}, below the 3:1 it needs (WCAG 1.4.11).")


def check_design(path, patterns_file, decisions_file, r):
    text = Path(path).read_text(encoding="utf-8")
    m = re.search(r"^Mode:\s*([\w-]+)", text, re.M)
    mode = m.group(1).lower() if m else ""
    if mode not in MODES:
        r.error("DS-MODE", f'No valid "Mode:" line ({", ".join(MODES)}).' + (f" Found: {mode}." if mode else ""))
        mode = "full"
    check_design_040(text, mode, r)
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

    if mode in ("full", "new-direction"):
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
    elif mode in ("full", "new-direction"):
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
  // The average of the opaque color stops of a gradient, or null for an image or a gradient without such stops.
  const gradColor = (img) => {
    if (!img || img === 'none' || /url\(/.test(img)) return null;
    const stops = (img.match(/rgba?\([^)]*\)/g) || []).filter(c => alpha(c) > 0.5).map(c => c.match(/[\d.]+/g).slice(0, 3).map(Number));
    if (!stops.length) return null;
    const avg = [0, 1, 2].map(i => Math.round(stops.reduce((t, c) => t + c[i], 0) / stops.length));
    return `rgb(${avg.join(', ')})`;
  };
  // The background color behind el. With approx, a gradient gives the average of its stops (for edges and
  // surfaces). Without approx, a gradient or an image gives null, and the text-contrast check skips the element.
  const bgOf = (el, approx) => { for (let e = el; e; e = e.parentElement) { const cs = getComputedStyle(e); if (/gradient|url\(/.test(cs.backgroundImage)) return approx ? gradColor(cs.backgroundImage) : null; const b = cs.backgroundColor; if (alpha(b) > 0.5) return b; } return 'rgb(255, 255, 255)'; };
  const ratio = (a, b) => { const x = lum(a), y = lum(b); return (Math.max(x, y) + 0.05) / (Math.min(x, y) + 0.05); };
  const visible = (el) => { const r = el.getBoundingClientRect(); const cs = getComputedStyle(el); return r.width > 1 && r.height > 1 && cs.visibility !== 'hidden' && cs.display !== 'none' && Number(cs.opacity) > 0 && !el.closest('[aria-hidden="true"]'); };
  const ownText = (el) => [...el.childNodes].filter(n => n.nodeType === 3).map(n => n.textContent).join('').trim();
  const emoji = /\p{Extended_Pictographic}/u;
  // 0.4.0: names, icons, non-text contrast, regions, and width.
  const CONTROLS = 'button, [role=button], a[href], [role=switch], [role=tab], [role=menuitem], input[type=button], input[type=submit]';
  const TARGETS = CONTROLS + ', input:not([type=hidden]), select, textarea, summary, [role=checkbox], [role=radio], [role=option]';
  const ICON = 'svg, img, i, [class*="icon"], [data-icon], .material-icons, [class*="material-symbols"]';
  const shown = (e) => { const r = e.getBoundingClientRect(); const cs = getComputedStyle(e); return r.width > 0 && r.height > 0 && cs.visibility !== 'hidden' && cs.display !== 'none'; };
  const pict = /[\p{Extended_Pictographic}️‍⃣]/gu;
  const clean = (s) => (s || '').replace(pict, '').replace(/[-]/g, '').replace(/\s+/g, ' ').trim();
  const lig = (e) => !!e.closest('.material-icons, [class*="material-symbols"], [class*="material-icons"]') || /material (icons|symbols)/i.test(getComputedStyle(e).fontFamily);
  const textOf = (el, keep) => { let s = ''; const w = document.createTreeWalker(el, NodeFilter.SHOW_TEXT); let n; while ((n = w.nextNode())) { const p = n.parentElement; if (!p || p.closest('svg, style, script') || lig(p) || !keep(p)) continue; s += ' ' + n.textContent; } return clean(s); };
  const visText = (el) => el.tagName === 'INPUT' ? clean(el.value) : textOf(el, (p) => { const r = p.getBoundingClientRect(); const cs = getComputedStyle(p); return r.width > 1 && r.height > 1 && cs.visibility !== 'hidden'; });
  const accName = (el) => {
    const lb = el.getAttribute('aria-labelledby');
    if (lb) { const t = clean(lb.split(/\s+/).map(id => { const x = document.getElementById(id); return x ? x.textContent : ''; }).join(' ')); if (t) return [t, 'labelledby']; }
    const al = clean(el.getAttribute('aria-label')); if (al) return [al, 'label'];
    if (el.tagName === 'INPUT') { const v = clean(el.value) || (el.type === 'submit' ? 'Submit' : ''); if (v) return [v, 'text']; }
    const t = textOf(el, (p) => { const ah = p.closest('[aria-hidden="true"]'); if (ah && el.contains(ah)) return false; const cs = getComputedStyle(p); return cs.display !== 'none' && cs.visibility !== 'hidden'; });
    if (t) return [t, 'text'];
    for (const s of el.querySelectorAll('svg')) { if (s.closest('[aria-hidden="true"]')) continue; const a = clean(s.getAttribute('aria-label')); if (a) return [a, 'label']; const tt = s.querySelector('title'); if (tt && clean(tt.textContent)) return [clean(tt.textContent), 'svg-title']; }
    for (const i of el.querySelectorAll('img')) { const a = clean(i.getAttribute('alt')); if (a) return [a, 'alt']; }
    const ti = clean(el.getAttribute('title')); if (ti) return [ti, 'title'];
    return ['', ''];
  };
  const iconOf = (el) => [...el.querySelectorAll(ICON)].find(e => shown(e) && (e.tagName.toLowerCase() === 'svg' || !e.closest('svg'))) || null;
  const iconKey = (el, ic) => {
    if (!ic) return '';
    const di = [ic, ...ic.querySelectorAll('[data-icon]')].find(x => x.getAttribute('data-icon')) || (el.getAttribute('data-icon') ? el : null);
    if (di) return 'data-icon:' + di.getAttribute('data-icon');
    const svg = ic.tagName.toLowerCase() === 'svg' ? ic : ic.querySelector('svg');
    if (svg) { const d = [...svg.querySelectorAll('path')].map(x => x.getAttribute('d') || '').join('|'); const u = [...svg.querySelectorAll('use')].map(x => x.getAttribute('href') || x.getAttribute('xlink:href') || '').join('|'); return 'svg:' + (d || u || markup(svg)); }
    if (ic.tagName === 'IMG') return 'img:' + (ic.getAttribute('src') || '');
    return 'font:' + [...ic.classList].filter(c => !/^(icon|is-|active|on|off)$/.test(c)).sort().join('.') + ':' + ic.textContent.trim();
  };
  const markup = (ic) => { const c = ic.cloneNode(true); for (const n of [c, ...c.querySelectorAll('*')]) for (const a of ['style', 'class', 'fill', 'stroke', 'color']) n.removeAttribute(a); return c.outerHTML.replace(/\s+/g, ' '); };
  const iconColor = (ic) => {
    const svg = ic.tagName.toLowerCase() === 'svg' ? ic : ic.querySelector('svg');
    if (svg) { for (const s of svg.querySelectorAll('path, circle, rect, line, polyline, polygon, ellipse, use, text')) { const cs = getComputedStyle(s); if (cs.fill && cs.fill !== 'none' && !/url\(/.test(cs.fill) && alpha(cs.fill) > 0 && cs.fillOpacity !== '0') return cs.fill; if (cs.stroke && cs.stroke !== 'none' && alpha(cs.stroke) > 0) return cs.stroke; } return null; }
    if (ic.tagName === 'IMG') return null;
    return getComputedStyle(ic).color;
  };
  const rings = (cs) => { const out = []; if (cs.boxShadow && cs.boxShadow !== 'none') for (const m of cs.boxShadow.matchAll(/(rgba?\([^)]*\))\s+(-?[\d.]+)px\s+(-?[\d.]+)px\s+([\d.]+)px\s+([\d.]+)px/g)) if (Number(m[4]) === 0 && Number(m[5]) >= 1) out.push(m[1]); return out; };
  // The fill, borders, outline, and box-shadow ring of el against the color next to it (outside).
  const edge = (el) => {
    const cs = getComputedStyle(el);
    const adj = el.parentElement ? bgOf(el.parentElement, true) : 'rgb(255, 255, 255)';
    if (!adj || /url\(/.test(cs.backgroundImage)) return null;
    const fill = /gradient/.test(cs.backgroundImage) ? gradColor(cs.backgroundImage) : alpha(cs.backgroundColor) > 0.5 ? cs.backgroundColor : null;
    const cands = fill ? [fill] : [];
    for (const s of ['Top', 'Right', 'Bottom', 'Left']) if (parseFloat(cs['border' + s + 'Width']) >= 1 && cs['border' + s + 'Style'] !== 'none' && alpha(cs['border' + s + 'Color']) > 0.5) cands.push(cs['border' + s + 'Color']);
    if (cs.outlineStyle !== 'none' && parseFloat(cs.outlineWidth) >= 1 && alpha(cs.outlineColor) > 0.5) cands.push(cs.outlineColor);
    cands.push(...rings(cs));
    return { adj, fill, best: cands.reduce((m, c) => Math.max(m, ratio(c, adj)), 1) };
  };
  // True when el paints its own fill or a visible border.
  const bordered = (el) => { const cs = getComputedStyle(el); return ['Top', 'Right', 'Bottom', 'Left'].some(s => parseFloat(cs['border' + s + 'Width']) >= 1 && cs['border' + s + 'Style'] !== 'none' && alpha(cs['border' + s + 'Color']) > 0.5); };
  const painted = (el) => { const cs = getComputedStyle(el); return alpha(cs.backgroundColor) > 0.5 || /gradient/.test(cs.backgroundImage) || bordered(el); };
  const LANDMARK = 'body, main, header, footer, nav, aside, form, dialog, [role=dialog], [role=main], [role=banner], [role=contentinfo], [role=navigation], [data-region]';
  const desc = (el) => { const c = el.getAttribute('class'); return `<${el.tagName.toLowerCase()}${el.id ? '#' + el.id : ''}${c ? ' class="' + c + '"' : ''}>`; };
  const inactive = (el) => el.disabled || el.getAttribute('aria-disabled') === 'true';
  const TEXTIN = /^(text|search|email|url|tel|password|number|date|datetime-local|month|week|time)$/;
  const SEGMENT = '[role=radiogroup], [role=tablist], [role=group], [class*="segment"], [class*="toggle-group"]';
  const STATUS_CLASS = /(^|[\s_-])(status|error|warning|success|alert)([\s_-]|$)/i;
  const TILE_CLASS = /(^|[\s_-])(chip|tile|row)s?([\s_-]|$)/i;
  const INTERACTIVE = CONTROLS + ', [tabindex]:not([tabindex="-1"]), [onclick], label, [role=option], [role=row], [role=gridcell]';
  const frames = [...document.querySelectorAll('[data-screen][data-state]')];
  for (const fr of frames) {
    const f = { screen: fr.dataset.screen, state: fr.dataset.state, issues: [] };
    const add = (rule, msg, level) => f.issues.push({ rule, msg, level: level || 'error' });
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
      if (['INPUT', 'SELECT', 'TEXTAREA'].includes(el.tagName) && !['hidden', 'submit', 'button', 'checkbox', 'radio'].includes(el.type) && !(el.labels && el.labels.length) && !el.getAttribute('aria-label') && !el.getAttribute('aria-labelledby')) add('TELL-09', `Input without a label: name="${el.name || ''}" placeholder="${el.placeholder || ''}"`);
    }
    f.controls = []; f.elements = [];
    for (const el of els.filter(e => e.matches(CONTROLS))) {
      const cs = getComputedStyle(el);
      const vt = visText(el), ic = iconOf(el);
      const raw = el.tagName === 'INPUT' ? (el.value || '') : el.textContent;
      const iconOnly = !vt && (!!ic || raw.trim() !== '');
      const [name, src] = accName(el);
      if (!name) add('TELL-08', `Control without an accessible name (an emoji or an icon-font ligature is not a name): ${desc(el)}`);
      if (iconOnly) {
        if (el.matches(primarySel)) add('TELL-25', `Icon-only primary action "${name}": give it a text label: ${desc(el)}`);
        if (src === 'title') add('TELL-27', `Icon-only control "${name}" gets its name from title only. A title does not show on touch or keyboard focus: ${desc(el)}`, 'warning');
        if (name && /^(un|turn (on|off)|enable|disable)/i.test(name) && !/^undo\b/i.test(name)) add('TELL-21', `Toggle name "${name}" says the next action. Use a fixed name and aria-pressed: ${desc(el)}`);
        if (el.closest('nav, [role=navigation], [role=tablist], [role=menubar]')) add('TELL-29', `Icon-only navigation item "${name}": give it a visible label: ${desc(el)}`, 'warning');
        else if ((el.hasAttribute('aria-controls') || el.hasAttribute('aria-expanded') || el.hasAttribute('aria-haspopup')) && /menu|navigat/i.test(name)) add('TELL-29', `Icon-only menu button "${name}": show the word "Menu": ${desc(el)}`, 'warning');
        if (!inactive(el)) {
          const col = ic && iconColor(ic), bg = bgOf(el);
          const ir = col && bg ? ratio(col, bg) : 99;
          if (ir < 3) add('TELL-24', `Icon ${ir.toFixed(2)}:1 against ${bg} (needs 3:1) in "${name}": ${desc(el)}`);
          const e = edge(el), bgA = bgOf(el, true), irA = col && bgA ? ratio(col, bgA) : 99;
          if (e && Math.max(e.best, irA === 99 ? 1 : irA) < 3) add('MK-NONTEXT', `Icon-only control "${name}": icon and edge below 3:1 against ${e.adj}: ${desc(el)}`);
        }
      }
      const key = el.id || el.getAttribute('data-testid') || el.getAttribute('data-element') || '';
      f.controls.push({ key, name, src, iconOnly, pressed: el.getAttribute('aria-pressed'), checked: el.getAttribute('aria-checked'), role: el.getAttribute('role') || '', text: vt,
        iconKey: iconKey(el, ic), markup: ic ? markup(ic) : '', color: ic ? iconColor(ic) : cs.color, bg: cs.backgroundColor, bgOn: alpha(cs.backgroundColor) > 0,
        border: cs.borderTopColor, borderW: cs.borderTopWidth, desc: desc(el) });
    }
    // Interactive tiles: for each control, the nearest element (the control or an ancestor) with its own fill
    // or border. Not the frame, a landmark, a region, or a container: an element that holds a region, a
    // heading, or more than 3 controls (a stage, a panel, or a device wrapper). Class names do not matter.
    const tiles = new Set();
    for (const c of els.filter(e => e !== fr && e.matches(INTERACTIVE))) {
      for (let a = c; a && a !== fr && fr.contains(a); a = a.parentElement) {
        if (a.matches(LANDMARK) || a.querySelector('[data-region], h1, h2, h3, h4, h5, h6') || a.querySelectorAll(TARGETS).length > 3) break;
        if (!painted(a)) continue;
        if (!a.matches(SEGMENT)) tiles.add(a);
        break;
      }
    }
    const tracks = new Set();
    for (const el of els) {
      if (el === fr) continue;
      const cs = getComputedStyle(el), role = el.getAttribute('role') || '', cls = el.getAttribute('class') || '', tag = el.tagName;
      if ((/^(status|alert)$/.test(role) || (STATUS_CLASS.test(cls) && !/icon|glyph/i.test(cls))) && !el.matches(CONTROLS) && !el.matches(ICON) && !el.closest('svg')) {
        if ((iconOf(el) || el.textContent.trim()) && !visText(el)) add('TELL-26', `Status shown by an icon without visible text: ${desc(el)}`);
      }
      if (inactive(el)) continue;
      let kind = '';
      if ((tag === 'INPUT' && TEXTIN.test(el.type)) || tag === 'SELECT' || tag === 'TEXTAREA') kind = 'input';
      else if (tag === 'INPUT' && /^(checkbox|radio)$/.test(el.type) && cs.appearance === 'none') kind = 'check';
      else if (/^(switch|checkbox|radio)$/.test(role) && tag !== 'INPUT' && !visText(el)) kind = 'check';
      else if ((el.getAttribute('aria-pressed') || el.getAttribute('aria-checked') || el.getAttribute('aria-selected')) === 'true' && el.parentElement && el.parentElement.matches(SEGMENT)) kind = 'segment';
      if (kind) {
        const e = edge(el);
        if (e) {
          let best = e.best;
          if (kind === 'check') for (const d of el.querySelectorAll('*')) { if (!shown(d)) continue; const x = edge(d); if (x) best = Math.max(best, x.best); }
          if (best < 3) add('MK-NONTEXT', `${kind === 'segment' ? 'Selected state' : 'Edge'} ${best.toFixed(2)}:1 against ${e.adj} (needs 3:1, WCAG 1.4.11): ${desc(el)}`);
        }
        // The track of a segmented control: when it has its own fill or border, it is the edge of the control.
        const tr = el.parentElement;
        if (kind === 'segment' && !tracks.has(tr) && painted(tr)) {
          tracks.add(tr);
          const x = edge(tr);
          if (x && (x.fill !== x.adj || bordered(tr)) && x.best < 3) add('MK-NONTEXT', `Track of a segmented control ${x.best.toFixed(2)}:1 against ${x.adj} (needs 3:1, WCAG 1.4.11): ${desc(tr)}`);
        }
      } else if (tiles.has(el) || ((el.matches(INTERACTIVE) || TILE_CLASS.test(cls)) && !el.matches(SEGMENT))) {
        const e = edge(el);
        if (e && (e.fill || painted(el)) && e.fill !== e.adj && e.best < 1.5) add('MK-SURFACE', `Fill and edge ${e.best.toFixed(2)}:1 against ${e.adj} (a raised surface needs 1.5:1): ${desc(el)}`, 'warning');
      }
    }
    const tg = els.filter(e => e.matches(TARGETS) && !(e.tagName === 'A' && getComputedStyle(e).display === 'inline')).map(e => ({ e, r: e.getBoundingClientRect() }));
    const small = (t) => t.r.width < 24 || t.r.height < 24;
    const ctr = (t) => [t.r.left + t.r.width / 2, t.r.top + t.r.height / 2];
    const toRect = (c, rc) => Math.hypot(Math.max(rc.left - c[0], 0, c[0] - rc.right), Math.max(rc.top - c[1], 0, c[1] - rc.bottom));
    for (const t of tg) {
      if (!small(t)) continue;
      const c = ctr(t);
      const hit = tg.find(o => o !== t && !o.e.contains(t.e) && !t.e.contains(o.e) && (small(o) ? Math.hypot(c[0] - ctr(o)[0], c[1] - ctr(o)[1]) < 24 : toRect(c, o.r) < 12));
      if (hit) add('TELL-28', `Target ${Math.round(t.r.width)} x ${Math.round(t.r.height)} px, below 24 x 24 px and too close to ${desc(hit.e)} (WCAG 2.5.8): ${desc(t.e)}`, 'warning');
    }
    for (const e of fr.querySelectorAll('[data-element]')) {
      if (!visible(e)) continue;
      const reg = e.closest('[data-region]');
      if (!reg) add('MK-REGION-MISSING', `data-element "${e.dataset.element}" has no data-region around it`, 'warning');
      else f.elements.push({ element: e.dataset.element, region: reg.dataset.region });
    }
    const fb = fr.getBoundingClientRect(), vw = document.documentElement.clientWidth;
    const over = Math.round(Math.max(fr.scrollWidth - fr.clientWidth, fb.right - vw, -fb.left));
    if (over > 1) add('MK-WIDTH', `${innerWidth} px: the frame overflows ${over} px to the side`);
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


def same_look(a, b):
    """True when 2 states of a toggle have the same icon, text, fill shape, and border width."""
    return a["markup"] and a["markup"] == b["markup"] and a["text"] == b["text"] \
        and a["bgOn"] == b["bgOn"] and a["borderW"] == b["borderW"]


def cross_frame(frames, r):
    """The rules that compare frames: TELL-21, 22, 23, 30, and MK-REGION.

    frames: dicts with file, width, screen, state, controls, and elements, as MOCKUP_JS returns them.
    A control is the same in 2 frames of one screen when it has the same key (id, data-testid,
    or data-element). Each finding is reported once.
    """
    done = set()

    def report(level, rule, what, msg):
        if (rule, what) in done:
            return
        done.add((rule, what))
        (r.error if level == "error" else r.warn)(rule, msg)
    groups = {}
    for fr in frames:
        for c in fr.get("controls", []):
            if c.get("key"):
                groups.setdefault((fr["width"], fr["screen"], c["key"]), []).append((fr, c))
    for (_, screen, key), items in groups.items():
        for (fa, a), (fb, b) in itertools.combinations(items, 2):
            if fa is fb:
                continue
            where = f'{fa["file"]} [{screen}: {fa["state"]} / {fb["state"]}] {a["desc"]}'
            toggle = {a["pressed"], b["pressed"]} == {"true", "false"}
            if toggle and (a["iconOnly"] or b["iconOnly"]) and a["name"] != b["name"]:
                report("error", "TELL-21", (screen, key), f'{where}: the toggle name changes with the state '
                       f'("{a["name"]}" / "{b["name"]}"). Keep one name and use aria-pressed (PAT-TOGGLE-02).')
            stateful = any(x["pressed"] in ("true", "false", "mixed") or x["checked"] in ("true", "false", "mixed")
                           or x["role"] == "switch" for x in (a, b))
            if a["iconKey"] and b["iconKey"] and a["iconKey"] != b["iconKey"] and not stateful:
                report("error", "TELL-22", (screen, key), f'{where}: the icon changes between the frames, but the control '
                       "has no aria-pressed, aria-checked, or role=switch. The state is not exposed (PAT-TOGGLE-02).")
            if toggle and same_look(a, b):
                diff = (a["color"], a["bg"], a["border"]) != (b["color"], b["bg"], b["border"])
                report("error", "TELL-23", (screen, key), f'{where}: the 2 states of the toggle ' +
                       ("differ only in color." if diff else "look the same.") +
                       " Show the state by shape: a slashed or filled icon, or a fill that shows and hides (PAT-TOGGLE-02).")
    names = {}
    for fr in frames:
        for c in fr.get("controls", []):
            if c.get("iconOnly") and c.get("iconKey") and c.get("name"):
                names.setdefault(c["iconKey"], {}).setdefault(c["name"].lower(), (c["name"], fr["file"], fr["screen"]))
    for k, got in names.items():
        if len(got) > 1:
            listed = "; ".join(f'"{n}" ({f} {s})' for n, f, s in got.values())
            report("warning", "TELL-30", k, f"One icon has {len(got)} names: {listed}. One icon has one meaning (PAT-ICON-09).")
    for w in sorted({fr["width"] for fr in frames}):
        homes = {}
        for fr in frames:
            if fr["width"] != w:
                continue
            for e in fr.get("elements", []):
                homes.setdefault(e["element"], {}).setdefault(e["region"], set()).add(fr["screen"])
        for el, regions in homes.items():
            screens = set().union(*regions.values())
            if len(regions) > 1 and len(screens) > 1:
                listed = "; ".join(f'{reg} ({", ".join(sorted(sc))})' for reg, sc in sorted(regions.items()))
                report("error", "MK-REGION", el, f'data-element "{el}" is in {len(regions)} regions at {w} px: '
                       f"{listed}. Give each element one home.")


def check_mockups(files, design, primary, shots, r):
    try:
        from playwright.sync_api import sync_playwright
    except ImportError:
        r.error("MK-PLAYWRIGHT", "Playwright for Python is missing: pip install playwright.")
        return 3, {}
    found = {}
    stats = {"frames": 0, "tells": 0, "widths": [w for w, _ in WIDTHS]}
    collected, seen = [], {}
    with sync_playwright() as p:
        browser = p.chromium.launch()
        for w, h in WIDTHS:
            first = w == WIDTHS[0][0]
            page = browser.new_page(viewport={"width": w, "height": h})
            for f in files:
                page.goto(Path(f).resolve().as_uri())
                page.wait_for_timeout(300)
                res = page.evaluate(MOCKUP_JS, [primary, css_props(f)])
                if first and res["tokens"]["colors"] <= 1:
                    r.warn("MK-TOKENS", f"{f}: no color tokens found (CSS custom properties). The color check is weak.")
                if first and not res["frames"]:
                    r.error("MK-FRAME", f"{f}: no frame (an element with data-screen and data-state).")
                file_seen = seen.setdefault(f, set())  # count each issue once per file, not once per frame or width
                for fr in res["frames"]:
                    if first:
                        stats["frames"] += 1
                        found.setdefault(fr["screen"], set()).add(fr["state"])
                    collected.append({"file": Path(f).name, "width": w, "screen": fr["screen"], "state": fr["state"],
                                      "controls": fr.get("controls", []), "elements": fr.get("elements", [])})
                    for i in fr["issues"]:
                        key = (i["rule"], i["msg"])
                        if key in file_seen:
                            continue
                        file_seen.add(key)
                        stats["tells"] += 1
                        (r.warn if i.get("level") == "warning" else r.error)(
                            i["rule"], f'{Path(f).name} [{fr["screen"]}/{fr["state"]}]: {i["msg"]}')
                if shots:
                    # One image per frame and width, so a reviewer sees each state at full size.
                    Path(shots).mkdir(parents=True, exist_ok=True)
                    for i, fr in enumerate(res["frames"]):
                        loc = page.locator(f'[data-screen="{fr["screen"]}"][data-state="{fr["state"]}"]').first
                        state = re.sub(r"[^a-z0-9-]+", "-", fr["state"].lower())
                        try:
                            loc.screenshot(path=str(Path(shots) / f"{Path(f).stem}__{i + 1:02d}-{state}__{w}.png"))
                        except Exception:  # a frame with no box (hidden) gets no image
                            pass
            page.close()
        browser.close()
    cross_frame(collected, r)
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
                            if i["rule"] in ("MK-PROPOSED", "MK-WIDTH", "MK-REGION-MISSING"):
                                continue  # RV-OVERFLOW checks the width of the page
                            stats["tells"] += 1
                            rec["issues"].append(i)
                            (r.warn if i.get("level") == "warning" else r.error)(i["rule"], f"{where}: {i['msg']}")
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


EDGE_TOKEN = re.compile(r"border|outline|focus|ring|stroke")
DECOR_TOKEN = re.compile(r"divider|separator|hairline|subtle|decor|shadow")
FOCUS_TOKEN = re.compile(r"focus|ring|outline")
# A selector of a control whose edge identifies it (WCAG 1.4.11). Buttons and chips with a
# visible text label do not need an edge, so they are not here.
CONTROL_SEL = re.compile(r"input|select|textarea|check|radio|switch|toggle|segment|field|control|:focus")
MODIFIERS = {"color", "colour", "clr", "border", "outline", "focus", "ring", "stroke", "strong", "default",
             "base", "weak", "hover", "active", "visible", "width"}


def token_words(name):
    """The words of a token name after the color prefix: --fl-color-card-border -> [card, border]."""
    words = name.strip("-").split("-")
    for i, w in enumerate(words):
        if w in ("color", "colour", "clr"):
            return words[i + 1:]
    return words


def check_nontext(css, colors, r):
    """SYS-NONTEXT and SYS-SURFACE: non-text contrast of edge and surface tokens (WCAG 1.4.11)."""
    used = {}
    for sel, body in re.findall(r"([^{}]+)\{([^{}]*)\}", css):
        sel = " ".join(sel.split())
        if sel.startswith(":root") or sel.startswith("@"):
            continue
        for prop, val in re.findall(r"([\w-]+)\s*:\s*([^;]+)", body):
            if re.match(r"(border|outline|box-shadow)", prop.strip().lower()):
                for t in re.findall(r"var\(\s*(--[\w-]+)", val):
                    used.setdefault(t, []).append(sel)
    tokens_only = not any(EDGE_TOKEN.search(t) for t in used)
    bg_like = [k for k in colors if re.search(r"(^|-)(bg|background|surface|canvas)", k)
               and not re.search(r"inverse|overlay|scrim", k)]
    for k in colors:
        if not EDGE_TOKEN.search(k) or DECOR_TOKEN.search(k):
            continue
        if not FOCUS_TOKEN.search(k) and not tokens_only:
            if not any(CONTROL_SEL.search(sel) for sel in used.get(k, [])):
                continue  # used only on decorative parts (a card, a table), or not used
        quals = [w for w in token_words(k) if w not in MODIFIERS and not w.isdigit()]
        targets = []
        if quals:
            targets = [b for b in colors if b != k and not EDGE_TOKEN.search(b)
                       and not re.search(r"text|ink|fg|on-|muted", b) and any(q in token_words(b) for q in quals)]
        for b in targets or bg_like:
            c = contrast(colors[k], colors[b])
            if c < 3:
                r.error("SYS-NONTEXT", f"{k} on {b}: {c:.2f}:1 (a control edge or focus ring needs 3:1, WCAG 1.4.11).")
    stage = [k for k in colors if re.search(r"(^|-)(bg|background|canvas|page|stage)$", k)]
    raised = [k for k in colors if re.search(r"surface|raised|elevated|tile|panel|card", k)
              and not re.search(r"text|ink|fg|on-|border|shadow|inverse|sunken|inset|well", k)]
    for s_ in raised:
        for b in stage:
            if colors[s_] == colors[b]:
                continue
            c = contrast(colors[s_], colors[b])
            if c < 1.5:
                r.warn("SYS-SURFACE", f"{s_} on {b}: {c:.2f}:1. A raised surface below 1.5:1 does not show; "
                       "give an interactive tile on it a 3:1 edge.")


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
    check_nontext(css, colors, r)
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

    def missing(*paths):
        # A missing input file is a usage error: say which file, and exit 2 without a traceback.
        gone = [p for p in paths if p and not Path(p).is_file()]
        for p in gone:
            print(f"uxcheck: file not found: {p}", file=sys.stderr)
        return bool(gone)

    r = Report()
    if not argv or argv[0] not in ("design", "mockup", "system", "review"):
        print(__doc__, file=sys.stderr)
        return 2
    cmd = argv.pop(0)
    if cmd == "system":
        if not argv:
            print(__doc__, file=sys.stderr)
            return 2
        if missing(*argv):
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
        if missing(argv[0], patterns, decisions):
            return 2
        extra = check_design(argv[0], patterns, decisions, r)
        return r.finish(as_json, extra)
    if cmd == "review":
        out, axe, primary = opt("--out"), opt("--axe"), opt("--primary") or PRIMARY
        if len(argv) != 1 or not out:
            print(__doc__, file=sys.stderr)
            return 2
        if missing(argv[0]):
            return 2
        code, extra = check_review(argv[0], out, axe, primary, r, keys=not no_keys)
        res = r.finish(as_json, extra)
        return code if code is not None else res
    design, primary, shots = opt("--design"), opt("--primary") or PRIMARY, opt("--shots")
    if not argv:
        print(__doc__, file=sys.stderr)
        return 2
    if missing(*argv, design):
        return 2
    code, extra = check_mockups(argv, design, primary, shots, r)
    res = r.finish(as_json, extra)
    return code if code is not None else res


if __name__ == "__main__":
    sys.exit(main())
