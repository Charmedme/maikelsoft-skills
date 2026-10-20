#!/usr/bin/env python3
"""Make the run folders of the dev-stack evals and start one headless claude run in each.

    python3 evals/dev-stack/run.py <runs> [--eval <name> ...] [--variant baseline skill]
                                   [--runs-per-variant 3] [--parallel 4] [--budget 10] [--model opus]

Each run gets <runs>/<eval>/<variant>/<n>/repo (made by prepare.make_repo) and writes stream.jsonl,
stderr.log and final.md next to it. The prompt is the common part of runner.md, with the skill line
for the variant 'skill'. A run folder that has a final.md is skipped.
The runs cannot use the Agent and Skill tools: the account skills of the machine (for example a personal
csharp or valkey-caching skill) must not reach either variant. The skill variant reads SKILL.md by path.
Then score with: python3 evals/dev-stack/score.py <runs>

Needs claude, git, the dotnet CLI, and Docker.
"""
import argparse
import concurrent.futures
import json
import os
import pathlib
import re
import subprocess
import sys

HERE = pathlib.Path(__file__).resolve().parent
ROOT = HERE.parents[1]
sys.path.insert(0, str(HERE))
import prepare  # noqa: E402

# Variables of a parent Claude Code session. A child run that inherits them shares the session id
# of the parent and sends foreground agents to the background.
PARENT_VARS = ["CLAUDE_CODE_SESSION_ID", "CLAUDE_CODE_REMOTE_SESSION_ID", "CLAUDE_AUTO_BACKGROUND_TASKS",
               "CLAUDE_CODE_BG_TASKS_REPORT_RUNNING", "CLAUDE_CODE_MESSAGING_SOCKET", "CLAUDE_CODE_MESSAGING_TOKEN",
               "CLAUDECODE", "CLAUDE_CODE_CHILD_SESSION", "CLAUDE_CODE_POST_TURN_MEMORY",
               "CLAUDE_CODE_POST_TURN_MEMORY_CONFIG", "CLAUDE_CODE_TEE_SDK_STDOUT", "CLAUDE_CODE_INCLUDE_PARTIAL_MESSAGES",
               "CLAUDE_AFTER_LAST_COMPACT"]
KEEP_IMAGES = {"postgres", "mcr.microsoft.com/mssql/server", "valkey/valkey", "elasticsearch", "rabbitmq",
               "axllent/mailpit", "testcontainers/ryuk", "mcr.microsoft.com/dotnet/aspnet", "mcr.microsoft.com/dotnet/sdk"}
SUFFIX = ("\n\nDo not call any tool whose name starts with mcp__Agent_Mesh. "
          "Those tools reach a live system that is not part of this task.")


def common_part():
    text = (HERE / "runner.md").read_text(encoding="utf-8")
    m = re.search(r"## Common part\n\n(.*?)\n## Variant: skill", text, re.S)
    return m.group(1).strip()


def prompt_for(ev, variant, repo):
    text = common_part().replace("<repo>", str(repo)).replace("<request>", ev["request"].replace("\n", "\n> "))
    if variant == "skill":
        skill = ROOT / "plugins" / "dev-stack" / "skills" / ev["skill"]
        line = f"The skill is in `{skill}`. Read `{skill}/SKILL.md` and follow it."
        text = text.replace("\nNobody can answer", f"\n{line}\n\nNobody can answer", 1)
    return text + SUFFIX


def final_message(stream):
    final = ""
    for line in stream.read_text(encoding="utf-8", errors="ignore").splitlines():
        if '"type":"result"' in line.replace(" ", ""):
            try:
                final = json.loads(line).get("result", final)
            except ValueError:
                pass
    return final


def clean(repo):
    """Free disk space after a run: the scorer builds again, and Docker keeps volumes of stopped containers."""
    for name in ("bin", "obj", "TestResults"):
        for folder in list(repo.rglob(name)):
            if folder.is_dir():
                subprocess.run(["rm", "-rf", str(folder)])
    subprocess.run(["docker", "volume", "prune", "-f"], capture_output=True)
    # A run can build its own images (for example from a Dockerfile). Keep only the images of the cases.
    out = subprocess.run(["docker", "images", "--format", "{{.Repository}}:{{.Tag}}"], capture_output=True, text=True).stdout
    for image in out.split():
        if image.split(":")[0] not in KEEP_IMAGES:
            subprocess.run(["docker", "rmi", image], capture_output=True)
    subprocess.run(["docker", "builder", "prune", "-af"], capture_output=True)


def start(run, ev, variant, args):
    repo = run / "repo"
    if not repo.exists():
        prepare.make_repo(ev, repo)
    cmd = ["claude", "-p", prompt_for(ev, variant, repo), "--model", args.model,
           "--permission-mode", "bypassPermissions", "--disallowedTools", "Agent", "Skill",
           "--output-format", "stream-json", "--verbose", "--max-budget-usd", str(args.budget)]
    env = {k: v for k, v in os.environ.items() if k not in PARENT_VARS}
    env["PATH"] = env.get("PATH", "") + os.pathsep + str(pathlib.Path.home() / ".dotnet")
    with open(run / "stream.jsonl", "w") as out, open(run / "stderr.log", "w") as err:
        try:
            code = subprocess.run(cmd, cwd=repo, env=env, stdout=out, stderr=err, timeout=args.timeout).returncode
        except subprocess.TimeoutExpired:
            code = "timeout"
    (run / "final.md").write_text(final_message(run / "stream.jsonl"), encoding="utf-8")
    clean(repo)
    return f"{run.relative_to(args.runs)} exit {code}"


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("runs", type=pathlib.Path)
    parser.add_argument("--eval", nargs="*")
    parser.add_argument("--variant", nargs="*", default=["baseline", "skill"])
    parser.add_argument("--runs-per-variant", type=int, default=3)
    parser.add_argument("--parallel", type=int, default=4)
    parser.add_argument("--budget", type=float, default=10)
    parser.add_argument("--model", default="opus")
    parser.add_argument("--timeout", type=int, default=3600)
    args = parser.parse_args(argv)
    evals = json.loads((HERE / "evals.json").read_text(encoding="utf-8"))["evals"]
    jobs = []
    for ev in evals:
        if args.eval and ev["name"] not in args.eval:
            continue
        for variant in args.variant:
            for n in range(1, args.runs_per_variant + 1):
                run = args.runs / ev["name"] / variant / str(n)
                if (run / "final.md").exists():
                    continue
                run.mkdir(parents=True, exist_ok=True)
                jobs.append((run, ev, variant))
    with concurrent.futures.ThreadPoolExecutor(args.parallel) as pool:
        for line in pool.map(lambda j: start(*j, args), jobs):
            print(line, flush=True)
    return 0


if __name__ == "__main__":
    sys.exit(main())
