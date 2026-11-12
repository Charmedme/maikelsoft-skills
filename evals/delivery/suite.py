#!/usr/bin/env python3
"""Delivery test suite: stories from easy to very hard on pinned real projects, with hidden tests.

    python3 evals/delivery/suite.py setup                       # clone the pinned fixtures into .cache
    python3 evals/delivery/suite.py verify [story ...]          # base fails, reference passes
    python3 evals/delivery/suite.py run <runs> --arm baseline [--story ...] [--repeat 1]
    python3 evals/delivery/suite.py score <runs>

A story folder holds story.md and answers.md (what the run sees), meta.json, base.patch (optional,
applied before the run: it removes a feature or adds project files), reference.patch (the known
solution), and hidden/ (the cases, which the run never sees). Each story must fail its hidden cases
on the base and pass them on the reference, or `verify` reports it as broken.

The cases use four layers: the case in domain words (hidden/test_*.py), a DSL (acceptance/dsl),
a protocol driver (acceptance/drivers), and the system under test, started here.
"""
import argparse
import base64
import concurrent.futures
import importlib.util
import json
import os
import pathlib
import shutil
import socket
import subprocess
import sys
import tempfile
import time
import urllib.request

HERE = pathlib.Path(__file__).resolve().parent
ROOT = HERE.parents[1]
CACHE = HERE / ".cache"
STORIES = HERE / "stories"
FIXTURES = json.loads((HERE / "fixtures.json").read_text(encoding="utf-8"))
GIT = ["git", "-c", "user.name=Dev", "-c", "user.email=dev@example.com", "-c", "commit.gpgsign=false", "-C"]
HURL_IMAGE = "ghcr.io/orange-opensource/hurl:latest"
sys.path.insert(0, str(HERE.parent))
import runwatch  # noqa: E402


def _load(name, path):
    spec = importlib.util.spec_from_file_location(name, path)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


runner = _load("reviewflow_run", HERE.parent / "review-flow" / "run.py")
ARMS = {"baseline": ["dev-stack"], "flow": ["dev-flow", "dev-stack"], "build": ["dev-flow", "dev-stack"],
        "buildspec": ["dev-flow", "dev-stack"], "flowspec": ["dev-flow", "dev-stack"],
        "team": ["dev-team", "dev-stack"], "both": ["dev-flow", "dev-team", "dev-stack"]}
NEUTRAL = "Deliver the story in {story}. The answer sheet of the human is {answers}. Take each answer of the human from that sheet. Work on a new branch and commit."
PROMPTS = {
    "baseline": NEUTRAL,
    "both": NEUTRAL,
    "flow": "Use the dev-flow:plan-and-build skill to build the approved item in {story}. The answer sheet of the human is {answers}. The human is also the product owner. Take each answer of the human from that sheet.",
    "build": "Use the dev-flow:build skill to build the item in {story}. The item file is the ticket. The answer sheet of the human is {answers}. The human is also the product owner. Take each answer of the human from that sheet. The sheet approves the item, the spec, and each gate.",
    "team": "Use the dev-team:deliver-story skill to deliver the story in {story}. The answer sheet of the human is {answers}. Take each answer of the human from that sheet.",
}
PROMPTS["buildspec"] = PROMPTS["build"]  # the same prompt: only the ticket differs (ticket.md with a ready Behaviour spec)
PROMPTS["flowspec"] = PROMPTS["flow"]  # the control: the old flow with the same ticket; run it with a _plugins folder of dev-flow 2.1.0
SUFFIX = " Nobody can answer questions in this run. Do not push. Do not call any tool whose name starts with mcp__. Work only inside the current folder and the input folder."


def sh(cmd, cwd=None, env=None, check=True, quiet=True):
    result = subprocess.run(cmd, cwd=cwd, env=env, text=True, capture_output=quiet)
    if check and result.returncode != 0:
        raise RuntimeError(f"{' '.join(map(str, cmd))} failed ({result.returncode}):\n{(result.stdout or '')[-2000:]}\n{(result.stderr or '')[-2000:]}")
    return result


# fixtures
def fixture_dir(name):
    return CACHE / "fixtures" / name


