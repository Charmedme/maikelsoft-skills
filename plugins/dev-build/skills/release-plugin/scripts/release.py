#!/usr/bin/env python3
"""Release helper for a Claude Code plugin marketplace repository.

    release.py status <plugin dir>                 report version, tag, changelog, proposed bump
    release.py apply <plugin dir> <version>        write the new version in all release files
    release.py bump-check --base <git ref>         exit 1 if a changed plugin has no new version
    release.py pr --head <branch> --title <t> --body-file <f> [--base main]
                                                   open a pull request with `gh api`

Tags have the form <plugin>--v<version>, the form of `claude plugin tag`.
The script uses only the Python standard library. It runs from any folder
inside the repository.
"""
import argparse
import datetime
import json
import pathlib
import re
import subprocess
import sys

SEMVER = re.compile(r"^(\d+)\.(\d+)\.(\d+)$")
# A version heading in Keep a Changelog form: "## [1.2.0] - 2026-10-13".
VERSION_HEADING = re.compile(r"^## \[(\d+\.\d+\.\d+)\](?: - (\d{4}-\d{2}-\d{2}))?", re.M)
UNRELEASED = re.compile(r"^## \[Unreleased\][^\n]*\n(.*?)(?=^## \[|^\[[^\]]+\]: |\Z)", re.M | re.S)
LINK = re.compile(r"^\[([^\]]+)\]: (\S+)$", re.M)
# Sections of Keep a Changelog that make a major, minor, or patch change.
MAJOR_SECTIONS = {"Removed"}
MAJOR_WORDS = re.compile(r"\b(BREAKING|Action required)\b", re.I)
MINOR_SECTIONS = {"Added", "Changed", "Deprecated"}
PATCH_SECTIONS = {"Fixed", "Security"}
# Changes in these sub-folders of a plugin do not reach the user, so they need no new version.
NO_BUMP_DIRS = ("evals/",)


def git(*args, cwd=None, check=True):
    out = subprocess.run(["git", *args], cwd=cwd, capture_output=True, text=True)
    if check and out.returncode != 0:
        raise SystemExit(f"git {' '.join(args)} failed: {out.stderr.strip()}")
    return out.stdout.strip()


def repo_root(start: pathlib.Path) -> pathlib.Path:
    return pathlib.Path(git("rev-parse", "--show-toplevel", cwd=start))


def load_json(path: pathlib.Path):
    return json.loads(path.read_text(encoding="utf-8"))


def parse_version(text: str):
    m = SEMVER.match(text or "")
    if not m:
        raise SystemExit(f"'{text}' is not a version in the form MAJOR.MINOR.PATCH.")
    return tuple(int(x) for x in m.groups())


def marketplace(root: pathlib.Path):
    path = root / ".claude-plugin" / "marketplace.json"
    return (path, load_json(path)) if path.is_file() else (path, None)


def plugin_info(plugin_dir: pathlib.Path):
    manifest = plugin_dir / ".claude-plugin" / "plugin.json"
    if not manifest.is_file():
        raise SystemExit(f"{plugin_dir}: no .claude-plugin/plugin.json. Give the folder of one plugin.")
    data = load_json(manifest)
    return manifest, data


def unreleased_sections(changelog: str) -> dict:
    """Return {section name: [entries]} of the [Unreleased] part."""
    m = UNRELEASED.search(changelog)
    if not m:
        return {}
    sections, current = {}, None
    for line in m.group(1).splitlines():
        head = re.match(r"^### (\w[\w ]*)", line)
        if head:
            current = head.group(1).strip()
            sections.setdefault(current, [])
        elif current and line.startswith("- "):
            sections[current].append(line[2:].strip())
    return {k: v for k, v in sections.items() if v}


def propose_bump(version, sections: dict):
    """Return (bump, reason). bump is major, minor, patch, or None."""
    if not sections:
        return None, "[Unreleased] has no entries."
    names = set(sections)
    text = " ".join(e for v in sections.values() for e in v)
    if names & MAJOR_SECTIONS or MAJOR_WORDS.search(text):
        if version[0] == 0:  # SemVer: in 0.y.z a breaking change raises the minor number
            return "minor", "a breaking change in a 0.y.z version"
        return "major", "a removal or a breaking change"
    if names & MINOR_SECTIONS:
        return "minor", "new or changed behavior: " + ", ".join(sorted(names & MINOR_SECTIONS))
    if names <= PATCH_SECTIONS:
        return "patch", "only fixes"
    return "minor", "sections that are not standard: " + ", ".join(sorted(names - PATCH_SECTIONS))


def bumped(version, bump):
    major, minor, patch = version
    if bump == "major":
        return f"{major + 1}.0.0"
    if bump == "minor":
        return f"{major}.{minor + 1}.0"
    return f"{major}.{minor}.{patch + 1}"


def tags_for(root, name):
    tags = git("tag", "-l", f"{name}--v*", cwd=root, check=False).split()
    good = [t for t in tags if SEMVER.match(t.split("--v", 1)[1])]
    return sorted(good, key=lambda t: parse_version(t.split("--v", 1)[1]))


