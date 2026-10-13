---
name: release-plugin
description: Plugin release in a Claude Code plugin marketplace repository. Use when the user wants a plugin released or published, its version raised, or the release pull request opened. Runs the CI checks locally first. Writes in plain language, ASD-STE100 for English and B1 for Dutch.
allowed-tools: Bash(python3 ${CLAUDE_SKILL_DIR}/scripts/release.py *), Bash(python3 ${CLAUDE_SKILL_DIR}/scripts/cirun.py *)
---

# Release a plugin

This skill takes a plugin from a finished branch to an open pull request with green CI. It runs the CI checks on this computer before the push, so CI does not fail after the push. It stops at the pull request. A person merges.

- Use it for: a new version of a plugin in a marketplace repository (`.claude-plugin/marketplace.json` plus `plugins/<plugin>/`).
- Not for: writing the skill itself (use `build-skill`), or a changelog without a release (use `doc-changelog`).

The scripts are in `${CLAUDE_SKILL_DIR}/scripts/`. Each script uses only the Python standard library.

## Steps

1. **Start from the latest main.** Run `git fetch origin main` and rebase the branch on `origin/main`. The checks must see the code after the merge. Two pull requests can each pass and still fail together. Done when `git merge-base --is-ancestor origin/main HEAD` exits 0.
2. **Read the status.** Run `python3 ${CLAUDE_SKILL_DIR}/scripts/release.py status plugins/<plugin>`. Fix each `PROBLEM` line. When `[Unreleased]` is empty, write the entries from the commits since the last tag. Read `references/changelog.md` for the entry rules. Done when the status shows 0 problems and a proposed version.
3. **Ask for the version.** Send one message to the human: the proposed version, the reason, and the `[Unreleased]` entries. Wait for the answer. The version decides what the users get, so the human decides it. Done when the human confirms a version.
4. **Write the version.** Run `release.py apply plugins/<plugin> <version>`. It changes `plugin.json`, the changelog, and the README table. It also changes the marketplace entry when that entry has a version. Done when it prints a `CHANGED` line for each file.
5. **Run CI locally.** Run `python3 ${CLAUDE_SKILL_DIR}/scripts/cirun.py`. It runs each `run:` step of the pull request workflows. Fix each `FAIL`, then run it again. Use the fix that the step output gives. For example, run the sync script of the repository for stale copies. Done when it shows `0 failed`.
6. **Commit and push.** Use the commit style of `git log`. Push with `git push -u origin <branch>`. Done when the push shows the branch on the remote.
7. **Open the pull request.** Write the body to a file. Give what changed, the version, the check results, and the test results. Run `release.py pr --head <branch> --title "<title>" --body-file <file>`. This uses `gh api`, which works when the GitHub connector has no permission to open a pull request. Done when it prints `PR <url>`.
8. **Wait for CI.** Run `gh pr checks <number> --watch`. When a check fails, fix it, push, and wait again. Done when all checks pass.
9. **Report.** Give the version, the link to the pull request, and the result of each check. Say what happens after the merge (step 10). Merge only when the human says so in this conversation, because a merge publishes the version to all users.
10. **After the merge.** The tag `<plugin>--v<version>` comes from the tag workflow on `main`. When the repository has no tag workflow, read `references/ci.md` and offer to add it. Until then, give the human the command `claude plugin tag plugins/<plugin> --push`.

Optional: when the session belongs to a claude.ai project, write the test results of the release to the project.

## When no human is there

- Take the proposed version in step 3. Say in the report that the human did not confirm it.
- Stop after step 8. Do not merge.

## When a step fails

- `push` gives 403: the session has no push permission for the repository. Tell the human which permission is missing.
- `release.py pr` prints `NOT MADE`: give the human the compare link that it printed.
- A CI check fails only on GitHub: compare the job log with the local output of `cirun.py`. A step that uses `${{ }}` runs only on GitHub, so `cirun.py` skips it.

## Language

Write the changelog entries, the pull request text, and the report with the rules in `references/language.md`. Use English unless the human asks for Dutch.
