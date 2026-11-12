"""Turn the "Behaviour spec" section of a ticket into plain text, check it, and keep a fingerprint for each rule.

Usage:
    python3 specsync.py check <file> [--from adf|html|md|auto] [--whole] [--kind story|bug] [--format text|json]
    python3 specsync.py copy <file> --item KEY --out docs/work/<item>/spec.md [--from ...] [--whole] [--kind ...]
    python3 specsync.py diff <file> --baseline <dir or spec-baseline.json> [--from ...] [--whole] [--format text|json]

The input is the raw text of a ticket: Jira ADF JSON, Azure DevOps HTML, or Markdown.
The agent reads the ticket with the tracker tool. This script does not call a tracker or the network.

The section is the text under a heading "Behaviour spec" ("Behavior spec", "Gedragsspecificatie"),
up to the next heading of the same or a higher level. Text outside the section is ignored.
With --whole, the script takes all the text as the section.

The canonical text is the same for each source format: NFC, decoded entities, no tags, no
markup, one space between words, no empty lines, straight quotes, one bullet form.

The script checks:
  - SPEC-SECTION: the text has a Behaviour spec section (not with --whole).
  - SPEC-RULE-ID: each Rule has a unique ID R<n>. Each scenario is in a Rule.
  - SPEC-RULES: the spec has 1 to 7 rules.
  - SPEC-SCENARIOS: each rule has 1 to 4 scenarios. Each row of an Examples table is one scenario.
  - SPEC-STEPS: each scenario has a When step and a Then step.
  - SPEC-LIMIT: a rule with "(limit: ...)" has scenarios "below", "at", and "above"
    (Dutch: "onder", "op", "boven").
  - With --kind bug, SPEC-BUG-NOW: a rule has a "Now:" line, and SPEC-BUG-UNCHANGED:
    a rule "Unchanged" ("Ongewijzigd") has 1 or more scenarios.

The fingerprint of a rule is the SHA-256 of its canonical text. The section has one too.
"copy" writes the spec into spec.md and the fingerprints into spec-baseline.json.
"diff" compares a new copy of the ticket with the baseline and names each rule that changed.

Exit code 1 means an error (check, copy) or a change (diff). It uses the Python standard library only.
"""
from __future__ import annotations

import argparse
import datetime
import difflib
import hashlib
import html
import json
import pathlib
import re
import sys
import unicodedata
from html.parser import HTMLParser

SECTION_NAME = re.compile(r"^(behaviou?r spec|gedragsspecificatie)\s*:?$", re.I)
QUOTES = str.maketrans({"\u2018": "'", "\u2019": "'", "\u201a": "'", "\u201b": "'", "\u2032": "'",
                        "\u201c": '"', "\u201d": '"', "\u201e": '"', "\u201f": '"', "\u2033": '"'})
BULLET = re.compile(r"^(?:[-*+\u2022]|\d+[.)])\s+")
SEPARATOR = re.compile(r"^\|[\s:|-]*-[\s:|-]*$")
HTML_TAG = re.compile(r"</?(p|div|br|span|h[1-6]|ul|ol|li|table|tr|td|th|pre|code|b|strong|i|em|u|a|blockquote)\b[^<>]*>", re.I)
KEYWORDS = [("feature", r"Feature|Functionaliteit"), ("background", r"Background|Achtergrond"), ("rule", r"Rule|Regel"),
            ("outline", r"Scenario Outline|Scenario Template|Abstract Scenario"),
            ("scenario", r"Scenario|Example|Voorbeeld"), ("examples", r"Examples|Voorbeelden"), ("now", r"Now|Nu")]
KEYWORD = [(kind, re.compile(rf"^(?:{words})\s*:\s*(.*)$", re.I)) for kind, words in KEYWORDS]
STEP = re.compile(r"^(Given|When|Then|And|But|Stel|Gegeven|Als|Wanneer|Dan|En|Maar)\s+\S")
STEP_TYPE = {"Given": "given", "Stel": "given", "Gegeven": "given", "When": "when", "Als": "when",
             "Wanneer": "when", "Then": "then", "Dan": "then"}
