#!/usr/bin/env python3
"""Check prototype content against the proto-shell/1 standard.

Usage: python3 protocheck.py <content.html or prototype.html> [--json]
       python3 protocheck.py --header-only <file> [--json]

--header-only checks a prototype outside the shell (a logic prototype, or a
page of the real product with a variant bar) for the standard header.

Exit code 0 when there are no errors, 1 when there are errors, 2 on bad usage.
"""
import json
import re
import sys
from html.parser import HTMLParser
from pathlib import Path

ID = re.compile(r"^[a-z0-9][a-z0-9-]*$")
VARIANT = re.compile(r"^[A-Z]$")
COLOR = re.compile(r"#[0-9a-fA-F]{3,8}\b|\b(?:rgb|rgba|hsl|hsla|oklch|lab|lch)\(")
MAX_BYTES = 800_000
LOREM = re.compile(r"lorem ipsum|dolor sit amet", re.I)


class Collect(HTMLParser):
    """Collect elements with attributes and the tag sequence of one template."""

    def __init__(self):
        super().__init__()
        self.tags, self.attrs = [], []

    def handle_starttag(self, tag, attrs):
        self.tags.append(tag)
        self.attrs.append((tag, dict(attrs), self.getpos()[0]))


class Report:
    def __init__(self):
        self.items = []

    def error(self, msg):
        self.items.append(("error", msg))

    def warn(self, msg):
        self.items.append(("warning", msg))

    def count(self, kind):
        return sum(1 for k, _ in self.items if k == kind)


def css_declarations(css):
    css = re.sub(r"/\*.*?\*/", "", css, flags=re.S)
    for m in re.finditer(r"([-\w]+)\s*:\s*([^;{}]+);", css):
        yield m.group(1), m.group(2), css[: m.start()].count("\n") + 1


