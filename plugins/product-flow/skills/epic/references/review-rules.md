# Review rules

These rules apply to each review in the flows. They apply to the plan review, the step review, the code review, and the security review. They also apply to the UX review and to the comments on a pull request.

## The reviewer

1. **A new agent for each review.** The reviewer did not write the work, and it did not share the context of the builder. Start a new sub-agent.
2. **Give it the documents, not the conversation.** Give `spec.md`, `plan.md`, and the diff from the base commit. Also give read access to the repository and the review file of earlier rounds (`review.md` in the work folder).
3. **Give it the history.** The earlier findings with their status: open, fixed, or rejected with the reason. The reviewer:
   - does not post a closed finding again (fixed or rejected), unless it has a new fact,
   - checks that each fixed finding did not come back,
   - looks for the same class of mistake in other places, and lists each place.
4. **Read `REVIEW.md` from the base.** Use `git show "<base>:REVIEW.md"`. Then the change cannot relax its own review. When the file does not exist, use these rules alone.

## Each finding

- **Evidence.** A file and a line. A claim about behaviour cites the code or a test, not a name. Before the finding goes in the report, try to prove it wrong.
- **Level.** One of these:

  | Level | Meaning | Action |
  |-------|---------|--------|
  | `blocking` | A bug, a broken rule of the spec, a security finding of Critical or High, or a rule of `REVIEW.md` under "What must be fixed". | A fix pass before the person gets the work. |
  | `issue` | A real problem that does not block: a risk, a missing test, a weak name with an effect on the reader. | In the report, for the person. |
  | `nit` | Polish without an effect on behaviour. | In the report, at most 5 in full. No fix pass by itself. |

- **Older code.** A finding on a line that the change does not touch or make reachable is `older`. It goes in a section "Older problems". Nobody fixes it in this item.
- A skip rule in `REVIEW.md` never hides a security finding of Critical or High.

## Comments on a pull request

Sort each comment into one class. Both flows use the same classes.

| Class | Meaning | Action |
|-------|---------|--------|
| `fix` | The comment is right. The code must change. | A fix, with a failing test first. |
| `prove` | The comment names a failure that nobody showed. | A test that fails for it. When the test passes, the class becomes `reject`, with the test as evidence. |
| `question` | The reviewer asks why. The code stays. | A reply with the reason and its source. |
| `decided` | A decision, the spec, or the design settled it. | A reply with the decision or the scenario. |
| `product` | Scope, value, or visible behaviour. | A question to the product owner. |
| `reject` | The comment is wrong. | A reply with evidence: a test, a line, or a source. |
| `nit` | The author marks it as a nit, or it has no effect on behaviour. | A short reply. A fix row that changes the same method can take it. |
| `older` | A line that this change does not affect. | A reply, and a row in "Older problems". No fix. |

A comment with the same cause as a `reject` or `decided` row of an earlier round gets a reply that cites that row. A comment with a new fact opens the row again.

## Rounds

The cap of each review is in `cycle.md`. Stop before the cap when one of these is true, and ask the person:

- a new finding has the same cause as a finding that a fix closed before,
- an earlier fix of this item broke the behaviour.

## The review file

Write each finding in `review.md` of the work folder, in a table. The columns are Round, Location, Level, Finding, and Status. The file also holds the base commit, so a restart and a later review can read it. The report to the person gives the counts and at most 3 lines.

Done when each finding has a level and a status, and `review.md` has the round.
