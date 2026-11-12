# Final gate

The human gets one report and decides. Open findings stop the gate.

## Steps

1. **Checks.** Run `workcheck.py --final docs/work/<item>` and `speclock.py check`. Both must show 0 errors. Run `runheader.py write` with the step `final gate`.
2. **Open findings.** Read `review.md`. A blocking finding, or a security finding of Critical or High, that is still open stops the gate. Give the options of `cycle.md`. Do not give the report as done.
3. **Report.** Use the template below. Keep it below 250 words. Copy each number from the output of a command.
4. **Learn from a miss.** A miss is a finding that a review, a held-back test, or a mutation found after the scenario loop. It is also a cap that the run reached, or a correction of the human. Give one proposal for the most important miss. It is a line in `CLAUDE.md`, a change of a skill, or a new check. Give the exact text. Do not make the change: the human decides.
5. **Next.** End with the `Next:` line of `context.md`. Usually: `Next: open the pull request, then /review-comments when it has comments.`

## Template of the report

~~~markdown
**<item key>: <title>** (<size>)

**Testing**
- Locked tests: <n> files, <n> tests, all pass. `speclock.py check`: 0 changes.
- Full suite: <passed>/<total>. Failures before the change: <n>.
- Held-back tests: <passed>/<total>, or "not for this size".
- Mutation check: <n> survived, each with its answer or question.

**Risk**
- <The risks of tiers.md or riskcheck.py, and the result of the security review.>
- <Open findings of level issue, at most 3 lines. The rest is in review.md.>

**How to undo**
- <The revert of the commits, and each step that a revert does not undo: a migration, a setting, data.>

**Commits**
- `<hash>` <message>

**Proposal** (only after a miss)
- <One change, with its exact text.>

Next: <command>
~~~