def readme_versions(root, name):
    """Versions in table rows of the root README that name the plugin."""
    readme = root / "README.md"
    if not readme.is_file():
        return []
    found = []
    for line in readme.read_text(encoding="utf-8").splitlines():
        if line.startswith("|") and re.search(r"(^|[^\w-])" + re.escape(name) + r"([^\w-]|$)", line):
            found += re.findall(r"\b\d+\.\d+\.\d+\b", line)
    return found


def status(plugin_dir: pathlib.Path) -> dict:
    root = repo_root(plugin_dir)
    _, data = plugin_info(plugin_dir)
    name, version_text = data.get("name"), data.get("version")
    problems = []
    if not version_text:
        problems.append("plugin.json has no version. Users then get each commit. Add a version.")
        version_text = "0.0.0"
    version = parse_version(version_text)
    changelog_path = plugin_dir / "CHANGELOG.md"
    changelog = changelog_path.read_text(encoding="utf-8") if changelog_path.is_file() else ""
    if not changelog:
        problems.append("The plugin has no CHANGELOG.md.")
    released = VERSION_HEADING.findall(changelog)
    newest = released[0][0] if released else None
    if newest and newest != version_text:
        problems.append(f"plugin.json has {version_text}, but the newest changelog version is {newest}.")
    for label, url in LINK.findall(changelog):
        if "/compare/" in url or "/releases/tag/" in url:
            if f"{name}--v" not in url:
                problems.append(f"Changelog link [{label}] does not use the tag form {name}--v<version>: {url}")
    _, market = marketplace(root)
    market_version = None
    if market:
        entries = [p for p in market.get("plugins", []) if p.get("name") == name]
        if not entries:
            problems.append(f"marketplace.json has no entry for {name}.")
        elif "version" in entries[0]:
            market_version = entries[0]["version"]
            if market_version != version_text:
                problems.append(f"marketplace.json has {market_version}, plugin.json has {version_text}. "
                                "plugin.json wins, so remove the marketplace version or make them equal.")
    for v in readme_versions(root, name):
        if v != version_text:
            problems.append(f"The README table shows {v} for {name}, plugin.json has {version_text}.")
    sections = unreleased_sections(changelog)
    bump, reason = propose_bump(version, sections)
    tags = tags_for(root, name)
    return {
        "plugin": name,
        "version": version_text,
        "marketplace_version": market_version,
        "last_tag": tags[-1] if tags else None,
        "tag_for_version_exists": f"{name}--v{version_text}" in tags,
        "unreleased": sections,
        "proposed_bump": bump,
        "proposed_version": bumped(version, bump) if bump else None,
        "reason": reason,
        "problems": problems,
    }


def apply(plugin_dir: pathlib.Path, new_version: str, date: str):
    root = repo_root(plugin_dir)
    manifest, data = plugin_info(plugin_dir)
    name, old = data["name"], data.get("version", "0.0.0")
    if parse_version(new_version) <= parse_version(old):
        raise SystemExit(f"{new_version} is not higher than the current version {old}.")
    changelog_path = plugin_dir / "CHANGELOG.md"
    text = changelog_path.read_text(encoding="utf-8")
    m = UNRELEASED.search(text)
    if not m or not unreleased_sections(text):
        raise SystemExit("[Unreleased] has no entries. Write the changelog entries first.")
    body = m.group(1).strip("\n")
    new_part = f"## [Unreleased]\n\n## [{new_version}] - {date}\n\n{body}\n\n"
    text = text[:m.start()] + new_part + text[m.end():]
    # Compare links: [Unreleased] starts at the new tag; the new version compares with the old tag.
    repo_url = repo_web_url(root)
    links = dict(LINK.findall(text))
    text = LINK.sub("", text).rstrip() + "\n\n"
    new_links = {"Unreleased": f"{repo_url}/compare/{name}--v{new_version}...HEAD"}
    first_release = not VERSION_HEADING.findall(text)[1:]
    new_links[new_version] = (f"{repo_url}/releases/tag/{name}--v{new_version}" if first_release
                              else f"{repo_url}/compare/{name}--v{old}...{name}--v{new_version}")
    for label, url in links.items():
        if label not in new_links and label.lower() != "unreleased":
            new_links[label] = url
    text += "\n".join(f"[{k}]: {v}" for k, v in new_links.items()) + "\n"
    changelog_path.write_text(text, encoding="utf-8")
    raw = manifest.read_text(encoding="utf-8")
    manifest.write_text(re.sub(r'("version"\s*:\s*")[^"]*(")', r"\g<1>" + new_version + r"\2", raw, count=1),
                        encoding="utf-8")
    changed = [str(manifest.relative_to(root)), str(changelog_path.relative_to(root))]
    market_path, market = marketplace(root)
    if market and any(p.get("name") == name and "version" in p for p in market.get("plugins", [])):
        raw = market_path.read_text(encoding="utf-8")
        pattern = r'("name"\s*:\s*"' + re.escape(name) + r'"[^{}]*?"version"\s*:\s*")[^"]*(")'
        market_path.write_text(re.sub(pattern, r"\g<1>" + new_version + r"\2", raw, count=1, flags=re.S),
                               encoding="utf-8")
        changed.append(str(market_path.relative_to(root)))
    readme = root / "README.md"
    if readme.is_file():
        lines = readme.read_text(encoding="utf-8").splitlines(keepends=True)
        hit = False
        for i, line in enumerate(lines):
            if line.startswith("|") and re.search(r"(^|[^\w-])" + re.escape(name) + r"([^\w-]|$)", line) \
                    and old in line:
                lines[i] = line.replace(old, new_version)
                hit = True
        if hit:
            readme.write_text("".join(lines), encoding="utf-8")
            changed.append("README.md")
    return changed


