#!/usr/bin/env python3
"""Copy the shared files into each skill of each plugin.

    python3 scripts/sync_shared.py          # write the copies
    python3 scripts/sync_shared.py --check  # exit 1 if a copy is missing or different

Each skill gets:
    references/language.md  <- shared/language.md
    scripts/doccheck.py     <- shared/doccheck.py
    scripts/convert.py      <- shared/convert.py

A SKILL.md that has these two marker lines also gets shared/workflow.md between them:
    <!-- BEGIN shared/workflow.md -->
    <!-- END shared/workflow.md -->
"""
import argparse
import pathlib
import re
import sys

ROOT = pathlib.Path(__file__).resolve().parents[1]
SHARED = ROOT / "shared"
COPIES = {
    SHARED / "language.md": pathlib.Path("references") / "language.md",
    SHARED / "doccheck.py": pathlib.Path("scripts") / "doccheck.py",
    SHARED / "convert.py": pathlib.Path("scripts") / "convert.py",
}
BLOCK = re.compile(r"(<!-- BEGIN shared/workflow\.md -->\n).*?(<!-- END shared/workflow\.md -->)", re.DOTALL)


def skill_dirs():
    return sorted(p.parent for p in ROOT.glob("plugins/*/skills/*/SKILL.md"))


BEGIN = "<!-- BEGIN shared/workflow.md -->"
END = "<!-- END shared/workflow.md -->"


def expected_files(skill: pathlib.Path) -> dict:
    files = {skill / rel: src.read_text(encoding="utf-8") for src, rel in COPIES.items()}
    skill_md = skill / "SKILL.md"
    text = skill_md.read_text(encoding="utf-8")
    begin = [line.strip() for line in text.splitlines()].count(BEGIN)
    end = [line.strip() for line in text.splitlines()].count(END)
    if (begin, end) not in ((0, 0), (1, 1)) or (begin and not BLOCK.search(text)):
        raise SystemExit(f"{skill_md.relative_to(ROOT)}: the workflow markers must be one BEGIN line and "
                         f"one END line, each alone on its line.")
    if BLOCK.search(text):
        workflow = (SHARED / "workflow.md").read_text(encoding="utf-8")
        files[skill_md] = BLOCK.sub(lambda m: m.group(1) + workflow + m.group(2), text)
    return files


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--check", action="store_true", help="report drift, write nothing")
    args = parser.parse_args()

    drift = []
    skills = skill_dirs()
    for skill in skills:
        for target, content in expected_files(skill).items():
            if target.exists() and target.read_text(encoding="utf-8") == content:
                continue
            drift.append(target.relative_to(ROOT))
            if not args.check:
                target.parent.mkdir(parents=True, exist_ok=True)
                target.write_text(content, encoding="utf-8")

    if args.check and drift:
        print("Out of date. Run: python3 scripts/sync_shared.py")
        for path in drift:
            print(f"  {path}")
        return 1
    print(f"{len(skills)} skills checked, {len(drift)} {'stale' if args.check else 'updated'}.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
