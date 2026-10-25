#!/usr/bin/env python3
"""Compare the runs of each case and variant: are the results stable?

For each <case>/<variant> in a runs folder, the script compares the runs (normally 3) two by two:

- verdict: the verdict or status word of each final.md is the same in all runs.
  Review: 'merge', 'merge with changes', or 'do not merge'. Write: 'done', 'partial', or 'blocked'.
  When the script cannot find a word, it prints '?'.
- found: for a review case, the mean Jaccard overlap of the found planted ids. The ids come from
  <run>/grade.json ('found'), or from a graded folder and its map (--graded, --map).
- headings: the mean Jaccard overlap of the Markdown headings of final.md (lower case, no numbers or punctuation).
- words: the coefficient of variation (standard deviation / mean) of the word count of final.md.
  0 means the same length in all runs. A higher value means less stable answers.

Usage:
    python3 evals/dev-stack/consistency.py <runs> [--graded <folder> --map <map.json>] [--json <out>]

The script only reads the runs folder.
"""
import argparse
import itertools
import json
import pathlib
import re
import statistics
import sys
from collections import defaultdict
sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent))
import cases

HERE = pathlib.Path(__file__).resolve().parent

REVIEW_VERDICTS = [
    ("do not merge", r"\b(do not|don't|cannot|can't|not ready to|must not|should not)\s+(be\s+)?merge(d)?\b|\brequest(ed)? changes\b|\bblock(ed|s|ing)?\b|\bnot mergeable\b|\breject(ed)?\b"),
    ("merge with changes", r"\bmerge (after|once|when|with)\b|\bapprove(d)? with (changes|comments|conditions)\b"),
    ("merge", r"\b(ok|okay|safe|ready) to merge\b|\bapprove(d)?\b|\bmerge\b"),
]
WRITE_STATUS = [
    ("blocked", r"^(status|result)?\s*:?\s*\**\s*blocked\b|\b(work|task|story|change) (is )?blocked\b|\bcould not (finish|complete)\b"),
    ("partial", r"\bpartial(ly)? (done|complete|implemented)\b|\bnot (yet )?(finished|complete)\b|\bremaining work\b"),
    ("done", r"\b(build|tests?|checks?)\b.{0,60}\b(pass|passes|passed|green|succeeded)\b|\bdone\b|\bcompleted?\b|\bimplemented\b|\bbuilt\b"),
]
LABEL = re.compile(r"^\W*(verdict|status|result|merge decision|recommendation)\b\W*", re.I)


def verdict(text, kind):
    """Return the verdict word.
    1. A line that starts with 'Verdict', 'Status' or 'Result' (also as a heading), with the next 2 lines.
    2. The first 15 lines.
    3. The whole text."""
    table = REVIEW_VERDICTS if kind == "review" else WRITE_STATUS
    lines = text.splitlines()
    labeled = []
    for i, line in enumerate(lines):
        if LABEL.match(line.lstrip("#* ")):
            labeled.append("\n".join([LABEL.sub("", line.lstrip("#* "))] + lines[i + 1:i + 3]))
    for scope in labeled + ["\n".join(lines[:15]), text]:
        for word, pattern in table:
            if re.search(pattern, scope, re.I | re.M):
                return word
    return "?"


def headings(text):
    out = set()
    in_code = False
    for line in text.splitlines():
        if line.strip().startswith("```"):
            in_code = not in_code
            continue
        m = re.match(r"^(#{1,6})\s+(.*)", line)
        if m and not in_code:
            h = re.sub(r"`[^`]*`", "", m.group(2).lower())
            h = re.sub(r"[^a-z ]+", " ", h)
            h = re.sub(r"\s+", " ", h).strip()
            if h:
                out.add(f"{len(m.group(1))}:{h}")
    return out


def jaccard(a, b):
    if not a and not b:
        return 1.0
    return len(a & b) / len(a | b)


