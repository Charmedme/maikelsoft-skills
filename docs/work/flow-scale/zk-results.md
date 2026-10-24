# Results: real items of zitten-kloten (2026-10-24)

Case: issues #36 to #39 of zitten-kloten: the house rules Kaartruil, Dubbele Inzet, Botte Schande, and a time limit. The PO approved them before commit `2e11a51` built them. Start point: `8916d85`, off the mesh. Hidden probe: the real `tests/houserules.test.js` of that build (10 tests: 0 of 10 on the start point, 10 of 10 on the real build). The item has a short technical contract with the function names that the tests call.

## Result

| Arm | Model | Hidden tests | Cost | Time | Note |
|-----|-------|--------------|------|------|------|
| Baseline | Opus | 8, 9, 9 | $1.72 to $2.43 | 4 to 7 min | |
| dev-flow 2.0 | Opus | stop, 8, 8 | $6.16, $14.33, $13.93 | 3 to 57 min | large, to dev-team |
| Spec-first (first version) | Opus | stop, 8, stop | $7.98, $14.74, $8.01 | 27 to 47 min | large, to dev-team |
| Baseline | Sonnet | 8, 8, 8 | $0.62 to $0.71 | 2 min | |
| dev-flow 2.0 | Sonnet | 8, 7, 8 | $1.07 to $1.23 | 3 to 4 min | |
| Spec-first (first version) | Sonnet | 8, 8, 8 | not measured | stopped at 25 min | large, to dev-team |
| Spec-first with BA step, one session | Sonnet | 8, 8, 8 | $1.27 to $1.50 | 4 to 6 min | |

- Each arm that built code failed the same 2 hidden tests:
  - The time limit: the real build goes on at exactly 2:00. Each run went on only after 2:00.
  - The Kaartruil: the real build puts the old cards back on the draw pile. The runs put them on the discard pile. The item says "terug naar de stapel": it does not say which pile.
- Both gaps are product questions. No arm asked them.
- The stops of the first flows asked about the pause during a time limit. Decision Q44 answered that before the build.
- The large path to dev-team took 25 to 57 minutes and up to $14.74 for the same result. In one session, the new flow takes 4 to 6 minutes and $1.27 to $1.50: about 2 times the baseline on Sonnet.

## What did not work

- The BA step did not run. Each run read `example-mapping.md`, but no run wrote the rules and examples, and no run loaded the `example-mapping` skill. The rule "an item without examples needs example mapping" was in the intake reference only, as one step of many.
- The mutation check ran in each run, but it finds a rule that no test checks. It does not find a rule that a test checks with the wrong value. The runs had tests for the deadline, so the mutation died.
- One run of 3 cited Q44.

## Next fix

Make example mapping a step of its own, with a file that the gate Spec shows: `examples.md`. It holds the rules, and an example below, at, and above each limit. It lists each word with more than one meaning (for example "de stapel"). It holds the answers from decisions with their IDs, and the open questions. A checker confirms that each criterion has examples and that each limit has an example at the limit.
