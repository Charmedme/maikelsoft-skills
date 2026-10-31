#!/usr/bin/env python3
"""Path and pattern risk check of one change, from the Base of a run.

    python3 riskcheck.py --root . --base <Base> [--default-branch <name>] [--since <file>] [--out <file>]

Output: first line the compare point, then the level, one line for each reason (kind and path), one
section for test edits. With --since, reasons that the file does not hold get a line "new reason:".
Exit code 0 when the script ran (also on high), 2 when it could not run.
Python 3.9 or later, standard library only.
"""
import argparse
import collections
import fnmatch
import pathlib
import re
import subprocess
import sys

AUTH_WORDS = {"auth", "authn", "authz", "authentication", "authorization", "authorize", "login", "logout",
              "signin", "signout", "oauth", "oidc", "identity", "password", "passwords", "permission",
              "permissions", "jwt", "sso", "saml", "secret", "secrets", "credential", "credentials"}
AUTH_PAIRS = {("sign", "in"), ("sign", "out"), ("log", "in"), ("log", "out"), ("o", "auth")}
TEST_WORDS = {"test", "tests", "spec", "specs", "fixture", "fixtures", "e2e", "__tests__", "testing"}
GUIDES = ("REVIEW.md", "CLAUDE.md")
OUT_PREFIX = ("docs/", "artifacts/")
GENERATED = re.compile(r"(package-lock\.json|yarn\.lock|pnpm-lock\.yaml|\.Designer\.cs|ModelSnapshot\.cs|\.min\.js)$")
LOCK_DOC = re.compile(r"\.(md|markdown|txt|rst)$", re.I)

ADDED = [("anonymous", re.compile(r"\[AllowAnonymous\]|\.AllowAnonymous\(\)|@PermitAll|permitAll\(\)"))]
COMMENT = re.compile(r"^\s*(//|#|\*|/\*|<!--|--|echo\b)")
NUGET = re.compile(r"<Package(?:Reference|Version)\s+[^>]*Include=\"([^\"]+)\"(?:[^>]*Version=\"([^\"]+)\")?")
NPM = re.compile(r"^\s*\"(@?[\w./-]+)\"\s*:\s*\"[\^~]?(\d[^\"]*)\"")
PYREQ = re.compile(r"^([A-Za-z0-9_.-]+)\s*(?:==|>=|~=)\s*([\d.]+)")
ASSERT = re.compile(r"\bAssert\.|\.Should\(\)|\.Should[A-Z]\w*\(|\bexpect\(|\bself\.assert[A-Z]|^\s*assert\b|\.Received\(|\bVerify\(")
SKIP = re.compile(r"\[Ignore\b|Skip\s*=|\bxit\(|\bxdescribe\(|\b(it|test|describe)\.skip\(|@unittest\.skip|\bpytest\.mark\.skip|\.skip\(|\bAssert\.(Inconclusive|Skip)")


class CannotRun(Exception):
    pass


def git(root, *args, check=True):
    p = subprocess.run(["git", "-C", root, *args], capture_output=True, text=True, errors="replace")
    if check and p.returncode:
        raise CannotRun(f"git {' '.join(args)}: {p.stderr.strip()}")
    return p


def words(path):
    """Split on / . - _ and at each capital letter (a run of capitals stays one word), lower case."""
    out = []
    for part in re.split(r"[/.\-_ ]+", path):
        out += re.findall(r"[A-Z]+(?![a-z])|[A-Z]?[a-z0-9]+|[A-Z]", part)
    return [w.lower() for w in out if w]


def is_out(path):
    if path.startswith(OUT_PREFIX) or path.endswith(".log"):
        return True
    return bool(set(words(path)) & TEST_WORDS)


def is_test(path):
    return bool(set(words(path)) & TEST_WORDS)


