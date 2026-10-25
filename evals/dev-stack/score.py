#!/usr/bin/env python3
"""Score the 'check' assertions of evals.json for each run of the dev-stack skills.

Layout of a runs folder:
    <runs>/<eval name>/<variant>/<run number>/repo/       the copy of the fixture after the run
    <runs>/<eval name>/<variant>/<run number>/final.md    the final message of the agent

Usage:
    python3 evals/dev-stack/score.py <runs>
    python3 evals/dev-stack/score.py --repo <folder> --eval <eval name> [--only <id or check> ...]

The first form scores all runs, prints one line for each run and a summary for each
eval and variant, and writes <run>/checks.json and <runs>/scores.json.
The second form scores one folder and prints the JSON result. With --only, it scores only the
named assertions (for example the grep checks, which do not build and do not change the folder).
grep_absent skips comment lines, unless the assertion has "comments": true.
file_exists needs "files", and takes "exclude" and a "contains" regex.
The 'judge' assertions need a grader agent (grader.md).

Needs Python 3 (standard library only), the dotnet CLI (SDK 10.0.402), and Docker for
the integration tests.
"""
import argparse
import fnmatch
import hashlib
import json
import os
import pathlib
import re
import shutil
import subprocess
import sys
import tempfile
import time
import xml.etree.ElementTree as ET
from collections import defaultdict
sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent))
import cases  # noqa: E402

HERE = pathlib.Path(__file__).resolve().parent
FIXTURES = HERE / "fixtures"
PROBES = HERE / "probes"
PG_IMAGE = "postgres:18.6-alpine"
NO_WARN_AS_ERROR = ["-p:TreatWarningsAsErrors=false", "-p:WarningsAsErrors=", "-p:MSBuildTreatWarningsAsErrors=false"]
SKIP_DIRS = {".git", "bin", "obj", "TestResults", "node_modules", ".vs", ".idea"}
DENIED = [
    (re.compile(r"^MediatR", re.I), None, "MediatR is not allowed"),
    (re.compile(r"^AutoMapper", re.I), None, "AutoMapper is not allowed"),
    (re.compile(r"^FluentAssertions$", re.I), 8, "FluentAssertions 8 or later needs a paid license"),
]
TEST_ATTR = re.compile(r"^\s*\[(?:TestMethod|DataTestMethod)\b", re.M)
TIMEOUT_BUILD = 600
TIMEOUT_TEST = 1200


def dotnet_env():
    env = dict(os.environ)
    root = env.get("DOTNET_ROOT", "/root/.dotnet")
    env["DOTNET_ROOT"] = root
    env["PATH"] = os.pathsep.join([root, os.path.join(root, "tools"), env.get("PATH", "")])
    env.setdefault("DOTNET_CLI_TELEMETRY_OPTOUT", "1")
    env.setdefault("DOTNET_NOLOGO", "1")
    env.setdefault("DOTNET_SKIP_FIRST_TIME_EXPERIENCE", "1")
    return env


def run(cmd, cwd, timeout):
    try:
        r = subprocess.run(cmd, cwd=cwd, capture_output=True, text=True, timeout=timeout, env=dotnet_env())
        return r.returncode, r.stdout + r.stderr
    except subprocess.TimeoutExpired:
        return 124, f"timeout after {timeout} s"
    except FileNotFoundError as e:
        return 127, str(e)


def tail(text, pattern=None, lines=3):
    rows = [l.strip() for l in text.splitlines() if l.strip()]
    if pattern:
        rows = [l for l in rows if re.search(pattern, l)] or rows
    return " | ".join(rows[-lines:])[:400]


def files(root):
    out = []
    for dirpath, dirnames, filenames in os.walk(root):
        dirnames[:] = [d for d in dirnames if d not in SKIP_DIRS]
        for name in filenames:
            out.append(pathlib.Path(dirpath, name).relative_to(root).as_posix())
    return sorted(out)


def match(rel, patterns):
    return any(fnmatch.fnmatch(rel, p) or (p.startswith("**/") and fnmatch.fnmatch(rel, p[3:])) for p in patterns)


