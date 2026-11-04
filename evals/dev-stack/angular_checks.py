"""Check kinds for the cases with "stack": "angular". score.py sends these kinds here.

- ng_build: `npx ng build` exits 0.
- ng_test: `npx ng test --no-watch` exits 0.
- ui_probe: build the app, start the mock API of the fixture with the built app, and run the named tests of
  probes/<probe>/probe.py with Playwright (probes/angular-common/harness.py). Each named test must pass.
- tests_added_min: new it( and test( calls in *.spec.ts files, compared with the fixture.

All work runs in one copy of the run for each repo, so the run folder does not change. node_modules comes
from the run (hard links when possible), or from `npm ci` into a cache that is shared by package-lock.json hash.
The mock API is the one of the fixture, not the one of the run: a run cannot change the test hooks.
"""
import atexit
import hashlib
import json
import os
import pathlib
import re
import shutil
import socket
import subprocess
import sys
import tempfile
import time
import urllib.request

HERE = pathlib.Path(__file__).resolve().parent
FIXTURES = HERE / "fixtures"
PROBES = HERE / "probes"
HARNESS = PROBES / "angular-common" / "harness.py"
CACHE = pathlib.Path(os.environ.get("EVAL_ANGULAR_CACHE", pathlib.Path.home() / ".cache" / "maikelsoft-evals" / "angular"))
KINDS = {"ng_build", "ng_test", "ui_probe"}
COPY_SKIP = {".git", "node_modules", "dist", ".angular", "coverage", "out-tsc", "__pycache__"}
NODE_SKIP = (".vite", ".cache", ".vitest")
TIMEOUT_INSTALL = 900
TIMEOUT_BUILD = 600
TIMEOUT_TEST = 600
TIMEOUT_PROBE = 900
SPEC_TEST = re.compile(r"^\s*(?:it|test)(?:\.(?:only|concurrent|each\s*\([^)]*\)|each\s*`[^`]*`))?\s*\(", re.M)

_WORK = {}
_RESULTS = {}


def handles(kind, ev):
    return kind in KINDS or (kind == "tests_added_min" and ev.get("stack") == "angular")


def node_env():
    env = dict(os.environ)
    env.update({"CI": "1", "NG_CLI_ANALYTICS": "false", "NO_COLOR": "1", "FORCE_COLOR": "0"})
    return env


def run(cmd, cwd, timeout, env=None):
    try:
        r = subprocess.run(cmd, cwd=cwd, capture_output=True, text=True, timeout=timeout, env=env or node_env())
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


# --- Test count ---------------------------------------------------------------------------------

def spec_files(root):
    out = []
    for dirpath, dirnames, filenames in os.walk(root):
        dirnames[:] = [d for d in dirnames if d not in COPY_SKIP]
        out += [pathlib.Path(dirpath, f) for f in filenames if f.endswith(".spec.ts")]
    return out


def strip_comments(text):
    text = re.sub(r"/\*.*?\*/", "", text, flags=re.S)
    return re.sub(r"^\s*//.*$", "", text, flags=re.M)


def count_spec_tests(root):
    """Number of it( and test( calls (with .only, .concurrent, or .each) in the *.spec.ts files, without comments."""
    return sum(len(SPEC_TEST.findall(strip_comments(p.read_text(encoding="utf-8", errors="ignore"))))
               for p in spec_files(pathlib.Path(root)))


# --- Work copy with node_modules --------------------------------------------------------------

def link_tree(src, dst):
    """Copy a folder with hard links (fast, no extra space). Fall back to a real copy across file systems."""
    code, _ = run(["cp", "-al", str(src), str(dst)], ".", TIMEOUT_INSTALL)
    if code != 0:
        shutil.rmtree(dst, ignore_errors=True)
        shutil.copytree(src, dst, symlinks=True)
    for name in NODE_SKIP:
        shutil.rmtree(dst / name, ignore_errors=True)


def cached_node_modules(lock):
    """node_modules for this package-lock.json, made once with npm ci."""
    key = hashlib.sha256(lock.read_bytes()).hexdigest()[:16]
    target = CACHE / "node_modules" / key
    if (target / "node_modules" / ".package-lock.json").is_file():
        return target / "node_modules", None
    shutil.rmtree(target, ignore_errors=True)
    target.mkdir(parents=True)
    shutil.copy(lock, target / "package-lock.json")
    shutil.copy(lock.parent / "package.json", target / "package.json")
    code, out = run(["npm", "ci", "--prefer-offline", "--no-audit", "--no-fund"], target, TIMEOUT_INSTALL)
    if code != 0:
        shutil.rmtree(target, ignore_errors=True)
        return None, "npm ci failed: " + tail(out, r"ERR", 2)
    return target / "node_modules", None


