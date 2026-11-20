#!/usr/bin/env python3
"""convert: make HTML, PDF, or Word files from a checked Markdown source.

Usage:
    python3 convert.py <file.md> --to html,pdf,docx

The output files go next to the source, with the same name. The script uses
pandoc. For PDF, it also needs a PDF engine (typst, wkhtmltopdf, weasyprint,
xelatex, or pdflatex). It uses the Python standard library only.

Exit codes:
    0  each format is made
    1  pandoc failed
    2  a tool is missing; the script prints the command to run
"""
from __future__ import annotations

import argparse
import pathlib
import shutil
import subprocess
import sys

FORMATS = ("html", "pdf", "docx")
PDF_ENGINES = ("typst", "wkhtmltopdf", "weasyprint", "xelatex", "pdflatex")


def pdf_engine(which=shutil.which) -> str | None:
    """The first PDF engine that is installed, or None."""
    return next((e for e in PDF_ENGINES if which(e)), None)


def command(source: pathlib.Path, fmt: str, engine: str | None = None) -> list[str]:
    """The pandoc command that makes one format."""
    target = source.with_suffix("." + fmt)
    cmd = ["pandoc", str(source), "-o", str(target)]
    if fmt == "html":
        cmd += ["--standalone", "--metadata", f"pagetitle={source.stem}"]
    if fmt == "pdf":
        cmd += [f"--pdf-engine={engine or '<engine>'}"]
    return cmd


def main(argv: list[str] | None = None, which=shutil.which, run=subprocess.run) -> int:
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("source", help="the Markdown file")
    parser.add_argument("--to", required=True, help="comma-separated list: html, pdf, docx")
    args = parser.parse_args(argv)

    source = pathlib.Path(args.source)
    wanted = [f.strip().lower() for f in args.to.split(",") if f.strip()]
    unknown = [f for f in wanted if f not in FORMATS]
    if unknown:
        print(f"convert: unknown format: {', '.join(unknown)}. Use: {', '.join(FORMATS)}", file=sys.stderr)
        return 2
    if not source.is_file():
        print(f"convert: cannot read {source}", file=sys.stderr)
        return 2

    status = 0
    for fmt in wanted:
        engine = pdf_engine(which) if fmt == "pdf" else None
        cmd = command(source, fmt, engine)
        missing = [] if which("pandoc") else ["pandoc"]
        if fmt == "pdf" and not engine:
            missing.append(f"a PDF engine ({', '.join(PDF_ENGINES)})")
        if missing:
            print(f"NOT MADE {fmt}: install {' and '.join(missing)}, then run: {' '.join(cmd)}")
            status = max(status, 2)
            continue
        result = run(cmd, capture_output=True, text=True)
        if result.returncode != 0:
            print(f"FAILED {fmt}: {result.stderr.strip()[:300]}")
            status = 1 if status == 0 else status
            continue
        print(f"MADE {source.with_suffix('.' + fmt)}")
    return status


if __name__ == "__main__":
    sys.exit(main())
