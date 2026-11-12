# Plan (medium and large)

`plan.md` holds the design choices of this item, the files, and the order of the scenarios. The ticket and `spec.md` hold the behaviour: do not copy the rules into the plan. Use `templates/plan.md`.

```
docs/work/<item>/
  run.md                  the header and the log of the run
  ticket.txt              the raw text of the ticket, for specsync.py
  spec.md                 the spec block, the design rules, and the checks
  spec-baseline.json      the fingerprint of each rule
  plan.md                 the design choices, the files, and the scenarios
  research.md
  decisions.md            technical decisions only
  review.md               the findings of each review, with their round and status
```

## Steps

1. **Research.** Read `research.md` and do its steps. Done when each question has an answer with a source, or a mark.
2. **Technical questions.** Ask the questions that the research did not settle, in one message. One decision in each question, with your recommended answer and the reason in one sentence. Number them Q1, Q2, so the human can answer "Q3: yes". Write each answer in `decisions.md` (`templates/decisions.md`). A product question goes to the PO (`po-questions.md`), not into this list.
3. **Write the plan.** The section "## Scenarios" lists the rules in the order of the build, each with its test file. Put a rule that other rules need first. Done when `workcheck.py` shows 0 errors.
4. **Plan review.** Start a new sub-agent as the plan reviewer (`review-rules.md`). Give it `spec.md`, `plan.md`, `decisions.md`, and read access to the repository. It checks three things. Does each part of `spec.md` have a check? Does the plan fit the system? Is a risk of `tiers.md` missing? Fix each blocking finding, and start a new reviewer for the next round. Medium: 1 round. Large: at most 3 rounds (`cycle.md`). When the held-back tests apply, the reviewer of the last round also writes them (`heldback.md`). Done when a round finds no new blocking finding.
5. **Plan gate.** Show the size, the design choices, the list of scenarios with their test files, and the open findings. Ask: "Do you approve the plan and the list of scenarios?" A change to the Behaviour spec goes back to the PO. Done when the human said yes.

## Spike

Make a spike only for a technical risk: "Does this library, query, API, or data model work as we need?" A spike is throwaway code in `<work folder>/spike/` that answers that one question. Write the answer and the reason in `decisions.md`. Remove `spike/` from the branch before the final gate, in a commit of its own.