RULE_ID = re.compile(r"^(R\d+)\b[\s:.\-\u2013]*(.*)$", re.I)
LIMIT = re.compile(r"\((?:limit|grens)\s*:\s*([^)]*)\)", re.I)
UNCHANGED = re.compile(r"^(unchanged|ongewijzigd)\b", re.I)
LIMIT_WORDS = {"below": ("below", "onder"), "at": ("at", "op"), "above": ("above", "boven")}
BEGIN = "<!-- BEGIN behaviour spec: {} -->"
END = "<!-- END behaviour spec -->"
BLOCK = re.compile(r"<!-- BEGIN behaviour spec: [^\n]*? -->\n.*?<!-- END behaviour spec -->", re.S)


def find_doc(data):
    """The first ADF node with "type": "doc" in a JSON value, or None."""
    if isinstance(data, dict):
        if data.get("type") == "doc":
            return data
        data = list(data.values())
    found = (find_doc(value) for value in data) if isinstance(data, list) else ()
    return next((doc for doc in found if doc is not None), None)


def detect(text: str) -> str:
    """Find the format of the raw text: adf, html, or md."""
    if text.lstrip().startswith(("{", "[")):
        try:
            if find_doc(json.loads(text)) is not None:
                return "adf"
        except ValueError:
            pass
    return "html" if HTML_TAG.search(text) else "md"


ADF_BLOCKS = {"paragraph", "heading", "codeBlock", "bulletList", "orderedList", "listItem",
              "blockquote", "panel", "table", "tableRow", "tableHeader", "tableCell"}


def adf_text(node) -> str:
    """The text of an ADF node. A hard break and a block end a line. Marks are ignored."""
    kind = node.get("type")
    if kind == "text":
        return node.get("text", "")
    if kind == "hardBreak":
        return "\n"
    if kind in ("mention", "emoji", "status", "date", "inlineCard"):
        attrs = node.get("attrs") or {}
        return str(attrs.get("text") or attrs.get("shortName") or attrs.get("url") or "")
    content = node.get("content") or []
    sep = "\n" if any(c.get("type") in ADF_BLOCKS for c in content) else ""
    return sep.join(adf_text(c) for c in content)


def adf_items(text: str) -> list:
    """Lines of Jira ADF JSON as (heading level, text). Level 0 is a line that is not a heading."""
    items = []

    def walk(node):
        kind = node.get("type")
        if kind == "heading":
            items.append((int((node.get("attrs") or {}).get("level", 1)), adf_text(node).replace("\n", " ")))
        elif kind in ("paragraph", "codeBlock"):
            items.extend((0, line) for line in adf_text(node).split("\n"))
        elif kind == "tableRow":
            cells = [adf_text(c).replace("\n", " ") for c in node.get("content") or []]
            items.append((0, "| " + " | ".join(cells) + " |"))
        else:
            for child in node.get("content") or []:
                walk(child)

    walk(find_doc(json.loads(text)) or {})
    return items