def default_branch(root, name):
    if name:
        if git(root, "rev-parse", "--verify", "-q", name + "^{commit}", check=False).returncode:
            raise CannotRun(f"default branch {name} does not exist")
        return name
    exists = lambda ref: git(root, "rev-parse", "--verify", "-q", ref + "^{commit}", check=False).returncode == 0
    p = git(root, "symbolic-ref", "-q", "refs/remotes/origin/HEAD", check=False)
    remote = p.stdout.strip().replace("refs/remotes/", "") if p.returncode == 0 else ""
    if remote and exists(remote):
        local = remote.split("/", 1)[1]
        # A local default branch that is ahead of the remote holds work that a story can merge.
        if exists(local) and git(root, "merge-base", "--is-ancestor", remote, local, check=False).returncode == 0:
            return local
        return remote
    for b in ("main", "master"):
        if exists(b):
            return b
    raise CannotRun("no default branch found")


def compare_point(root, base, default):
    """Return (compare point, notes)."""
    notes = []
    p = git(root, "rev-parse", "--verify", "-q", base + "^{commit}", check=False)
    if p.returncode:
        raise CannotRun(f"Base {base} does not exist")
    base = p.stdout.strip()
    head = git(root, "rev-parse", "HEAD").stdout.strip()
    mb_p = git(root, "merge-base", "HEAD", default, check=False)
    if mb_p.returncode:
        raise CannotRun(f"no merge base of HEAD and {default}")
    mb = mb_p.stdout.strip()
    keep = git(root, "merge-base", "--is-ancestor", mb, base, check=False).returncode == 0
    point = base if keep else mb
    # A branch that is already in the default branch has an empty diff from the merge base.
    if point == head:
        if base == head:
            raise CannotRun(f"Base {base[:9]} is HEAD, so no diff exists")
        point = base
    if git(root, "merge-base", "--is-ancestor", point, "HEAD", check=False).returncode:
        notes.append(f"note: compare point {point[:9]} is not an ancestor of HEAD. An amended parent is a known gap, so the diff can hold the parent.")
    return point, head, notes


def path_reasons(path, sensitive):
    reasons = []
    for pat in sensitive:
        if fnmatch.fnmatch(path, pat) or path.startswith(pat.rstrip("/") + "/"):
            reasons.append(("sensitive-path", path))
    if is_out(path):
        return reasons
    w = words(path)
    if {"migration", "migrations", "migrate"} & set(w) and not GENERATED.search(path):
        reasons.append(("migration", path))
    if path.startswith(".github/workflows/") or re.search(r"(^|/)(azure-pipelines[^/]*\.ya?ml|\.gitlab-ci\.yml|Jenkinsfile)$", path):
        reasons.append(("ci-workflow", path))
    if set(w) & AUTH_WORDS or set(zip(w, w[1:])) & AUTH_PAIRS:
        reasons.append(("auth-name", path))
    return reasons


def parse_diff(text):
    """Yield (path, added lines, removed lines) from a unified diff with -U0."""
    files, cur = {}, None
    old = None
    for line in text.splitlines():
        if line.startswith("--- "):
            old = line[4:]
        elif line.startswith("+++ "):
            p = line[4:]
            if p == "/dev/null":
                p = old
            p = p[2:] if p[:2] in ("a/", "b/") else p
            cur = files.setdefault(p, ([], []))
        elif cur is not None and line.startswith("+"):
            cur[0].append(line[1:])
        elif cur is not None and line.startswith("-"):
            cur[1].append(line[1:])
    return files


def major(v):
    m = re.match(r"\D*(\d+)", v or "")
    return int(m.group(1)) if m else None


def package_reasons(root, point, path, added, removed):
    name = path.rsplit("/", 1)[-1]
    if name.endswith((".csproj", ".fsproj", ".props", ".targets")):
        rx = NUGET
    elif name == "package.json":
        rx = NPM
    elif re.match(r"requirements.*\.txt$", name):
        rx = PYREQ
    else:
        return []
    old = {m.group(1): m.group(2) for line in removed if (m := rx.search(line))}
    out = []
    for line in added:
        m = rx.search(line)
        if not m:
            continue
        package, version = m.group(1), m.group(2)
        if package in old:
            if major(version) != major(old[package]):
                out.append(("major-version", path))
        # A moved or new project file can refer to a package that the repository already has.
        elif git(root, "grep", "-qF", package, point, "--", "*.csproj", "*.fsproj", "*.props", "*.targets", "package.json", "requirements*.txt", check=False).returncode:
            out.append(("new-package", path))
    return out


