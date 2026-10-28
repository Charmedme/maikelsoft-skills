#!/usr/bin/env python3
"""Score the `check` assertions of the ux-design 0.4.0 cases (run4.py), and take the screenshots for the grader.

    python3 evals/ux-design/score4.py <runs>

party-new-direction (files in repo/docs/work/ndk-12-new-direction/):
  C1 mode       ux-design.md has the line `Mode: new-direction`.
  C2 style      a "Style direction" section with a table of 3 or 4 rows.
  C3 stop       the run stops before the concepts and the states: the reply ends with NEEDS_HUMAN, no
                "Selected:" line, and no mockup frame with a state other than default (direction files excepted).

party-review-faults (mockups in repo/docs/work/ndk-14-lobby-stand/mockups/): a Playwright probe of each
frame at 390 px. "avoided" means that the produced mockups do not have the planted fault.
  F1a tiles     the player tile (the box around "Bram" in lobby/default) has a fill or a border of 1.5:1 or more.
  F1b segment   the doelscore control (the group around the button "7") has a track fill or border of 3:1 or more.
  F2 input      the name input has a border or a fill of 3:1 or more against the color behind it.
  F3 region     music toggle and leave control: the same region (data-region, else the top, middle,
                or bottom band of the frame) on lobby/default and stand/default.
  F4 icons      music, chat, and microphone controls: each has a state (aria-pressed, aria-checked,
                a checkbox, or role=switch) and an icon (svg, img, or an element with an icon class).
  F5 name       the game sounds toggle on the stand has a state and the same name in each frame.
  P1 stop       the reply ends with NEEDS_HUMAN and each frame is a default frame (stop after pass 1).

Writes scores.json in each case folder, and shots/ in each run folder.
"""
import argparse
import json
import re
from pathlib import Path

from playwright.sync_api import sync_playwright

SLUG = {"party-new-direction": "ndk-12-new-direction", "party-review-faults": "ndk-14-lobby-stand"}

