#!/usr/bin/env python3
"""Check backlog items (user stories) in a Markdown file.

    python3 backlogcheck.py <file.md> [<file.md> ...] [--json]

An item starts at a heading line (## or #) and has a "**Type:**" line in
its block. The checker finds the rules of references/story-rules.md that a
script can decide. English and Dutch items are supported.

Output: one line for each finding, then one summary line for each file:
    <file>: <n> errors, <n> warnings | <n> items
Exit code: 1 when a file has an error, else 0.
"""
import argparse
import json
import pathlib
import re
import sys

TITLE_MAX = 80          # T1: a longer title is hard to scan on a board
TITLE_HARD_MAX = 255    # Jira and Azure DevOps reject a longer title
CRITERIA_MAX = 8        # A7: more criteria means the story is too large
CRITERIA_MIN = 1        # A1

STORY_TYPES = {"user story", "story", "product backlog item", "pbi", "requirement", "issue"}
SPECIAL_TYPES = {"spike", "enabler"}

STORY_EN = re.compile(r"\bas an? (?P<role>.+?),? i want (?P<goal>.+?)(?:,? so that (?P<value>.+))?$", re.I)
STORY_NL = re.compile(r"\bals (?P<role>.+?),? wil ik (?P<goal>.+?)(?:,? zodat (?P<value>.+))?$", re.I)
GENERIC_ROLES = {"user", "a user", "an user", "gebruiker", "een gebruiker", "de gebruiker", "the user",
                 "person", "someone", "iemand", "end user", "eindgebruiker"}  # too wide, see S1
LAYER_WORDS = re.compile(r"\b(api|database|db|backend|back-end|frontend|front-end|endpoint|table|tabel|"
                         r"service layer|migration|migratie|schema)\b", re.I)
VAGUE_WORDS = re.compile(r"\b(fast|quick|quickly|easy|easily|user-friendly|intuitive|simple|robust|clear|clearly|"
                         r"short|main|relevant|appropriate|etc|"
                         r"snel|makkelijk|eenvoudig|gebruiksvriendelijk|intuïtief|duidelijk|duidelijke|kort|korte|"
                         r"belangrijkste|relevant|relevante|passend|enz)\b\.?", re.I)
# S1: a qualifier before "user" does not make the role specific ("signed-in user", "app user").
GENERIC_SUFFIX = re.compile(r"^(?:[\w-]+\s+){0,2}(user|gebruiker)s?$", re.I)
EDGE_WORDS = re.compile(r"\b(when|if|unless|no|not|none|never|error|fails?|invalid|empty|wrong|expired|"
                        r"als|wanneer|tenzij|geen|niet|nooit|fout|foute|ongeldig|leeg|verlopen)\b", re.I)
CLICK_WORDS = re.compile(r"\b(clicks?|clicking|press(es)?|taps?|klikt|klik|drukt)\b", re.I)
# A5: a Gherkin line starts with Given/Gegeven, or has the full Given ... when ... then form.
GHERKIN = re.compile(r"^\s*(?:[-*]\s*(?:\[[ xX]\]\s*)?)?\**(given|gegeven)\b|\bgiven\b.+\bwhen\b.+\bthen\b|"
                     r"\bgegeven\b.+\bwanneer\b.+\bdan\b|^\s*\**(then|and)\**\s", re.I)
CRITERIA_HEAD = re.compile(r"^#{2,6}\s*(acceptance criteria|acceptatiecriteria)\s*$", re.I)
HEADING = re.compile(r"^(#{1,6})\s+(.*)$")
TYPE_LINE = re.compile(r"^\*\*(type|soort):\*\*\s*(.+)$", re.I)
PARENT_LINE = re.compile(r"^\*\*(parent|ouder|bovenliggend):\*\*\s*(.+)$", re.I)
ITEM_LINE = re.compile(r"^\s*[-*]\s+(\[[ xX]\]\s+)?(.*)$")


class Finding:
    def __init__(self, line, severity, rule, message, text):
        self.line, self.severity, self.rule, self.message, self.text = line, severity, rule, message, text.strip()

    def as_dict(self):
        return self.__dict__


def split_items(lines):
    """Return (start line, heading level, title, block lines) for each item with a Type line."""
    heads = [(i, len(m.group(1)), m.group(2).strip()) for i, l in enumerate(lines) if (m := HEADING.match(l))]
    items = []
    for n, (i, level, title) in enumerate(heads):
        end = len(lines)
        for j, lvl, _ in heads[n + 1:]:
            if lvl <= level:
                end = j
                break
        block = lines[i + 1:end]
        if any(TYPE_LINE.match(l.strip()) for l in block[:6]):
            items.append((i, level, title, block))
    return items


