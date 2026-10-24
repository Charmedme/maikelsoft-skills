# Concept: a BA in the team, small steps, and spec holes (2026-10-24)

Input: the summary of the video of Dave Farley that the owner gave (`AI_Code_Summary.md`), and the first zitten-kloten runs (issues #36 to #39).

## What the summary adds to our spec-first flow

| Point of Farley | Our flow now | Gap |
|-----------------|--------------|-----|
| Write one scenario, watch it fail, then let the agent pass it | All test cases first, one gate, then build all | No small steps. The run builds all scenarios in one go |
| Red, green, review, commit for each scenario | One commit for each ticket or item | Errors show late |
| Keep the spec out of the reach of the agent | Hook and hash lock | Covered |
| Mutation testing finds holes in the specification | Optional report, not used | Not used. Our runs show the hole that it finds (below) |
| Reading code becomes a choice from the risk | Review sub-agent on medium items | Covered |

## What the zitten-kloten runs show

1. **Each arm missed the same boundary.** The real test expects the round to go on at exactly 2:00. Each built run went on only after 2:00. Neither the issue nor the contract says which. A mutation of `>=` to `>` in the deadline check survives each acceptance test that the agents wrote. A mutation check finds that hole, and the hole is a question for the PO.
2. **The flows asked the PO a question that the PO had answered.** Three flow runs stopped to ask what happens to the time limit during a pause. Decision Q44 in the backlog context file answers it: "Bij een pauze stopt de lopende ronde. De ronde gaat daarna verder waar hij stopte." The decision existed before the build. The flows did not search the decision log, because the issue does not link to it.
3. **The tests that the agent writes from its own examples do not find gaps.** The spec-first flow wrote its own examples, and they did not cover the boundary. Farley says the same: tests that the agent writes confirm what the agent thinks, not what the human means.

## Proposal

### A. A business analyst (BA) role: example mapping

Example mapping is the "three amigos" step of BDD. The PO, a developer, and a tester turn an item into rules, examples, and questions before the build. A BA skill does the preparation:

1. List the rules of each criterion.
2. Give each rule examples, with a value on each side of each limit (the exact boundary included).
3. Search the existing product decisions first. Look in the backlog context file of product-flow (its "Decisions" section), the linked items, and the comments. A question that a decision answers gets the answer, with the decision ID.
4. Only open questions go to the PO, with a recommended answer each.

The skill lives in product-flow (`example-mapping`), because it is product work. dev-flow starts it at intake when the item has no examples, or too few.

### B. The PO stays a human

An AI PO that decides product questions brings back the double front end: two sources of product decisions. It would also invent answers that only the business can give. The BA recommends. The human PO decides. One exception for solo work: when the owner is the PO, an answer that cites an existing decision (for example Q44) counts as approved. A new decision still needs the human.

### C. Small steps

After the gate Spec, build one scenario at a time: make it red, make it green, review the diff of that step, commit. The next scenario starts only after the commit.

### D. A boundary mutation check

A script (`mutate.py`) changes each comparison in the changed lines, one at a time (`>` to `>=`, `<` to `<=`, `==` to `!=`, and back). It runs the acceptance tests for each change and puts the file back. A mutation that survives shows a rule without an example. The flow turns each survivor into an example question for the BA, or a question for the PO. It uses no package and works for each language with a test command. When Stryker is installed, the flow can use it.

## How we measure it

Rerun zitten-kloten #36 to #39 with the new flow, on Sonnet (one third of the cost of Opus, the same result so far), 3 runs:

- The hidden tests (10).
- The known gaps that the run finds: the deadline boundary, the pause (answered by Q44), and where the old cards of the Kaartruil go.
- The stops that it avoids because a decision answers the question.
- The cost and the time, with the stop rules of the run script.

## Decisions of the human (2026-10-24)

| No | Decision |
|----|----------|
| B1 | The owner gave no preference for the roles. Assumption: a BA skill (example mapping) in product-flow, and the PO stays a human. |
| B2 | In solo work, an answer that cites an existing decision counts as approved. |
| B3 | Build one scenario at a time, the boundary mutation check, and Stryker when the project has it. |