PROBE = r"""
(frame) => {
  const parse = (c) => { const m = c.match(/rgba?\(([^)]+)\)/); if (!m) return null;
    const p = m[1].split(/[ ,\/]+/).filter(Boolean).map(Number); return [p[0], p[1], p[2], p.length > 3 ? p[3] : 1]; };
  const blend = (top, under) => { const a = top[3]; return [0,1,2].map(i => top[i]*a + under[i]*(1-a)).concat([1]); };
  const behind = (el) => { const chain = []; let e = el; while (e) { chain.push(e); e = e.parentElement; }
    let col = [255,255,255,1]; for (let i = chain.length - 1; i >= 0; i--) { const c = parse(getComputedStyle(chain[i]).backgroundColor);
      if (c && c[3] > 0) col = blend(c, col); } return col; };
  const lum = (c) => { const f = (v) => { v /= 255; return v <= 0.04045 ? v/12.92 : Math.pow((v+0.055)/1.055, 2.4); };
    return 0.2126*f(c[0]) + 0.7152*f(c[1]) + 0.0722*f(c[2]); };
  const ratio = (a, b) => { const x = lum(a), y = lum(b); return (Math.max(x,y)+0.05)/(Math.min(x,y)+0.05); };
  const edge = (el) => { // best of fill and border against the color behind the element
    const under = el.parentElement ? behind(el.parentElement) : [255,255,255,1];
    const s = getComputedStyle(el); let best = 1;
    const bg = parse(s.backgroundColor); if (bg && bg[3] > 0) best = Math.max(best, ratio(blend(bg, under), under));
    for (const side of ['Top','Right','Bottom','Left']) { const w = parseFloat(s['border'+side+'Width']);
      const c = parse(s['border'+side+'Color']); if (w >= 1 && s['border'+side+'Style'] !== 'none' && c && c[3] > 0)
        best = Math.max(best, ratio(blend(c, under), under)); }
    const ow = parseFloat(s.outlineWidth); const oc = parse(s.outlineColor);
    if (ow >= 1 && s.outlineStyle !== 'none' && oc && oc[3] > 0) best = Math.max(best, ratio(blend(oc, under), under));
    const sh = s.boxShadow; const c = sh && sh !== 'none' ? parse(sh) : null; // a ring: box-shadow 0 0 0 <spread>
    if (c && c[3] > 0 && /0px 0px 0px [1-9]/.test(sh)) best = Math.max(best, ratio(blend(c, under), under));
    return Math.round(best * 100) / 100; };
  const visible = (el) => { const r = el.getBoundingClientRect(); const s = getComputedStyle(el);
    return r.width > 0 && r.height > 0 && s.visibility !== 'hidden' && s.display !== 'none'; };
  const name = (el) => (el.getAttribute('aria-label') || (el.getAttribute('aria-labelledby') ?
    el.getAttribute('aria-labelledby').split(/\s+/).map(id => (document.getElementById(id)||{}).textContent||'').join(' ') : '')
    || el.innerText || el.getAttribute('title') || '').trim().replace(/\s+/g, ' ');
  const fr = frame.getBoundingClientRect();
  const controls = [...frame.querySelectorAll('button, [role=button], [role=switch], [role=checkbox], input[type=checkbox], a[href], label')]
    .filter(visible).map(el => {
      const r = el.getBoundingClientRect(); const reg = el.closest('[data-region]');
      const rel = (r.top + r.height/2 - fr.top) / Math.max(fr.height, 1);
      let stateful = el.hasAttribute('aria-pressed') || el.hasAttribute('aria-checked') || el.getAttribute('role') === 'switch'
        || (el.tagName === 'INPUT' && el.type === 'checkbox') || !!el.querySelector('input[type=checkbox]');
      if (el.tagName === 'LABEL') { const inp = el.control || el.querySelector('input'); stateful = !!(inp && (inp.type === 'checkbox' || inp.getAttribute('role') === 'switch')); }
      const icon = !!el.querySelector('svg, img, [class*=icon], [class*=icoon], [data-icon]');
      return { name: name(el), text: (el.innerText||'').trim(), stateful, icon,
        pressed: el.getAttribute('aria-pressed') || el.getAttribute('aria-checked'),
        region: reg ? reg.getAttribute('data-region') : null, band: rel < 0.2 ? 'top' : (rel > 0.8 ? 'bottom' : 'middle'),
        edge: edge(el) }; });
  // player tile around "Bram"
  let tile = null; const walker = document.createTreeWalker(frame, NodeFilter.SHOW_TEXT);
  while (walker.nextNode()) { if (/\bBram\b/.test(walker.currentNode.textContent)) { let e = walker.currentNode.parentElement;
      for (let i = 0; i < 6 && e && e !== frame; i++, e = e.parentElement) { const s = getComputedStyle(e);
        const bg = parse(s.backgroundColor); const bw = parseFloat(s.borderTopWidth) + parseFloat(s.borderLeftWidth);
        if ((bg && bg[3] > 0) || bw > 0 || ['LI','BUTTON'].includes(e.tagName)) { tile = { tag: e.tagName, edge: edge(e) };
          if ((bg && bg[3] > 0) || bw > 0) break; } } if (tile) break; } }
  // segmented control: the group around a button with text "7"
  let seg = null; const seven = [...frame.querySelectorAll('button, [role=radio], label, input')].find(b => (b.innerText||b.value||'').trim() === '7');
  if (seven) { let e = seven.parentElement; for (let i = 0; i < 3 && e && e !== frame; i++, e = e.parentElement) {
      const s = getComputedStyle(e); const bg = parse(s.backgroundColor); const bw = parseFloat(s.borderTopWidth);
      if ((bg && bg[3] > 0) || bw > 0 || e.getAttribute('role') === 'radiogroup' || e.getAttribute('role') === 'group') { seg = { tag: e.tagName, edge: edge(e) }; break; } } }
  const inputs = [...frame.querySelectorAll('input:not([type=checkbox]):not([type=radio]):not([type=hidden]), textarea')].filter(visible)
    .map(el => ({ name: el.getAttribute('aria-label') || (el.labels && el.labels[0] ? el.labels[0].innerText : '') || el.placeholder, edge: edge(el) }));
  return { controls, tile, seg, inputs };
}
"""

