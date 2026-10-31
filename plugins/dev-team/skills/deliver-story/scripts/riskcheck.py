#!/usr/bin/env python3
"""Path and pattern risk check of one change, from the Base of a run.

    python3 riskcheck.py --root . --base <Base> [--default-branch <name>] [--since <file>] [--out <file>]

Output: first line the compare point, then HEAD, the level, the line "raise", one line for each reason (kind
and path), and one section for test edits. "raise: yes" means that a reason asks for a security review. The other
reasons ask for a warning at G2. With --since, a reason that the file does not hold gets a line "new reason:",
a reason of the file that went away gets a line "gone reason:", and the test edits since the HEAD of the file get
their own line, because an edit of a test that this change added does not show in the diff from the compare point.
Exit code 0 when the script ran (also on high), 2 when it could not run.
Python 3.9 or later, standard library only.
"""
import argparse
import collections
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
REMOVED = [("auth-removed", re.compile(r"\[Authorize\b|\.RequireAuthorization\(|\.UseAuthorization\(|\.UseAuthentication\(|@PreAuthorize|@Secured|@RolesAllowed"))]
SECURITY_KINDS = {"auth-name", "anonymous", "auth-removed", "ci-workflow"}
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


def path_reasons(path):
    reasons = []
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


def test_edits(diff):
    """Return (lost assertions, added skips) of a parsed diff. A reformatted assertion is not lost."""
    removed, added, skips = [], collections.Counter(), []
    for path, (new, old) in diff.items():
        if is_test(path):
            removed += [(path, line.strip()) for line in old if ASSERT.search(line)]
            added.update(squeeze(line) for line in new if ASSERT.search(line))
            skips += [(path, line.strip()) for line in new if SKIP.search(line)]
    lost = []
    for path, line in removed:
        if added[squeeze(line)] > 0:
            added[squeeze(line)] -= 1
        else:
            lost.append((path, line))
    return lost, skips


def edits_since(root, commit):
    """The test edits from an earlier HEAD to this HEAD, or None when the commit is not an ancestor."""
    if git(root, "merge-base", "--is-ancestor", commit, "HEAD", check=False).returncode:
        return None
    return test_edits(parse_diff(git(root, "diff", "-U0", "--no-renames", "--no-color", commit, "HEAD").stdout))


def run(root, base, default_name, since_head=None):
    default = default_branch(root, default_name)
    point, head, notes = compare_point(root, base, default)
    names = git(root, "diff", "--name-only", "--no-renames", f"{point}...HEAD").stdout.splitlines()
    diff = parse_diff(git(root, "diff", "-U0", "--no-renames", "--no-color", f"{point}...HEAD").stdout)
    reasons, guide = [], []
    for path in names:
        if path in GUIDES:
            guide.append(path)
        reasons += path_reasons(path)
    for path, (added, removed) in diff.items():
        if is_out(path) or GENERATED.search(path) or LOCK_DOC.search(path) or path in GUIDES:
            continue
        reasons += package_reasons(root, point, path, added, removed)
        reasons += [(kind, path) for kind, rx in ADDED for line in added if not COMMENT.search(line) and rx.search(line)]
        reasons += [(kind, path) for kind, rx in REMOVED for line in removed if not COMMENT.search(line) and rx.search(line)]
    lost, skips = test_edits(diff)
    reasons = sorted(set(reasons))
    return {"point": point, "default": default, "head": head, "notes": notes, "level": "high" if reasons else "normal",
            "raise": any(kind in SECURITY_KINDS for kind, _ in reasons), "reasons": reasons, "guide": guide,
            "lost": lost, "skips": skips, "files": names, "since": edits_since(root, since_head) if since_head else None}


def edit_line(label, lost, skips):
    edited = sorted({p for p, _ in lost + skips})
    return f"{label}: {len(lost)} assertions removed, {len(skips)} skips added" + (f" in {', '.join(edited)}" if edited else "")


def render(r, since=None):
    lines = [f"compare point: {r['point']} (default branch {r['default']})", f"head: {r['head']}", f"level: {r['level']}",
             f"raise: {'yes' if r['raise'] else 'no'}"]
    lines += r["notes"]
    lines += [f"reason: {k} {p}" for k, p in r["reasons"]]
    if since is not None:
        now = {f"reason: {k} {p}" for k, p in r["reasons"]}
        lines += [f"new {line}" for line in sorted(now) if line not in since]
        lines += [f"gone {line}" for line in since if line.startswith("reason: ") and line not in now]
    lines += [f"guide file changed: {p}" for p in r["guide"]]
    lines.append(edit_line("test edits", r["lost"], r["skips"]))
    lines += [f"  assertion removed: {p}: {l}" for p, l in r["lost"]]
    lines += [f"  skip added: {p}: {l}" for p, l in r["skips"]]
    if r["since"]:
        lines.append(edit_line("test edits since the last check", *r["since"]))
        lines += [f"  assertion removed: {p}: {l}" for p, l in r["since"][0]]
        lines += [f"  skip added: {p}: {l}" for p, l in r["since"][1]]
    return "\n".join(lines) + "\n"


def main(argv=None):
    a = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    a.add_argument("--root", default=".")
    a.add_argument("--base", required=True)
    a.add_argument("--default-branch")
    a.add_argument("--since", help="an earlier output file: print new and gone reasons, and the test edits since its HEAD")
    a.add_argument("--out")
    args = a.parse_args(argv)
    try:
        since, since_head = None, None
        if args.since:
            since = pathlib.Path(args.since).read_text(encoding="utf-8").splitlines() if pathlib.Path(args.since).is_file() else []
            since_head = next((line[6:].strip() for line in since if line.startswith("head: ")), None)
        text = render(run(args.root, args.base, args.default_branch, since_head), since)
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
