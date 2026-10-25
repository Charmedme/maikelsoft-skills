#!/usr/bin/env python3
"""Turn checked HTML mockups into the files of a Claude Design canvas.

    python3 html2dc.py css   <mockups/*.html> --root <dir>
    python3 html2dc.py build <mockups/*.html> --root <dir> --css <screen>=/_blob/<id> ... --title "<name>"
                             [--ds <folder>=<design system url>] [--width 1440]

The mockups follow the mockup mode of the ux-designer agent: one HTML file for each screen, and one
<section data-screen="<screen>" data-state="<state>"> for each state.

css    Joins the stylesheets of each screen (the linked CSS files and the <style> blocks) into
       <dir>/assets/<screen>.css. Upload each file as an asset of the canvas. The upload gives the
       /_blob/<id> for --css.
build  Writes <dir>/project/<screen>--<state>.dc.html for each state, and <dir>/project/canvas.json.
       One row of artboards for each screen, with a title note. With Playwright, it measures the
       height of each state at the width. Without Playwright, the height is 900 px.

The output follows the artboard rules of the Design type: the support.js line, closed elements,
quoted attributes, a data-dc-script block, and a fluid page root (expand "fill").
Exit code: 0 when all is written, 1 when a mockup has no state section, 2 for a usage error.
"""
import argparse
import datetime
import html
import json
import re
import sys
from html.parser import HTMLParser
from pathlib import Path

VOID = {"area", "base", "br", "col", "embed", "hr", "img", "input", "link", "meta", "source", "track", "wbr"}
DROP = {"script", "iframe", "object", "embed", "noscript"}
SEG = re.compile(r"[^A-Za-z0-9_.-]+")


class Node:
    def __init__(self, tag, attrs, parent=None):
        self.tag, self.attrs, self.parent, self.children = tag, attrs, parent, []


class Tree(HTMLParser):
    """A small tree builder. It closes open elements when an end tag of an ancestor comes."""

    def __init__(self):
        super().__init__(convert_charrefs=True)
        self.root = Node("#root", [])
        self.cur = self.root
        self.styles, self.links, self.in_style, self.title = [], [], False, ""
        self.in_title = False

    def handle_starttag(self, tag, attrs):
        if tag == "style":
            self.in_style = True
            self.styles.append("")
            return
        if tag == "title":
            self.in_title = True
            return
        if tag == "link":
            a = dict(attrs)
            if (a.get("rel") or "").lower() == "stylesheet" and a.get("href"):
                self.links.append(a["href"])
            return
        if tag in ("p", "li", "option", "tr", "td", "th") and self.cur.tag == tag:
            self.cur = self.cur.parent
        node = Node(tag, attrs, self.cur)
        self.cur.children.append(node)
        if tag not in VOID:
            self.cur = node

    def handle_startendtag(self, tag, attrs):
        self.handle_starttag(tag, attrs)
        if tag not in VOID and self.cur.tag == tag:
            self.cur = self.cur.parent

    def handle_endtag(self, tag):
        if tag == "style":
            self.in_style = False
            return
        if tag == "title":
            self.in_title = False
            return
        n = self.cur
        while n is not self.root and n.tag != tag:
            n = n.parent
        if n is not self.root:
            self.cur = n.parent

    def handle_data(self, data):
        if self.in_style:
            self.styles[-1] += data
        elif self.in_title:
            self.title += data
        else:
            self.cur.children.append(data)


def find_sections(node, out):
    for c in node.children:
        if isinstance(c, Node):
            a = dict(c.attrs)
            if c.tag == "section" and a.get("data-state"):
                out.append(c)
            else:
                find_sections(c, out)
    return out


def serialize(node, warn):
    if isinstance(node, str):
        if "{{" in node:
            warn.append("text with {{ was changed to { { (the canvas reads {{ as a hole)")
            node = node.replace("{{", "{ {")
        return html.escape(node, quote=False)
    if node.tag in DROP:
        return ""
    attrs = []
    for k, v in node.attrs:
        if k.startswith("on"):
            continue
        attrs.append(f'{k}="{html.escape(v if v is not None else "", quote=True)}"' if v is not None else f'{k}=""')
    start = f"<{node.tag}" + ("" if not attrs else " " + " ".join(attrs)) + ">"
    if node.tag in VOID:
        return start
    return start + "".join(serialize(c, warn) for c in node.children) + f"</{node.tag}>"


def parse(path):
    t = Tree()
    t.feed(Path(path).read_text(encoding="utf-8"))
    return t


def screen_css(path, tree):
    parts = []
    for href in tree.links:
        if re.match(r"^[a-z]+:", href):
            continue
        f = (Path(path).parent / href).resolve()
        if f.exists():
            parts.append(f"/* {f.name} */\n" + f.read_text(encoding="utf-8"))
    parts += [f"/* inline style of {Path(path).name} */\n" + s for s in tree.styles]
    css = "\n".join(parts)
    # url() to local files cannot load from the canvas.
    return re.sub(r"url\((?!['\"]?(data:|/_blob/|https:))[^)]*\)", "none", css)


def screen_of(path, sections):
    a = dict(sections[0].attrs) if sections else {}
    return a.get("data-screen") or Path(path).stem


