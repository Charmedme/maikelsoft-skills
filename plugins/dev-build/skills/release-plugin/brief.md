# Brief: release-plugin

Status: approved (G1, 2026-10-13)

## Need
Maikel releases plugins of the maikelsoft-skills marketplace from claude.ai and Claude Code sessions. Each release today has the same faults. The PR tool gives 403. The tag push is blocked, so no tag exists. CI fails on `doccheck.py` after the push.

## Workflow
1. Sync the shared files and run the unit tests. Input: the branch. Goes wrong when: a plugin-scoped shared file is new.
2. Run all CI checks locally: `doccheck.py --level standard` on each Markdown file, `sync_shared.py --check`, `claude plugin validate`. Goes wrong when: this step is skipped, and CI fails after the push.
3. Propose the version bump from `[Unreleased]`: Removed or breaking is major, Added is minor, only Fixed is patch. The human confirms.
4. Update `plugin.json`, `CHANGELOG.md` (new section, ISO date, compare links), the README table, and `marketplace.json`.
5. Commit on a branch, push, open the PR with `gh api`. Goes wrong when: the MCP PR tool is used (403).
6. Wait for green CI. Merge only when the human says so.
7. Optional: write the results to the claude.ai project.
8. After the merge, a CI job on `main` makes the tag with `claude plugin tag --push`.

## Output
A PR with green CI, and a short report: version, checks, PR link.

## Triggers
- Must start: "release dev-build", "publish this plugin", "bump the version and open the PR".
- Must not start: "write the changelog entry" (doc-changelog), "build a new skill" (build-skill).

## Success criteria
- All local checks show 0 errors before the push.
- The version in `plugin.json` and the newest changelog heading are equal.
- The changelog compare links use `<plugin>--v<version>`.
- The PR is open, made with `gh api`, and no merge happened without the human.
- The bump type follows the `[Unreleased]` entries, and the human confirmed it.

## Limits
- No merge without the human, because a merge publishes to all users.
- No tag from the skill, because the CI job makes it.

## Language and runtime
Output in English (ASD-STE100). Dutch (B1) when the human asks. Claude Code and claude.ai sessions with a shell.

## Repo changes in the same release
- CI job: tag each plugin whose `<plugin>--v<version>` tag does not exist (also the 4 current releases).
- CI check: a changed plugin folder without a new version fails.
- Tag format `<plugin>--v<version>` in CONTRIBUTING, publish.md, and all changelog links.

## Decisions
| No | Decision | Who |
|----|----------|-----|
| Q1 | A skill in dev-build | human |
| Q2 | Walkthrough as above, plus the optional project upload | human |
| Q3 | Faults: PR 403, tag blocked, CI fails after push, no tag | human |
| Q4 | Tag format `<plugin>--v<version>` | human |
| Q5 | A CI job makes the tags | human |
| Q6 | Proposed bump plus a CI check on the version | human |
| Q7 | Stop at an open PR with green CI | human |
| Q14 | English STE, Dutch B1 on request | human |
