# Review instructions

<!-- /setup writes this file as REVIEW.md at the root of the repository. The reviewers read it
from the base commit, so a change cannot relax its own review. Change it in a pull request of its own.
Replace each <...> line. Remove a section that you do not need. -->

## What must be fixed before a merge
<What failure looks like in this repository. For example: a broken rule of the behaviour spec, data that a change can lose, a public API that changes without a new version.>

## Do not report
<Generated paths, and each rule that CI already enforces. For example: `src/Generated/`, formatting that `dotnet format` fixes.>

## Always check
<The rules of this repository that a reviewer can miss. For example: each endpoint has an authorization policy; each migration can run twice.>

## Nits
Show at most 5 nits in full in each review. Put each other nit on one line.

## Evidence
A claim about behaviour needs a file and a line. Start the summary with a count, and say so when there is nothing to fix.

## Risk tiers
<Paths and changes that always give a high risk, so the security review always runs. For example: `src/Auth/`, `**/Migrations/`, `.github/workflows/`, a new package.>
