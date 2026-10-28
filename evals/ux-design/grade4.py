#!/usr/bin/env python3
"""Grade the `judge` assertions of the ux-design 0.4.0 cases: one grader run for each eval run.

    python3 evals/ux-design/grade4.py <runs> [--model claude-sonnet-5-5] [--parallel 4]

Run score4.py first (it makes the screenshots). The grader sees the context of the case and the
output of one run, without the variant name. It writes grade.json in the run folder; a run that
has one is skipped.
"""
import argparse
import concurrent.futures as cf
import json
import re
import shutil
import subprocess
import sys
from pathlib import Path

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
from run import child_env  # noqa: E402

SLUG = {"party-new-direction": "ndk-12-new-direction", "party-review-faults": "ndk-14-lobby-stand"}

DIRECTION = """You are a senior visual designer. You judge the output of one design run. Judge the content, not the length.

The product is a Dutch party card game. Its existing look is the "Gameshow" foundation: `context/tokens.css`,
`context/components.css`, and the current templates in `context/*.component.html`. The owner asked for
3 concepts with "a totally new approach", "not an updated version of what is there now".

The run output is in `run/`: the design file `run/ux-design.md`, the mockups `run/mockups/*.html` (and any CSS
that they use), and screenshots in `run/shots/*.png` (look at each image). The reply of the run is `run/final.md`.

List each style direction or concept that the run proposes (from the Style direction table, the Options table,
or the mockups). For each, decide against the existing Gameshow look:
- color: true when the palette changes (other base and accent hues than the purple stage #1a0b3b with pink #e01f63, yellow #ffd23f, and violet #8a6cff).
- type: true when the display and the body font families change (not Bungee and Nunito).
- card: true when the card look changes (not a white card with a 3 px dark border, radius about 14 px, and a hard drop shadow).
Use the mockups and the screenshots when they exist; else use the text of the design.

J1 passes when at least one direction or concept has color, type, and card all true.

Reply with only one JSON object:
{"directions": [{"name": "...", "color": true, "type": true, "card": true, "reason": "..."}], "J1": {"pass": true, "which": "...", "reason": "..."}}"""

FAULTS = """You are a senior UX and accessibility reviewer. You judge the output of one mockup run. Judge the content, not the length.

The input of the run was an approved design, `context/ux-design-approved.md`, on the Gameshow tokens
(`context/tokens.css`, `context/components.css`). The design has planted faults and two decoys. The run output is in
`run/`: the work folder (the design file, which the run may have changed, `mockups/*.html`, `critique.md`, and other
notes) and screenshots in `run/shots/*.png`. The reply of the run is `run/final.md`. Read each text file of `run/`.

Planted faults:
- F1: raised surfaces #2e1a63 on the stage #1a0b3b (about 1.3:1) with no border, for the player tiles and the doelscore segmented control. No visible edge.
- F2: the name input has a border #42277f on #1a0b3b (about 1.6:1), below the 3:1 for a control edge.
- F3: the same element is in a different place on each screen: the music toggle is in the top bar of the lobby but at the bottom of the stand panel; "Spel verlaten" is at the bottom of the lobby but in the top bar of the stand.
- F4: music, chat, and microphone are text toggles ("Muziek: aan"), not icon buttons with an on and off state (aria-pressed).
- F5: the icon-only game sounds toggle changes its accessible name ("Dempen" / "Dempen opheffen") instead of a fixed name with a state.
Decoys (NOT faults):
- D1: the decorative card stack (#0d0620 on #1a0b3b, aria-hidden, not interactive). Decoration needs no 3:1.
- D2: the text button "Spelregels" without a border. A control with a visible text label needs no edge.

For each fault, "named" is true when the run output says that this is a problem, or says that the run changed it
for that reason (in the critique, the notes, a changed design, comments in the mockups, or the reply). A silent
change without a reason is not "named". Quote the words.
For each decoy, "flagged" is true when the run output calls it a fault or a contrast or edge problem that needs a fix.

Reply with only one JSON object:
{"F1": {"named": true, "quote": "..."}, "F2": {...}, "F3": {...}, "F4": {...}, "F5": {...}, "D1": {"flagged": false, "quote": "..."}, "D2": {...}}"""