def select(root, include, exclude=()):
    return [f for f in files(root) if match(f, include) and not match(f, exclude)]


def read(path):
    try:
        return path.read_text(encoding="utf-8", errors="ignore")
    except OSError:
        return ""


def packages(root):
    """Return {package name (lower): (name, version)} from CPM, csproj and props files."""
    found = {}
    for rel in select(root, ["**/*.csproj", "**/*.props", "**/*.targets", "*.csproj", "*.props"]):
        try:
            tree = ET.parse(root / rel)
        except ET.ParseError:
            continue
        for el in tree.iter():
            tag = el.tag.split("}")[-1]
            if tag not in ("PackageVersion", "PackageReference", "GlobalPackageReference"):
                continue
            name = el.get("Include") or el.get("Update")
            if not name:
                continue
            version = el.get("Version") or el.findtext("Version") or el.get("VersionOverride") or ""
            key = name.lower()
            if key not in found or (version and not found[key][1]):
                found[key] = (name, version)
    return found


def major(version):
    m = re.match(r"\[?\s*(\d+)", version or "")
    return int(m.group(1)) if m else None


def fixture_of(ev):
    return FIXTURES / ev["fixture"]


def check_package_policy(repo, ev, approved):
    before, after = packages(fixture_of(ev)), packages(repo)
    approved_l = {a.lower() for a in approved}
    problems = []
    for key, (name, version) in sorted(after.items()):
        for pattern, min_major, reason in DENIED:
            if pattern.search(name) and (min_major is None or (major(version) or 0) >= min_major):
                problems.append(f"{name} {version}: {reason}")
        if key not in before and key not in approved_l:
            problems.append(f"{name} {version}: new package without approval")
        elif key in before and before[key][1] and version and major(version) != major(before[key][1]) and key not in approved_l:
            problems.append(f"{name} {before[key][1]} -> {version}: major version change without approval")
    added = sorted(after[k][0] for k in after if k not in before)
    return not problems, {"added": added, "problems": problems}


HASH_COMMENT = (".editorconfig", ".sh", ".yml", ".yaml", ".ps1", ".gitignore", ".txt")
SLASH_COMMENT = (".cs", ".csx", ".js", ".ts", ".fs")
XML_COMMENT = (".csproj", ".props", ".targets", ".xml", ".config", ".slnx", ".resx", ".ruleset")


def code_part(rel, lines):
    """Yield (line number, line without comments). Comment lines become empty.
    - Shell and YAML: a line that starts with '#'.
    - In an .editorconfig file, a line that starts with '#' or with a semicolon.
    - C#: '//' comments (not in a string) and '/* */' blocks.
    - XML: '<!-- -->' blocks."""
    name = rel.rsplit("/", 1)[-1].lower()
    in_block = False
    for n, line in enumerate(lines, 1):
        if name.endswith(HASH_COMMENT) or name == ".editorconfig":
            starts = ("#", ";") if name == ".editorconfig" else ("#",)
            yield n, "" if line.lstrip().startswith(starts) else line
        elif name.endswith(SLASH_COMMENT):
            out, i = "", 0
            while i < len(line):
                if in_block:
                    end = line.find("*/", i)
                    if end < 0:
                        i = len(line)
                    else:
                        in_block, i = False, end + 2
                    continue
                if line.startswith("/*", i) and out.count('"') % 2 == 0:
                    in_block, i = True, i + 2
                elif line.startswith("//", i) and out.count('"') % 2 == 0:
                    break
                else:
                    out += line[i]
                    i += 1
            yield n, out
        elif name.endswith(XML_COMMENT):
            out, i = "", 0
            while i < len(line):
                if in_block:
                    end = line.find("-->", i)
                    in_block, i = (True, len(line)) if end < 0 else (False, end + 3)
                elif line.startswith("<!--", i):
                    in_block, i = True, i + 4
                else:
                    out += line[i]
                    i += 1
            yield n, out
        else:
            yield n, line


