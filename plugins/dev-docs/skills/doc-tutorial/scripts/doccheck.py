#!/usr/bin/env python3
"""doccheck: count-based checks for clear technical text.

The checks follow the ASD-STE100 writing rules (English) and
Begrijpelijk Nederlands B1 (Dutch). The script only reports. It does not
change the text. It uses the Python standard library only.

Usage:
    python3 doccheck.py [--lang auto|en|nl] [--level light|standard|strict]
                        [--format text|json] [--source ORIGINAL] [--comments] [FILE ...]

With --source, the script also compares the rewrite with the original and
reports lost code lines, lost link targets, and changed heading anchors.
With --comments, the files are source code. The script checks the comments
that start a line, and reports the line numbers of the source file.

With no FILE, the script reads stdin. Exit code 1 means one or more errors.
"""
from __future__ import annotations

import argparse
import json
import re
import sys
from collections import Counter
from dataclasses import asdict, dataclass

# Limits ---------------------------------------------------------------
MAX_WORDS = {"en": {"procedural": 20, "descriptive": 25}, "nl": {"procedural": 20, "descriptive": 20}}
MAX_PARAGRAPH_SENTENCES = 6

# Rule IDs. The labels are our own words. STE numbers refer to ASD-STE100 Issue 9.
RULES = {
    "STE-5.1": "Procedural sentence is too long",
    "STE-6.3": "Descriptive sentence is too long",
    "B1-LEN": "Sentence is too long for B1",
    "STE-6.6": "Paragraph has too many sentences",
    "STE-8.1": "Semicolon",
    "HOUSE-DASH": "Em-dash or dash used as a pause",
    "STE-4.2": "Contraction",
    "STE-1.1": "Word that STE does not approve (modal verb)",
    "STE-3.6": "Possible passive voice",
    "STE-3.5": "Possible -ing form after a comma",
    "B1-PASSIVE": "Mogelijke lijdende vorm",
    "HOUSE-WORDS": "Filler word or formal word with a simple alternative",
    "KEEP-CODE": "Rewrite lost or changed a code block",
    "KEEP-LINK": "Rewrite lost a link target",
    "KEEP-ANCHOR": "Rewrite changed a heading anchor",
}

# Word lists (our own, not taken from the STE dictionary) --------------
EN_MODALS = {"should", "may", "might", "would", "shall"}
EN_CONTRACTION = re.compile(
    r"\b(\w+n['’]t|(it|that|there|what|here|let|he|she|who|where)['’]s|\w+['’](re|ve|ll|d|m))\b",
    re.IGNORECASE,
)
EN_IRREGULAR_PARTICIPLES = {
    "built", "chosen", "done", "driven", "found", "given", "held", "hidden", "kept",
    "known", "made", "put", "read", "seen", "sent", "set", "shown", "taken", "thrown",
    "written", "broken", "begun", "bound", "brought", "caught", "fed", "left", "lost",
    "meant", "paid", "run", "said", "sold", "spent", "split", "told", "thought", "understood",
}
EN_BE = r"(?:is|are|was|were|be|been|being)"
EN_PASSIVE = re.compile(rf"\b{EN_BE}\s+(?:\w+ly\s+)?(\w+ed|{'|'.join(sorted(EN_IRREGULAR_PARTICIPLES))})\b", re.IGNORECASE)
EN_ING_AFTER_COMMA = re.compile(r",\s+(\w+ing)\b")
EN_ING_ALLOW = {"including", "according", "during", "following", "something", "nothing", "anything", "everything", "string", "thing", "king", "ring", "spring"}
EN_HOUSE_WORDS = [
    "utilize", "utilise", "leverage", "in order to", "prior to", "ensure", "simply", "just",
    "easily", "obviously", "seamless", "seamlessly", "robust", "powerful", "comprehensive",
    "it is worth noting", "please note", "basically", "very",
]
NL_PASSIVE_AUX = re.compile(r"\b(word|wordt|worden|werd|werden)\b", re.IGNORECASE)
NL_PARTICIPLE = re.compile(r"\bge\w{2,}(?:d|t|en)\b", re.IGNORECASE)
NL_PREFIX_PARTICIPLE = re.compile(r"^(?:be|ver|her|ont|er)\w{2,}(?:d|t|en)$", re.IGNORECASE)
NL_HOUSE_WORDS = [
    "teneinde", "middels", "dient te", "dienen te", "in het kader van", "ten behoeve van",
    "betreffende", "inzake", "alsmede", "reeds", "omtrent", "derhalve", "dan wel",
    "ten aanzien van", "met betrekking tot", "gelieve", "simpelweg", "gewoon",
]