def main() -> int:
    args = [a for a in sys.argv[1:] if not a.startswith("--")]
    if len(args) != 1:
        print("Usage: protocheck.py <file> [--json]", file=sys.stderr)
        return 2
    text = Path(args[0]).read_text(encoding="utf-8")
    r = Report()
    if "--header-only" in sys.argv:
        check_header(text, r)
        return finish(r)

    m = re.search(r'<script[^>]*id="proto-manifest"[^>]*>(.*?)</script>', text, re.S)
    if not m:
        r.error('No <script type="application/json" id="proto-manifest">.')
        return finish(r)
    try:
        man = json.loads(m.group(1))
    except json.JSONDecodeError as e:
        r.error(f"The manifest is not valid JSON: {e}.")
        return finish(r)

    # Manifest keys
    if man.get("standard") != "proto-shell/1":
        r.error('Manifest "standard" must be "proto-shell/1".')
    for key in ("title", "question", "kind", "scope", "start", "screens", "variants", "themes"):
        if not man.get(key):
            r.error(f'Manifest key "{key}" is missing or empty.')
    if man.get("kind") and man["kind"] not in ("throwaway", "evolutionary"):
        r.error('Manifest "kind" must be "throwaway" or "evolutionary".')
    if len(str(man.get("title", ""))) > 40:
        r.error("The title is longer than 40 characters. The header shows a short name.")
    sc = str(man.get("scope", ""))
    if len(sc) > 120 or sc.count(". ") > 0:
        r.error("The scope must be one sentence of max 120 characters. The header shows it next to the kind.")
    if man.get("lang") and str(man["lang"])[:2] not in ("en", "nl", "de"):
        r.warn(f'Language "{man["lang"]}" has no header text. The header uses English.')
    q = str(man.get("question", ""))
    if q and not q.rstrip().endswith("?"):
        r.warn("The question does not end with a question mark. Write it as one question.")

    screens = {s.get("id"): s for s in man.get("screens", []) if isinstance(s, dict)}
    for sid, s in screens.items():
        if not sid or not ID.match(sid):
            r.error(f'Screen id "{sid}" must match [a-z0-9-].')
        states = s.get("states") or []
        if not states:
            r.error(f'Screen "{sid}" has no states.')
        for st in states:
            if not ID.match(str(st)):
                r.error(f'State "{st}" of screen "{sid}" must match [a-z0-9-].')
    variants = [v for v in man.get("variants", []) if isinstance(v, dict)]
    vids = [v.get("id") for v in variants]
    for v in variants:
        if not VARIANT.match(str(v.get("id", ""))):
            r.error(f'Variant id "{v.get("id")}" must be one capital letter.')
        if len(variants) > 1 and not v.get("hypothesis"):
            r.error(f'Variant "{v.get("id")}" has no hypothesis.')
    if len(variants) > 3:
        r.error("Max 3 variants.")
    if len(set(vids)) != len(vids):
        r.error("Variant ids are not unique.")

    def state_ok(screen, state):
        return screen in screens and (state is None or state in (screens[screen].get("states") or []))

    st = man.get("start") or {}
    if not state_ok(st.get("screen"), st.get("state")):
        r.error(f"The start {st} is not a screen and state of the manifest.")

    # Templates
    tpls = {}
    for tm in re.finditer(r"<template([^>]*)>(.*?)</template>", text, re.S):
        a = dict(re.findall(r'([\w-]+)="([^"]*)"', tm.group(1)))
        if "data-screen" not in a:
            continue
        key = (a["data-screen"], a.get("data-variant", "*"))
        if key in tpls:
            r.error(f"Two templates for screen {key[0]}, variant {key[1]}.")
        c = Collect()
        c.feed(tm.group(2))
        tpls[key] = (c, tm.group(2))
    for sid in screens:
        for v in vids or ["*"]:
            if (sid, v) not in tpls and (sid, "*") not in tpls:
                r.error(f'Screen "{sid}" has no template for variant {v} and no shared template.')
    for (sid, v) in tpls:
        if sid not in screens:
            r.error(f'Template for unknown screen "{sid}".')
        if v != "*" and v not in vids:
            r.error(f'Template for unknown variant "{v}" (screen {sid}).')

    # Offline: no network calls, no external files
    if re.search(r"\bfetch\s*\(|XMLHttpRequest|new\s+WebSocket|navigator\.sendBeacon", text):
        r.error("Network call found (fetch, XMLHttpRequest, WebSocket, or sendBeacon). The prototype must work offline.")
    if re.search(r"""(?:src|href)\s*=\s*["'](?:https?:)?//""", text) or re.search(r"url\(\s*[\"']?(?:https?:)?//", text):
        r.error("External file found (a script, a style sheet, a font, or an image). Put everything in the file.")
    if len(text.encode("utf-8")) > MAX_BYTES:
        r.warn(f"The file is larger than {MAX_BYTES // 1000} KB. Remove embedded images.")

    # Variants differ in structure
    if len(vids) > 1:
        differ = False
        for sid in screens:
            seqs = {tuple(tpls[(sid, v)][0].tags) for v in vids if (sid, v) in tpls}
            if len(seqs) > 1:
                differ = True
        if not differ:
            r.error("The variants differ only in style or text, not in structure. Change the layout, the order, or the main action.")

    # Attributes in templates
    task_ids = {t.get("id") for t in man.get("tasks", []) if isinstance(t, dict)}
    note_ids = {str(n.get("n")) for n in man.get("notes", []) if isinstance(n, dict)}
    success_used, notes_used = set(), set()
    for (sid, v), (c, raw) in tpls.items():
        where = f"template {sid}/{v}"
        if LOREM.search(raw):
            r.error(f"{where}: lorem ipsum. Use real labels and real-looking data.")
        sts = screens.get(sid, {}).get("states") or []
        for tag, at, line in c.attrs:
            if "data-goto" in at:
                tgt, _, tst = at["data-goto"].partition(":")
                if not state_ok(tgt, tst or None):
                    r.error(f'{where} line {line}: data-goto="{at["data-goto"]}" is not a screen and state.')
            if "data-set-state" in at and at["data-set-state"] not in sts:
                r.error(f'{where} line {line}: data-set-state="{at["data-set-state"]}" is not a state of {sid}.')
            for k in ("data-show-state", "data-hide-state"):
                for x in at.get(k, "").split():
                    if x not in sts:
                        r.error(f'{where} line {line}: {k} "{x}" is not a state of {sid}.')
            if "data-task-success" in at:
                success_used.add(at["data-task-success"])
                if at["data-task-success"] not in task_ids:
                    r.error(f'{where} line {line}: data-task-success="{at["data-task-success"]}" is not a task id.')
            if "data-note" in at:
                notes_used.add(at["data-note"])
                if at["data-note"] not in note_ids:
                    r.error(f'{where} line {line}: data-note="{at["data-note"]}" has no note in the manifest.')
            if "style" in at and COLOR.search(at["style"]):
                r.error(f"{where} line {line}: a color value in a style attribute. Use a token.")
            if tag == "img" and "alt" not in at:
                r.error(f"{where} line {line}: <img> without alt.")
            if tag in ("input", "select", "textarea") and at.get("type") not in ("hidden", "submit", "button") \
                    and not (at.get("aria-label") or at.get("aria-labelledby") or at.get("id") or at.get("title")):
                r.warn(f"{where} line {line}: <{tag}> without a label (aria-label, or an id for a <label>).")

    # States show a change
    css_m = re.search(r'<(style|script)[^>]*id="proto-css"[^>]*>(.*?)</\1>', text, re.S)
    css = css_m.group(2) if css_m else ""
    if not css_m:
        r.error('No <style id="proto-css">.')
    script_m = re.search(r'<script[^>]*id="proto-script"[^>]*>(.*?)</script>', text, re.S)
    script = script_m.group(1) if script_m else ""
    for sid, s in screens.items():
        raw_all = " ".join(raw for (x, _), (_, raw) in tpls.items() if x == sid)
        for stt in (s.get("states") or [])[1:]:
            shown = re.search(r'data-(?:show|hide)-state="[^"]*\b' + re.escape(stt) + r'\b', raw_all)
            styled = re.search(r'data-state="?' + re.escape(stt), css)
            scripted = stt in script
            if not (shown or styled or scripted):
                r.warn(f'State "{stt}" of screen "{sid}" shows no change (no data-show-state, no CSS, no script).')

    # Tokens only
    for prop, val, line in css_declarations(css):
        if not prop.startswith("--") and COLOR.search(val):
            r.error(f'proto-css line {line}: "{prop}: {val.strip()}" uses a color value. Define it as a token and use var().')
    for t in man.get("themes", []):
        if t != (man.get("themes") or [""])[0] and not re.search(r'data-theme="?' + re.escape(t), css):
            r.error(f'Theme "{t}" has no [data-theme="{t}"] tokens in proto-css.')
    if not re.search(r'data-design="?wireframe', css):
        r.warn("No wireframe tokens. The shell uses grayscale for the Wireframe design.")

    # Tasks
    tasks = [t for t in man.get("tasks", []) if isinstance(t, dict)]
    if len(vids) > 1 and not tasks:
        r.warn("Two or more variants but no tasks: Test mode cannot run an A/B test.")
    for t in tasks:
        tid = t.get("id")
        if not tid or not t.get("text"):
            r.error(f"Task {t} needs an id and a text.")
        tst = t.get("start") or man.get("start") or {}
        if not state_ok(tst.get("screen"), tst.get("state")):
            r.error(f'Task "{tid}": start is not a screen and state.')
        ok = t.get("success") or {}
        if not ok:
            r.warn(f'Task "{tid}" has no success rule. Success will be self-reported.')
        if ok.get("screen") and not state_ok(ok["screen"], ok.get("state")):
            r.error(f'Task "{tid}": the success screen and state do not exist.')
        if ok.get("element") and tid not in success_used and f"success('{tid}')" not in script and f'success("{tid}")' not in script:
            r.error(f'Task "{tid}" has an element success rule, but no data-task-success="{tid}" and no proto.success("{tid}").')
        if re.search(r"\b(click|tap|press)\b", str(t.get("text", "")), re.I):
            r.warn(f'Task "{tid}" names the action ("click"). Write the goal of the user, not the steps.')
    for n in note_ids - notes_used:
        r.warn(f"Note {n} is not on an element (data-note).")
    if "</script" in script:
        r.error("proto-script contains </script>. Split the string.")

    return finish(r)


