# Design: the delivery suite (2026-11-02)

The plan of 2026-11-02, version 6, starts with a better test suite. The last comparison could not separate the flows: a plain session passed 125 of 126 hidden tests. This suite must have stories that a plain session fails, and easy stories that show the cost of each flow.

## Decisions

| No | Decision | Reason |
|----|----------|--------|
| D1 | Two sets now: `conduit` (RealWorld on ASP.NET Core) and `dashboard` (Practical.CleanArchitecture on .NET 10, PostgreSQL, and Angular). The security set (OWASP Juice Shop) comes later. | The owner asked for a dashboard on the technology of `dev-stack`, plus projects with a known solution. |
| D2 | Each project is pinned to one commit. A patch makes the dashboard run without an identity server and without cloud services. | A run must start the same each time, and two runs must work at the same time. |
| D3 | The tests use four layers: the case in domain words, a DSL, a protocol driver, and the system. | Farley: the case stays true when the implementation changes. The test author does not need the code. |
| D4 | One agent writes the hidden tests, another agent writes the dashboard references, and neither sees the work of the other. The Conduit references are the upstream code or written by the orchestrator, who did not see the tests. | Independent checks, against tunnel vision. |
| D5 | The upstream RealWorld API tests run as regression checks on each Conduit story. | A later story must not break earlier behaviour (defect type 16). The upstream tests have an outside author. |
| D6 | A story is valid when its hidden tests fail on the base and pass on the reference, three times in a row. | A test must be able to go red, and the known solution must pass. |
| D7 | A hard story stays only when a plain session fails at least one hidden test in a calibration run. An easy story stays as a cost control. | The suite must separate the flows, and it must show the overhead of a flow on a small change. |
| D8 | The story texts look like real tickets and are not in ASD-STE100. CI does not check `evals/*/stories/`. | They are input data, like the eval fixtures. |

## Stories

| Story | Set | Size | Difficulty | Trap |
|-------|-----|------|------------|------|
| `rw-title-trim` | conduit | patch | easy | Cost control |
| `rw-favorites` | conduit | small | easy | Known solution, the flag in each list |
| `rw-paging-limits` | conduit | small | medium | Exact limits on two endpoints |
| `rw-comments` | conduit | medium | medium | A comment of another article |
| `rw-followers` | conduit | medium | medium | A rule only in `REVIEW.md`, migrations for two databases |
| `rw-scheduled-publish` | conduit | large | hard | One rule on each read path, time offsets, the tag list |
| `rw-account-delete` | conduit | large | hard | Access after removal, restricted foreign keys |
| `rw-revisions` | conduit | team | very hard | A real race, slug history |
| `dash-name-length` | dashboard | patch | easy | Cost control |
| `dash-product-code` | dashboard | medium | medium | Uniqueness after a trim and without case, a race |
| `dash-lost-update` | dashboard | medium | medium | A concurrency token that the API ignores |
| `dash-audit-range` | dashboard | large | hard | Day limits at UTC+14 and UTC-12 |
| `dash-csv-import` | dashboard | team | very hard | All or nothing, audit trail, two permissions |
| `dash-delete-a11y` | dashboard | medium | medium | Focus, names, and a status message in a browser |

The traps come from the 30 generic defect types of the internal set. No story text comes from the internal set.

## Seams that the references found

- The Conduit test `MigrationTests` counts the migrations. Each story with a schema change must update it.
- The Conduit build treats analyzer warnings as errors. A generated migration fails on IDE0161 and CA1861 until the developer changes it.
- In Conduit, the navigation `Person.Followers` holds the people that the person follows. The name is the opposite of its content.

## Verification (2026-11-02)

Each of the 14 stories is valid. The hidden tests fail on the base. The reference passes each hidden test three times in a row, and the own tests of the project pass on the reference.

| Set | Stories | Hidden cases | Cases that fail on the base |
|-----|---------|--------------|-----------------------------|
| conduit | 8 | 216, with the upstream files | 4 to 19 for each story |
| dashboard | 6 | 70 | 3 to 18 for each story |

## Calibration with a plain session (Sonnet, dev-stack only)

| Story | Difficulty | Runs | Result | Cost of all runs |
|-------|------------|------|--------|------------------|
| `rw-title-trim` | easy | 3 | All pass | $0.86 |
| `rw-favorites` | easy | 3 | All pass | $1.07 |
| `rw-paging-limits` | medium | 3 | All pass | $0.75 |
| `rw-comments` | medium | 3 | 22 of 24 each time. The status of a new comment is 200, and the specification says 201. | $1.33 |
| `rw-followers` | medium | 3 | All pass | $1.41 |
| `rw-scheduled-publish` | hard | 3 | All pass | $1.93 |
| `rw-account-delete` | hard | 3 | All pass | $1.60 |
| `rw-revisions` | very hard | 3 | 30 of 31 each time. The article list has no `version`. | $2.30 |
| `dash-name-length` | easy | 1 | All pass | $0.25 |
| `dash-product-code` | medium | 1 | 10 of 11. A seeded code can be used again with other case. | $0.59 |
| `dash-lost-update` | medium | 1 | All pass | $0.64 |
| `dash-audit-range` | hard | 1 | 3 of 18. The search gives 500 on PostgreSQL. The run did not start the endpoint. | $0.45 |
| `dash-csv-import` | very hard | 1 | All pass | $0.79 |
| `dash-delete-a11y` | medium | 1 | 8 of 9. The page scrolls sideways at 390 px. | $0.28 |

The results of the three Conduit runs were the same each time. The calibration cost $14.25 in total.

### What the calibration shows

1. A plain session passes most API stories with clear criteria, also the hard ones. The difficulty that we gave a story does not predict a miss.
2. Five stories separate: `rw-comments`, `rw-revisions`, `dash-product-code`, `dash-audit-range`, and `dash-delete-a11y`. The misses are of four kinds:
   - A detail of an outside specification (status 201).
   - A rule that applies to a second read path (the list).
   - Data that exists before the change (the seeded codes, the navigation bar).
   - Code that the run did not start against the real database.
3. These kinds match the defect data: most real defects are not in the criteria of the story. Two steps can find them: "prove it" (start the system, run the full suite) and an independent review.

### Decisions about the stories

- Keep the five stories that separate as the comparison set.
- Keep `rw-title-trim` and `dash-name-length` as cost controls.
- Keep the other seven as regression stories: a flow must not make them worse, and they show the cost of a flow. Decision D7 says to rewrite them. This needs a separate decision of the owner, because their traps were real and a plain session still found them.