def stage(run, case):
    dst = run / "grading"
    if dst.exists():
        shutil.rmtree(dst)
    repo = run / "repo"
    fixture = HERE / "fixtures" / case
    ctx = dst / "context"
    ctx.mkdir(parents=True)
    shutil.copy(fixture / "design-system" / "gameshow" / "tokens.css", ctx)
    shutil.copy(fixture / "design-system" / "gameshow" / "components.css", ctx)
    work = repo / "docs" / "work" / SLUG[case]
    out = dst / "run"
    shutil.copytree(work, out, ignore=shutil.ignore_patterns("shots", "*.png", "story.md"), dirs_exist_ok=True)
    if case == "party-new-direction":
        for t in (fixture / "src" / "app").glob("*/*.html"):
            shutil.copy(t, ctx / t.name)
        for extra in ("design-system",):  # a new foundation that the run made outside the work folder
            src = repo / extra
            for f in src.rglob("*.css") if src.exists() else []:
                if "gameshow" not in f.parts:
                    (out / "foundation").mkdir(exist_ok=True)
                    shutil.copy(f, out / "foundation" / f.name)
    else:
        shutil.copy(fixture / "docs" / "work" / SLUG[case] / "ux-design.md", ctx / "ux-design-approved.md")
    (out / "shots").mkdir(exist_ok=True)
    for png in sorted((run / "shots").glob("*.png")):
        shutil.copy(png, out / "shots" / png.name)
    shutil.copy(run / "final.md", out / "final.md")
    return dst


def grade(run, case, model):
    target = run / "grade.json"
    if target.exists():
        return json.loads(target.read_text())
    work = stage(run, case)
    prompt = DIRECTION if case == "party-new-direction" else FAULTS
    res = {"_error": "no answer"}
    for _ in range(2):
        cmd = ["claude", "-p", prompt, "--model", model, "--permission-mode", "default",
               "--allowedTools", "Read", "Glob", "Grep", "--disallowedTools", "Write", "Edit", "Bash", "Agent", "Skill",
               "--output-format", "json"]
        r = subprocess.run(cmd, cwd=work, env=child_env(), capture_output=True, text=True, timeout=1800)
        text, cost = "", 0.0
        try:
            ev = json.loads(r.stdout)
            text, cost = ev.get("result") or "", ev.get("total_cost_usd") or 0.0
        except json.JSONDecodeError:
            pass
        try:
            res = json.loads(re.search(r"\{.*\}", text, re.S).group(0))
            res["_cost"] = cost
            break
        except (json.JSONDecodeError, AttributeError):
            res = {"_error": (r.stdout + r.stderr)[-300:]}
    target.write_text(json.dumps(res, indent=2, ensure_ascii=False), encoding="utf-8")
    return res


def main():
    p = argparse.ArgumentParser()
    p.add_argument("runs")
    p.add_argument("--model", default="claude-sonnet-5-5")
    p.add_argument("--parallel", type=int, default=4)
    p.add_argument("--variant", nargs="*", default=["old", "new"])
    args = p.parse_args()
    root = Path(args.runs)
    jobs = [(m.parent, case) for case in SLUG for v in args.variant for m in sorted((root / case / v).glob("*/meta.json"))]
    with cf.ThreadPoolExecutor(max_workers=args.parallel) as ex:
        for (run, case), res in zip(jobs, ex.map(lambda j: grade(j[0], j[1], args.model), jobs)):
            print(case, run.parent.name, run.name, json.dumps(res, ensure_ascii=False)[:400], flush=True)


if __name__ == "__main__":
    main()