def repo_web_url(root):
    url = git("remote", "get-url", "origin", cwd=root, check=False)
    m = re.search(r"github\.com[:/](.+?)(?:\.git)?$", url)
    return f"https://github.com/{m.group(1)}" if m else "https://github.com/OWNER/REPO"


def bump_check(base: str, root: pathlib.Path) -> int:
    _, market = marketplace(root)
    if not market:
        raise SystemExit("No .claude-plugin/marketplace.json in the repository.")
    errors = 0
    for entry in market.get("plugins", []):
        source = entry.get("source")
        if not isinstance(source, str) or not source.startswith("./"):
            continue  # a remote source is released in its own repository
        rel = source[2:].rstrip("/") + "/"
        files = git("diff", "--name-only", f"{base}...HEAD", "--", rel, cwd=root).splitlines()
        files = [f for f in files if not f[len(rel):].startswith(NO_BUMP_DIRS)]
        if not files:
            continue
        manifest = f"{rel}.claude-plugin/plugin.json"
        old_raw = git("show", f"{base}:{manifest}", cwd=root, check=False)
        old = json.loads(old_raw).get("version") if old_raw else None
        new = load_json(root / manifest).get("version")
        if old is not None and old == new:
            errors += 1
            print(f"ERROR {entry['name']}: {len(files)} changed file(s) but the version is still {new}. "
                  f"Raise the version in {manifest} and add a changelog entry.")
        else:
            print(f"OK    {entry['name']}: {old or 'new'} -> {new}")
    return 1 if errors else 0


def open_pr(root, head, base, title, body_file):
    url = repo_web_url(root)
    slug = url.split("github.com/", 1)[1]
    payload = {"title": title, "head": head, "base": base,
               "body": pathlib.Path(body_file).read_text(encoding="utf-8")}
    out = subprocess.run(["gh", "api", "-X", "POST", f"repos/{slug}/pulls", "--input", "-", "--jq", ".html_url"],
                         input=json.dumps(payload), capture_output=True, text=True)
    if out.returncode == 0:
        print(f"PR {out.stdout.strip()}")
        return 0
    print(f"NOT MADE: gh api gave an error: {out.stderr.strip()[:300]}")
    print(f"Open the PR in the browser: {url}/compare/{base}...{head}?expand=1")
    return 1


def main():
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    sub = parser.add_subparsers(dest="cmd", required=True)
    s = sub.add_parser("status")
    s.add_argument("plugin_dir")
    s.add_argument("--json", action="store_true")
    a = sub.add_parser("apply")
    a.add_argument("plugin_dir")
    a.add_argument("version")
    a.add_argument("--date", default=datetime.date.today().isoformat())
    b = sub.add_parser("bump-check")
    b.add_argument("--base", required=True)
    p = sub.add_parser("pr")
    p.add_argument("--head", required=True)
    p.add_argument("--base", default="main")
    p.add_argument("--title", required=True)
    p.add_argument("--body-file", required=True)
    args = parser.parse_args()

    if args.cmd == "status":
        result = status(pathlib.Path(args.plugin_dir).resolve())
        if args.json:
            print(json.dumps(result, indent=2))
        else:
            print(f"Plugin:   {result['plugin']} {result['version']}")
            print(f"Last tag: {result['last_tag'] or 'none'}")
            for section, entries in result["unreleased"].items():
                print(f"Unreleased {section}: {len(entries)}")
            if result["proposed_bump"]:
                print(f"Proposed: {result['proposed_bump']} -> {result['proposed_version']} ({result['reason']})")
            else:
                print(f"Proposed: nothing to release ({result['reason']})")
            for problem in result["problems"]:
                print(f"PROBLEM   {problem}")
            print(f"{len(result['problems'])} problem(s)")
        return 1 if result["problems"] else 0
    if args.cmd == "apply":
        parse_version(args.version)
        for path in apply(pathlib.Path(args.plugin_dir).resolve(), args.version, args.date):
            print(f"CHANGED {path}")
        return 0
    root = repo_root(pathlib.Path.cwd())
    if args.cmd == "bump-check":
        return bump_check(args.base, root)
    return open_pr(root, args.head, args.base, args.title, args.body_file)


if __name__ == "__main__":
    sys.exit(main())
