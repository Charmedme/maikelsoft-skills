# Results: shared parts of plan version 7

Date: 2026-11-04. Brief: `brief.md` (G1 approved on 2026-11-04).

## Files

| File | State |
|------|-------|
| `shared/cycle.md` | New. The caps, the size table, and the step back at the cap. |
| `shared/review-rules.md` | New. The reviewer, the levels `blocking`, `issue`, `nit`, and `older`, the classes of comments, and `review.md`. |
| `shared/review-template.md` | New. The `REVIEW.md` template for `/setup`. |
| `shared/context.md` | New. The role line, `.claude/rules/delivery.md`, the missing facts, and the `Next:` line. |
| `shared/runheader.py` | New. Commands `read`, `write`, and `adopt`. |
| `shared/specsync.py` | New. Commands `check`, `copy`, and `diff`. Formats ADF, Azure DevOps HTML, and Markdown. English and Dutch keywords. |
| `shared/docscheck.py` | New. Findings `DOC-NAMES`, `DOC-CHANGELOG`, and `DOC-README`, and the option `--ok`. |
| `shared/speclock.py` | Moved from dev-flow. New option `--rule` for `approve` and `unlock`. |
| `shared/mutate.py` | Moved from dev-flow. No change. |
| `scripts/sync_shared.py` | The 9 files are in `NAMED_COPIES`. A letter, a digit, "_", or "-" before a file name stops the match. |

## Checks

| Check | Result |
|-------|--------|
| `python3 -m unittest discover -s tests` | 436 tests, OK (1 skipped) |
| New and changed test files | `test_specsync` 24, `test_runheader` 10, `test_docscheck` 7, `test_sync_shared` 4, `test_speclock` and `test_mutate` 18 |
| `python3 scripts/sync_shared.py --check` | 59 skills, 0 stale |
| `doccheck.py --level standard` on the new Markdown files | 0 errors. One warning in `review-rules.md` stays, because the sentence lists the column names of a table. |

## Success criteria of the brief

| Criterion | Result |
|-----------|--------|
| The same fingerprints from ADF JSON, Azure DevOps HTML, and Markdown | Pass |
| One changed rule gives only that rule | Pass, from each of the 3 formats |
| Errors for more than 7 rules and more than 4 scenarios in a rule | Pass |
| Errors for a limit rule without below, at, and above, and a bug spec without `Now:` or "Unchanged" | Pass |
| `speclock.py unlock --rule R3` unlocks only the file of R3, and the old commands still work | Pass |
| The sync check passes, with a test for the new mappings | Pass |

## Choices made during the build

These choices were not in the brief.

- The line numbers of a `specsync.py` finding count the lines of the section, because ADF has no source lines.
- A plain or bold line "Behaviour spec" starts the section when the ticket has no heading. A heading always wins.
- A description line, a tag, or a Background at the level of the Feature is in the fingerprint of the section, not of a rule. A change there gives "CHANGED text of the section outside the rules", with no rule named.
- Gherkin comments (`#`) are not in the canonical text.
- The script removes `**`, `__`, and backticks only from Markdown.
- The script reads a text as HTML only for known tag names. So a placeholder such as `<email>` stays text.
- The ADF reader takes the first `doc` node, so the full JSON of a Jira issue also works.
- `(grens: ...)` is the Dutch form of `(limit: ...)`.
- `diff` exits 2 when the new copy has no section.
- `spec-baseline.json` holds the item, the kind, the time, and the fingerprints. `spec.md` has no fingerprint.

## Not done in this step

- No skill names the new files yet. Steps 4 to 8 add the names, and then the sync makes the copies.
- `workcheck.py` and `runcheck.py` read the header in step 6 and 7.
- The text files get their test through the evals of steps 5 to 7.
