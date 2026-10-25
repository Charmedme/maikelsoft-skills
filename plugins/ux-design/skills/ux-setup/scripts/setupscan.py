#!/usr/bin/env python3
"""Find the design facts of a project before the ux-setup questions.

    python3 setupscan.py [<project folder>] [--json]

It reads files and runs a few version commands. It changes nothing. It reports:
  design_section   the "## Design" section of CLAUDE.md, when it exists
  brand_files      DESIGN.md, PRODUCT.md, and where they are
  tokens           DTCG files (JSON with $value), and CSS files with 10 or more custom properties
  tailwind         a Tailwind config file, or "@theme" in a CSS file
  storybook        a .storybook folder
  figma_links      Figma file and design links in the text files
  openpencil       .op files, and the version of the op CLI
  plugins          figma and openpencil-skill in `claude plugin list`
  ui_stack         UI frameworks from package.json, and other signs (.csproj with Blazor, .razor, .vue)
  run              the dev scripts of package.json
  languages        the lang attributes of HTML files, and the names of locale files
  playwright       Python Playwright, and @playwright/test in node_modules
The Claude Design Systems of the account are not in this report. Find them with the Artifact
tool (list, type "Design System") when the session has it.
Exit code: 0.
"""
import json
import os
import re
import shutil
import subprocess
import sys
from pathlib import Path

SKIP = {".git", "node_modules", "dist", "build", ".next", "bin", "obj", ".venv", "venv", "__pycache__", "coverage", ".angular"}
TEXT = {".md", ".txt", ".json", ".html", ".htm", ".css", ".scss", ".ts", ".tsx", ".js", ".jsx", ".vue", ".svelte", ".razor", ".cshtml", ".yml", ".yaml"}
UI_DEPS = ["react", "next", "vue", "nuxt", "svelte", "@sveltejs/kit", "@angular/core", "solid-js", "astro", "tailwindcss",
           "@mui/material", "@chakra-ui/react", "@radix-ui/react-slot", "bootstrap", "lit"]


def files(root, limit=20000):
    n = 0
    for dirpath, dirnames, filenames in os.walk(root):
        dirnames[:] = [d for d in dirnames if d not in SKIP and not d.startswith(".") or d == ".storybook"]
        for f in filenames:
            n += 1
            if n > limit:
                return
            yield Path(dirpath) / f


def run(cmd):
    if not shutil.which(cmd[0]):
        return None
    try:
        r = subprocess.run(cmd, capture_output=True, text=True, timeout=20)
        return (r.stdout + r.stderr).strip()
    except (OSError, subprocess.TimeoutExpired):
        return None


def main(argv):
    as_json = "--json" in argv
    args = [a for a in argv if a != "--json"]
    root = Path(args[0] if args else ".").resolve()
    rel = lambda p: str(p.relative_to(root))
    facts = {"project": str(root), "design_section": None, "brand_files": {}, "tokens": [], "tailwind": [],
             "storybook": False, "figma_links": [], "openpencil": {"files": [], "cli": None},
             "plugins": {}, "ui_stack": [], "run": {}, "languages": [], "playwright": {}}
    claude_md = root / "CLAUDE.md"
    if claude_md.exists():
        m = re.search(r"^## Design\s*$(.*?)(?=^## |\Z)", claude_md.read_text(encoding="utf-8"), re.S | re.M)
        if m:
            facts["design_section"] = m.group(1).strip()
    langs, figma = set(), set()
    for p in files(root):
        name = p.name
        if name in ("DESIGN.md", "PRODUCT.md"):
            facts["brand_files"].setdefault(name, []).append(rel(p))
        if p.suffix == ".op":
            facts["openpencil"]["files"].append(rel(p))
        if name.startswith("tailwind.config"):
            facts["tailwind"].append(rel(p))
        if p.parent.name == ".storybook":
            facts["storybook"] = True
        if p.suffix == ".csproj":
            t = p.read_text(encoding="utf-8", errors="replace")
            if "Microsoft.NET.Sdk.BlazorWebAssembly" in t or "Components" in t and "Razor" in t:
                facts["ui_stack"].append(f"Blazor ({rel(p)})")
        if p.suffix in (".razor",) and "Razor components" not in facts["ui_stack"]:
            facts["ui_stack"].append("Razor components")
        if re.search(r"(^|[/\\])(locales?|i18n|lang|translations)[/\\]", rel(p)) and p.suffix in (".json", ".yml", ".yaml", ".po", ".resx"):
            langs.add(p.stem.split(".")[0])
        if p.suffix == ".resx":
            m = re.search(r"\.([a-z]{2}(-[A-Z]{2})?)\.resx$", name)
            if m:
                langs.add(m.group(1))
        if p.suffix not in TEXT or p.stat().st_size > 2_000_000:
            continue
        try:
            t = p.read_text(encoding="utf-8", errors="replace")
        except OSError:
            continue
        for u in re.findall(r"https://(?:www\.)?figma\.com/(?:file|design|proto)/[A-Za-z0-9]+[^\s)\"'<>]*", t):
            figma.add(u.split("?")[0])
        if p.suffix in (".html", ".htm"):
            langs.update(re.findall(r"<html[^>]*\blang=\"([a-zA-Z-]+)\"", t))
        if p.suffix == ".json" and '"$value"' in t:
            try:
                json.loads(t)
                facts["tokens"].append({"file": rel(p), "kind": "dtcg", "tokens": t.count('"$value"')})
            except json.JSONDecodeError:
                pass
        if p.suffix in (".css", ".scss"):
            n = len(re.findall(r"--[\w-]+\s*:", t))
            if n >= 10:
                facts["tokens"].append({"file": rel(p), "kind": "css-custom-properties", "tokens": n})
            if "@theme" in t:
                facts["tailwind"].append(f"{rel(p)} (@theme)")
        if name == "package.json" and "node_modules" not in p.parts:
            try:
                pkg = json.loads(t)
            except json.JSONDecodeError:
                continue
            deps = {**pkg.get("dependencies", {}), **pkg.get("devDependencies", {})}
            facts["ui_stack"] += [f"{d} {deps[d]}" for d in UI_DEPS if d in deps]
            for s in ("dev", "start", "serve", "storybook", "preview"):
                if s in pkg.get("scripts", {}):
                    facts["run"][f"{rel(p.parent) or '.'}: npm run {s}"] = pkg["scripts"][s]
            if "@playwright/test" in deps:
                facts["playwright"]["node"] = deps["@playwright/test"]
    facts["figma_links"] = sorted(figma)[:20]
    facts["languages"] = sorted(langs)
    facts["openpencil"]["cli"] = run(["op", "--version"])
    plist = run(["claude", "plugin", "list"])
    if plist is not None:
        facts["plugins"] = {"figma": bool(re.search(r"\bfigma@", plist)),
                            "openpencil-skill": "openpencil-skill" in plist}
    try:
        import playwright  # noqa: F401
        facts["playwright"]["python"] = True
    except ImportError:
        facts["playwright"]["python"] = False
    if as_json:
        print(json.dumps(facts, indent=2))
    else:
        for k, v in facts.items():
            print(f"{k}: {json.dumps(v)}")
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