class _Html(HTMLParser):
    """Collect the lines of Azure DevOps HTML. A block tag and <br> end a line."""

    BLOCKS = {"p", "div", "li", "ul", "ol", "pre", "blockquote", "table", "thead", "tbody", "section",
              "article", "dl", "dt", "dd", "hr"}

    def __init__(self):
        super().__init__(convert_charrefs=True)
        self.items, self.buf, self.level, self.pre, self.skip, self.row, self.in_cell = [], [], 0, 0, 0, None, False

    def flush(self):
        text, self.buf = "".join(self.buf), []
        if self.level:
            self.items.append((self.level, text.replace("\n", " ")))
        else:
            self.items.extend((0, line) for line in text.split("\n"))

    def handle_starttag(self, tag, attrs):
        if tag in ("script", "style"):
            self.skip += 1
        elif self.in_cell:
            self.buf.append(" ")
        elif tag == "br":
            self.buf.append("\n")
        elif re.fullmatch(r"h[1-6]", tag):
            self.flush()
            self.level = int(tag[1])
        elif tag == "tr":
            self.flush()
            self.row = []
        elif tag in ("td", "th") and self.row is not None:
            self.buf, self.in_cell = [], True
        elif tag in self.BLOCKS:
            self.pre += tag == "pre"
            self.flush()

    def handle_endtag(self, tag):
        if tag in ("script", "style"):
            self.skip = max(0, self.skip - 1)
        elif tag in ("td", "th") and self.in_cell:
            self.row.append("".join(self.buf).replace("\n", " "))
            self.buf, self.in_cell = [], False
        elif tag == "tr" and self.row is not None:
            self.items.append((0, "| " + " | ".join(self.row) + " |"))
            self.row, self.buf = None, []
        elif self.in_cell:
            self.buf.append(" ")
        elif re.fullmatch(r"h[1-6]", tag):
            self.flush()
            self.level = 0
        elif tag in self.BLOCKS:
            self.flush()
            self.pre = max(0, self.pre - (tag == "pre"))

    def handle_data(self, data):
        if not self.skip:
            self.buf.append(data if self.pre and not self.in_cell else re.sub(r"[\r\n]+", " ", data))


def html_items(text: str) -> list:
    """Lines of Azure DevOps HTML as (heading level, text)."""
    parser = _Html()
    parser.feed(text)
    parser.close()
    parser.flush()
    return parser.items


def md_items(text: str) -> list:
    """Lines of Markdown as (heading level, text). A line in a code fence is never a heading."""
    items, fence = [], None
    for raw in text.splitlines():
        stripped = raw.strip()
        mark = re.match(r"^(`{3,}|~{3,})", stripped)
        if mark and (fence is None or stripped.startswith(fence)):
            fence = None if fence else mark.group(1)[0] * 3
            continue
        line = html.unescape(re.sub(r"\*\*|__|`", "", raw))
        heading = None if fence else re.match(r"^\s{0,3}(#{1,6})\s+(.*?)\s*#*\s*$", line)
        items.append((len(heading.group(1)), heading.group(2)) if heading else (0, line))
    return items


READERS = {"adf": adf_items, "html": html_items, "md": md_items}


def clean(line: str) -> str:
    """The canonical form of one line."""
    line = unicodedata.normalize("NFC", line).translate(QUOTES)
    line = BULLET.sub("", re.sub(r"\s+", " ", line).strip())
    if line.startswith("|"):
        if SEPARATOR.match(line):
            return ""
        cells = [c.strip() for c in line.strip().strip("|").split("|")]
        line = "| " + " | ".join(cells) + " |"
    return line


def section(items: list, whole: bool = False) -> list | None:
    """The canonical lines of the Behaviour spec section, or None when there is no section."""
    if whole:
        raw = [t for level, t in items if not (level and SECTION_NAME.match(clean(t)))]
    else:
        named = [(i, level) for i, (level, t) in enumerate(items) if SECTION_NAME.match(clean(t))]
        # Use a heading first. When the ticket has no heading, use a bold or plain line with the name.
        start, top = ([(i, lv) for i, lv in named if lv] or [(i, 99) for i, _ in named] or [(None, 0)])[0]
        if start is None:
            return None
        raw = []
        for level, t in items[start + 1:]:
            if level and level <= top:
                break
            raw.append(t)
    return [c for c in map(clean, raw) if c and not c.startswith("#")]


def read_spec(text: str, fmt: str = "auto", whole: bool = False) -> list | None:
    fmt = detect(text) if fmt == "auto" else fmt
    return section(READERS[fmt](text), whole)