def new_hits(repo, ev, patterns, include, exclude, scope="new", comments=False):
    """Lines that match a pattern. Scope 'new': only lines that are not in the same file of the fixture.
    Scope 'all': every line (use it when the fixture itself has the old code that the run must remove).
    The check skips comments, unless comments=True."""
    base = fixture_of(ev)
    hits = []
    for rel in select(repo, include, exclude):
        lines = read(repo / rel).splitlines()
        old = set(l.strip() for l in read(base / rel).splitlines()) if scope == "new" and (base / rel).is_file() else set()
        numbered = enumerate(lines, 1) if comments else code_part(rel, lines)
        for n, code in numbered:
            if lines[n - 1].strip() in old or not code.strip():
                continue
            for p in patterns:
                if re.search(p, code):
                    hits.append(f"{rel}:{n}: {lines[n - 1].strip()[:120]}")
    return hits


def count_tests(root):
    return sum(len(TEST_ATTR.findall(read(root / rel))) for rel in select(root, ["tests/**/*.cs"]))


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def check_unchanged(repo, ev):
    base = fixture_of(ev)
    a, b = set(files(base)), set(files(repo))
    changed = sorted(f for f in a & b if digest(base / f) != digest(repo / f))
    added = sorted(b - a)
    removed = sorted(a - b)
    ok = not changed and not removed and not [f for f in added if not f.endswith((".md", ".sarif", ".txt", ".log"))]
    return ok, {"changed": changed[:10], "added": added[:10], "removed": removed[:10]}


def check_migration(repo, ev):
    base = fixture_of(ev)
    old = set(select(base, ["**/Migrations/*.cs"]))
    new = [f for f in select(repo, ["**/Migrations/*.cs"]) if f not in old and not f.endswith((".Designer.cs", "ModelSnapshot.cs"))]
    if not new:
        return False, "no new migration"
    projects = select(repo, ["src/**/*.csproj"])
    if not projects:
        return False, "no project under src"
    for cmd in (["dotnet", "tool", "restore"], ["dotnet", "build", projects[0]]):
        code, out = run(cmd, repo, TIMEOUT_BUILD)
        if code != 0:
            return False, " ".join(cmd[:3]) + " failed: " + tail(out, r"error", 2)
    code, out = run(["dotnet", "ef", "migrations", "has-pending-model-changes", "--project", projects[0]], repo, TIMEOUT_BUILD)
    return code == 0, {"new": new, "pending_check": tail(out, r"changes|error", 2)}


def check_banned_api(repo):
    pkgs = packages(repo)
    if "microsoft.codeanalysis.bannedapianalyzers" not in pkgs:
        return False, "BannedApiAnalyzers not referenced"
    banned = " ".join(read(repo / f) for f in select(repo, ["**/BannedSymbols*.txt"]))
    missing = [s for s in ("P:System.DateTime.UtcNow", "P:System.DateTime.Now") if s not in banned]
    if missing:
        return False, "BannedSymbols misses " + ", ".join(missing)
    projects = select(repo, ["src/**/*.csproj"])
    if not projects:
        return False, "no project under src"
    with tempfile.TemporaryDirectory() as tmp:
        copy = pathlib.Path(tmp) / "repo"
        shutil.copytree(repo, copy, ignore=shutil.ignore_patterns(*SKIP_DIRS))
        probe = copy / pathlib.Path(projects[0]).parent / "BannedProbe.cs"
        probe.write_text(
            "namespace BannedProbe;\n\ninternal static class Probe\n{\n"
            "    internal static DateTime Read() => DateTime.UtcNow;\n}\n", encoding="utf-8")
        code, out = run(["dotnet", "build", projects[0], "-warnaserror"], copy, TIMEOUT_BUILD)
    return code != 0 and "RS0030" in out, tail(out, r"RS0030|error", 2)


# ---------------------------------------------------------------------------
# Checks for the trap cases. Each one works on a temporary copy of the run.
# ---------------------------------------------------------------------------

_CACHE = {}


def copy_repo(repo, tmp):
    copy = pathlib.Path(tmp) / "repo"
    shutil.copytree(repo, copy, ignore=shutil.ignore_patterns(*SKIP_DIRS))
    return copy