def setup(names=None):
    for name in names or ["conduit"]:
        config = FIXTURES[name]
        target = fixture_dir(name)
        if not target.exists():
            target.parent.mkdir(parents=True, exist_ok=True)
            sh(["git", "clone", "-q", config["url"], str(target)])
        sh(["git", "-C", str(target), "checkout", "-q", "--detach", config["commit"]])
        upstream = config.get("upstream_tests")
        if upstream:
            spec = CACHE / "upstream" / name
            if not spec.exists():
                sh(["git", "clone", "-q", upstream["url"], str(spec)])
            sh(["git", "-C", str(spec), "checkout", "-q", "--detach", upstream["commit"]])
        print(f"{name}: {config['commit'][:7]} ready")


def story_meta(story):
    return json.loads((STORIES / story / "meta.json").read_text(encoding="utf-8"))


def make_workspace(story, repo, with_reference=False):
    """A fresh git repo of the fixture with the story base applied, tagged eval-start."""
    meta = story_meta(story)
    config = FIXTURES[meta["fixture"]]
    source = fixture_dir(meta["fixture"])
    if repo.exists():
        shutil.rmtree(repo)
    shutil.copytree(source, repo, ignore=shutil.ignore_patterns(".git", "bin", "obj", "TestResults", "node_modules"))
    if config.get("patch"):
        sh(["git", "apply", "--whitespace=nowarn", str(HERE / config["patch"])], cwd=repo)
    for path in config.get("strip", []):
        target = repo / path
        if target.is_dir():
            shutil.rmtree(target)
        elif target.exists():
            target.unlink()
    sh(["git", "init", "-q", "-b", "main", str(repo)])
    base = STORIES / story / "base.patch"
    if base.exists():
        sh(["git", "-C", str(repo), "apply", "--whitespace=nowarn", str(base)])
    for args in (["add", "-A"], ["commit", "-qm", "Project at the start of the story"], ["tag", "eval-start"]):
        sh([*GIT, str(repo), *args])
    if with_reference:
        sh(["git", "-C", str(repo), "apply", "--whitespace=nowarn", str(STORIES / story / "reference.patch")])
    return repo


# system under test
def free_port():
    with socket.socket() as s:
        s.bind(("127.0.0.1", 0))
        return s.getsockname()[1]


class Conduit:
    """Starts the Conduit API of a workspace on a free port with its own SQLite file."""

    def __init__(self, repo, workdir, ui=False):
        self.repo, self.workdir = repo, workdir
        self.port = free_port()
        self.url = f"http://127.0.0.1:{self.port}"
        self.ui_url = None
        self.process = None

    def build(self):
        env = dict(os.environ, DOTNET_CLI_TELEMETRY_OPTOUT="1", DOTNET_NOLOGO="1")
        return sh(FIXTURES["conduit"]["build"], cwd=self.repo, env=env, check=False)

    def start(self):
        dll = self.repo / "src" / "Conduit" / "bin" / "Debug" / "net10.0" / "Conduit.dll"
        env = dict(os.environ,
                   Jwt__SigningKey=base64.b64encode(os.urandom(32)).decode(),
                   Database__Provider="sqlite",
                   ConnectionStrings__Conduit=f"Data Source={self.workdir / 'conduit.db'}",
                   ApiPrefix="api", ASPNETCORE_URLS=self.url, ASPNETCORE_ENVIRONMENT="Development")
        self.log = open(self.workdir / "sut.log", "w")
        self.process = subprocess.Popen(["dotnet", str(dll)], cwd=self.repo / "src" / "Conduit", env=env,
                                        stdout=self.log, stderr=subprocess.STDOUT)
        deadline = time.time() + 90
        while time.time() < deadline:
            if self.process.poll() is not None:
                return False
            try:
                with urllib.request.urlopen(self.url + "/api/tags", timeout=3) as reply:
                    if reply.status == 200:
                        return True
            except OSError:
                time.sleep(1)
        return False

    def stop(self):
        if self.process and self.process.poll() is None:
            self.process.terminate()
            try:
                self.process.wait(20)
            except subprocess.TimeoutExpired:
                self.process.kill()
        if getattr(self, "log", None):
            self.log.close()