def workspace(repo):
    """One copy of the run for all Angular checks of this repo: (folder, None) or (None, error)."""
    key = str(repo)
    if key in _WORK:
        return _WORK[key]
    CACHE.mkdir(parents=True, exist_ok=True)
    root = pathlib.Path(tempfile.mkdtemp(prefix="ng-", dir=CACHE))
    atexit.register(shutil.rmtree, root, True)
    copy = root / "repo"
    shutil.copytree(repo, copy, ignore=shutil.ignore_patterns(*COPY_SKIP))
    error = None
    lock = copy / "package-lock.json"
    if (repo / "node_modules" / ".package-lock.json").is_file():
        link_tree(repo / "node_modules", copy / "node_modules")
    elif lock.is_file():
        modules, error = cached_node_modules(lock)
        if modules:
            link_tree(modules, copy / "node_modules")
    else:
        error = "no package-lock.json"
    _WORK[key] = (None, error) if error else (copy, None)
    return _WORK[key]


def ng(repo, step):
    """Run 'build' or 'test' once for each repo. Return (exit code, short log)."""
    key = (str(repo), step)
    if key not in _RESULTS:
        copy, error = workspace(repo)
        if error:
            _RESULTS[key] = (1, error)
        elif step == "build":
            start = time.monotonic()
            code, out = run(["npx", "ng", "build"], copy, TIMEOUT_BUILD)
            _RESULTS[key] = (code, tail(out, r"error|ERROR|Application bundle|Output location", 3)
                             + f" ({time.monotonic() - start:.0f} s)")
        else:
            start = time.monotonic()
            code, out = run(["npx", "ng", "test", "--no-watch"], copy, TIMEOUT_TEST)
            _RESULTS[key] = (code, tail(out, r"Tests |Test Files|FAIL|Error", 3) + f" ({time.monotonic() - start:.0f} s)")
    return _RESULTS[key]


# --- UI probes ----------------------------------------------------------------------------------

def free_port():
    with socket.socket() as s:
        s.bind(("127.0.0.1", 0))
        return s.getsockname()[1]


def browser_dir(copy):
    found = sorted(copy.glob("dist/*/browser/index.html"))
    return found[0].parent if found else None


def wait_http(url, seconds=20):
    end = time.monotonic() + seconds
    while time.monotonic() < end:
        try:
            with urllib.request.urlopen(url, timeout=2):
                return True
        except Exception:  # noqa: BLE001 - the server is not up yet
            time.sleep(0.2)
    return False


def mock_server(ev):
    """The pristine mock API of the case: the fixture of the case, else its base fixture."""
    for name in (ev.get("fixture"), ev.get("base")):
        if name and (FIXTURES / name / "mock-api" / "server.py").is_file():
            return FIXTURES / name / "mock-api" / "server.py"
    return None


def probe_results(repo, ev, probe):
    """Run all tests of probes/<probe>/probe.py once for each repo. Return (results dict or None, log)."""
    key = (str(repo), "probe", probe)
    if key in _RESULTS:
        return _RESULTS[key]
    code, log = ng(repo, "build")
    copy, _ = workspace(repo)
    static = browser_dir(copy) if copy else None
    server = mock_server(ev)
    if code != 0 or static is None:
        _RESULTS[key] = (None, "build failed: " + log)
        return _RESULTS[key]
    if server is None:
        _RESULTS[key] = (None, "no mock-api/server.py in the fixture")
        return _RESULTS[key]
    port = free_port()
    env = {**node_env(), "PORT": str(port), "STATIC_DIR": str(static), "API_DELAY_MS": "50"}
    proc = subprocess.Popen([sys.executable, str(server)], cwd=copy, env=env,
                            stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
    out_file = copy.parent / f"probe-{probe}.json"
    try:
        if not wait_http(f"http://127.0.0.1:{port}/api/products"):
            _RESULTS[key] = (None, "the mock API did not start")
            return _RESULTS[key]
        start = time.monotonic()
        code, out = run([sys.executable, str(HARNESS), str(PROBES / probe / "probe.py"),
                         "--base-url", f"http://127.0.0.1:{port}", "--out", str(out_file)], copy, TIMEOUT_PROBE)
        results = json.loads(out_file.read_text(encoding="utf-8")) if out_file.is_file() else None
        _RESULTS[key] = (results, tail(out, None, 2) + f" ({time.monotonic() - start:.0f} s)")
    finally:
        proc.terminate()
        try:
            proc.wait(10)
        except subprocess.TimeoutExpired:
            proc.kill()
    return _RESULTS[key]


def check_ui_probe(repo, ev, a):
    results, log = probe_results(repo, ev, a["probe"])
    if not results:
        return False, f"probe did not run: {log}"
    got, ok = {}, True
    for name in a["tests"]:
        r = results.get(name, {"outcome": "Missing", "message": log})
        passed = r["outcome"] == "Passed"
        ok &= passed
        got[name] = r["outcome"] + ("" if passed else f": {r['message'][:200]}")
    return ok, got


def check(kind, repo, ev, a, fixture):
    """Score one assertion. Return (ok, got)."""
    if kind == "tests_added_min":
        got = count_spec_tests(repo) - count_spec_tests(fixture)
        return got >= a["value"], got
    if kind == "ng_build":
        code, log = ng(repo, "build")
        return code == 0, f"exit {code}: {log}"
    if kind == "ng_test":
        code, log = ng(repo, "test")
        return code == 0, f"exit {code}: {log}"
    if kind == "ui_probe":
        return check_ui_probe(repo, ev, a)
    raise ValueError(f"not an Angular check: {kind}")