def project_dir(root, area):
    """Folder of the first project under 'src' or 'tests'."""
    projects = select(root, [f"{area}/**/*.csproj"])
    return (root / projects[0]).parent if projects else None


def add_probe(copy, probe):
    """Copy the files of probes/<probe>/ into the project that probe.json names (src or tests)."""
    folder = PROBES / probe
    meta = json.loads(read(folder / "probe.json") or "{}")
    target = project_dir(copy, meta.get("target", "tests"))
    if target is None:
        return None
    dest = target / meta.get("folder", "EvalProbes")
    dest.mkdir(parents=True, exist_ok=True)
    for f in folder.glob("*.cs"):
        shutil.copy(f, dest / f.name)
    return dest


def parse_trx(folder):
    """Return {test name: (outcome, message)} from the TRX files in folder."""
    out = {}
    ns = "{http://microsoft.com/schemas/VisualStudio/TeamTest/2010}"
    for trx in pathlib.Path(folder).glob("**/*.trx"):
        try:
            tree = ET.parse(trx)
        except ET.ParseError:
            continue
        for r in tree.iter(ns + "UnitTestResult"):
            msg = r.find(f"{ns}Output/{ns}ErrorInfo/{ns}Message")
            out[r.get("testName")] = (r.get("outcome"), (msg.text or "") if msg is not None else "")
    return out


def run_tests(copy, results, extra=()):
    cmd = ["dotnet", "test", *NO_WARN_AS_ERROR, "--report-trx", "--results-directory", str(results), *extra]
    code, out = run(cmd, copy, TIMEOUT_TEST)
    return code, out, parse_trx(results)


def probe_results(repo, probe):
    """Run the probe tests of probes/<probe> on a copy of the run. Cached for each repo and probe."""
    key = (str(repo), probe)
    if key not in _CACHE:
        with tempfile.TemporaryDirectory() as tmp:
            copy = copy_repo(repo, tmp)
            if add_probe(copy, probe) is None:
                _CACHE[key] = (None, "no project for the probe")
            else:
                code, out, results = run_tests(copy, pathlib.Path(tmp) / "trx", ["--filter", "FullyQualifiedName~EvalProbes"])
                _CACHE[key] = (results, tail(out, r"error|total:|failed:", 3))
    return _CACHE[key]


def check_probe_tests(repo, a):
    """Each listed probe test must pass. An entry can also be a dict:
    {"test": name, "outcome": "Failed", "message_absent": text} for a test that must fail in a given way."""
    results, log = probe_results(repo, a["probe"])
    if results is None or not results:
        return False, f"probe did not run: {log}"
    got, ok = {}, True
    for spec in a["tests"]:
        spec = {"test": spec} if isinstance(spec, str) else spec
        outcome, msg = results.get(spec["test"], ("Missing", log))
        good = outcome == spec.get("outcome", "Passed")
        if good and spec.get("message_absent"):
            good = spec["message_absent"].lower() not in msg.lower()
        ok &= good
        got[spec["test"]] = outcome + ("" if good else f": {msg.strip().splitlines()[0][:200] if msg.strip() else ''}")
    return ok, got


def check_mutant_killed(repo, a):
    """Insert a bug into the production code. The tests of the run must fail (not the build)."""
    m = a["mutation"]
    with tempfile.TemporaryDirectory() as tmp:
        copy = copy_repo(repo, tmp)
        target = copy / m["file"]
        text = read(target)
        if m["after"] not in text:
            return False, "mutation point not found: " + m["after"]
        target.write_text(text.replace(m["after"], m["after"] + m["insert"], 1), encoding="utf-8")
        code, out, results = run_tests(copy, pathlib.Path(tmp) / "trx")
    failed = sorted(n for n, (o, _) in results.items() if o == "Failed")
    if not results:
        return False, "tests did not run: " + tail(out, r"error", 2)
    return bool(failed), {"failed_tests": failed[:5], "total": len(results)}