EN_MARKERS = {"the", "a", "an", "and", "of", "to", "is", "you", "for", "with", "on", "that", "this", "it", "are", "in"}
NL_MARKERS = {"de", "het", "een", "en", "van", "je", "is", "niet", "voor", "met", "op", "dat", "die", "zijn", "u", "in"}

ABBREVIATIONS = {"e.g.", "i.e.", "vs.", "cf.", "bijv.", "bv.", "o.a.", "d.w.z.", "m.b.t.", "i.p.v.", "z.o.z.", "nr.", "no."}


@dataclass
class Finding:
    file: str
    line: int
    severity: str  # error | warning
    rule: str
    message: str
    text: str


@dataclass
class Unit:
    """A paragraph or a list item, with the line where it starts."""
    line: int
    text: str
    kind: str  # procedural | descriptive


# Markdown handling ------------------------------------------------------
ORDERED_ITEM = re.compile(r"^\s*\d+[.)]\s+")
BULLET_ITEM = re.compile(r"^\s*[-*+]\s+")
HEADING = re.compile(r"^\s{0,3}#{1,6}\s")
FENCE = re.compile(r"^\s*(`{3,}|~{3,})")


def fence_marker(raw: str) -> tuple[str, int] | None:
    """The fence character and length of a fence line, or None."""
    m = FENCE.match(raw)
    return (m.group(1)[0], len(m.group(1))) if m else None


def closes(marker: tuple[str, int], fence: tuple[str, int], raw: str) -> bool:
    """A closing fence has the same character, at least the same length, and no info text."""
    return marker[0] == fence[0] and marker[1] >= fence[1] and not raw.strip().lstrip(fence[0]).strip()

TABLE_RULE = re.compile(r"^\|[\s:|-]+\|?$")
VERSION_HEADING = re.compile(r"^\s*#+\s*\[[^\]]+\]\s+-\s+\d{4}-\d{2}-\d{2}")  # Keep a Changelog


def mask_inline(text: str) -> str:
    """Replace items that count as one word (STE rule 8.6) with one token."""
    text = re.sub(r"`[^`]+`", " CODE ", text)
    text = re.sub(r"\{:[^}]*\}", " ", text)  # kramdown attributes
    text = re.sub(r"!\[[^\]]*\]\([^)]*\)", " IMAGE ", text)
    text = re.sub(r"\[[^\]]+\]\([^)]*\)", " LINK ", text)
    text = re.sub(r"https?://\S+", " URL ", text)
    # A short quoted label counts as one word. A long quote counts as its words.
    text = re.sub(r"\"([^\"]{1,80})\"|“([^”]{1,80})”",
                  lambda m: " QUOTE " if len((m.group(1) or m.group(2)).split()) <= 4
                  else " " + (m.group(1) or m.group(2)) + " ",
                  text)
    text = re.sub(r"<!--.*?-->", " ", text)
    text = re.sub(r"\{\{[^}]*\}\}", " VAR ", text)
    text = re.sub(r"[*_]{1,3}", "", text)
    return text


