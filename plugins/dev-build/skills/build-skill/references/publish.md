# Publish a skill

Publish only after the human approves the test results.

## Contents

- Put the skill in a plugin
- Release steps
- Install check

## Put the skill in a plugin

A plugin is a folder that holds skills and agents, plus a manifest.

```
plugins/<plugin>/
  .claude-plugin/plugin.json
  README.md
  CHANGELOG.md
  skills/<skill>/SKILL.md
```

`plugin.json` has `name`, `displayName`, `version`, `description`, `author`, `license`, and `keywords`. A marketplace file (`.claude-plugin/marketplace.json`) lists each plugin with `name`, `source`, `description`, `category`, and `tags`.

## Release steps

1. Run the unit tests of the repo.
2. Copy the shared files into the skill (`python3 scripts/sync_shared.py`) and check (`--check`).
3. Run `claude plugin validate --strict ./plugins/<plugin>` and `claude plugin validate .`.
4. Raise the version in `plugin.json`. Add an entry to `CHANGELOG.md` (Keep a Changelog format). Add the plugin to the README table and to `marketplace.json`.
5. Show the human the diff. Commit on a branch. Open a pull request. Merge after approval.
6. Tag the release `<plugin>-v<version>`.

Do not push to the main branch or tag without the human's approval.

## Install check

After the release, install the plugin in a clean session. Ask for a task that must trigger the skill. Check that it triggers and that the output is right.
