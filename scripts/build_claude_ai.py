#!/usr/bin/env python3
"""Make one plugin zip for an upload to claude.ai.

    python3 scripts/build_claude_ai.py                        # product-flow into dist/claude-ai
    python3 scripts/build_claude_ai.py --plugin dev-docs --out dist/claude-ai
    python3 scripts/build_claude_ai.py --check                # build in a temp folder, report only

The script reads plugins/<plugin>/ and writes <out>/<plugin>.zip. The zip holds the
plugin as its top level: .claude-plugin/plugin.json, skills/, and the other folders and
files of the plugin. It leaves out shared/, because each skill has its own copy of the
shared files (scripts/sync_shared.py). It also leaves out __pycache__ and *.pyc. The
files go into the zip unchanged. Exit 1 when the plugin fails a check.

What we know (2026-11-06):

- Zip layout. An upload of a zip with <name>/SKILL.md one folder down failed with:
  "Zip must contain a .claude-plugin/plugin.json file or a top-level SKILL.md declaring
  plugin components". A zip of the plugin with .claude-plugin/plugin.json at its top
  level installed. So the script makes the plugin zip.
- Description. The support article gives 200 characters:
  https://support.claude.com/en/articles/12512198-how-to-create-custom-skills
  The platform docs give 1,024:
  https://platform.claude.com/docs/en/agents-and-tools/agent-skills/overview
  The plugin zip installed with descriptions of up to 511 characters. So more than
  1,024 is an error, and more than 200 is no problem in a plugin zip.
- Name. 1-64 characters, a-z, 0-9 and "-", and the same as the folder name.
- Size. No official page gives a size or file-count limit. The plugin zip of
  product-flow (138 files, 399 KB) installed.

Standard library only. Python 3.9+.
"""
import argparse
import json
import pathlib
import re
import sys
import tempfile
import zipfile

ROOT = pathlib.Path(__file__).resolve().parents[1]
NAME_RE = re.compile(r"^[a-z0-9]+(-[a-z0-9]+)*$")
NAME_MAX = 64
DESC_MAX = 1024
SKIP_TOP = {"shared"}
SKIP_DIRS = {"__pycache__"}
SKIP_SUFFIXES = {".pyc"}
KEY_RE = re.compile(r"^([A-Za-z0-9_-]+):(.*)$")
# A fixed date makes the same input give the same zip.
ZIP_DATE = (2020, 1, 1, 0, 0, 0)


def split_frontmatter(text):
    """Return the frontmatter lines and the body. Raise ValueError without frontmatter."""
    lines = text.split("\n")
    if not lines or lines[0].strip() != "---":
        raise ValueError("SKILL.md does not start with a '---' line")
    for i in range(1, len(lines)):
        if lines[i].strip() == "---":
            return lines[1:i], "\n".join(lines[i + 1:])
    raise ValueError("SKILL.md has no closing '---' line")


def parse_fields(lines):
    """Group the frontmatter lines by top-level key, in their order.

    An indented line or a blank line belongs to the key above it.
    Returns a list of (key, lines) pairs.
    """
    fields = []
    for line in lines:
        match = KEY_RE.match(line)
        if match:
            fields.append((match.group(1), [line]))
        elif fields:
            fields[-1][1].append(line)
        elif line.strip():
            raise ValueError(f"frontmatter line has no key: {line!r}")
    return fields


def scalar(field_lines):
    """Return the text value of a field, with a block value joined to one line."""
    first = KEY_RE.match(field_lines[0]).group(2).strip()
    rest = [line.strip() for line in field_lines[1:] if line.strip()]
    if first in ("", ">", "|", ">-", "|-", ">+", "|+"):
        return " ".join(rest)
    value = " ".join([first] + rest)
    if len(value) >= 2 and value[0] == value[-1] and value[0] in "'\"":
        value = value[1:-1]
    return value