def check_item(start, title, block):
    f = []
    kind = ""
    parent = None
    for l in block:
        s = l.strip()
        if m := TYPE_LINE.match(s):
            kind = m.group(2).strip().lower()
        if m := PARENT_LINE.match(s):
            parent = m.group(2).strip()
    special = kind in SPECIAL_TYPES
    if kind in FEATURE_TYPES:
        return check_feature(start, title, block, parent)
    if kind not in STORY_TYPES and not special:
        return f  # another item type: no rules yet

    if len(title) > TITLE_HARD_MAX:
        f.append(Finding(start, "error", "T1", f"Title is longer than {TITLE_HARD_MAX} characters.", title))
    elif len(title) > TITLE_MAX:
        f.append(Finding(start, "warning", "T1", f"Title is longer than {TITLE_MAX} characters.", title))
    if LAYER_WORDS.search(title):
        f.append(Finding(start, "warning", "T2", "Title names a layer of the system. Write a vertical story.", title))
    if parent is None:
        f.append(Finding(start, "warning", "PARENT", "No **Parent:** line. Write the parent or \"None\".", title))

    # The story sentence (S1, S2)
    if not special:
        sentence = None
        for k, l in enumerate(block):
            m = STORY_EN.search(l.strip()) or STORY_NL.search(l.strip())
            if m and not l.strip().startswith(("-", "*", "#")):
                sentence = (start + 1 + k, m, l)
                break
        if not sentence:
            f.append(Finding(start, "error", "S1", "No story sentence (\"As a <role>, I want <goal>, so that <value>.\").", title))
        else:
            ln, m, text = sentence
            role = re.sub(r"^(a|an|the|een|de|het)\s+", "", m.group("role").strip().lower())
            if role in GENERIC_ROLES or m.group("role").strip().lower() in GENERIC_ROLES:
                f.append(Finding(ln, "error", "S1", "The role is too wide. Name the type of user.", text))
            elif GENERIC_SUFFIX.match(role):
                f.append(Finding(ln, "warning", "S1", "The role is a kind of \"user\". Name the type of user.", text))
            joiners = r"\b(en|of)\b" if STORY_NL.search(text.strip()) else r"\b(and|or)\b"
            if re.search(joiners, m.group("goal"), re.I):
                f.append(Finding(ln, "warning", "S2", "The goal has \"and\" or \"or\". Is it two stories?", text))

    # Acceptance criteria (A1 to A7)
    crit_start = next((k for k, l in enumerate(block) if CRITERIA_HEAD.match(l.strip())), None)
    criteria = []
    if crit_start is not None:
        for k in range(crit_start + 1, len(block)):
            s = block[k].strip()
            if HEADING.match(s):
                break
            if m := ITEM_LINE.match(block[k]):
                criteria.append((start + 1 + k, m.group(2), block[k]))
            elif GHERKIN.match(s):
                criteria.append((start + 1 + k, s, block[k]))
    if len(criteria) < CRITERIA_MIN:
        f.append(Finding(start, "error", "A1", "No acceptance criteria.", title))
    # A6: one criterion should cover an edge case or an error. A condition word is the signal.
    if criteria and not special and not any(EDGE_WORDS.search(t) for _, t, _ in criteria):
        f.append(Finding(start, "warning", "A6", "No criterion for an edge case or an error.", title))
    if len(criteria) > CRITERIA_MAX:
        f.append(Finding(start, "warning", "A7", f"{len(criteria)} criteria. More than {CRITERIA_MAX}: split the story.", title))
    for ln, text, raw in criteria:
        if GHERKIN.match(text) or GHERKIN.match(raw):
            f.append(Finding(ln, "error", "A5", "Given/When/Then is not used. Write one checkable result.", raw))
        if VAGUE_WORDS.search(text):
            f.append(Finding(ln, "warning", "A3", f"Vague word \"{VAGUE_WORDS.search(text).group(0)}\". Use a number or an example.", raw))
        if CLICK_WORDS.search(text):
            f.append(Finding(ln, "warning", "A4", "Steps in the screen. Write the result for the user.", raw))
        if not re.match(r"^\s*[-*]\s+\[[ xX]\]", raw) and not GHERKIN.match(text):
            f.append(Finding(ln, "warning", "A5", "Write the criterion as a checklist item (- [ ]).", raw))
    return f