def keyword(line: str):
    """The kind of a canonical line, and its text after the keyword."""
    for kind, pattern in KEYWORD:
        m = pattern.match(line)
        if m:
            return kind, m.group(1).strip()
    kind = "step" if STEP.match(line) else "row" if line[:1] == "|" else "tag" if line[:1] == "@" else "text"
    return kind, line


def parse(lines: list) -> dict:
    """Parse canonical lines. Returns the rules, the scenarios outside a rule, and an indented layout."""
    rules, loose, layout = [], [], []
    rule = scen = examples = None
    last, indent = None, 0
    for n, line in enumerate(lines, 1):
        kind, rest = keyword(line)
        if kind == "feature":
            rule = scen = examples = None
            indent = 0
        elif kind == "rule":
            m = RULE_ID.match(rest)
            limit = LIMIT.search(rest)
            title = LIMIT.sub("", m.group(2) if m else rest).strip()
            rule = {"id": m.group(1).upper() if m else None, "title": title, "line": n, "now": None,
                    "limit": limit.group(1).strip() if limit else None, "scenarios": [], "text": []}
            rules.append(rule)
            scen = examples = None
            indent = 2
        elif kind == "now":
            if rule:
                rule["now"] = rest
            indent = 4
        elif kind in ("background", "scenario", "outline"):
            scen = {"name": rest, "line": n, "types": set(), "outline": kind == "outline", "rows": 0,
                    "words": rest, "background": kind == "background"}
            examples, last = None, None
            indent = 4 if rule else 2
            if kind != "background":
                (rule["scenarios"] if rule else loose).append(scen)
        elif kind == "examples":
            examples = {"header": False}
            indent = (4 if rule else 2) + 2
        elif kind == "step" and scen:
            word = line.split(" ", 1)[0]
            last = STEP_TYPE.get(word, last)
            if last:
                scen["types"].add(last)
            indent = (4 if rule else 2) + 2
        elif kind == "row" and scen:
            if examples is not None:
                if examples["header"]:
                    scen["rows"] += 1
                    scen["words"] += " " + line
                examples["header"] = True
            indent = (4 if rule else 2) + 4
        layout.append((indent + (2 if kind in ("text", "tag") and indent else 0), line))
        if rule and kind not in ("feature", "text", "tag"):
            rule["text"].append(line)
    for r in rules:
        r["count"] = sum(s["rows"] if s["outline"] else 1 for s in r["scenarios"])
    return {"rules": rules, "loose": loose, "layout": layout}


def check(lines: list | None, kind: str = "story") -> tuple:
    """Check the spec. Returns (findings, parsed spec). Each finding is (line, id, message)."""
    if lines is None:
        return [(0, "SPEC-SECTION", 'No "Behaviour spec" section. Add the heading, or use --whole for a field with only the spec')], None
    spec = parse(lines)
    rules, found = spec["rules"], []
    if not rules:
        found.append((1, "SPEC-RULES", "The spec has no rules. Add a line \"Rule: R1 <title>\""))
    if len(rules) > 7:
        found.append((rules[7]["line"], "SPEC-RULES", f"The spec has {len(rules)} rules, the maximum is 7. Split the item"))
    seen = set()
    for r in rules:
        name = r["id"] or r["title"]
        if not r["id"]:
            found.append((r["line"], "SPEC-RULE-ID", f'The rule "{r["title"]}" has no ID. Use "Rule: R<n> <title>"'))
        elif r["id"] in seen:
            found.append((r["line"], "SPEC-RULE-ID", f"The rule ID {r['id']} is not unique"))
        seen.add(r["id"])
        if r["count"] == 0 or r["count"] > 4:
            found.append((r["line"], "SPEC-SCENARIOS", f"Rule {name} has {r['count']} scenarios. Give it 1 to 4"))
        if r["limit"] is not None:
            words = " ".join(s["words"] for s in r["scenarios"]).lower()
            missing = [case for case, names in LIMIT_WORDS.items()
                       if not any(re.search(rf"\b{w}\b", words) for w in names)]
            if missing:
                found.append((r["line"], "SPEC-LIMIT", f"Rule {name} has a limit, but no scenario: {', '.join(missing)}"))
    found += [(s["line"], "SPEC-RULE-ID", f'The scenario "{s["name"]}" is not in a Rule') for s in spec["loose"]]
    for s in spec["loose"] + [s for r in rules for s in r["scenarios"]]:
        missing = [t.capitalize() for t in ("when", "then") if t not in s["types"]]
        if missing:
            found.append((s["line"], "SPEC-STEPS", f'The scenario "{s["name"]}" has no {" and no ".join(missing)} step'))
    if kind == "bug":
        if not any(r["now"] for r in rules):
            found.append((1, "SPEC-BUG-NOW", 'A bug spec needs a "Now:" line with the current behaviour under the broken rule'))
        if not any(UNCHANGED.match(r["title"]) and r["count"] for r in rules):
            found.append((1, "SPEC-BUG-UNCHANGED", 'A bug spec needs a rule "Unchanged" with the scenarios that must stay green'))
    return sorted(found, key=lambda f: (f[0], f[1])), spec