def units_from_markdown(source: str) -> tuple[list[Unit], list[tuple[int, str]]]:
    """Split Markdown into prose units. Also return all non-code lines for punctuation checks."""
    lines = source.splitlines()
    units: list[Unit] = []
    prose_lines: list[tuple[int, str]] = []
    in_fence = False
    in_front_matter = bool(lines) and lines[0].strip() == "---"
    buf: list[str] = []
    buf_start = 0
    buf_kind = "descriptive"

    def flush() -> None:
        nonlocal buf
        if buf:
            units.append(Unit(buf_start, " ".join(s.strip() for s in buf), buf_kind))
            buf = []

    fence: tuple[str, int] | None = None   # (character, length) of the open fence
    in_list = False
    prev_blank = True
    in_indented_code = False
    for i, raw in enumerate(lines, start=1):
        if in_front_matter:
            if i > 1 and raw.strip() == "---":
                in_front_matter = False
            continue
        marker = fence_marker(raw)
        if fence is None and marker:
            flush()
            fence = marker
            continue
        if fence is not None:
            if marker and closes(marker, fence, raw):
                fence = None
            continue
        stripped = raw.strip()
        indented = raw.startswith("    ") or raw.startswith("\t")
        if stripped and indented and (in_indented_code or (prev_blank and not in_list and not buf)):
            in_indented_code = True   # an indented code block
            prev_blank = False
            continue
        if stripped and not indented:
            in_indented_code = False
        prev_blank = not stripped
        if stripped and not indented and not (ORDERED_ITEM.match(raw) or BULLET_ITEM.match(raw)):
            in_list = False
        if ORDERED_ITEM.match(raw) or BULLET_ITEM.match(raw):
            in_list = True
        if stripped.startswith(">"):
            # A blockquote is prose (notes, warnings). Check it as its own paragraph.
            prose_lines.append((i, stripped))
            text = stripped.lstrip("> ").strip()
            if text:
                if not buf or buf_kind != "quote":
                    flush()
                    buf_start, buf_kind = i, "quote"
                buf.append(text)
            else:
                flush()
            continue
        if buf_kind == "quote":
            flush()
            buf_kind = "descriptive"
        if stripped.startswith("|"):
            # A table row: check each cell as a short descriptive text.
            flush()
            if not TABLE_RULE.match(stripped):
                # A cell with only "-" is an empty value, not a dash.
                cells = [c for c in re.split(r"(?<!\\)\|", stripped.strip("|")) if c.strip() != "-"]
                prose_lines.append((i, "| " + " | ".join(c.strip() for c in cells) + " |"))
                for cell in cells:
                    if cell.strip():
                        units.append(Unit(i, cell.strip(), "descriptive"))
            continue
        if not stripped or stripped.startswith("<"):
            flush()
            continue
        prose_lines.append((i, raw))
        if HEADING.match(raw):
            flush()
            continue
        if ORDERED_ITEM.match(raw) or BULLET_ITEM.match(raw):
            flush()
            buf_start, buf_kind = i, ("procedural" if ORDERED_ITEM.match(raw) else "descriptive")
            buf = [ORDERED_ITEM.sub("", BULLET_ITEM.sub("", raw))]
            continue
        if not buf:
            buf_start, buf_kind = i, "descriptive"
        buf.append(raw)
    flush()
    # A quote is descriptive text for the limits.
    for u in units:
        if u.kind == "quote":
            u.kind = "descriptive"
    return units, prose_lines


def split_sentences(text: str) -> list[str]:
    tokens = text.split()
    sentences: list[str] = []
    current: list[str] = []
    for idx, tok in enumerate(tokens):
        current.append(tok)
        nxt = tokens[idx + 1] if idx + 1 < len(tokens) else ""
        end = tok.endswith((".", "!", "?", ".)", "?)", "!)"))
        if end and tok.lower() not in ABBREVIATIONS and not re.fullmatch(r"\d+\.", tok):
            if not nxt or nxt[:1].isupper() or nxt[:1] in "(\"'`" or nxt[:1].isdigit():
                sentences.append(" ".join(current))
                current = []
    if current:
        sentences.append(" ".join(current))
    return [s for s in sentences if s.strip()]


def count_words(sentence: str) -> int:
    return len([w for w in sentence.split() if re.search(r"\w", w)])


def detect_lang(text: str) -> str:
    words = re.findall(r"[a-zA-Z]+", text.lower())
    en = sum(w in EN_MARKERS for w in words)
    nl = sum(w in NL_MARKERS for w in words)
    return "nl" if nl > en else "en"