def gate_run(repo, script, probe=None):
    """Run the CI script on a copy of the run. Add the probe files when 'probe' has a value. Cached."""
    key = (str(repo), script, probe or "")
    if key not in _CACHE:
        with tempfile.TemporaryDirectory() as tmp:
            copy = copy_repo(repo, tmp)
            if probe and add_probe(copy, probe) is None:
                _CACHE[key] = (None, "no project for the probe")
            else:
                _CACHE[key] = run(["bash", script], copy, TIMEOUT_BUILD)
    return _CACHE[key]


def check_ci_gate(repo, a):
    """Run the CI script of the run. With 'probe', add the probe files first.
    "expect": "pass" or "fail". With "expect": "fail" and "diagnostic", the failure must come from the probe.
    That is, the gate passes without the probe, or the output reports the diagnostic as an error.
    So a script that fails for another reason (for example a command that does not exist) does not count."""
    if not (repo / a["script"]).is_file():
        return False, f"{a['script']} does not exist"
    code, out = gate_run(repo, a["script"], a.get("probe"))
    if code is None:
        return False, out
    got = f"exit {code}: " + tail(out, r"error|warning|diagnostics|Warn", 2)
    if a.get("expect") != "fail":
        return code == 0, got
    if code == 0:
        return False, got
    if a.get("diagnostic"):
        clean, _ = gate_run(repo, a["script"])
        as_error = re.search(rf"\berror\s+{re.escape(a['diagnostic'])}\b", out)
        if clean != 0 and not as_error:
            return False, f"the gate also fails without the probe (exit {clean}), and {a['diagnostic']} is not an error: " + got
    return True, got


def check_build_probe(repo, a):
    """Add a probe file and build with -warnaserror. With "expect": "fail" (default) the build must fail
    with the diagnostic id. With "expect": "clean" the diagnostic must not appear."""
    with tempfile.TemporaryDirectory() as tmp:
        copy = copy_repo(repo, tmp)
        dest = add_probe(copy, a["probe"])
        if dest is None:
            return False, "no project for the probe"
        project = next(dest.parent.glob("*.csproj"))
        code, out = run(["dotnet", "build", str(project), "-warnaserror", "--no-incremental"], copy, TIMEOUT_BUILD)
    # Count only the diagnostic at a probe file. Other messages can name the id too
    # (for example 'EnableGenerateDocumentationFile: ... to enable IDE0005').
    rx = rf"EvalProbes[/\\][^(]+\(\d+,\d+\): (?:error|warning) {re.escape(a['diagnostic'])}:"
    hits = re.findall(rx, out)
    ok = (code != 0 and bool(hits)) if a.get("expect", "fail") == "fail" else not hits
    return ok, f"exit {code}, {len(hits)} x {a['diagnostic']} at the probe: " + tail(out, a["diagnostic"] + r"|error", 1)


def wait_pg(name, seconds=60):
    for _ in range(seconds):
        code, _ = run(["docker", "exec", name, "pg_isready", "-U", "postgres"], ".", 10)
        if code == 0:
            time.sleep(1)
            return True
        time.sleep(1)
    return False


def check_idempotent_script(repo, a):
    """Make the idempotent migration script and run it twice with psql on an empty PostgreSQL database."""
    projects = select(repo, ["src/**/*.csproj"])
    if not projects:
        return False, "no project under src"
    with tempfile.TemporaryDirectory() as tmp:
        copy = copy_repo(repo, tmp)
        sql = pathlib.Path(tmp) / "migrate.sql"
        for cmd in (["dotnet", "tool", "restore"], ["dotnet", "build", projects[0]],
                    ["dotnet", "ef", "migrations", "script", "--idempotent", "--project", projects[0], "-o", str(sql)]):
            code, out = run(cmd, copy, TIMEOUT_BUILD)
            if code != 0:
                return False, " ".join(cmd[:4]) + " failed: " + tail(out, r"error", 2)
        name = f"evalpg-{os.getpid()}-{int(time.time())}"
        code, out = run(["docker", "run", "-d", "--rm", "--name", name, "-e", "POSTGRES_PASSWORD=eval", PG_IMAGE], ".", 120)
        if code != 0:
            return False, "docker run failed: " + tail(out)
        try:
            if not wait_pg(name):
                return False, "PostgreSQL did not start"
            run(["docker", "cp", str(sql), f"{name}:/migrate.sql"], ".", 60)
            results = []
            for n in (1, 2):
                code, out = run(["docker", "exec", name, "psql", "-U", "postgres", "-v", "ON_ERROR_STOP=1", "-q", "-f", "/migrate.sql"], ".", 300)
                results.append(f"run {n}: exit {code}" + ("" if code == 0 else " " + tail(out, r"ERROR", 1)))
                if code != 0:
                    break
        finally:
            run(["docker", "rm", "-f", name], ".", 60)
    return all("exit 0" in r for r in results) and len(results) == 2, results