def sha(lines: list) -> str:
    return hashlib.sha256("\n".join(lines).encode("utf-8")).hexdigest()


def fingerprints(lines: list, spec: dict) -> dict:
    return {"section": sha(lines),
            "rules": [{"id": r["id"], "title": r["title"], "hash": sha(r["text"])} for r in spec["rules"]]}


def write_spec_md(path: pathlib.Path, key: str, spec: dict) -> None:
    """Write the spec block into spec.md. Keep each other part of the file."""
    body = "\n".join(" " * indent + line for indent, line in spec["layout"])
    block = f"{BEGIN.format(key)}\n```gherkin\n{body}\n```\n{END}"
    text = path.read_text(encoding="utf-8") if path.exists() else f"# Spec: {key}\n"
    if BLOCK.search(text):
        text = BLOCK.sub(lambda _: block, text, count=1)
    else:
        text = text.rstrip("\n") + "\n\n" + block + "\n"
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(text, encoding="utf-8")


def block_rules(path: pathlib.Path) -> dict:
    """The rule text of each rule in the spec.md block: {id: [lines]}. Empty when there is no block."""
    m = BLOCK.search(path.read_text(encoding="utf-8")) if path.exists() else None
    if not m:
        return {}
    lines = [clean(t) for t in m.group(0).splitlines()[1:-1] if not t.strip().startswith("```")]
    return {r["id"]: r["text"] for r in parse([t for t in lines if t])["rules"]}


def compare(lines: list, baseline: dict, old_text: dict) -> dict:
    """Compare a new copy with the baseline. Returns the changed, added, and removed rules."""
    spec = parse(lines)
    new = {r["id"]: r for r in spec["rules"]}
    old = {r["id"]: r for r in baseline.get("rules", [])}
    out = {"changed": [], "added": [], "removed": [], "section_changed": False}

    def entry(rid, title, before, after):
        diff = difflib.unified_diff(before, after, f"baseline {rid}", f"ticket {rid}", lineterm="")
        return {"id": rid, "title": title, "diff": "\n".join(diff)}

    for rid, r in new.items():
        if rid not in old:
            out["added"].append(entry(rid, r["title"], [], r["text"]))
        elif sha(r["text"]) != old[rid]["hash"]:
            out["changed"].append(entry(rid, r["title"], old_text.get(rid, []), r["text"]))
    for rid, r in old.items():
        if rid not in new:
            out["removed"].append(entry(rid, r["title"], old_text.get(rid, []), []))
    rules_changed = bool(out["changed"] or out["added"] or out["removed"])
    out["section_changed"] = not rules_changed and sha(lines) != baseline.get("section")
    out["any"] = rules_changed or out["section_changed"]
    return out