def nl_passive(sentence: str) -> bool:
    """A form of 'worden' near a participle. Prefixed participles (bekeken) must be within 3 words."""
    words = re.findall(r"\w+", sentence)
    for i, w in enumerate(words):
        if NL_PASSIVE_AUX.fullmatch(w):
            if NL_PARTICIPLE.search(sentence):
                return True
            window = words[max(0, i - 3): i] + words[i + 1: i + 4]
            if any(NL_PREFIX_PARTICIPLE.match(x) for x in window):
                return True
    return False


# Checks ------------------------------------------------------------------
def check(source: str, name: str, lang: str, level: str) -> tuple[list[Finding], dict]:
    units, prose_lines = units_from_markdown(source)
    if lang == "auto":
        lang = detect_lang(" ".join(u.text for u in units))
    findings: list[Finding] = []
    lengths: list[int] = []

    def add(line: int, severity: str, rule: str, message: str, text: str) -> None:
        findings.append(Finding(name, line, severity, rule, message, text[:90]))

    # Punctuation on all prose lines (headings included).
    for line_no, raw in prose_lines:
        visible = re.sub(r"`[^`]+`", "", BULLET_ITEM.sub("", ORDERED_ITEM.sub("", raw)))
        if ";" in visible and not re.search(r"&\w+;", visible):
            add(line_no, "error", "STE-8.1", "Use two sentences or a vertical list.", raw.strip())
        if VERSION_HEADING.match(raw):
            visible = re.sub(r"\s-\s", " ", visible, count=1)
        if "—" in visible or re.search(r"\s(?:–|--?)\s", visible):
            add(line_no, "error", "HOUSE-DASH", "Use a comma, a colon, parentheses or two sentences.", raw.strip())

    for unit in units:
        masked = mask_inline(unit.text)
        sentences = split_sentences(masked)
        if unit.kind == "descriptive" and len(sentences) > MAX_PARAGRAPH_SENTENCES:
            add(unit.line, "error", "STE-6.6", f"Paragraph has {len(sentences)} sentences (max {MAX_PARAGRAPH_SENTENCES}).", unit.text)
        for s in sentences:
            n = count_words(s)
            lengths.append(n)
            limit = MAX_WORDS[lang][unit.kind]
            if n > limit:
                rule = "B1-LEN" if lang == "nl" else ("STE-5.1" if unit.kind == "procedural" else "STE-6.3")
                add(unit.line, "error", rule, f"Sentence has {n} words (max {limit}).", s)
            if level == "light":
                continue
            low = s.lower()
            if lang == "en":
                for m in EN_CONTRACTION.finditer(s):
                    add(unit.line, "error", "STE-4.2", f"Write '{m.group(0)}' in full.", s)
                for w in re.findall(r"\b[a-z]+\b", low):
                    if w in EN_MODALS:
                        sev = "error" if level == "strict" else "warning"
                        add(unit.line, sev, "STE-1.1", f"'{w}': use 'must', 'can', or a statement of fact.", s)
                for m in EN_PASSIVE.finditer(s):
                    add(unit.line, "warning", "STE-3.6", f"'{m.group(0)}': name who does the action.", s)
                for m in EN_ING_AFTER_COMMA.finditer(s):
                    if m.group(1).lower() not in EN_ING_ALLOW:
                        add(unit.line, "warning", "STE-3.5", f"', {m.group(1)}': start a new sentence.", s)
                for phrase in EN_HOUSE_WORDS:
                    if re.search(rf"\b{re.escape(phrase)}\b", low):
                        add(unit.line, "warning", "HOUSE-WORDS", f"'{phrase}': use a simple word or delete it.", s)
            else:
                if nl_passive(s):
                    add(unit.line, "warning", "B1-PASSIVE", "Schrijf actief: noem wie iets doet.", s)
                for phrase in NL_HOUSE_WORDS:
                    if re.search(rf"\b{re.escape(phrase)}\b", low):
                        add(unit.line, "warning", "HOUSE-WORDS", f"'{phrase}': gebruik een eenvoudig woord.", s)

    metrics = {
        "language": lang,
        "level": level,
        "sentences": len(lengths),
        "avg_words_per_sentence": round(sum(lengths) / len(lengths), 1) if lengths else 0,
        "max_words_per_sentence": max(lengths) if lengths else 0,
        "errors": sum(f.severity == "error" for f in findings),
        "warnings": sum(f.severity == "warning" for f in findings),
    }
    return findings, metrics


