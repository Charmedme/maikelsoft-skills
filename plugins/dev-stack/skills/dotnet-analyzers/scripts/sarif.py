#!/usr/bin/env python3
"""Read the SARIF 2.1 files that the compiler writes (ErrorLog) and print the findings.

    python3 sarif.py [root]                # findings of each **/obj/*.sarif under root (default: .)
    python3 sarif.py [root] --summary      # count per rule ID, for a report-only adoption
    python3 sarif.py [root] --suppressed   # only the suppressed findings (#pragma, SuppressMessage)
    python3 sarif.py [root] --all-levels   # also the "note" level (Info rules that the build log hides)

Output: one line per finding, `path:line:column RULE-ID level message`, then a summary line.
Exit code: 1 when one or more findings that are not suppressed have the level "warning" or "error", else 0.

Python 3.9 or later, standard library only.
"""
import argparse
import collections
import json
import pathlib
import sys
from urllib.parse import unquote, urlparse

# Levels that "dotnet build" shows. "note" (Info) and "none" (Hidden) do not show in the build log.
SHOWN = {"warning", "error"}


def results(path):
    try:
        data = json.loads(path.read_text(encoding="utf-8-sig"))
    except (OSError, json.JSONDecodeError) as exc:
        print(f"{path}: cannot read ({exc})", file=sys.stderr)
        return
    for run in data.get("runs", []):
        for result in run.get("results", []):
            yield result


def location(result, root):
    try:
        physical = result["locations"][0]["physicalLocation"]
        uri = physical["artifactLocation"]["uri"]
        region = physical.get("region", {})
    except (KeyError, IndexError):
        return "(no location)"
    file = pathlib.Path(unquote(urlparse(uri).path)) if uri.startswith("file:") else pathlib.Path(uri)
    try:
        file = file.resolve().relative_to(root)
    except ValueError:
        pass
    return f"{file.as_posix()}:{region.get('startLine', 0)}:{region.get('startColumn', 0)}"


def main(argv=None):
    parser = argparse.ArgumentParser(description="Print the findings of the SARIF files in obj/ folders.")
    parser.add_argument("root", nargs="?", default=".")
    parser.add_argument("--summary", action="store_true", help="count per rule ID")
    parser.add_argument("--suppressed", action="store_true", help="only suppressed findings")
    parser.add_argument("--all-levels", action="store_true", help="also the note level")
    args = parser.parse_args(argv)

    root = pathlib.Path(args.root).resolve()
    files = sorted(p for p in root.rglob("obj/*.sarif") if "node_modules" not in p.parts)
    if not files:
        print(f"sarif: no obj/*.sarif under {root}. Set ErrorLog in Directory.Build.props and build.", file=sys.stderr)
        return 2

    seen, rows, counts, suppressed_count = set(), [], collections.Counter(), 0
    for path in files:
        for result in results(path):
            level = result.get("level", "warning")
            is_suppressed = bool(result.get("suppressions"))
            if not args.all_levels and level not in SHOWN:
                continue
            if args.suppressed != is_suppressed:
                suppressed_count += is_suppressed
                continue
            key = (result.get("ruleId"), location(result, root), result.get("message", {}).get("text", ""))
            if key in seen:
                continue  # a multi-target project reports the same finding once per target
            seen.add(key)
            rows.append((key[1], key[0], level, key[2]))
            counts[key[0]] += 1

    if args.summary:
        for rule, count in counts.most_common():
            print(f"{count:6} {rule}")
    else:
        for loc, rule, level, message in sorted(rows):
            print(f"{loc} {rule} {level} {message}")
    shown = sum(1 for r in rows if r[2] in SHOWN)
    label = "suppressed findings" if args.suppressed else "findings"
    print(f"sarif: {len(rows)} {label}, {len(counts)} rule IDs, {suppressed_count} suppressed not shown | {len(files)} files")
    return 1 if shown and not args.suppressed else 0


if __name__ == "__main__":
    sys.exit(main())