def check_skill(skill_md):
    """Return the errors of one SKILL.md."""
    folder = skill_md.parent
    try:
        lines, _ = split_frontmatter(skill_md.read_text(encoding="utf-8"))
        values = {key: scalar(field) for key, field in parse_fields(lines)}
    except ValueError as error:
        return [str(error)]
    errors = []
    name = values.get("name", "")
    desc = values.get("description", "")
    if not name:
        errors.append("name is missing")
    elif len(name) > NAME_MAX:
        errors.append(f"name has {len(name)} characters, the limit is {NAME_MAX}")
    elif not NAME_RE.match(name):
        errors.append(f"name {name!r} must use only a-z, 0-9 and single '-' between parts")
    if name and name != folder.name:
        errors.append(f"name {name!r} is not the same as the folder name {folder.name!r}")
    if not desc:
        errors.append("description is missing")
    elif len(desc) > DESC_MAX:
        errors.append(f"description has {len(desc)} characters, the limit is {DESC_MAX}")
    return errors


def check_plugin(plugin_dir):
    """Return the errors of the plugin, each with the place that it applies to."""
    errors = []
    manifest = plugin_dir / ".claude-plugin" / "plugin.json"
    try:
        if not json.loads(manifest.read_text(encoding="utf-8")).get("name"):
            errors.append("plugin.json: name is missing")
    except (OSError, ValueError) as error:
        errors.append(f"plugin.json: {error}")
    for skill_md in sorted((plugin_dir / "skills").glob("*/SKILL.md")):
        errors += [f"{skill_md.parent.name}: {message}" for message in check_skill(skill_md)]
    return errors


def plugin_files(plugin_dir):
    """Yield (path, name in the zip) for each file of the plugin that goes into the zip."""
    for path in sorted(plugin_dir.rglob("*")):
        rel = path.relative_to(plugin_dir)
        if rel.parts[0] in SKIP_TOP or any(part in SKIP_DIRS for part in rel.parts):
            continue
        if path.is_file() and path.suffix not in SKIP_SUFFIXES:
            yield path, rel.as_posix()


def write_zip(plugin_dir, target):
    """Write the zip with the plugin as its top level. Return the number of files."""
    count = 0
    with zipfile.ZipFile(target, "w", zipfile.ZIP_DEFLATED) as archive:
        for path, rel in plugin_files(plugin_dir):
            info = zipfile.ZipInfo(rel, date_time=ZIP_DATE)
            info.compress_type = zipfile.ZIP_DEFLATED
            info.external_attr = (path.stat().st_mode & 0o777) << 16
            archive.writestr(info, path.read_bytes())
            count += 1
    return count


def build(plugin_dir, out, out_print=print):
    """Check the plugin and write its zip into out. Return the number of errors."""
    errors = check_plugin(plugin_dir)
    for message in errors:
        out_print(f"ERROR {message}")
    if errors:
        return len(errors)
    out.mkdir(parents=True, exist_ok=True)
    target = out / f"{plugin_dir.name}.zip"
    count = write_zip(plugin_dir, target)
    out_print(f"{target}  {count} files, {target.stat().st_size / 1024:.1f} KB")
    return 0


def print_errors(message):
    """Print only the errors (for --check)."""
    if message.startswith("ERROR"):
        print(message)


def main(argv=None):
    parser = argparse.ArgumentParser(description="Make a claude.ai upload zip of a plugin.")
    parser.add_argument("--plugin", default="product-flow")
    parser.add_argument("--out", default="dist/claude-ai", help="output folder, relative to the root")
    parser.add_argument("--check", action="store_true", help="build in a temp folder and report only")
    parser.add_argument("--root", default=str(ROOT), help="repository root (for tests)")
    args = parser.parse_args(argv)
    root = pathlib.Path(args.root)
    plugin_dir = root / "plugins" / args.plugin
    if not (plugin_dir / ".claude-plugin" / "plugin.json").is_file():
        print(f"ERROR: no .claude-plugin/plugin.json in {plugin_dir}")
        return 1
    if args.check:
        with tempfile.TemporaryDirectory() as tmp:
            errors = build(plugin_dir, pathlib.Path(tmp), out_print=print_errors)
    else:
        errors = build(plugin_dir, root / args.out)
    if errors:
        print(f"{errors} error(s). No zip was written.")
        return 1
    if args.check:
        print(f"OK: {args.plugin} is ready for claude.ai.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
