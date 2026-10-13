# Changelog entries and the version

Read this file when `[Unreleased]` is empty or when you must check the proposed version.

## Contents

- Entries
- Version rules
- Tags and links

## Entries

The changelog follows Keep a Changelog 1.1.0. Each entry is for the user of the plugin, not for the developer.

1. Read the commits since the last tag: `git log <plugin>--v<version>..HEAD --oneline -- plugins/<plugin>`. When no tag exists, start from the commit of the newest changelog version.
2. Put each change that a user sees in one section: Added, Changed, Deprecated, Removed, Fixed, or Security.
3. Leave out changes that the user does not see: tests, evals, CI, and refactoring without a change in behavior.
4. Write one sentence for each entry. Name the skill or the file. Say what the user can do now.

Good: "`release-plugin` runs the CI checks locally before the push."
Bad: "Refactored release flow."

## Version rules

`release.py status` proposes the version from the sections. The rules come from Semantic Versioning 2.0.0:

| Sections in `[Unreleased]` | Change |
|----------------------------|--------|
| Removed, or an entry with "BREAKING" or "Action required" | major (minor in a 0.y.z version) |
| Added, Changed, or Deprecated | minor |
| Only Fixed or Security | patch |

A change to a skill text that changes its output is "Changed". A rename of a skill is "Removed" plus "Added", because a user who calls the old name gets nothing.

Users get the new version only when `version` in `plugin.json` changes. So each release raises it, also for one small fix.

## Tags and links

- The tag is `<plugin>--v<version>`, for example `dev-build--v1.2.0`. This is the form of `claude plugin tag`, and version ranges in `dependencies` use it.
- The compare link of `[Unreleased]` starts at the newest tag. The link of a version compares the tag before it with its own tag. `release.py apply` writes these links.