class Dashboard:
    """Starts the dashboard WebAPI of a workspace with its own PostgreSQL container (eval/start.sh of the fixture)."""

    def __init__(self, repo, workdir, ui=False):
        self.repo, self.workdir, self.ui = repo, workdir, ui
        self.port = free_port()
        self.url = f"http://127.0.0.1:{self.port}"
        self.env = dict(os.environ, PCA_API_PORT=str(self.port), PCA_DB_PORT=str(free_port()),
                        PCA_INSTANCE=f"eval{self.port}", PCA_DATA_DIR=str(workdir / "pca"), TZ="UTC")
        self.ui_url = None
        if ui:
            ui_port = free_port()
            self.env["PCA_UI_PORT"] = str(ui_port)
            self.ui_url = f"http://127.0.0.1:{ui_port}"

    def build(self):
        env = dict(os.environ, DOTNET_CLI_TELEMETRY_OPTOUT="1", DOTNET_NOLOGO="1")
        built = sh(FIXTURES["dashboard"]["build"], cwd=self.repo, env=env, check=False)
        if built.returncode == 0 and self.ui:
            npm = sh(["npm", "ci", "--no-audit", "--no-fund"], cwd=self.repo / "src" / "UIs" / "angular", check=False)
            if npm.returncode != 0:
                return npm
        return built

    def start(self):
        result = sh(["bash", "eval/start.sh"], cwd=self.repo, env=self.env, check=False)
        (self.workdir / "sut.log").write_text((result.stdout or "") + (result.stderr or ""))
        return result.returncode == 0

    def stop(self):
        sh(["bash", "eval/stop.sh"], cwd=self.repo, env=self.env, check=False)


SYSTEMS = {"conduit": Conduit, "dashboard": Dashboard}


def hidden_results(story, repo, passes=1):
    """Build and start the system, run the hidden cases (and the listed upstream API files) each pass."""
    meta = story_meta(story)
    workdir = pathlib.Path(tempfile.mkdtemp(prefix=f"sut-{story}-"))
    system = SYSTEMS[meta["fixture"]](repo, workdir, ui=bool(meta.get("ui")))
    built = system.build()
    if built.returncode != 0:
        return None, "build failed: " + (built.stdout or "")[-1500:]
    merged, misses = {}, {}
    for _ in range(passes):
        for db in workdir.glob("conduit.db*"):
            db.unlink()  # the dashboard gets a new database on each start
        if not system.start():
            system.stop()
            return None, "start failed: " + (workdir / "sut.log").read_text(errors="ignore")[-1500:]
        try:
            outcomes = run_cases(story, system.url, system.ui_url)
            outcomes.update(run_hurl(meta, system.url))
        finally:
            system.stop()
        for name, (outcome, message) in outcomes.items():
            if outcome != "Passed":
                misses[name] = misses.get(name, 0) + 1
            if name not in merged or outcome != "Passed":
                merged[name] = [outcome, message]
    return merged, ""


def run_cases(story, url, ui_url=None):
    folder = STORIES / story / "hidden"
    if not folder.is_dir():
        return {}
    env = dict(os.environ, SUT_URL=url)
    if ui_url:
        env["UI_URL"] = ui_url
    result = sh([sys.executable, "-I", str(HERE / "acceptance" / "run_cases.py"), str(folder)], env=env, check=False)
    try:
        return json.loads(result.stdout.strip().splitlines()[-1])
    except (ValueError, IndexError):
        return {"cases.load": ["Error", (result.stderr or "")[-400:]]}


def run_hurl(meta, url):
    files = meta.get("hurl") or []
    if not files:
        return {}
    spec = CACHE / "upstream" / meta["fixture"] / FIXTURES[meta["fixture"]]["upstream_tests"]["hurl"]
    outcomes = {}
    for name in files:
        cmd = ["docker", "run", "--rm", "--network", "host", "-v", f"{spec}:/h:ro", HURL_IMAGE, "--test", "--jobs", "1",
               "--variable", f"host={url}", "--variable", f"uid={int(time.time() * 1000)}{os.getpid()}", f"/h/{name}"]
        result = sh(cmd, check=False)
        lines = ((result.stderr or "") + (result.stdout or "")).strip().splitlines()
        # the first error with its source line and the assert says more than the summary
        detail = [line.strip() for line in lines if line.startswith("error") or "|" in line and ("actual" in line or "expected" in line or "HTTP" in line or "jsonpath" in line)]
        outcomes[f"upstream.{name}"] = ["Passed" if result.returncode == 0 else "Failed", " | ".join(detail[:6] or lines[-3:])[:600]]
    return outcomes