def mean_pairwise(sets):
    pairs = list(itertools.combinations(sets, 2))
    return sum(jaccard(a, b) for a, b in pairs) / len(pairs) if pairs else None


def word_cv(texts):
    counts = [len(re.findall(r"\S+", t)) for t in texts]
    if len(counts) < 2 or not statistics.mean(counts):
        return None, counts
    return statistics.pstdev(counts) / statistics.mean(counts), counts


def mean(values):
    values = [v for v in values if v is not None]
    return sum(values) / len(values) if values else None


def load_found(runs, graded, mapping):
    found = {}
    if graded and mapping:
        for gid, key in json.loads(pathlib.Path(mapping).read_text(encoding="utf-8")).items():
            grade = pathlib.Path(graded) / gid / "grade.json"
            if grade.is_file():
                found[key] = set(json.loads(grade.read_text(encoding="utf-8")).get("found", []))
    for grade in pathlib.Path(runs).glob("*/*/*/grade.json"):
        key = "/".join(grade.parent.relative_to(runs).parts)
        found.setdefault(key, set(json.loads(grade.read_text(encoding="utf-8")).get("found", [])))
    return found


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("runs", type=pathlib.Path)
    parser.add_argument("--graded", type=pathlib.Path, help="folder with <id>/grade.json")
    parser.add_argument("--map", type=pathlib.Path, help="JSON map {id: '<case>/<variant>/<n>'}")
    parser.add_argument("--json", type=pathlib.Path, help="write the table as JSON to this file")
    args = parser.parse_args(argv)
    evals = cases.load()
    found = load_found(args.runs, args.graded, args.map)

    groups = defaultdict(list)
    for final in sorted(args.runs.glob("*/*/*/final.md")):
        case, variant, n = final.parent.relative_to(args.runs).parts
        groups[(case, variant)].append((n, final.read_text(encoding="utf-8", errors="ignore")))

    rows = []
    for (case, variant), runs in sorted(groups.items()):
        kind = evals.get(case, {}).get("kind", "write")
        words = [verdict(text, kind) for _, text in runs]
        heads = mean_pairwise([headings(text) for _, text in runs])
        ids = [found.get(f"{case}/{variant}/{n}") for n, _ in runs]
        found_overlap = mean_pairwise(ids) if kind == "review" and all(i is not None for i in ids) else None
        cv, counts = word_cv([text for _, text in runs])
        rows.append({
            "case": case, "variant": variant, "kind": kind, "runs": len(runs),
            "verdicts": words, "same_verdict": len(set(words)) == 1 and "?" not in words,
            "found_jaccard": found_overlap, "heading_jaccard": heads, "words": counts, "words_cv": cv,
        })

    fmt = lambda v: "-" if v is None else f"{v:.2f}"
    header = f"{'case':28} {'variant':8} {'n':>2}  {'verdicts':44} {'same':5} {'found J':>7} {'head J':>6} {'words CV':>8}"
    print(header)
    print("-" * len(header))
    for r in rows:
        print(f"{r['case']:28} {r['variant']:8} {r['runs']:>2}  {' | '.join(r['verdicts']):44} "
              f"{'yes' if r['same_verdict'] else 'NO':5} {fmt(r['found_jaccard']):>7} {fmt(r['heading_jaccard']):>6} "
              f"{fmt(r['words_cv']):>8}")
    print()
    for variant in sorted({r["variant"] for r in rows}):
        sel = [r for r in rows if r["variant"] == variant]
        print(f"{variant}: same verdict {sum(r['same_verdict'] for r in sel)}/{len(sel)}, "
              f"mean found J {fmt(mean(r['found_jaccard'] for r in sel))}, "
              f"mean heading J {fmt(mean(r['heading_jaccard'] for r in sel))}, "
              f"mean words CV {fmt(mean(r['words_cv'] for r in sel))}")
    if args.json:
        args.json.write_text(json.dumps(rows, indent=2), encoding="utf-8")
    return 0


if __name__ == "__main__":
    sys.exit(main())