FEATURE_TYPES = {"feature", "kenmerk"}
FEATURE_CRITERIA_MAX = 6   # F5: more feature criteria means story detail moved up
FEATURE_STORIES_MAX = 8    # F3: more stories means the feature is too large
SECTION = lambda names: re.compile(r"^#{2,6}\s*(" + names + r")\s*$", re.I)
GOAL_HEAD = SECTION(r"goal|doel")
SCOPE_HEAD = SECTION(r"scope")
STORIES_HEAD = SECTION(r"stories|user stories")
MEASURE_LINE = re.compile(r"^\s*\**(measure|meten|maatstaf)\b", re.I)
SCOPE_IN = re.compile(r"^\s*[-*]\s+\**(in|binnen scope)\**:", re.I)
SCOPE_OUT = re.compile(r"^\s*[-*]\s+\**(out|buiten scope)\**:", re.I)


def section(block, head):
    """Return the lines of the first section with a heading that matches head."""
    start = next((k for k, l in enumerate(block) if head.match(l.strip())), None)
    if start is None:
        return None
    out = []
    for l in block[start + 1:]:
        if HEADING.match(l.strip()):
            break
        out.append(l)
    return out


def check_feature(start, title, block, parent):
    f = []
    if len(title) > TITLE_HARD_MAX:
        f.append(Finding(start, "error", "F1", f"Title is longer than {TITLE_HARD_MAX} characters.", title))
    elif len(title) > TITLE_MAX:
        f.append(Finding(start, "warning", "F1", f"Title is longer than {TITLE_MAX} characters.", title))
    if LAYER_WORDS.search(title):
        f.append(Finding(start, "warning", "F1", "Title names a layer of the system. Name the capability for the user.", title))
    if parent is None:
        f.append(Finding(start, "warning", "PARENT", "No **Parent:** line. Write the parent or \"None\".", title))
    goal = section(block, GOAL_HEAD)
    if goal is None or not any(l.strip() for l in goal):
        f.append(Finding(start, "error", "F2", "No goal. Write the outcome for the users and the business.", title))
    elif not any(MEASURE_LINE.match(l.strip().lstrip("-* ")) for l in goal):
        f.append(Finding(start, "warning", "F2", "The goal has no \"Measure:\" line.", title))
    scope = section(block, SCOPE_HEAD)
    if scope is None or not any(SCOPE_IN.match(l) for l in scope) or not any(SCOPE_OUT.match(l) for l in scope):
        f.append(Finding(start, "error", "F4", "The scope needs an \"In:\" line and an \"Out:\" line.", title))
    crit = section(block, CRITERIA_HEAD)
    items = [l for l in (crit or []) if ITEM_LINE.match(l)]
    if not items:
        f.append(Finding(start, "error", "F5", "No acceptance criteria for the feature.", title))
    elif len(items) > FEATURE_CRITERIA_MAX:
        f.append(Finding(start, "warning", "F5", f"{len(items)} criteria. Keep the feature at {FEATURE_CRITERIA_MAX} or less: detail goes in the stories.", title))
    for l in items:
        if GHERKIN.match(l):
            f.append(Finding(start, "error", "A5", "Given/When/Then is not used. Write one checkable result.", l))
        if VAGUE_WORDS.search(l):
            f.append(Finding(start, "warning", "A3", f"Vague word \"{VAGUE_WORDS.search(l).group(0)}\". Use a number or an example.", l))
    stories = section(block, STORIES_HEAD)
    count = len([l for l in (stories or []) if ITEM_LINE.match(l)])
    if count > FEATURE_STORIES_MAX:
        f.append(Finding(start, "warning", "F3", f"{count} stories. More than {FEATURE_STORIES_MAX}: split the feature.", title))
    return f


def check_file(path):
    lines = path.read_text(encoding="utf-8").splitlines()
    items = split_items(lines)
    findings = []
    for start, _level, title, block in items:
        findings += check_item(start + 1, title, block)
    return items, findings


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("files", nargs="+")
    parser.add_argument("--json", action="store_true")
    args = parser.parse_args()
    failed = False
    report = []
    for name in args.files:
        path = pathlib.Path(name)
        if not path.is_file():
            print(f"{name}: file not found")
            failed = True
            continue
        items, findings = check_file(path)
        errors = sum(x.severity == "error" for x in findings)
        warnings = len(findings) - errors
        failed |= errors > 0
        if args.json:
            report.append({"file": name, "items": len(items), "errors": errors, "warnings": warnings,
                           "findings": [x.as_dict() for x in findings]})
            continue
        for x in sorted(findings, key=lambda x: (x.severity != "error", x.line)):
            print(f"{name}:{x.line}: {x.severity.upper()} {x.rule} {x.message} | {x.text[:80]}")
        note = "" if items else " (no item with a **Type:** line found)"
        print(f"{name}: {errors} errors, {warnings} warnings | {len(items)} items{note}")
    if args.json:
        print(json.dumps(report, indent=2))
    return 1 if failed else 0


if __name__ == "__main__":
    sys.exit(main())
