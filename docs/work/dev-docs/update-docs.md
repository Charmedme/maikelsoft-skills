# dev-docs 2.0.0: the command /update-docs

Step 8 of the delivery plan, version 7. The plan gives dev-docs one command for the whole doc set: `/update-docs`. The plan calls it "a check and update of the whole doc set of a project". It replaces `doc-plan`. Nobody could answer questions during the build of this step. So each open decision got the recommended answer. This file gives each decision and its reason.

## Decisions

| ID | Decision | Reason |
|----|----------|--------|
| D1 | The skill folder `doc-plan` becomes `update-docs`. The old folder keeps only a `SKILL.md` that forwards to `dev-docs:update-docs`, with `user-invocable: false`. | This is the same pattern as `plan-and-build` in dev-flow 3.0.0 and the old names in product-flow 2.0.0. A call from another skill still works for one major version. |
| D2 | The scope is the whole doc set, with an optional folder in `$ARGUMENTS`. There is no scope for the docs of one change. | Plan version 7 is newer than design 7.2, which had two scopes. The docs of a change are a step of `/build` (`docscheck.py`), so a second entry for that job is not necessary. |
| D3 | The skill selects plan or audit mode from the repository and does not ask. | One question less. The rule is clear: a project with no page other than a README gets a plan. |
| D4 | An outdated page gets the new status "update" with the facts that changed. | "Check and update" in the plan. Before, an outdated page got only a ROT mark. |
| D5 | The skill ends with a report and a `Next:` line. It does not read the role line or the delivery facts. | Each command ends with the next step. The docs job is the same for each role, and the facts of the docs come from the repository. So the context step adds cost and no value. |
| D6 | New major version 2.0.0. | The typed command `/dev-docs:doc-plan` leaves the menu. The plan asks for a new major for each plugin of version 7. |

## Open

- No eval runs yet. The eval `plan-stops-for-approval` in `evals/dev-docs/evals.json` now accepts `update-docs`. Plan step 9 runs the comparison.