# verify
def verify(stories, keep, labels=("base", "reference")):
    report = {}
    for story in stories:
        entry = {}
        for label, with_reference in (("base", False), ("reference", True)):
            if label not in labels:
                continue
            repo = pathlib.Path(tempfile.mkdtemp(prefix=f"verify-{story}-{label}-")) / "repo"
            try:
                make_workspace(story, repo, with_reference)
            except RuntimeError as error:
                entry[label] = {"error": str(error)[:600]}
                continue
            results, note = hidden_results(story, repo, passes=1 if label == "base" else 3)
            if results is None:
                entry[label] = {"error": note[-600:]}
                continue
            failed = sorted(name for name, (outcome, _) in results.items() if outcome != "Passed")
            entry[label] = {"cases": len(results), "failed": failed,
                            "messages": {name: results[name][1] for name in failed}}
            if label == "reference" and FIXTURES[story_meta(story)["fixture"]].get("test"):
                own = sh(FIXTURES[story_meta(story)["fixture"]]["test"], cwd=repo, check=False)
                entry[label]["own_tests"] = "pass" if own.returncode == 0 else "FAIL: " + (own.stdout or "")[-600:]
            if not keep:
                shutil.rmtree(repo.parent, ignore_errors=True)
        base_ok = "failed" in entry.get("base", {}) and entry["base"]["failed"]
        ref_ok = "failed" in entry.get("reference", {}) and not entry["reference"]["failed"] and entry["reference"].get("own_tests", "pass") == "pass"
        entry["valid"] = bool(base_ok and ref_ok)
        report[story] = entry
        print(story, json.dumps(entry), flush=True)
    return report


# runs
def one(run, story, arm, args):
    repo = run / "repo"
    if not repo.exists():
        make_workspace(story, repo)
    inputs = run / "input"
    inputs.mkdir(exist_ok=True)
    for name in ("story.md", "answers.md"):
        shutil.copy(STORIES / story / name, inputs / name)
    if arm in ("buildspec", "flowspec"):  # the ticket with the reviewed Behaviour spec takes the place of story.md
        ticket = STORIES / story / "ticket.md"
        if not ticket.exists():
            raise FileNotFoundError(f"{ticket} is missing: the arms buildspec and flowspec need it")
        shutil.copy(ticket, inputs / "story.md")
    env = runner.environment()
    profile = run / "profile.md"
    profile.write_text("# Dev-team profile\n\n| ID | Answer | Date |\n|----|--------|------|\n", encoding="utf-8")
    env["DEV_TEAM_PROFILE"] = str(profile)
    env["DEV_FLOW_HELDBACK"] = str(run / "heldback")  # the held-back tests of dev-flow:build stay in the run
    before = runner.fingerprint(runner.REAL_PROFILE)
    prompt = PROMPTS[arm].format(story=inputs / "story.md", answers=inputs / "answers.md") + SUFFIX
    cmd = ["claude", "-p", prompt, "--model", args.model, "--permission-mode", "bypassPermissions",
           "--setting-sources", "project,local", "--output-format", "stream-json", "--verbose", "--max-budget-usd", str(args.budget)]
    for name in ARMS[arm]:
        cmd += ["--plugin-dir", runner.pinned_copy(ROOT / "plugins" / name, args.runs / "_plugins" / name, args.model)]
    code = runwatch.run_watched(cmd, repo, env, run, {"max_minutes": args.max_minutes, "stall_minutes": 12})
    (run / "final.md").write_text(runner.final_message(run / "stream.jsonl"), encoding="utf-8")
    if runner.fingerprint(runner.REAL_PROFILE) != before:
        raise runner.ProfileChanged(f"{run} changed {runner.REAL_PROFILE}. Restore the file before the next run.")
    return f"{run.relative_to(args.runs)} exit {code}"


def stream_facts(stream):
    facts = {"cost": None, "minutes": None, "skills": [], "agents": [], "peeked": False}
    if not stream.is_file():
        return facts
    text = stream.read_text(encoding="utf-8", errors="ignore")
    facts["peeked"] = "evals/delivery" in text or "delivery/stories" in text
    for line in text.splitlines():
        try:
            event = json.loads(line)
        except ValueError:
            continue
        if event.get("type") == "result":
            facts["cost"] = event.get("total_cost_usd")
            facts["minutes"] = round((event.get("duration_ms") or 0) / 60000, 1)
        elif event.get("type") == "assistant":
            for block in (event.get("message") or {}).get("content") or []:
                if isinstance(block, dict) and block.get("type") == "tool_use":
                    tool_input = block.get("input") or {}
                    if block.get("name") == "Skill":
                        facts["skills"].append(tool_input.get("skill", "?"))
                    elif block.get("name") in ("Agent", "Task"):
                        facts["agents"].append(tool_input.get("subagent_type", "?"))
    return facts


