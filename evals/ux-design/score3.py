#!/usr/bin/env python3
"""Score the batch 3 runs of run3.py.

    python3 evals/ux-design/score3.py <runs> --suite setup|brand

setup  Reads the "## Design" section of repo/CLAUDE.md. Counts the topics that it covers, the facts
       of the fixture that it has right, and the claims of a Figma plan or seat that it does not mark
       as a guess (invented facts).
brand  Runs brandcheck.py on repo/design-system/tokens.json and the copy check on PRODUCT.md. Counts the
       DESIGN.md sections in the order of the format, the contrast pairs, and the planted faults of the
       design system (faults.json) that the output names.
"""
import argparse
import json
import re
import subprocess
import sys
from pathlib import Path

HERE = Path(__file__).resolve().parent
BRAND = HERE.parents[1] / "plugins" / "ux-design" / "shared" / "brandcheck.py"

TOPICS = {
    "design tools": r"design tools?",
    "mockup tool": r"mockup",
    "design-system source": r"design.system|tokens",
    "brand files": r"brand|DESIGN\.md|PRODUCT\.md",
    "ui stack": r"stack|react|html and css|framework",
    "languages": r"language",
    "run": r"run|npm|start|serve|open the",
    "playwright": r"playwright",
}

FACTS = {
    "setup-a": {
        "tokens.css is the source": r"design-system/tokens\.css",
        "DESIGN.md and PRODUCT.md": r"DESIGN\.md[\s\S]*PRODUCT\.md|PRODUCT\.md[\s\S]*DESIGN\.md",
        "language en": r"\ben\b|English",
        "mockup tool html": r"mockup[^\n]*html",
        "Playwright Python": r"playwright[^\n]*python",
        "no Figma": r"(?i)none|no figma|not used",
    },
    "setup-b": {
        "tokens.tokens.json is the source": r"src/styles/tokens\.tokens\.json",
        "Figma link": r"Qx7Lm2Parts",
        "languages en and nl": r"(?i)\ben\b[^\n]*\bnl\b|\bnl\b[^\n]*\ben\b|english[^\n]*dutch|dutch[^\n]*english",
        "npm run dev": r"npm run dev",
        "port 5174": r"5174",
        "React": r"(?i)react",
        "Playwright Node": r"(?i)playwright[^\n]*(node|@playwright/test|npm|1\.56)",
    },
    "setup-c": {
        "no design system yet": r"(?i)none|no design system|not yet|foundation|to be created|no tokens",
        "PRODUCT.md": r"PRODUCT\.md",
        "no DESIGN.md yet": r"(?i)DESIGN\.md[^\n]*(missing|not yet|none|to (be )?(write|create)|does not exist|brand-rules)|no DESIGN\.md",
    },
}

SEAT = re.compile(r"(?i)\b(dev|full|view|collab)\s+seat\b|\b(professional|organization|enterprise|starter)\s+plan\b")
GUESS = re.compile(r"(?i)guess|unknown|assum|confirm|not known|tbd|\?|ask|unverified|not stated")


def design_section(repo):
    md = repo / "CLAUDE.md"
    if not md.exists():
        return ""
    m = re.search(r"^## Design\b.*?$(.*?)(?=^## |\Z)", md.read_text(encoding="utf-8"), re.S | re.M)
    return m.group(1) if m else ""


def score_setup(run):
    case = run.parts[-3]
    sec = design_section(run / "repo")
    topics = sum(1 for p in TOPICS.values() if re.search(p, sec, re.I))
    facts = {k: bool(re.search(p, sec, re.I)) for k, p in FACTS[case].items()}
    invented = [l.strip() for l in sec.splitlines() if SEAT.search(l) and not GUESS.search(l)]
    marked = len(re.findall(r"(?i)guess|assum|unknown|confirm", sec))
    meta = json.loads((run / "meta.json").read_text())
    return {"run": str(run.relative_to(run.parents[3])), "case": case, "variant": run.parts[-2], "has_section": bool(sec.strip()),
            "topics": topics, "facts": sum(facts.values()), "facts_total": len(facts), "missed": [k for k, v in facts.items() if not v],
            "invented_seat": invented, "guess_marks": marked, "lines": len([l for l in sec.splitlines() if l.strip().startswith("-")]),
            "cost": round(meta.get("cost_usd", 0), 2)}


