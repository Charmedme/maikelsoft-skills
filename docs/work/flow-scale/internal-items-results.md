# Results: real items of an internal project (2026-10-24)

Case: 4 approved items of an internal project: three optional rule variants and a time limit. Start point: the commit before the real build. Hidden probe: the real test file of that build (10 tests: 0 of 10 on the start point, 10 of 10 on the real build). The item has a short technical contract with the function names that the tests call.

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
| Required example mapping (`examples.md`) | Sonnet | 7, 8, 7 | $1.43 to $1.71 | 7 to 8 min | each run wrote `examples.md` |
| The same, with the word rule | Sonnet | 8, 8, 8 | $0.96 to $1.62 | 4 to 7 min | |

- Each arm that built code failed the same 2 hidden tests:
  - The time limit: the real build goes on at exactly 2:00. Each run went on only after 2:00.
  - The swap rule: the real build puts the old entries back on the source list. The runs put them on the archive list. The item says "back to the list": it does not say which list.
- Both gaps are product questions. No arm asked them.
- The stops of the first flows asked about the pause during a time limit. An earlier product decision answered that before the build.
- The large path to dev-team took 25 to 57 minutes and up to $14.74 for the same result. In one session, the new flow takes 4 to 6 minutes and $1.27 to $1.50: about 2 times the baseline on Sonnet.

## What did not work

- The BA step did not run. Each run read `example-mapping.md`, but no run wrote the rules and examples, and no run loaded the `example-mapping` skill. The rule "an item without examples needs example mapping" was in the intake reference only, as one step of many.
- The mutation check ran in each run, but it finds a rule that no test checks. It does not find a rule that a test checks with the wrong value. The runs had tests for the deadline, so the mutation died.
- One run of 3 cited that decision.

## Next fix

Make example mapping a step of its own, with a file that the gate Spec shows: `examples.md`. It holds the rules, and an example below, at, and above each limit. It lists each word with more than one meaning (for example "the list"). It holds the answers from decisions with their IDs, and the open questions. A checker confirms that each criterion has examples and that each limit has an example at the limit.

## Run 2: required example mapping

Each of the 3 runs wrote `examples.md` and ran `mutate.py`. `speclock.py check` was clean in each run.

| Gap | Before | With `examples.md` |
|-----|--------|--------------------|
| The round goes on at exactly 2:00 | 0 of 3 runs | 3 of 3 runs (`now >= deadline`). One run wrote the rows below, at, and above |
| "back to the list" (source list or archive list) | 0 of 3 asked | 1 of 3 listed the word with two readings, then answered it as "technical" (wrong list) |
| The decision on the pause cited | 1 of 3 | 0 of 3 |

The hidden score went down (7, 8, 7), for reasons outside the two gaps:

- Two runs set the deadline only when `now` is truthy (`now ? now + t : 0`). The hidden tests start at `now = 0`, so the round had no deadline. This is a real defect: 0 is a valid time. No example of the run used time 0.
- One run returned `false` when the deadline moved the round to the reading phase. The hidden test expects `true`. The contract of the case says "true when the next round starts", so the run followed the case text. This is a fault of the case, not of the run.

### Fix

The answer sheet said "take the recommendation of the agent for each other technical choice". The run used that line to answer a word of a criterion. Now:

- `example-mapping.md`: a word with more than one meaning in a criterion is a product question. A technical or assumed answer is not allowed. A word of the technical contract is marked `(contract)` and can have a technical answer.
- `examplecheck.py` gives an error for a technical or assumed answer to a word of a criterion. On the 3 files of this run, it finds the "list" answer and two other product words.

## Run 3: the word rule

| Gap | Run 2 | Run 3 |
|-----|-------|-------|
| The round goes on at exactly 2:00 (code) | 3 of 3 | 3 of 3, each run with the rows below, at, and above |
| "the list" asked to the PO | 0 of 3 | 0 of 3 |
| Deadline at `now = 0` | 1 of 3 correct | 3 of 3 correct |

- Each run failed the same 2 hidden tests:
  - The swap rule (the list). One run listed "the list" with two readings. It then answered with a related decision. That decision does not answer which list gets the old entries: the run made an inference and called it a decision. The two other runs wrote "archive list" with the source `item`. The item says only "the list".
  - The time limit test fails on the return value of `advance` at the deadline, in each run. The cause is the contract text of the case (refer to run 2). The boundary itself is correct in each run. With a correct contract, the expected score is 9 of 10. This is not measured.
- One run had an open question (P1) and built anyway, with its recommended answer. No example row had the source `open: P1`, so `examplecheck.py` gave no WAIT line.

## Conclusions

1. Example mapping with the rows below, at, and above closes the boundary gap. No earlier run had it. Each of the 6 runs with `examples.md` has it.
2. Text rules do not make Sonnet ask about "the list". The model finds a decision that seems close and treats it as the answer. Or it does not see two readings. A check cannot see if a cited decision really answers the question. Only a human at the gate Spec sees it, because `examples.md` shows the answer and its source on one line.
3. The cost on Sonnet is $0.96 to $1.71 for each run: about 2 times the baseline ($0.62 to $0.71). The baseline gets 8 of 10 with the wrong boundary.
4. Open: an open question without an `open:` row is not a WAIT for the checker. A cited decision must hold the words of the answer, not a related fact.