MUSIC = re.compile(r"muziek|music", re.I)
CHAT = re.compile(r"\bchat\b", re.I)
MIC = re.compile(r"microfoon|\bmic", re.I)
SOUND = re.compile(r"dempen|geluid|sound|mute", re.I)
LEAVE = re.compile(r"verlat|verlaat|leave", re.I)


def frames(page, path, width=390):
    page.set_viewport_size({"width": width, "height": 900})
    page.goto(path.resolve().as_uri())
    page.wait_for_timeout(150)
    return page.query_selector_all("section[data-screen][data-state]")


def shoot(page, mockups, out, keep):
    out.mkdir(exist_ok=True)
    n = 0
    for f in sorted(mockups.glob("*.html")):
        for fr in frames(page, f):
            st = fr.get_attribute("data-state") or ""
            if not keep(f, st) or n >= 24:
                continue
            try:
                fr.screenshot(path=str(out / f"{f.stem}--{fr.get_attribute('data-screen')}--{st}.png"))
                n += 1
            except Exception:
                pass
    return n


def section(text, title):
    m = re.search(rf"(?ims)^#+\s*{title}\b.*?$(.*?)(?=^#+\s|\Z)", text)
    return m.group(1) if m else None


def score_direction(run, page):
    work = run / "repo" / "docs" / "work" / SLUG["party-new-direction"]
    md = (work / "ux-design.md").read_text(encoding="utf-8") if (work / "ux-design.md").exists() else ""
    sec = section(md, "Style direction")
    rows = 0
    if sec:
        rows = len([l for l in sec.splitlines() if l.strip().startswith("|") and not re.match(r"^\|[\s:|-]+\|$", l.strip())]) - 1
    final = (run / "final.md").read_text(encoding="utf-8")
    last = [l for l in final.strip().splitlines() if l.strip()][-1:] or [""]
    mock = work / "mockups"
    states, files, frames_total = set(), [], 0
    if mock.exists():
        for f in sorted(mock.glob("*.html")):
            files.append(f.name)
            for fr in frames(page, f):
                frames_total += 1
                st = fr.get_attribute("data-state") or ""
                if not f.stem.startswith("direction") and st != "default":
                    states.add(f"{f.stem}:{st}")
        shoot(page, mock, run / "shots", lambda f, st: f.stem.startswith("direction") or st == "default")
    rec = {
        "C1_mode": bool(re.search(r"(?im)^\W*mode\W*\s*:?\s*\W*new-direction", md)),
        "C2_style_rows": rows, "C2_style": 3 <= rows <= 4,
        "needs_human": "NEEDS_HUMAN" in " ".join(last) or bool(re.search(r"NEEDS_HUMAN\W*$", final.strip())),
        "selected_line": bool(re.search(r"(?im)^\W*selected\W*:", md)),
        "state_frames": len(states), "files": files, "frames": frames_total,
    }
    rec["C3_stop"] = rec["needs_human"] and not rec["selected_line"] and rec["state_frames"] == 0
    return rec