def package_list(repo, vulnerable=False):
    key = (str(repo), "vulnerable" if vulnerable else "list")
    if key not in _CACHE:
        code, out = run(["dotnet", "restore"], repo, TIMEOUT_BUILD)
        cmd = ["dotnet", "package", "list", "--include-transitive", "--format", "json"] + (["--vulnerable"] if vulnerable else [])
        code, out = run(cmd, repo, TIMEOUT_BUILD)
        try:
            _CACHE[key] = json.loads(out[out.index("{"):])
        except ValueError:
            _CACHE[key] = {"error": tail(out, r"error", 2)}
    return _CACHE[key]


def iter_packages(data):
    for proj in data.get("projects", []):
        for fw in proj.get("frameworks", []):
            for kind in ("topLevelPackages", "transitivePackages"):
                for p in fw.get(kind, []):
                    yield pathlib.Path(proj.get("path", "")).name, p


def version_tuple(v):
    return tuple(int(x) for x in re.findall(r"\d+", v.split("-")[0])[:4])


def check_resolved_min(repo, a):
    """Every resolved version (direct or transitive) of each named package is at least the minimum."""
    data = package_list(repo)
    if "error" in data:
        return False, data["error"]
    problems, seen = [], {}
    for proj, p in iter_packages(data):
        for name, minimum in a["value"].items():
            if p["id"].lower() == name.lower():
                seen.setdefault(name, set()).add(p["resolvedVersion"])
                if version_tuple(p["resolvedVersion"]) < version_tuple(minimum):
                    problems.append(f"{proj}: {p['id']} {p['resolvedVersion']} < {minimum}")
    return not problems, {"resolved": {k: sorted(v) for k, v in seen.items()}, "problems": problems}


def check_file_exists(repo, a):
    """A file that matches 'files' (and not 'exclude') exists, and holds the regex 'contains' when it is given."""
    rx = re.compile(a["contains"]) if a.get("contains") else None
    found = [f for f in select(repo, a["files"], a.get("exclude", [])) if rx is None or rx.search(read(repo / f))]
    return bool(found), found[:5]


def check_no_vulnerable(repo, a):
    data = package_list(repo, vulnerable=True)
    if "error" in data:
        return False, data["error"]
    found = sorted({f"{p['id']} {p['resolvedVersion']}" for _, p in iter_packages(data) if p.get("vulnerabilities")})
    return not found, found


