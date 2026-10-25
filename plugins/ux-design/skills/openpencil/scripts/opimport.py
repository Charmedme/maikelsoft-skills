#!/usr/bin/env python3
"""Put checked HTML mockups into OpenPencil, one frame for each state.

    python3 opimport.py <mockups/*.html> --out <dir> [--op op] [--extractor <snapshot-extractor.js>] [--width 1440]

The mockups follow the mockup mode of the ux-designer agent: one HTML file for each screen, and one
<section data-screen="<screen>" data-state="<state>"> for each state.

For each state, the script renders the mockup in Chromium (Playwright) and runs the snapshot
extractor of OpenPencil on that section. The extractor reads the computed styles and the layout
boxes, so tables, grids, and wrapped rows keep their places. Then `op import:snapshot` puts the
state in the open OpenPencil document: one row of frames for each screen. At the end, `op
export-frames` writes a PNG of each frame to <dir>/frames for the review.

The extractor must match the importer. Without --extractor, the script downloads
crates/op-html/assets/snapshot-extractor.js of the tag that matches `op --version`, and keeps it in
<dir>. An OpenPencil app or `op start --headless` must run.

Known losses of the import: checkboxes and radio buttons, text-transform, and the text inside an input.
Exit code: 0 when each state is imported, 1 when a state fails, 2 for a usage or setup error.
"""
import argparse
import json
import subprocess
import sys
import urllib.request
from pathlib import Path

URL = "https://raw.githubusercontent.com/ZSeven-W/openpencil/v{v}/crates/op-html/assets/snapshot-extractor.js"


def op(args, cmd):
    try:
        r = subprocess.run([args.op, *cmd], capture_output=True, text=True, timeout=300)
    except FileNotFoundError:
        return 127, f"the op CLI ({args.op}) is not found"
    out = (r.stdout or "") + (r.stderr or "")
    try:
        return r.returncode, json.loads(r.stdout)
    except json.JSONDecodeError:
        return r.returncode, out.strip()


def extractor(args, out):
    if args.extractor:
        return Path(args.extractor).read_text(encoding="utf-8")
    code, ver = op(args, ["--version"])
    if code != 0 or not isinstance(ver, dict) or "version" not in ver:
        sys.exit(f"`{args.op} --version` failed. Install the op CLI first.")
    cached = out / f"snapshot-extractor-{ver['version']}.js"
    if not cached.exists():
        try:
            with urllib.request.urlopen(URL.format(v=ver["version"]), timeout=60) as r:
                cached.write_bytes(r.read())
        except OSError as e:
            sys.exit(f"Cannot download the extractor for op {ver['version']}: {e}. Use --extractor <file>.")
    return cached.read_text(encoding="utf-8")


def main(argv=None):
    p = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    p.add_argument("files", nargs="+")
    p.add_argument("--out", required=True)
    p.add_argument("--op", default="op")
    p.add_argument("--extractor")
    p.add_argument("--width", type=int, default=1440)
    args = p.parse_args(argv)
    out = Path(args.out).resolve()
    (out / "snapshots").mkdir(parents=True, exist_ok=True)
    code, status = op(args, ["status"])
    if not (isinstance(status, dict) and status.get("running")):
        if code == 127:
            print(f"{status}. Install it: see the OpenPencil README.", file=sys.stderr)
        else:
            print("No OpenPencil app or MCP server runs. Start one: op start --headless --file <file.op>", file=sys.stderr)
        return 2
    js = extractor(args, out).replace("var output = JSON.stringify(snapshot, null, 2);",
                                      "window.__opSnap = JSON.stringify(snapshot); return;")
    try:
        from playwright.sync_api import sync_playwright
    except ImportError:
        print("Python Playwright is missing: pip install playwright", file=sys.stderr)
        return 2
    done, failed, y = [], [], 0
    with sync_playwright() as pw:
        browser = pw.chromium.launch()
        page = browser.new_page(viewport={"width": args.width, "height": 900})
        for f in args.files:
            page.goto(Path(f).resolve().as_uri())
            states = page.eval_on_selector_all("section[data-state]", "e => e.map(x => [x.dataset.screen || '', x.dataset.state, x.getBoundingClientRect().height])")
            x, row_h = 0, 0
            for i, (screen, state, h) in enumerate(states):
                screen = screen or Path(f).stem
                page.evaluate("(i) => { window.__opSnap = null; globalThis.openpencilSnapshotOptions = {root: document.querySelectorAll('section[data-state]')[i]}; }", i)
                page.evaluate(js)
                snap = page.evaluate("() => window.__opSnap")
                sp = out / "snapshots" / f"{screen}--{state}.json"
                if not snap:
                    failed.append(f"{screen}/{state}: the extractor gave no snapshot")
                    continue
                sp.write_text(snap, encoding="utf-8")
                c, res = op(args, ["import:snapshot", str(sp), "--x", str(x), "--y", str(y)])
                if c != 0 or not isinstance(res, dict):
                    failed.append(f"{screen}/{state}: {str(res)[:200]}")
                else:
                    done.append({"screen": screen, "state": state, "x": x, "y": y, "nodes": res.get("nodeCount")})
                x += args.width + 160
                row_h = max(row_h, int(h))
            y += row_h + 320
        browser.close()
    # Name each new top-level frame "<screen> / <state>", in the order of the import.
    c, tree = op(args, ["get", "--depth", "0"])
    tops = [n["id"] for n in tree.get("nodes", []) if n.get("type") == "frame"] if isinstance(tree, dict) else []
    for node_id, rec in zip(tops[len(tops) - len(done):], done):
        op(args, ["set_node_name", f"node_id={node_id}", f"name={rec['screen']} / {rec['state']}"])
        rec["node"] = node_id
    (out / "frames").mkdir(exist_ok=True)
    c, res = op(args, ["export-frames", "--output-dir", str(out / "frames")])
    (out / "import.json").write_text(json.dumps({"imported": done, "failed": failed, "export": res}, indent=2), encoding="utf-8")
    print(f"Imported {len(done)} states, {len(failed)} failed. Frames in {out / 'frames'}.")
    for m in failed:
        print("FAILED " + m)
    print("Known losses: checkboxes and radio buttons, text-transform, and the text inside an input.")
    return 1 if failed else 0


if __name__ == "__main__":
    sys.exit(main())