# Code comments -----------------------------------------------------------------
C_LANGS = {".c", ".h", ".cc", ".cpp", ".hpp", ".cs", ".java", ".js", ".jsx", ".mjs", ".cjs", ".ts",
           ".tsx", ".go", ".rs", ".swift", ".kt", ".kts", ".scala", ".dart", ".php", ".groovy",
           ".css", ".scss", ".less"}
HASH_LANGS = {".py", ".sh", ".bash", ".zsh", ".rb", ".pl", ".r", ".ps1", ".yaml", ".yml", ".toml", ".tf"}
# XML doc tags, and doc tags such as "@param name -" (the TSDoc hyphen is part of the tag).
DOC_TAG = re.compile(r"</?\w+[^>]*>|@\w+(?:\s+\{[^}]*\})?(?:\s+[\w.$\[\]]+\s+-(?=\s|$))?")
DOCSTRING = re.compile(r'^[rRuUbB]?("""|\'\'\')')


def strip_trailing_comment(line: str, hash_lang: bool) -> str:
    """Remove a comment at the end of a code line, when it is not inside a string."""
    marker = r"\s#" if hash_lang else r"\s//"
    for m in re.finditer(marker, line):
        before = line[: m.start()]
        if before.count('"') % 2 == 0 and before.count("'") % 2 == 0:
            return before.rstrip()
    return line.rstrip()


def extract_comments(code: str, suffix: str) -> tuple[str, dict[int, int], list[tuple[int, str]]]:
    """Turn the comments of a source file into Markdown paragraphs.

    Reads the comments that start a line, block comments, and Python docstrings.
    Indented lines and doctest lines in a comment are code examples and go into a
    fence. Returns the Markdown, a map from Markdown line to source line, and the
    code lines (without comments) for a rewrite compare.
    """
    hash_lang = suffix.lower() in HASH_LANGS
    out: list[str] = []
    line_map: dict[int, int] = {}
    code_lines: list[tuple[int, str]] = []
    in_block = False                 # /* ... */
    doc_quote: str | None = None     # open docstring quote
    doc_indent = 0
    in_example = False
    last_code = ""                   # the last code line, for docstring detection

    def emit(text: str, src_line: int) -> None:
        nonlocal in_example
        example = text[:1] in (" ", "\t") or text.startswith((">>>", "... "))
        if example != in_example:
            out.append("```")
            in_example = example
        out.append(text.rstrip() if example else DOC_TAG.sub("", text).strip())
        line_map[len(out)] = src_line

    def end_paragraph() -> None:
        nonlocal in_example
        if in_example:
            out.append("```")
            in_example = False
        if out and out[-1] != "":
            out.append("")

    for n, raw in enumerate(code.splitlines(), start=1):
        s = raw.strip()
        if doc_quote:
            body = raw[doc_indent:] if not raw[:doc_indent].strip() else s
            end = doc_quote in body
            text = body.split(doc_quote)[0]
            if text.strip():
                emit(text, n)
            elif not end:
                end_paragraph()
            if end:
                doc_quote = None
                end_paragraph()
            continue
        if in_block:
            end = "*/" in s
            text = re.sub(r"^\* ?", "", raw.lstrip().split("*/")[0])
            if text.strip():
                emit(text, n)
            elif not end:
                end_paragraph()
            if end:
                in_block = False
                end_paragraph()
            continue
        docstring = DOCSTRING.match(s)
        if hash_lang and docstring and (not last_code or re.match(r"(async\s+)?(def|class)\b.*:$", last_code)):
            quote = docstring.group(1)
            body = s[docstring.end():]
            if quote in body:
                emit(body.split(quote)[0], n)
                end_paragraph()
            else:
                doc_quote, doc_indent = quote, len(raw) - len(raw.lstrip())
                if body.strip():
                    emit(body, n)
            continue
        if hash_lang and s.startswith("#") and not s.startswith("#!"):
            text = re.sub(r"^#+ ?", "", raw.lstrip())
        elif not hash_lang and s.startswith("//"):
            text = re.sub(r"^/{2,3}!? ?", "", raw.lstrip())
        elif not hash_lang and s.startswith("/*"):
            body = re.sub(r"^/\*+!? ?", "", s)
            if "*/" in body:
                emit(body.split("*/")[0], n)
                end_paragraph()
            else:
                in_block = True
                if body.strip():
                    emit(body, n)
            continue
        else:
            end_paragraph()
            if s:
                last_code = strip_trailing_comment(s, hash_lang)
                code_lines.append((n, " ".join(last_code.split())))
            continue
        if text.strip():
            emit(text, n)
        else:
            end_paragraph()
    end_paragraph()
    return "\n".join(out), line_map, code_lines