def check_header(text: str, r: Report) -> None:
    m = re.search(r'<(header|proto-header)[^>]*id="proto-header"([^>]*)>(.*?)</\1>', text, re.S)
    if not m:
        r.error('No standard header. Put the output of "protobuild.py --header" first in the body.')
        return
    attrs = dict(re.findall(r'(data-[\w-]+)="([^"]*)"', m.group(0)))
    for k in ("data-title", "data-kind", "data-scope"):
        if not attrs.get(k):
            r.error(f"The header has no {k}.")
    if attrs.get("data-kind") not in ("throwaway", "evolutionary"):
        r.error('The header kind must be "throwaway" or "evolutionary".')
    badges = ("Prototype, not the product", "Prototype, niet het product", "Prototyp, nicht das Produkt")
    if not any(b in m.group(3) for b in badges):
        r.error("The header has no standard badge text.")
    if text.find('id="proto-header"') > text.find("<body") + 400 and "<body" in text:
        r.warn("The header is not first in the body.")


def finish(r: Report) -> int:
    if "--json" in sys.argv:
        print(json.dumps([{"level": k, "message": m} for k, m in r.items], indent=2))
    else:
        for k, m in r.items:
            print(f"{k}: {m}")
        print(f"protocheck: {r.count('error')} errors, {r.count('warning')} warnings")
    return 1 if r.count("error") else 0


if __name__ == "__main__":
    sys.exit(main())
