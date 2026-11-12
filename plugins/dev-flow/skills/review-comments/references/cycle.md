# The cycle

Each phase of the work runs the same cycle: research, design, write, test, review, improve. One pass of the cycle is a round. Each phase has a cap on its rounds.

## Rules

1. **End early.** A round ends the cycle when its review finds no new blocking finding. Do not start a round to polish.
2. **Stop at the cap.** At the cap, do not start a new round. Step back and ask the person (see "At the cap").
3. **A new reviewer for each review.** Use the review rules in `review-rules.md`.
4. **Count each round.** Write the count in the work file of the phase, so a restart knows it.

## Caps

| Phase | Command | Test and review in each round | Cap | Exit |
|-------|---------|-------------------------------|-----|------|
| Idea and intent | `/to-backlog` | A new reviewer asks: is this the real problem, and what is missing? | 3 rounds | The product owner accepts `intent.md`. |
| Item and behaviour spec | `/to-backlog` and the item commands | The refine pass: a new reviewer checks INVEST and the scenarios. Open questions go to the product owner. | 3 rounds | The product owner approves: the spec is ready. |
| Spec and plan | `/build` | `specsync.py check`, then a new plan reviewer: does each part have a check, and does the plan fit the system? | 3 rounds | Plan gate: the person approves the plan and the list of scenarios. |
| Each scenario | `/build` | Red for the right reason, green. | 3 tries to get green | Commit. At the cap, stop and ask. |
| Check against the spec | `/build` | Full test suite, held-back tests, `speclock.py check`, `mutate.py`. | 2 fix rounds | All green, or the gap goes to the person. |
| Code review | `/build` | A new reviewer with the history of the findings. | Set by size (table below) | No open blocking finding. |
| Security review | `/build` | A separate agent with the secure-coding rules. | 2 rounds | No open finding of Critical or High. |
| Review comments | `/review-comments` | Sort each comment, fix the blocking ones, check the fix diff again. | Small 1, medium 2, large 3 | No open blocking comment. |

## Size

The size sets the rounds and the staffing. The risk sets the spec check and the security review.

| Step | Patch | Small | Medium | Large | Team |
|------|-------|-------|--------|-------|------|
| Full review passes | 1 (quick) | 1 | 1 | 2 | 2 |
| Round cap, then step back | 1 | 2 | 3 | 3 | 3 |
| Spec | One failing test | Locked scenarios | Locked scenarios | Locked scenarios | Locked scenarios |
| Test layers | Plain test | Plain tests in domain words | Four layers | Four layers | Four layers |
| Held-back tests | No | No | When a risk applies | Yes | Yes |
| Security review | When the risk check raises it | When the risk check raises it | In the code review | Pass 2 | Own agent |

## At the cap

Stop the phase. Give the person:

1. The rounds so far, with the findings of each round and their status.
2. What is still open, and why the rounds did not close it.
3. These options, with your recommendation:
   - a. One more round, with the change that makes it different from the last round.
   - b. Accept the open points. Write each one in the report as an open finding.
   - c. Stop, and send the open points back to the earlier phase (for example a spec question to the product owner).

Wait for the answer. Write the answer in the work file. A restart reads it and does not ask again.

Done when the phase has its exit, or the person chose an option at the cap.