def compare_code(original: str, rewrite: str, suffix: str, name: str) -> list[Finding]:
    """Comment rewrite: report each code line that the rewrite removed, changed, or added."""
    before = [c for _, c in extract_comments(original, suffix)[2]]
    after = extract_comments(rewrite, suffix)[2]
    out: list[Finding] = []
    remaining = Counter(before)
    for n, line in after:
        if remaining[line] > 0:
            remaining[line] -= 1
        else:
            out.append(Finding(name, n, "error", "KEEP-CODE", "A code line is new or changed. Change only comments.", line[:90]))
    for line, count in remaining.items():
        for _ in range(count):
            out.append(Finding(name, 0, "error", "KEEP-CODE", "A code line of the source is missing.", line[:90]))
    return out


# Rewrite comparison ----------------------------------------------------------
LINK_TARGET = re.compile(r"\]\(([^)\s]+)[^)]*\)|(https?://[^\s)>\"'`]+)|^\s*\[[^\]]+\]:\s*(\S+)")


def slug(heading: str) -> str:
    """GitHub-style anchor of a heading."""
    text = EXPLICIT_ID.sub("", heading.strip().lstrip("#")).strip()
    text = re.sub(r"`|\*", "", text).lower()
    text = re.sub(r"[^\w\- ]", "", text)
    return text.replace(" ", "-")


PROMPT = re.compile(r"^(?:\u25b6|\$|PS>|>)\s+")
EXPLICIT_ID = re.compile(r"\{#([\w\-]+)\}\s*$")


def code_line(line: str) -> str:
    """A code line without the shell prompt and without extra spaces."""
    return " ".join(PROMPT.sub("", line.strip()).split())


def facts(source: str) -> dict:
    """The parts of a document that a rewrite must keep."""
    blocks: list[list[str]] = []
    links, anchors = set(), set()
    block: list[str] | None = None
    fence: tuple[str, int] | None = None
    for raw in source.splitlines():
        marker = fence_marker(raw)
        if fence is None and marker:
            fence, block = marker, []
            continue
        if fence is not None:
            if marker and closes(marker, fence, raw):
                lines = [code_line(x) for x in block if code_line(x)]
                if lines:
                    blocks.append(lines)
                fence, block = None, None
            else:
                block.append(raw)
            continue
        for m in LINK_TARGET.finditer(raw):
            target = (m.group(1) or m.group(2) or m.group(3)).rstrip(".,*_")
            if not target.startswith("#"):
                links.add(target)
        if HEADING.match(raw) and not raw.lstrip().startswith("# "):
            # H1 is the page title, other pages link to the file.
            explicit = EXPLICIT_ID.search(raw)
            anchors.add(explicit.group(1) if explicit else slug(raw))
        elif HEADING.match(raw) and EXPLICIT_ID.search(raw):
            anchors.add(EXPLICIT_ID.search(raw).group(1))
    return {"blocks": blocks, "links": links, "anchors": anchors}