GOOGLE = ["overview", "colors", "typography", "layout", "elevation", "shapes", "components", "do"]


def score_brand(run):
    repo = run / "repo"
    out = {"run": str(run.relative_to(run.parents[3])), "variant": run.parts[-2]}
    tok = repo / "design-system" / "tokens.json"
    if tok.exists():
        r = subprocess.run([sys.executable, str(BRAND), "check", str(tok), "--json"], capture_output=True, text=True)
        try:
            res = json.loads(r.stdout)
            items = res["items"]
            out.update({"dtcg_errors": res["errors"], "tokens": res["tokens"], "pairs": res["pairs_checked"],
                        "no_usage": sum(1 for i in items if i["rule"] == "TK-USAGE"),
                        "contrast_fails": sum(1 for i in items if i["rule"] == "TK-CONTRAST"),
                        "error_rules": sorted({i["rule"] for i in items if i["level"] == "error"})})
        except (json.JSONDecodeError, KeyError):
            out["dtcg_errors"] = "unreadable"
    else:
        out["dtcg_errors"] = "no file"
    d = repo / "DESIGN.md"
    if d.exists():
        heads = [h.lower() for h in re.findall(r"^##\s+(.+)$", d.read_text(encoding="utf-8"), re.M)]
        found = [g for g in GOOGLE if any(h.startswith(g) for h in heads)]
        idx = [next(i for i, h in enumerate(heads) if h.startswith(g)) for g in found]
        out["design_sections"] = len(found)
        out["design_in_order"] = idx == sorted(idx)
        out["hard_soft"] = len(re.findall(r"(?im)^\s*-\s*(hard|soft)\b", d.read_text(encoding="utf-8")))
    p = repo / "PRODUCT.md"
    if p.exists():
        screens = [str(x) for x in (repo / "screens").glob("*.html")]
        r = subprocess.run([sys.executable, str(BRAND), "copy", "--product", str(p), *screens, "--json"], capture_output=True, text=True)
        try:
            res = json.loads(r.stdout)
            out["words_table"] = res.get("words", 0)
        except json.JSONDecodeError:
            out["words_table"] = 0
    text = ""
    for f in ("design-system-issues.md", "DESIGN.md"):
        if (repo / f).exists():
            text += (repo / f).read_text(encoding="utf-8").lower() + "\n"
    if (repo / "design-system-issues.md").exists():
        issues = (repo / "design-system-issues.md").read_text(encoding="utf-8").lower()
        faults = json.loads((HERE / "faults.json").read_text())
        named = []
        for f in faults:
            g1, g2 = f["doc"]
            if any(any(a in line for a in g1) and any(b in line for b in g2) for line in issues.splitlines()):
                named.append(f["id"])
        out["faults_named"] = named
    meta = json.loads((run / "meta.json").read_text())
    out["cost"] = round(meta.get("cost_usd", 0), 2)
    return out


def main():
    p = argparse.ArgumentParser()
    p.add_argument("runs")
    p.add_argument("--suite", required=True, choices=["setup", "brand"])
    args = p.parse_args()
    root = Path(args.runs) / args.suite
    runs = sorted(m.parent for m in root.glob("*/*/*/meta.json"))
    fn = score_setup if args.suite == "setup" else score_brand
    recs = [fn(r) for r in runs]
    for r in recs:
        print(json.dumps(r))
    (root / "scores.json").write_text(json.dumps(recs, indent=2), encoding="utf-8")


if __name__ == "__main__":
    main()