def measure(files, width):
    """Return {(screen, state): height} with Playwright, or {} without it."""
    try:
        from playwright.sync_api import sync_playwright
    except ImportError:
        return {}
    out = {}
    with sync_playwright() as p:
        b = p.chromium.launch()
        page = b.new_page(viewport={"width": width, "height": 900})
        for f in files:
            page.goto(Path(f).resolve().as_uri())
            for row in page.evaluate("""() => [...document.querySelectorAll('section[data-state]')].map(s =>
                    [s.dataset.screen || '', s.dataset.state, Math.ceil(s.getBoundingClientRect().height)])"""):
                out[(row[0] or Path(f).stem, row[1])] = max(200, row[2])
        b.close()
    return out


def cmd_css(args):
    root = Path(args.root)
    (root / "assets").mkdir(parents=True, exist_ok=True)
    for f in args.files:
        t = parse(f)
        secs = find_sections(t.root, [])
        name = SEG.sub("-", screen_of(f, secs))
        out = root / "assets" / f"{name}.css"
        out.write_text(screen_css(f, t), encoding="utf-8")
        print(f"{name}\t{out}")
    print("Upload each file as an asset of the canvas. Then run build with --css <screen>=<the /_blob/ url>.")
    return 0


def cmd_build(args):
    root = Path(args.root)
    proj = root / "project"
    proj.mkdir(parents=True, exist_ok=True)
    css = dict(v.split("=", 1) for v in args.css or [])
    for k, v in css.items():
        if not v.startswith("/_blob/"):
            print(f"--css {k}: the value must be the /_blob/<id> url of the upload.", file=sys.stderr)
            return 2
    heights = measure(args.files, args.width)
    boards, order, notes, warn = {}, [], {}, []
    y, code = 0, 0
    lang = "en"
    for f in args.files:
        t = parse(f)
        secs = find_sections(t.root, [])
        if not secs:
            print(f"{f}: no <section data-state> found.", file=sys.stderr)
            code = 1
            continue
        screen = screen_of(f, secs)
        sname = SEG.sub("-", screen)
        if sname not in css:
            warn.append(f"{sname}: no --css url. The artboards have no styles.")
        row_h = 0
        x = 0
        for s in secs:
            state = dict(s.attrs)["data-state"]
            fname = f"{sname}--{SEG.sub('-', state)}.dc.html"
            fname = re.sub(r"^[^A-Za-z0-9_]+", "", fname)
            h = heights.get((screen, state), 900)
            body = serialize(s, warn)
            link = f'<link rel="stylesheet" href="{css[sname]}">\n' if sname in css else ""
            props = json.dumps({"$preview": {"width": args.width, "height": h}})
            doc = (f'<!doctype html>\n<html lang="{lang}">\n<head>\n<meta charset="utf-8">\n'
                   f"<title>{html.escape(screen)}: {html.escape(state)}</title>\n"
                   f'<script src="./support.js"></script>\n{link}</head>\n<body>\n<x-dc>\n'
                   f"<helmet>\n<style>\nbody{{margin:0}}\n</style>\n</helmet>\n"
                   f'<div style="width: 100%; box-sizing: border-box">\n{body}\n</div>\n</x-dc>\n'
                   f"<script type=\"text/x-dc\" data-dc-script data-props='{props}'>\n"
                   "class Component extends DCLogic {\nrenderVals() {\nreturn {};\n}\n}\n</script>\n</body>\n</html>\n")
            (proj / fname).write_text(doc, encoding="utf-8")
            boards[fname] = {"x": x, "y": y, "w": args.width, "h": h, "title": f"{screen} / {state}", "expand": "fill"}
            order.append(fname)
            x += args.width + 80
            row_h = max(row_h, h)
        notes[SEG.sub("_", sname)[:40]] = {"x": 0, "y": y - 300, "text": screen, "kind": "title1", "maxW": max(x - 80, 400)}
        y += row_h + 420
    if order and "Main.dc.html" not in boards:
        first = order[0]
        (proj / "Main.dc.html").write_text((proj / first).read_text(encoding="utf-8"), encoding="utf-8")
        (proj / first).unlink()
        boards["Main.dc.html"] = boards.pop(first)
        order[0] = "Main.dc.html"
    index = {"v": 3, "createdOnFiles": {"v": 1, "at": datetime.datetime.now(datetime.timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ")},
             "title": args.title, "launch": {"view": "canvas"}, "pages": [], "boards": boards, "order": order,
             "notes": notes, "designSystems": []}
    for d in args.ds or []:
        folder, url = d.split("=", 1)
        index["designSystems"].append({"title": folder, "namespace": folder, "artifact": url, "version": None,
                                       "copiedAt": index["createdOnFiles"]["at"]})
    (proj / "canvas.json").write_text(json.dumps(index, indent=2) + "\n", encoding="utf-8")
    for w in sorted(set(warn)):
        print("WARNING " + w)
    print(f"Wrote {len(order)} artboards and project/canvas.json in {root}. Measured heights: {'yes' if heights else 'no'}.")
    return code


def main(argv=None):
    p = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    sub = p.add_subparsers(dest="cmd", required=True)
    c = sub.add_parser("css")
    c.add_argument("files", nargs="+")
    c.add_argument("--root", required=True)
    c.set_defaults(fn=cmd_css)
    c = sub.add_parser("build")
    c.add_argument("files", nargs="+")
    c.add_argument("--root", required=True)
    c.add_argument("--css", action="append")
    c.add_argument("--title", required=True)
    c.add_argument("--ds", action="append")
    c.add_argument("--width", type=int, default=1440)
    c.set_defaults(fn=cmd_build)
    args = p.parse_args(argv)
    return args.fn(args)


if __name__ == "__main__":
    sys.exit(main())