def score(runs, passes):
    table = {}
    for stream in sorted(runs.glob("*/*/*/stream.jsonl")):
        run = stream.parent
        story, arm, number = run.relative_to(runs).parts
        facts = stream_facts(stream)
        repo = run / "repo"
        facts["commits"] = len(sh([*GIT, str(repo), "log", "--oneline", "--all", "--not", "eval-start"], check=False).stdout.splitlines())
        results, note = hidden_results(story, repo, passes)
        if results is None:
            facts.update({"built": False, "note": note[-400:], "passed": "0/0", "failed": []})
        else:
            failed = sorted(name for name, (outcome, _) in results.items() if outcome != "Passed")
            facts.update({"built": True, "note": "", "passed": f"{len(results) - len(failed)}/{len(results)}", "failed": failed})
        table[f"{story}/{arm}/{number}"] = facts
        print(f"{story}/{arm}/{number}", json.dumps(facts), flush=True)
    (runs / "scores.json").write_text(json.dumps(table, indent=1), encoding="utf-8")


def all_stories():
    return sorted(p.name for p in STORIES.iterdir() if (p / "meta.json").exists())


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    sub = parser.add_subparsers(dest="command", required=True)
    setup_cmd = sub.add_parser("setup")
    setup_cmd.add_argument("fixture", nargs="*")
    verify_cmd = sub.add_parser("verify")
    verify_cmd.add_argument("story", nargs="*")
    verify_cmd.add_argument("--keep", action="store_true")
    verify_cmd.add_argument("--out", type=pathlib.Path)
    verify_cmd.add_argument("--only", choices=["base", "reference"])
    run_cmd = sub.add_parser("run")
    run_cmd.add_argument("runs", type=pathlib.Path)
    run_cmd.add_argument("--story", nargs="*")
    run_cmd.add_argument("--arm", nargs="*", default=["baseline"])
    run_cmd.add_argument("--repeat", type=int, default=1)
    run_cmd.add_argument("--first", type=int, default=1)
    run_cmd.add_argument("--parallel", type=int, default=2)
    run_cmd.add_argument("--budget", type=float, default=10)
    run_cmd.add_argument("--max-minutes", type=float, default=45)
    run_cmd.add_argument("--model", default="sonnet")
    score_cmd = sub.add_parser("score")
    score_cmd.add_argument("runs", type=pathlib.Path)
    score_cmd.add_argument("--passes", type=int, default=2)
    args = parser.parse_args(argv)
    dotnet_root = pathlib.Path.home() / ".dotnet"
    os.environ.setdefault("DOTNET_ROOT", str(dotnet_root))
    if str(dotnet_root) not in os.environ.get("PATH", "").split(os.pathsep):
        os.environ["PATH"] = str(dotnet_root) + os.pathsep + os.environ.get("PATH", "")
    if args.command == "setup":
        setup(args.fixture or None)
    elif args.command == "verify":
        report = verify(args.story or all_stories(), args.keep, [args.only] if args.only else ("base", "reference"))
        if args.out:
            args.out.write_text(json.dumps(report, indent=1), encoding="utf-8")
    elif args.command == "score":
        score(args.runs.resolve(), args.passes)
    else:
        args.runs = args.runs.resolve()
        jobs = []
        for story in args.story or all_stories():
            for arm in args.arm:
                for number in range(args.first, args.first + args.repeat):
                    run = args.runs / story / arm / str(number)
                    if not (run / "final.md").exists():
                        run.mkdir(parents=True, exist_ok=True)
                        jobs.append((run, story, arm))
        # copy each plugin once before the runs start, so that parallel runs do not copy it at the same time
        for name in {plugin for arm in args.arm for plugin in ARMS[arm]}:
            runner.pinned_copy(ROOT / "plugins" / name, args.runs / "_plugins" / name, args.model)
        with concurrent.futures.ThreadPoolExecutor(args.parallel) as pool:
            for line in pool.map(lambda job: one(*job, args), jobs):
                print(line, flush=True)
    return 0


if __name__ == "__main__":
    sys.exit(main())
