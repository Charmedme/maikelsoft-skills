#!/usr/bin/env python3
"""Wrap prototype content in the fixed proto-shell/1 shell.

Usage: python3 protobuild.py <content.html> <prototype.html> [--artifact]
       python3 protobuild.py --header <content.html or manifest.json>

--header prints the standard prototype header as an HTML snippet. Use it in
each prototype that does not use the shell (a logic prototype, or a variant
bar in the real product). The snippet goes first in the body.

--artifact leaves out the doctype, html, head, and body tags, because the
Claude artifact host adds its own skeleton.

The content file holds the part between PROTO-CONTENT START and END:
the manifest, the prototype styles, the screen templates, and an optional script.
The output is one self-contained HTML file that opens with a double-click.
"""
import html
import json
import re
import sys
from pathlib import Path

# In a skill, the shell files are in references/proto-shell/. In the repository, they are next to this script.
_HERE = Path(__file__).resolve().parent
ASSETS = _HERE.parent / "references" / "proto-shell"
if not (ASSETS / "shell.js").exists():
    ASSETS = _HERE
START = "<!-- PROTO-CONTENT START -->"
END = "<!-- PROTO-CONTENT END -->"


def header_strings() -> dict:
    return json.loads((ASSETS / "strings.json").read_text(encoding="utf-8"))


def header_snippet(manifest: dict) -> str:
    lang = str(manifest.get("lang", "en"))[:2]
    strings = header_strings()
    loc = strings.get(lang, strings["en"])
    kind = str(manifest.get("kind", "throwaway"))
    values = {
        "badge": loc["badge"], "title": manifest.get("title", "Prototype"), "kind": kind,
        "kindLabel": loc.get(kind, kind), "scope": manifest.get("scope", ""),
    }
    out = (ASSETS / "proto-header.html").read_text(encoding="utf-8")
    for k, v in values.items():
        out = out.replace("{{" + k + "}}", html.escape(str(v)))
    return out


def read_manifest(path: Path):
    text = path.read_text(encoding="utf-8")
    if path.suffix == ".json":
        return json.loads(text), text
    m = re.search(r'<script[^>]*id="proto-manifest"[^>]*>(.*?)</script>', text, re.S)
    return (json.loads(m.group(1)) if m else None), text


def main() -> int:
    if "--header" in sys.argv:
        paths = [a for a in sys.argv[1:] if not a.startswith("--")]
        if len(paths) != 1:
            print("Usage: protobuild.py --header <content.html or manifest.json>", file=sys.stderr)
            return 2
        manifest, _ = read_manifest(Path(paths[0]))
        if not manifest:
            print("error: no manifest found", file=sys.stderr)
            return 1
        print(header_snippet(manifest), end="")
        return 0
    args = [a for a in sys.argv[1:] if not a.startswith("--")]
    artifact = "--artifact" in sys.argv
    if len(args) != 2:
        print("Usage: protobuild.py <content.html> <prototype.html> [--artifact]", file=sys.stderr)
        return 2
    src, out = Path(args[0]), Path(args[1])
    text = src.read_text(encoding="utf-8")
    if START in text and END in text:
        text = text[text.index(START) + len(START):text.index(END)]
    m = re.search(r'<script[^>]*id="proto-manifest"[^>]*>(.*?)</script>', text, re.S)
    if not m:
        print("error: no <script id=\"proto-manifest\"> in the content", file=sys.stderr)
        return 1
    try:
        manifest = json.loads(m.group(1))
    except json.JSONDecodeError as e:
        print(f"error: the manifest is not valid JSON: {e}", file=sys.stderr)
        return 1
    # The content CSS must not style the shell page: store it as inert text.
    text, n = re.subn(r'<style([^>]*)id="proto-css"([^>]*)>(.*?)</style>',
                      lambda m: '<script type="text/css" id="proto-css">' + m.group(3) + '</script>', text, flags=re.S)
    if n != 1:
        print('error: the content needs exactly one <style id="proto-css">', file=sys.stderr)
        return 1
    title = html.escape(str(manifest.get("title", "Prototype")))
    lang = html.escape(str(manifest.get("lang", "en")))
    text += '\n<script type="application/json" id="proto-strings">' + json.dumps(header_strings(), ensure_ascii=False) + "</script>"
    css = (ASSETS / "shell.css").read_text(encoding="utf-8")
    js = (ASSETS / "shell.js").read_text(encoding="utf-8")
    page = (
        f'<!doctype html>\n<html lang="{lang}">\n<head>\n<meta charset="utf-8">\n'
        f'<meta name="viewport" content="width=device-width, initial-scale=1">\n'
        f"<title>Prototype: {title}</title>\n"
        f'<meta name="generator" content="proto-shell/1">\n'
        f"<style id=\"shell-css\">\n{css}</style>\n</head>\n<body>\n"
        f"{START}\n{text.strip()}\n{END}\n"
        f"<script id=\"shell-js\">\n{js}</script>\n</body>\n</html>\n"
    )
    if artifact:
        page = (
            f"<title>{title}</title>\n<meta name=\"generator\" content=\"proto-shell/1\">\n"
            f"<style id=\"shell-css\">\n{css}</style>\n"
            f"{START}\n{text.strip()}\n{END}\n"
            f"<script id=\"shell-js\">\n{js}</script>\n"
        )
    out.write_text(page, encoding="utf-8")
    print(f"protobuild: wrote {out} ({len(page) // 1024} KB)")
    return 0


if __name__ == "__main__":
    sys.exit(main())
