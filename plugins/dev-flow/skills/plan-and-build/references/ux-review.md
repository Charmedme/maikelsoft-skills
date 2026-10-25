# UX review (small, medium, and large tier)

The UX review checks the built screens against the approved design: `ux-design.md`, the mockups, and the prototype. It runs after the review of the code and before the gate Done. It needs the `ux-design` plugin, Playwright, and the run command of the app in the `## Design` section of `CLAUDE.md`.

## Steps

1. **Start the agent.** Start the `ux-design:ux-designer` agent in `review` mode, with the model `sonnet` (the Agent tool takes a `model` parameter). In the tests, Sonnet found the same defects as Opus at less than half the cost. Give it the work folder, the base of the diff, and `light` for the small tier.
2. **Read the result.** The agent writes `ux-review.md` and the walk file `tests/ux/<slug>.walk.json`. Commit the walk file with the work.
3. **Fix.** When the verdict is `CHANGES_REQUIRED`, fix each P0 and P1 finding with the owner `dev`. Do not change an approved acceptance test. Run the walk again with `uxcheck.py review`. There is one fix pass, and no second review.
4. **Questions.** A finding with the owner `design` or `po` is a product question. Read `po-questions.md`.
5. **Report.** In the report of the gate Done, give the verdict and the count of findings for each severity. Also list the open findings.

When the agent returns `NEEDS_HUMAN` (no Playwright, no run command, or the app does not start), give its message to the human. Do not skip the review without a word.
