#!/usr/bin/env python3
"""Find the build, test, format, and lint tools of a repository, run them, and write one checks file.

    python3 checks.py                         # the current folder, write checks.md
    python3 checks.py --root <repo> --out <file>
    python3 checks.py --only build,test       # run some checks only
    python3 checks.py --list                  # show the checks that apply, run nothing

Checks (each runs only when the repository has the tool):
    build    dotnet build -warnaserror (a .sln, .slnx, or .csproj file)
    test     dotnet test
    format   dotnet format --verify-no-changes
    npm      npm run build, npm test, npm run lint (scripts in package.json that exist)
    tsc      npx tsc --noEmit (a tsconfig.json file)
    docker   hadolint (a Dockerfile, when hadolint is installed)
    k8s      kubeconform (Kubernetes manifests, when kubeconform is installed)

Output file: a Markdown table with one row per check (pass, fail, or skipped), the command,
and the last lines of the output of each failed check. The run time is in the file, so a
later reader can compare it with the time of the last change.
Exit code: 1 when a check fails, else 0.

Python 3.9 or later, standard library only.
"""
import argparse
import datetime
import json
import os
import pathlib
import shutil
import subprocess
import sys

SKIP_DIRS = {".git", "bin", "obj", "node_modules", "dist", ".angular", "TestResults", "coverage"}
TAIL_LINES = 15
TIMEOUT_SECONDS = {"build": 900, "test": 1800, "format": 600}
DEFAULT_TIMEOUT = 900


def find(root, patterns, max_depth=4):
    """Return the files below root that match one of the glob patterns, skipping build folders."""
    hits = []
    for dirpath, dirnames, filenames in os.walk(root):
        rel = pathlib.Path(dirpath).relative_to(root)
        dirnames[:] = [d for d in dirnames if d not in SKIP_DIRS and not d.startswith(".")]
        if len(rel.parts) >= max_depth:
            dirnames[:] = []
        for name in filenames:
            if any(pathlib.PurePath(name).match(p) for p in patterns):
                hits.append(pathlib.Path(dirpath) / name)
    return sorted(hits)


def dotnet_target(root):
    """Return the solution or project file to build, or None. One solution at the top wins."""
    top = sorted(list(root.glob("*.slnx")) + list(root.glob("*.sln")))
    if len(top) == 1:
        return top[0]
    if top:
        return None  # more than one solution: let the human choose
    projects = find(root, ["*.csproj"])
    return projects[0] if len(projects) == 1 else None


def plan(root):
    """Return the list of (name, command, reason) for the checks that apply to this repository."""
    checks = []
    has_dotnet = bool(find(root, ["*.csproj", "*.sln", "*.slnx"]))
    if has_dotnet:
        target = dotnet_target(root)
        arg = [str(target.relative_to(root))] if target else []
        checks.append(("build", ["dotnet", "build", *arg, "-warnaserror", "--nologo"], ".NET project"))
        checks.append(("test", ["dotnet", "test", *arg], ".NET project"))
        checks.append(("format", ["dotnet", "format", *arg, "--verify-no-changes"], ".NET project"))
    package_json = root / "package.json"
    if package_json.is_file():
        try:
            scripts = json.loads(package_json.read_text(encoding="utf-8")).get("scripts", {})
        except ValueError:
            scripts = {}
        for script in ("build", "test", "lint"):
            if script in scripts:
                cmd = ["npm", "run", script] if script != "test" else ["npm", "test", "--", "--watch=false"]
                checks.append((f"npm-{script}", cmd, "package.json script"))
        if (root / "tsconfig.json").is_file():
            checks.append(("tsc", ["npx", "--no-install", "tsc", "--noEmit"], "tsconfig.json"))
    dockerfiles = find(root, ["Dockerfile", "*.Dockerfile"])
    if dockerfiles and shutil.which("hadolint"):
        checks.append(("docker", ["hadolint", *[str(p.relative_to(root)) for p in dockerfiles]], "Dockerfile"))
    manifests = [p for p in find(root, ["*.yaml", "*.yml"]) if "kind:" in p.read_text(encoding="utf-8", errors="ignore")
                 and "apiVersion:" in p.read_text(encoding="utf-8", errors="ignore")]
    if manifests and shutil.which("kubeconform"):
        checks.append(("k8s", ["kubeconform", "-strict", "-summary", *[str(p.relative_to(root)) for p in manifests]],
                       "Kubernetes manifests"))
    return checks


def run(root, name, command):
    """Run one check. Return (status, tail of the output)."""
    timeout = TIMEOUT_SECONDS.get(name, DEFAULT_TIMEOUT)
    env = dict(os.environ, DOTNET_CLI_TELEMETRY_OPTOUT="1", MSBUILDDISABLENODEREUSE="1", CI="true")
    try:
        proc = subprocess.run(command, cwd=root, capture_output=True, text=True, timeout=timeout, env=env)
    except FileNotFoundError:
        return "skipped", f"{command[0]} is not installed"
    except subprocess.TimeoutExpired:
        return "fail", f"timeout after {timeout} s"
    lines = (proc.stdout + proc.stderr).strip().splitlines()
    return ("pass" if proc.returncode == 0 else "fail"), "\n".join(lines[-TAIL_LINES:])


def write_report(out, root, results):
    now = datetime.datetime.now(datetime.timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ")
    lines = [
        "# Checks",
        "",
        f"Run at {now} by `checks.py` in `{root}`.",
        "",
        "| Check | Result | Command |",
        "|-------|--------|---------|",
    ]
    for name, command, status, _ in results:
        lines.append(f"| {name} | {status} | `{' '.join(command)}` |")
    if not results:
        lines.append("| - | skipped | No build, test, or lint tool found. |")
    for name, _, status, tail in results:
        if status != "pass" and tail:
            lines += ["", f"## {name}: {status}", "", "```", tail, "```"]
    out.write_text("\n".join(lines) + "\n", encoding="utf-8")


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("--root", type=pathlib.Path, default=pathlib.Path.cwd())
    parser.add_argument("--out", type=pathlib.Path, default=None, help="default: <root>/checks.md")
    parser.add_argument("--only", default="", help="comma-separated check names")
    parser.add_argument("--list", action="store_true")
    args = parser.parse_args(argv)
    root = args.root.resolve()
    checks = plan(root)
    if args.only:
        wanted = {w.strip() for w in args.only.split(",") if w.strip()}
        checks = [c for c in checks if c[0] in wanted or c[0].split("-")[0] in wanted]
    if args.list:
        for name, command, reason in checks:
            print(f"{name}\t{' '.join(command)}\t({reason})")
        return 0
    results = []
    for name, command, _ in checks:
        status, tail = run(root, name, command)
        results.append((name, command, status, tail))
        print(f"{name}: {status}")
    out = args.out or (root / "checks.md")
    write_report(out, root, results)
    print(f"Wrote {out}")
    return 1 if any(r[2] == "fail" for r in results) else 0


if __name__ == "__main__":
    sys.exit(main())
