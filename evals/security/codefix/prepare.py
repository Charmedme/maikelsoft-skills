#!/usr/bin/env python3
"""Make the run folder of the codefix choice test.

    python3 evals/security/codefix/prepare.py <runs> [--models haiku sonnet] [--repeats 3] [--set-size 5]

The challenges of one skill go into sets of at most --set-size challenges. One run answers one set.
<runs>/tasks/<set>.md is the task text of a set. <runs>/arms/<skill>/ is the skill folder of the working
tree. <runs>/plan.json has one entry for each run, for the workflow. answers.json stays out of <runs>.
"""
import argparse
import json
import pathlib
import shutil

HERE = pathlib.Path(__file__).resolve().parent
ROOT = HERE.parents[2]
SKILL_DIRS = {"secure-coding": ROOT / "plugins/dev-stack/skills/secure-coding",
              "web-security": ROOT / "plugins/frontend-stack/skills/web-security"}

INTRO = """Each challenge below shows code with a security defect, and 3 or 4 candidate fixes as diffs against
that code. For each challenge, pick the one option that fixes the defect best. A fix that breaks the
feature, or that only hides the defect, is not the best fix. Give the reason in at most 2 sentences.
"""


def task_text(challenges: list) -> str:
    parts = [INTRO]
    for c in challenges:
        parts.append(f"\n## {c['id']}\n\nCode ({c['language']}):\n\n```{c['language']}\n{c['original']}```\n")
        for letter, diff in c["options"].items():
            parts.append(f"\nOption {letter}:\n\n```diff\n{diff}```\n")
    return "".join(parts)


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("runs")
    parser.add_argument("--models", nargs="+", default=["haiku", "sonnet"])
    parser.add_argument("--repeats", type=int, default=3)
    parser.add_argument("--set-size", type=int, default=5)
    args = parser.parse_args()
    runs = pathlib.Path(args.runs).resolve()
    (runs / "tasks").mkdir(parents=True, exist_ok=True)
    challenges = json.loads((HERE / "challenges.json").read_text(encoding="utf-8"))

    sets = {}
    for skill in SKILL_DIRS:
        mine = [c for c in challenges if c["skill"] == skill]
        count = -(-len(mine) // args.set_size)
        for i in range(count):
            chunk = mine[i::count]
            sid = f"{skill}-{i + 1}"
            sets[sid] = {"skill": skill, "ids": [c["id"] for c in chunk]}
            (runs / "tasks" / f"{sid}.md").write_text(task_text(chunk), encoding="utf-8")
        arm = runs / "arms" / skill
        if not arm.exists():
            shutil.copytree(SKILL_DIRS[skill], arm)

    plan = []
    for n in range(1, args.repeats + 1):
        for model in args.models:
            for sid, s in sets.items():
                for arm in ("baseline", "skill"):
                    plan.append({"id": f"{sid}/{arm}/{model}-{n}", "set": sid, "arm": arm, "model": model,
                                 "skill": s["skill"] if arm == "skill" else None, "ids": s["ids"]})
    (runs / "sets.json").write_text(json.dumps(sets, indent=1), encoding="utf-8")
    (runs / "plan.json").write_text(json.dumps(plan, indent=1), encoding="utf-8")
    print(f"{len(sets)} sets, {len(plan)} runs in {runs / 'plan.json'}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