def score_faults(run, page):
    mock = run / "repo" / "docs" / "work" / SLUG["party-review-faults"] / "mockups"
    data = {}
    if mock.exists():
        for f in sorted(mock.glob("*.html")):
            for fr in frames(page, f):
                key = f"{fr.get_attribute('data-screen')}/{fr.get_attribute('data-state')}"
                data[key] = fr.evaluate(PROBE)
        shoot(page, mock, run / "shots", lambda f, st: st in ("default", "sound-off", "guest"))
    lobby = next((v for k, v in data.items() if k.startswith("lobby") and k.endswith("/default")), None) \
        or next((v for k, v in data.items() if k.startswith("lobby")), None)
    stand_frames = {k: v for k, v in data.items() if k.startswith("stand")}
    stand = stand_frames.get("stand/default") or next(iter(stand_frames.values()), None)

    def find(frame, rx):
        return [c for c in (frame or {}).get("controls", []) if rx.search(c["name"])]

    def place(c):
        return c["region"] or c["band"]

    final = (run / "final.md").read_text(encoding="utf-8")
    rec = {"frames": len(data), "non_default": sorted(k for k in data if not k.endswith("/default")),
           "needs_human": bool(re.search(r"NEEDS_HUMAN\W*$", final.strip()))}
    # P1: the run stopped after pass 1 (NEEDS_HUMAN, and only default frames). The prompt of this case gives
    # the OK of the owner on the key screens, so a correct 0.4.0 run does not stop here (P1 false).
    rec["P1_stopped"] = rec["needs_human"] and not rec["non_default"]
    rec["F1a_tile_edge"] = (lobby or {}).get("tile", {}) and lobby["tile"]["edge"]
    rec["F1a_avoided"] = bool(rec["F1a_tile_edge"]) and rec["F1a_tile_edge"] >= 1.5
    rec["F1b_segment_edge"] = (lobby or {}).get("seg") and lobby["seg"]["edge"]
    rec["F1b_avoided"] = bool(rec["F1b_segment_edge"]) and rec["F1b_segment_edge"] >= 3
    ins = (lobby or {}).get("inputs") or []
    rec["F2_input_edge"] = ins[0]["edge"] if ins else None
    rec["F2_avoided"] = bool(ins) and ins[0]["edge"] >= 3
    places = {}
    for label, rx in (("music", MUSIC), ("leave", LEAVE)):
        a, b = find(lobby, rx), find(stand, rx)
        places[label] = [place(a[0]) if a else None, place(b[0]) if b else None]
    rec["F3_places"] = places
    rec["F3_avoided"] = all(p[0] and p[0] == p[1] for p in places.values())
    toggles = {}
    for label, rx in (("music", MUSIC), ("chat", CHAT), ("mic", MIC)):
        cs = [c for v in data.values() for c in v["controls"] if rx.search(c["name"])]
        toggles[label] = {"found": len(cs), "stateful": all(c["stateful"] for c in cs) if cs else False,
                          "icon": all(c["icon"] for c in cs) if cs else False}
    rec["F4_toggles"] = toggles
    rec["F4_avoided"] = all(t["stateful"] and t["icon"] for t in toggles.values())
    snd = [c for v in stand_frames.values() for c in v["controls"] if SOUND.search(c["name"])]
    rec["F5_names"] = sorted({c["name"] for c in snd})
    rec["F5_avoided"] = bool(snd) and all(c["stateful"] for c in snd) and len(rec["F5_names"]) == 1
    return rec


def main():
    p = argparse.ArgumentParser()
    p.add_argument("runs")
    args = p.parse_args()
    root = Path(args.runs)
    with sync_playwright() as pw:
        browser = pw.chromium.launch()
        page = browser.new_page()
        for case, fn in (("party-new-direction", score_direction), ("party-review-faults", score_faults)):
            recs = []
            for meta in sorted((root / case).glob("*/*/meta.json")):
                run = meta.parent
                m = json.loads(meta.read_text())
                try:
                    rec = fn(run, page)
                except Exception as e:  # a broken mockup must not stop the scoring of the other runs
                    rec = {"error": str(e)[:300]}
                rec.update({"run": f"{run.parent.name}/{run.name}", "cost": round(m.get("cost_usd", 0), 2),
                            "seconds": m.get("seconds"), "turns": m.get("turns"), "exit": m.get("exit")})
                recs.append(rec)
                print(case, json.dumps(rec))
            if recs:
                (root / case / "scores.json").write_text(json.dumps(recs, indent=2), encoding="utf-8")
        browser.close()


if __name__ == "__main__":
    main()