HTML_COMMENT = re.compile(r"<!--.*?-->", re.DOTALL)


def compare(original: str, rewrite: str, name: str) -> list[Finding]:
    """Report what a rewrite lost: code lines, link targets, heading anchors.

    HTML comments are not visible content, so the compare ignores them on both sides.
    """
    before, after = facts(HTML_COMMENT.sub("", original)), facts(HTML_COMMENT.sub("", rewrite))
    out: list[Finding] = []
    kept_lines = {line for block in after["blocks"] for line in block}
    for block in before["blocks"]:
        missing = [line for line in block if line not in kept_lines]
        if missing:
            out.append(Finding(name, 0, "error", "KEEP-CODE",
                               f"{len(missing)} line(s) of a code block are missing or changed.", missing[0][:90]))
    for link in sorted(before["links"] - after["links"]):
        out.append(Finding(name, 0, "error", "KEEP-LINK", "A link target of the source is missing.", link[:90]))
    for anchor in sorted(before["anchors"] - after["anchors"]):
        out.append(Finding(name, 0, "warning", "KEEP-ANCHOR", "A heading anchor changed. Links to it can break.", "#" + anchor))
    return out


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("files", nargs="*")
    parser.add_argument("--lang", choices=["auto", "en", "nl"], default="auto")
    parser.add_argument("--level", choices=["light", "standard", "strict"], default="standard")
    parser.add_argument("--format", choices=["text", "json"], default="text")
    parser.add_argument("--source", help="rewrite mode: the original document. Reports lost code, links, and anchors.")
    parser.add_argument("--comments", action="store_true",
                        help="the files are source code: check the comments (always English). "
                             "With --source, report each code line that changed.")
    args = parser.parse_args(argv)

    try:
        inputs = [(f, open(f, encoding="utf-8").read()) for f in args.files] or [("<stdin>", sys.stdin.read())]
        original = open(args.source, encoding="utf-8").read() if args.source else None
    except OSError as e:
        print(f"doccheck: cannot read {e.filename}: {e.strerror}", file=sys.stderr)
        return 2
    all_findings: list[Finding] = []
    report = []
    for name, source in inputs:
        suffix = "." + name.rsplit(".", 1)[-1].lower() if "." in name else ""
        if args.comments:
            if suffix not in C_LANGS | HASH_LANGS:
                print(f"doccheck: --comments does not know the comment style of '{name}'. "
                      f"Known: {' '.join(sorted(C_LANGS | HASH_LANGS))}", file=sys.stderr)
                return 2
            text, line_map, _ = extract_comments(source, suffix)
            findings, metrics = check(text, name, "en", args.level)
            for f in findings:
                f.line = line_map.get(f.line, f.line)
        else:
            findings, metrics = check(source, name, args.lang, args.level)
        if original is not None:
            kept = compare_code(original, source, suffix, name) if args.comments else compare(original, source, name)
            findings += kept
            metrics["errors"] += sum(f.severity == "error" for f in kept)
            metrics["warnings"] += sum(f.severity == "warning" for f in kept)
        all_findings += findings
        report.append({"file": name, "metrics": metrics, "findings": [asdict(f) for f in findings]})

    if args.format == "json":
        print(json.dumps(report, indent=2, ensure_ascii=False))
    else:
        for entry in report:
            for f in entry["findings"]:
                print(f"{f['file']}:{f['line']}: {f['severity'].upper()} {f['rule']} {f['message']} | {f['text']}")
            m = entry["metrics"]
            print(
                f"{entry['file']}: {m['errors']} errors, {m['warnings']} warnings | "
                f"{m['sentences']} sentences, avg {m['avg_words_per_sentence']} words, max {m['max_words_per_sentence']} "
                f"| lang={m['language']} level={m['level']}"
            )
    return 1 if any(f.severity == "error" for f in all_findings) else 0


if __name__ == "__main__":
    sys.exit(main())