def score_run(ev, repo, variant=None, only=None):
    results = []
    for a in ev["assertions"]:
        if "check" not in a or a.get("variant", variant) != variant:
            continue
        if only and a["check"] not in only and a.get("id") not in only:
            continue
        kind = a["check"]
        if kind == "build_warnaserror":
            code, out = run(["dotnet", "build", "-warnaserror", "--no-incremental"], repo, TIMEOUT_BUILD)
            ok, got = code == 0, tail(out, r"Warning\(s\)|Error\(s\)|error", 3)
        elif kind == "tests_pass":
            code, out = run(["dotnet", "test"], repo, TIMEOUT_TEST)
            ok, got = code == 0, tail(out, r"total:|failed:|succeeded:|Test run summary|error", 4)
        elif kind == "tests_added_min":
            got = count_tests(repo) - count_tests(fixture_of(ev))
            ok = got >= a["value"]
        elif kind == "package_policy":
            ok, got = check_package_policy(repo, ev, a.get("approved", []))
        elif kind == "grep_absent":
            pats = a["value"] if isinstance(a["value"], list) else [a["value"]]
            got = new_hits(repo, ev, pats, a["files"], a.get("exclude", []), a.get("scope", "new"), a.get("comments", False))
            ok, got = not got, got[:8]
        elif kind == "grep_present":
            rx = re.compile(a["value"])
            got = [f for f in select(repo, a["files"], a.get("exclude", [])) if rx.search(read(repo / f))]
            ok = bool(got)
        elif kind == "files_unchanged":
            ok, got = check_unchanged(repo, ev)
        elif kind == "migration_ok":
            ok, got = check_migration(repo, ev)
        elif kind == "banned_api_enforced":
            ok, got = check_banned_api(repo)
        elif kind == "format_clean":
            code, out = run(["dotnet", "format", "--verify-no-changes"], repo, TIMEOUT_BUILD)
            ok, got = code == 0, tail(out, r"error|warning", 3) or f"exit {code}"
        elif kind == "probe_tests":
            ok, got = check_probe_tests(repo, a)
        elif kind == "mutant_killed":
            ok, got = check_mutant_killed(repo, a)
        elif kind == "ci_gate":
            ok, got = check_ci_gate(repo, a)
        elif kind == "build_probe":
            ok, got = check_build_probe(repo, a)
        elif kind == "idempotent_script":
            ok, got = check_idempotent_script(repo, a)
        elif kind == "resolved_min":
            ok, got = check_resolved_min(repo, a)
        elif kind == "no_vulnerable_packages":
            ok, got = check_no_vulnerable(repo, a)
        elif kind == "file_exists":
            ok, got = check_file_exists(repo, a)
        else:
            raise SystemExit(f"Unknown check: {kind}")
        results.append({"id": a.get("id"), "check": kind, "ok": bool(ok), "got": got})
    return results


def load_evals():
    return cases.load()


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("runs", type=pathlib.Path, nargs="?")
    parser.add_argument("--repo", type=pathlib.Path, help="score one folder")
    parser.add_argument("--eval", help="eval name for --repo")
    parser.add_argument("--variant", default=None, help="variant for assertions that are limited to one variant")
    parser.add_argument("--only", action="append", help="score only these assertion ids or check kinds (with --repo)")
    args = parser.parse_args(argv)
    evals = load_evals()

    if args.repo:
        if args.eval not in evals:
            parser.error(f"--eval must be one of: {', '.join(evals)}")
        results = score_run(evals[args.eval], args.repo.resolve(), args.variant, args.only)
        print(json.dumps({"eval": args.eval, "repo": str(args.repo), "passed": sum(r["ok"] for r in results),
                          "total": len(results), "results": results}, indent=2, default=str))
        return 0

    if not args.runs:
        parser.error("give a runs folder or --repo")
    scores = {}
    summary = defaultdict(lambda: [0, 0])
    for run_dir in sorted(args.runs.glob("*/*/*")):
        if not (run_dir / "repo").is_dir():
            continue
        name, variant, number = run_dir.relative_to(args.runs).parts
        if name not in evals:
            continue
        results = score_run(evals[name], (run_dir / "repo").resolve(), variant)
        (run_dir / "checks.json").write_text(json.dumps(results, indent=2, default=str), encoding="utf-8")
        scores[f"{name}/{variant}/{number}"] = results
        passed = sum(r["ok"] for r in results)
        summary[(name, variant)][0] += passed
        summary[(name, variant)][1] += len(results)
        failed = ", ".join(f"{r['id']} {r['check']}" for r in results if not r["ok"])
        print(f"{name}/{variant}/{number}: {passed}/{len(results)}" + (f" | failed: {failed}" if failed else ""))
    print()
    for (name, variant), (p, t) in sorted(summary.items()):
        print(f"{name} {variant}: {p}/{t}")
    (args.runs / "scores.json").write_text(json.dumps(scores, indent=2, default=str), encoding="utf-8")
    return 0


if __name__ == "__main__":
    sys.exit(main())