def load(args) -> list | None:
    text = pathlib.Path(args.file).read_text(encoding="utf-8-sig")
    return read_spec(text, args.source, args.whole)


def print_findings(found: list, spec: dict | None, fmt: str) -> None:
    rules = spec["rules"] if spec else []
    if fmt == "json":
        print(json.dumps({"errors": [{"line": n, "id": i, "message": m} for n, i, m in found],
                          "rules": [{"id": r["id"], "title": r["title"], "scenarios": r["count"],
                                     "limit": r["limit"], "now": r["now"]} for r in rules]}, indent=2))
        return
    for n, i, m in found:
        print(f"ERROR line {n} {i}: {m}")
    print(f"specsync: {len(rules)} rules, {sum(r['count'] for r in rules)} scenarios, {len(found)} errors")


def cmd_check(args) -> int:
    found, spec = check(load(args), args.kind)
    print_findings(found, spec, args.format)
    return 1 if found else 0


def cmd_copy(args) -> int:
    lines = load(args)
    found, spec = check(lines, args.kind)
    if found:
        print_findings(found, spec, "text")
        print("specsync copy: refused. Fix the errors on the ticket first.")
        return 1
    out = pathlib.Path(args.out)
    write_spec_md(out, args.item, spec)
    base = {"item": args.item, "kind": args.kind,
            "time": datetime.datetime.now(datetime.timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ"),
            **fingerprints(lines, spec)}
    (out.parent / "spec-baseline.json").write_text(json.dumps(base, indent=2) + "\n", encoding="utf-8")
    print(f"specsync copy: {len(spec['rules'])} rules written to {out} and {out.parent / 'spec-baseline.json'}")
    return 0


def cmd_diff(args) -> int:
    path = pathlib.Path(args.baseline)
    path = path / "spec-baseline.json" if path.is_dir() else path
    baseline = json.loads(path.read_text(encoding="utf-8"))
    lines = load(args)
    if lines is None:
        print('ERROR SPEC-SECTION: No "Behaviour spec" section in the new copy')
        return 2
    out = compare(lines, baseline, block_rules(path.parent / "spec.md"))
    if args.format == "json":
        print(json.dumps(out, indent=2))
    else:
        for state in ("changed", "added", "removed"):
            for e in out[state]:
                print(f"{state.upper()} {e['id']} {e['title']}")
                print(e["diff"] or "(spec.md has no text for this rule)")
        if out["section_changed"]:
            print("CHANGED text of the section outside the rules")
        counts = ", ".join(f"{len(out[s])} {s}" for s in ("changed", "added", "removed"))
        print(f"specsync diff: {counts}" if out["any"] else "specsync diff: no change")
    return 1 if out["any"] else 0


def main(argv=None) -> int:
    ap = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    sub = ap.add_subparsers(dest="command", required=True)
    for name in ("check", "copy", "diff"):
        p = sub.add_parser(name)
        p.add_argument("file", help="the raw text of the ticket field")
        p.add_argument("--from", dest="source", choices=("adf", "html", "md", "auto"), default="auto")
        p.add_argument("--whole", action="store_true", help="take all the text as the section")
        if name == "diff":
            p.add_argument("--baseline", required=True, help="a folder or spec-baseline.json")
        else:
            p.add_argument("--kind", choices=("story", "bug"), default="story")
        if name == "copy":
            p.add_argument("--item", required=True)
            p.add_argument("--out", required=True, help="docs/work/<item>/spec.md")
        else:
            p.add_argument("--format", choices=("text", "json"), default="text")
    args = ap.parse_args(argv)
    return {"check": cmd_check, "copy": cmd_copy, "diff": cmd_diff}[args.command](args)


if __name__ == "__main__":
    sys.exit(main())
