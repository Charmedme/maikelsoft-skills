#!/usr/bin/env python3
"""Make the run folders and the skill arms of the stack-split behaviour test.

    python3 evals/stack-split/prepare.py <runs> [--tag dev-stack-v1.3.0] [--models haiku sonnet opus] [--repeats 2]

<runs>/arms/current/<skill>/ is the skill folder of the tag (the "current" arm).
<runs>/arms/new/<skill>/ is the skill folder of the working tree (the "new" arm).
<runs>/w/<hash>/ is the work folder of one run, with the seed files of the scenario. The name hides the arm
from the judge.
The script writes <runs>/plan.json: one entry for each run, for the workflow.
"""
import argparse
import json
import pathlib
import shutil
import subprocess
import sys
import tarfile
import io
import hashlib

HERE = pathlib.Path(__file__).resolve().parent
ROOT = HERE.parents[1]
ARMS = ("baseline", "current", "new")


def copy_tag_skill(tag: str, skill: str, target: pathlib.Path) -> None:
    """Extract plugins/dev-stack/skills/<skill> of the tag into target."""
    prefix = f"plugins/dev-stack/skills/{skill}/"
    data = subprocess.run(["git", "-C", str(ROOT), "archive", "--format=tar", tag, prefix],
                          check=True, capture_output=True).stdout
    with tarfile.open(fileobj=io.BytesIO(data)) as tar:
        for member in tar.getmembers():
            if not member.isfile():
                continue
            rel = member.name[len(prefix):]
            out = target / rel
            out.parent.mkdir(parents=True, exist_ok=True)
            out.write_bytes(tar.extractfile(member).read())


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("runs")
    parser.add_argument("--tag", default="dev-stack-v1.3.0")
    parser.add_argument("--models", nargs="+", default=["haiku", "sonnet", "opus"])
    parser.add_argument("--repeats", type=int, default=2)
    parser.add_argument("--scenario", nargs="*", help="only these scenarios")
    args = parser.parse_args()

    runs = pathlib.Path(args.runs).resolve()
    scenarios = json.loads((HERE / "scenarios.json").read_text(encoding="utf-8"))
    if args.scenario:
        scenarios = {k: v for k, v in scenarios.items() if k in args.scenario}

    plan = []
    for sid, sc in scenarios.items():
        new_src = ROOT / "plugins" / sc["skill"].split("/")[0] / "skills" / sc["skill"].split("/")[1]
        if not (new_src / "SKILL.md").exists():
            print(f"{sid}: {new_src.relative_to(ROOT)}/SKILL.md does not exist", file=sys.stderr)
            return 1
        new_arm = runs / "arms" / "new" / new_src.name
        cur_arm = runs / "arms" / "current" / sc["current"]
        if not new_arm.exists():
            shutil.copytree(new_src, new_arm)
        if not cur_arm.exists():
            copy_tag_skill(args.tag, sc["current"], cur_arm)
        for arm in ARMS:
            for model in args.models:
                for n in range(1, args.repeats + 1):
                    run_id = f"{sid}/{arm}/{model}-{n}"
                    work = runs / "w" / hashlib.sha1(run_id.encode()).hexdigest()[:10]
                    work.mkdir(parents=True, exist_ok=True)
                    for rel, content in sc["seed"].items():
                        f = work / rel
                        f.parent.mkdir(parents=True, exist_ok=True)
                        if not f.exists():
                            f.write_text(content, encoding="utf-8")
                    skill_dir = {"baseline": None, "current": cur_arm, "new": new_arm}[arm]
                    plan.append({"id": run_id, "scenario": sid, "arm": arm, "model": model,
                                 "work": str(work), "skill": str(skill_dir) if skill_dir else None,
                                 "task": sc["task"]})
    (runs / "plan.json").write_text(json.dumps(plan, indent=1), encoding="utf-8")
    print(f"{len(plan)} runs in {runs / 'plan.json'}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
