# Concept: hard test cases for dev-stack

Date: 2026-10-21. Input: the divergence analysis of batch 2 (`../batch2/divergence.md`).

## The problem

The batch 2 cases are too easy for Opus. Without the skill, Opus passes most traps in 3 of 3 runs, and both variants find 80 of the 89 planted mistakes in every run. Only 38 items separate the variants. A case that both variants always pass measures nothing.

Three causes:

1. The request gives the trap away. Examples: "We run three instances" points to the cache backplane. A list of package versions points to the transitive version trap.
2. Each case has one main trap in new code. A strong model looks for it.
3. The review cases have no decoys, so a review that flags everything loses nothing.

## The hard cases

Each of the 11 batch 2 skills gets two new cases. The old cases stay, so the old results stay comparable.

| Case | Name |
|------|------|
| Hard write case | `<skill>-hard` |
| Hard review case | `<skill>-review-hard` |

### Rules for a hard write case

- The request is in business words. It does not name a trap, a risk, or a technical detail that points to a trap. It names the approved packages without versions.
- 6 traps or more. 4 or more have a check assertion or a hidden probe test.
- 2 or more traps are in the existing code: a bug that the new feature touches. The agent must fix it, or report it as an open point. A probe test or a judge item checks it.
- 2 or more traps are quiet defaults: no error and no warning, only wrong behavior. Examples from batch 2: classic queues, publisher confirms off, 5 traces per second.
- The fixture is a real service with 60 source files or more. It has more modules, more tests, and code that has nothing to do with the task.
- The start point builds, and its tests pass.
- Judge items test one thing each (lesson of E11). A judge item tests behavior or a stated decision, not the words of the skill.

### Rules for a hard review case

- 8 to 10 planted mistakes. 4 or more are subtle: in existing code next to the change, over two files, or a version fact.
- 3 decoys or more: code that looks wrong but is correct. A finding on a decoy is a false positive. The answer sheet lists them under `decoys`.
- The review request does not hint at the mistakes.
- The diff is larger: 10 changed files or more.

### Self-test

Each hard write case has `selftest/<case>/` with `common`, `naive`, `good`, and its own `expected.json`.

- `naive`: the solution that a strong engineer writes without the version facts. It fails 4 or more trap checks.
- `good`: a correct solution. It passes each check.
- `fixture`: the start point. Build and tests pass.

## Measure

Run each hard case 3 times without the skill and 3 times with it, on Opus. A hard case is good when the baseline fails 3 or more items in 2 or more of its runs. Then run the divergence analysis.

## Files

| Path | Content |
|------|---------|
| `evals/dev-stack/cases/<case>.json` | One case object, in the form of an entry of `evals.json` |
| `evals/dev-stack/fixtures/<skill>-hard/` | The fixture of the write case |
| `evals/dev-stack/fixtures/review-<skill>-hard/` | The fixture of the review case, with the branches of `prepare.py` |
| `evals/dev-stack/planted/review-<skill>-hard.json` | `planted` and `decoys` |
| `evals/dev-stack/probes/<skill>-hard/` | The hidden probe tests |
| `evals/dev-stack/selftest/<skill>-hard/` | The overlays and `expected.json` |