def squeeze(line):
    return re.sub(r"\s+", "", line)


def run(root, base, default_name):
    default = default_branch(root, default_name)
    point, head, notes = compare_point(root, base, default)
    show = git(root, "show", f"{point}:REVIEW.md", check=False)
    sensitive = []
    if show.returncode == 0:
        inside = False
        for line in show.stdout.splitlines():
            if line.startswith("#"):
                inside = line.lstrip("# ").strip().lower() == "sensitive paths"
            elif inside and line.strip():
                sensitive.append(line.strip().lstrip("-* ").strip("` "))
    names = git(root, "diff", "--name-only", "--no-renames", f"{point}...HEAD").stdout.splitlines()
    diff = parse_diff(git(root, "diff", "-U0", "--no-renames", "--no-color", f"{point}...HEAD").stdout)
    reasons, guide, removed_asserts, added_asserts, skips = [], [], [], collections.Counter(), []
    for path in names:
        if path in GUIDES:
            guide.append(path)
        reasons += path_reasons(path, sensitive)
    for path, (added, removed) in diff.items():
        if is_test(path):
            removed_asserts += [(path, line.strip()) for line in removed if ASSERT.search(line)]
            added_asserts.update(squeeze(line) for line in added if ASSERT.search(line))
            skips += [(path, line.strip()) for line in added if SKIP.search(line)]
        if is_out(path) or GENERATED.search(path) or LOCK_DOC.search(path) or path in GUIDES:
            continue
        reasons += package_reasons(root, point, path, added, removed)
        code = [line for line in added if not COMMENT.search(line)]
        reasons += [(kind, path) for kind, rx in ADDED for line in code if rx.search(line)]
    lost = []
    for path, line in removed_asserts:
        if added_asserts[squeeze(line)] > 0:
            added_asserts[squeeze(line)] -= 1
        else:
            lost.append((path, line))
    reasons = sorted(set(reasons))
    return {"point": point, "default": default, "head": head, "notes": notes, "level": "high" if reasons else "normal",
            "reasons": reasons, "guide": guide, "lost": lost, "skips": skips, "files": names}


def render(r, since=None):
    lines = [f"compare point: {r['point']} (default branch {r['default']})", f"level: {r['level']}"]
    lines += r["notes"]
    lines += [f"reason: {k} {p}" for k, p in r["reasons"]]
    if since is not None:
        lines += [f"new reason: {k} {p}" for k, p in r["reasons"] if f"reason: {k} {p}" not in since]
    lines += [f"guide file changed: {p}" for p in r["guide"]]
    edited = sorted({p for p, _ in r["lost"] + r["skips"]})
    lines.append(f"test edits: {len(r['lost'])} assertions removed, {len(r['skips'])} skips added" + (f" in {', '.join(edited)}" if edited else ""))
    lines += [f"  assertion removed: {p}: {l}" for p, l in r["lost"]]
    lines += [f"  skip added: {p}: {l}" for p, l in r["skips"]]
    return "\n".join(lines) + "\n"


def main(argv=None):
    a = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    a.add_argument("--root", default=".")
    a.add_argument("--base", required=True)
    a.add_argument("--default-branch")
    a.add_argument("--since", help="an earlier output file: print each reason that it does not hold")
    a.add_argument("--out")
    args = a.parse_args(argv)
    try:
        since = None
        if args.since:
            since = pathlib.Path(args.since).read_text(encoding="utf-8").splitlines() if pathlib.Path(args.since).is_file() else []
        text = render(run(args.root, args.base, args.default_branch), since)
    except CannotRun as e:
        print(f"riskcheck: {e}", file=sys.stderr)
        return 2
    sys.stdout.write(text)
    if args.out:
        with open(args.out, "w", encoding="utf-8") as f:
            f.write(text)
    return 0


if __name__ == "__main__":
    sys.exit(main())
