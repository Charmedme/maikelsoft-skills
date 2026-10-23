# Results: tiers and one product front end (2026-10-23)

Test of the draft dev-flow 2.0.0 (`plan-and-build` from the approved item, with tiers) and the change to `dev-team:deliver-story`. Eval set of dev-team on the orders-api fixture, model Opus, headless, with an answer sheet. Arm `flow` loads dev-flow, dev-team, and dev-stack. New case: bug ORD-45 (list with an empty customer id), a patch.

## Rounds

| Round | Skill change | Runs |
|-------|--------------|------|
| 1 | Draft | 24: 6 cases with flow, plus the bug with team and baseline |
| 2 | Narrower contract risk. No assumed package, service, or fact of the human | 9: bug, get-order, order-mail |
| 3 | No assumed gate Plan while a question waits. Team decisions through `team-decisions` (in the medium reference only) | 9: order-mail, get-order, cancel-order |
| 4 | The team decision rule moved to the rules of each tier | 6: get-order, cancel-order |

## Final result for each case

The last round of each case. Cost and time are the median of 3 runs. Team and baseline for the five old stories come from dev-team rounds 1 and 2, and the light mode check (2026-10-18).

| Case | Flow checks | Flow tier | Flow cost | Team cost | Baseline cost |
|------|-------------|-----------|-----------|-----------|---------------|
| Bug ORD-45 (round 2) | 9/9 x3 | small x3 | $0.84 | $3.40 | $0.36 |
| get-order (round 4) | 9/9 x3 | small x3 | $1.28 | $4.14 | $0.40 |
| cancel-order (round 4) | 9/9 x3 | small x3 | $1.54 | $8.01 | $0.57 |
| cancel-shop-day (round 1) | 9/9, 8/9, 8/9 (D8) | medium x3 | $2.89 | $11.11 | $0.77 |
| ship-history (round 1) | 7/9 x3 (D8, D11) | medium x3 | $3.45 | $9.72 | $1.12 |
| order-mail, trap (round 3) | 3/3 x3: stops before the build | medium x3 | $1.18 | $3.12 | built the feature |

- Each delivered flow run in all rounds passes the build, the tests, the package policy, and each hidden probe. That includes the DST probes of cancel-shop-day.
- The flow costs 3 to 5 times less than the team on each case. It costs 2 to 3 times the baseline.
- The trap: the flow stops in 3 of 3 runs, as the team does. It drafts the comment for the PO and does not post it.

## Verdict for each change

| Change | Test | Observed | Verdict |
|--------|------|----------|---------|
| Narrower contract risk | Bug and get-order, round 1 against round 2 | Tier right in 1 of 6, then 6 of 6. Bug cost $1.21 to $0.84 | helped |
| No assumed package, service, or fact | order-mail, round 1 against round 2 | Asks for MailKit, SMTP, and the mail address in 3 of 3. Still builds the parts that do not depend on them | helped, not enough |
| No assumed gate Plan while a question waits | order-mail, round 2 against round 3 | Code built in 3 of 3, then 0 of 3. Cost $4.31 to $1.18 | helped |
| Team decision rule in the medium reference | get-order and cancel-order, round 3 | D8 failed in 4 of 6. The small tier does not read that file | no observed effect |
| Team decision rule for each tier | get-order and cancel-order, round 4 | D8 passed in 6 of 6, with 2 ADRs in each run | helped |

## Faults of the eval found

- The tier check first read the whole stream, also the example line in `tiers.md`. Fixed: it reads only the plan and the text of the agents.
- One test failure (get-order round 3, D3) came from a Docker restart during the run. The rescore passed 10 of 10.
- Expected tiers changed. cancel-order accepts small or medium: the cancel rule is in the domain already. ship-history accepts medium or large: one session passed all probes.

## Gaps

- cancel-shop-day and ship-history did not run again after round 1. Their D8 failure has the same cause as get-order, but that is not tested.
- No large item ran through `dev-team:deliver-story` from the flow. No case of the set reaches the large tier now.
- The judge assertions (J) are not graded in these rounds.
- The product question path ran only unattended: no comment was posted to a real tracker.
- 3 runs per case. The trap result (0 of 3 against 3 of 3) gives p = 0.25 with a two-sided sign test.
