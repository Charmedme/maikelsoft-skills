# Retrospective: the review flow work

Date: 2026-10-31. This document looks back at the work that led to `design.md`, from the first source files to the two refinement cycles. The author is the Claude session that did the work, so the view of the owner can differ.

## Summary

- The result is a design, an eval plan, build notes, a comparison, this retrospective, and the new `dev-team`. Outside this folder, the owner approved one setting. The test runs also wrote 3 rows to the real profile of the owner, which was not approved: see "The first comparison".
- The first two cycles changed the design a lot. The independent reviews gave 27 findings: 15 in cycle 1 and 12 in cycle 2. Rounds 3 and 4 gave 50 more.
- The owner found three gaps that the session had missed: the nit cap, the size of the evals, and the order "raise first, then warn".
- The largest mistake of the session was late use of the primary source.

## Timeline

| Step | What happened | Result |
|------|---------------|--------|
| 1. Sources | The session read the transcript, the Dutch file, and the playbook, and it compared them with the plugins | 10 gaps, a list of ideas not to adopt, and the finding that 16 skills showed no description |
| 2. Agreement | The owner agreed and added the evals | The session started the `build-skill` method |
| 3. Brief | The session asked 4 questions at gate G1 | The owner answered 3 of them, and asked for research on the fourth |
| 4. Eval research | The session read the guide, other benchmarks, and the docs. It also found the Anthropic post | The set grew from 5 cases to 20 |
| 5. Nits | The owner asked how nits get solved, and pointed to the transcript | Gap 11, the nit rules |
| 6. Cycle 1 | Understand, research, reason, improve, review | Draft v1 and 15 findings |
| 7. Cycle 2 | The same steps, with the findings as input | The design document and 12 findings |
| 8. Final | The session applied the findings and wrote these two files | `design.md` and this file |

## Cycle 1

| Stage | What the session did | What it gave |
|-------|----------------------|--------------|
| Understand | Read `run-files.md`, `runcheck.py`, the templates, the security report, and the language rules | Follow-up numbers also cover changes that the human asks for. So a simple count of follow-ups is wrong. |
| Research | Read the post in full, the sync script, `stackcheck.py`, three books of the library, the Google standard, and the evals article | The post asks for the fields Testing, Risk, and How to revert. Two books gave the rule on stop conditions and the rule that few trials give only bands. |
| Reason | Walked the ideas through the current files | A raised run needs no `reflection.md`. `REVIEW.md` must come from Base. A nit can hide as a `suggestion`. |
| Improve | Wrote draft v1 in the scratchpad | 13 parts, with one open question |
| Review | One independent agent read the draft and the code | 15 findings, and an answer to the open question |

The most important findings of this review:

- `old` hid real bugs in a follow-up, because the session measured it against the fix diff.
- Scoring by file and line cannot prove a catch. The repository already had better tools for this in `evals/dev-stack/`.
- A planned `RUN-NITS` finding had no remedy at G2.
- The G2 warning fired on each raised run.

## Cycle 2

| Stage | What the session did | What it gave |
|-------|----------------------|--------------|
| Understand | Checked the claims of the review against the files, and did not trust them | The main claims were correct |
| Research | Read the eval tools, the docs on `REVIEW.md`, and the severity levels | The Pre-existing level, the base-branch rule, and the fact that local `/code-review` does not read `REVIEW.md` |
| Reason | Decided each of the 15 findings | The session cut the rounds counter and `RUN-NITS`, and it moved test edits to a report |
| Improve | Wrote the design in the language rules | `design.md`, first version |
| Review | A second, separate agent tested the document, also with git experiments in a small repository | 12 findings |

The most important findings of this review:

- The definition of a catch gave the old version a score of 0. So the eval could not compare the two versions.
- The post says that the 59% was code "there since the reviewer's first pass". This is not the same as code older than the pull request. So Origin alone did not solve the problem of Part A.
- `RUN-ORIGIN` gave a false finding, and it missed its own case, when a fix moved lines.
- Older problems did not reach the human, although the design said that the human decides.
- "Move to medium" made `workcheck.py` fail in `dev-flow`.
- The marker fix did not solve the `RUN-EVIDENCE` problem that it named.

The session accepted the findings, with two changes of its own. A raise adds a warning and does not start a senior to update `design.md`. A finding outside the fix diff stays visible as an open finding, which keeps the fix of the first review.

## What changed from the first list to the final design

| First proposal | Final design | Reason |
|----------------|--------------|--------|
| Enforce the round cap in `runcheck.py` | A text rule, with the count after the step Sort | A human is in each round, and the counter jammed |
| Count nits with `runcheck.py` | The evals measure the nits | A finding at G2 has no remedy |
| Only code in the diff can block | Three Origin values, and a separate scope for a follow-up | The simple rule hid real bugs |
| A checker for wrong Origin tags | No checker. The eval measures tags | A test showed false findings |
| Raise the tier in `dev-flow` | Keep the tier and add the review | A raise made `workcheck.py` fail |
| A warning for each new high path | Compare the reasons of two runs from Base | The warning fired on each fix in an auth folder |
| 5 cases, scored by a script | 20 cases, with blind graders and tools that exist | 5 cases cannot show a change, and a location proves nothing |
| A security review on each high fix | A raise only when no review has run yet | The human reads each round |
| A hook that only warns | Wait for data | No evidence for the need. This one needs the decision of the owner. |
| One large release | 5 pull requests, in order of dependence | The block Warnings must come before the rules that use it |

The final design has fewer checker rules than the first proposal. The reviews removed more than they added.

## What went well

- **Independent review.** Each review had no access to the reasoning of the session, and it tested the claims against the code. The findings were concrete, with a file and a line.
- **Experiments.** The second review ran real git commands. Reading alone did not show the false finding of `RUN-ORIGIN` or the wrong hash from a subfolder.
- **Checks of the review.** The session checked the main claims of the review before it used them.
- **Questions of the owner.** Each question of the owner changed the design: the nits, the evals, and the order of raise and warn.
- **Honest limits.** The design says what it cannot measure. It also says that no independent reviewer has read the final text.

## What went wrong

These are mistakes of the session.

1. **Late use of the primary source.** The session said that the Anthropic post was not in the folder, and it went on with a summary. It found the post only during the eval research. A search at the start was cheap, and the post has the adoption list, the clean cases, and the metrics.
2. **A number without research.** The session proposed 5 cases and gave a cost range without a source. Each source that it read later gave a larger number. The owner had to ask.
3. **A missed rule in the first source.** The transcript says to cap nits. The first list did not have the cap for a project without `REVIEW.md`. The owner had to point to it.
4. **Claims about enforcement that did not hold.** The session told the owner that `runcheck.py` can enforce the round cap and count the nits. It said that the rule "does not depend on the agent". Both claims fell in the reviews. The session had not traced the flow at G2.
5. **A wrong claim about scoring.** The session said that matching needs no judge model. The repository already had a grader rule against this. A finding at the correct line with the wrong reason does not count. The session had not read `evals/dev-stack/` at that point.
6. **Fix by addition.** Draft v1 answered most problems with a new rule or a new checker. The reviews showed that many of these added cost and no protection.
7. **A question that mixed with a gate.** The `build-skill` method needs a clear yes at G1. The session asked for G1 three times, and the owner still has not given it, because new questions came first. This is correct under the method, but the session could have split the two earlier.

One choice is not clear. The session read "the evals from point 2" as the evals that the voting idea depends on. The owner did not correct this, but the owner also did not confirm it.

## Rounds 3 and 4

The owner answered the four decisions on 2026-10-31, and asked for two more rounds before the build. The owner also asked to see the plan first when the findings are not minor. They were not minor, so no build has started.

| Round | New research | Review | Findings |
|-------|--------------|--------|----------|
| 3 | The current code against the owner's answers, a git test of merges and rebases | An independent review with experiments | 3 major, 13 minor |
| 4 | A simulation of the eval noise, and the risk rule over the real history of the owner's projects | A build rehearsal in a copy, with 339 passing tests | 7 major, 27 minor |

Earlier rounds read the design. The rehearsal built it, and it found more. The most important results:

- The compare point gave a false `normal` when HEAD is in the default branch. It looked correct in each earlier test.
- `RUN-EVIDENCE` reads only 33 of 138 real check cells. The earlier fix looked like cover and gave none.
- The session wrote decision 8 to remove noise, but the rule moved the noise into the block Warnings.
- The eval rule "must not fall" had the fault that the session fixed for lost cases in the same round.
- Option c of the round cap could not start, because other rules restarted the old run.
- The rule list of `riskcheck.py` lost 4 rules, because real data showed no effect.

**A mistake of the session.** The session counted reverts in project A with a search that also read commit bodies, and it put 17 and 381 in the design. The subjects give about 4 and 375.

**The trend.** Four rounds gave 15, 12, 16, and 34 findings. The number did not fall, because each round used a new method. The major findings moved from the flow rules to measurement and to detail at the edges. A fifth round on paper has a low value. The first real run is the next good test.

## The first comparison

The session built the version and tested it, and it recommended a push. An independent review then said not to ship it as it was, and the session withdrew the recommendation. The owner cut the release.

Mistakes of the session:

1. **A test that changed real data.** The runner set a path for the profile and did not make the file. The seniors used the home path, and 3 test answers went into the real profile. The report said that user settings did not reach the runs. The session did not check this claim.
2. **A rule in the wrong file.** The tighter fix rule went into `senior-practice`, but the lead writes the fix prompt. The session had the prompt in its run files and did not read it.
3. **A cause without evidence.** The report gave the security review as the cause of the higher cost. The time data did not support that.
4. **A test written by the author of the rules.** The fixtures gave the classes away, and the bugs were easy. So the test showed that rules fire, not that they help.
5. **Late use of the premise.** The review work did not start from the earlier decision that executable specifications are the main control (`docs/work/flow-scale/executable-specs-concept.md`). A review catches what a locked test does not. The session treated the review as the main control.

## Lessons for the skills

These lessons can go into the plugins. The plugins do not have them yet.

| Lesson | Where it can go |
|--------|-----------------|
| When a source is a summary, look for the primary source before the first proposal | `dev-build` research step, `CONTRIBUTING.md` |
| Search for existing tools in the repository before you propose a new one | `senior-practice`, design step (the sibling rule covers code only) |
| A new rule must name when it fires and what the remedy is at that point | `build-skill` writing rules |
| An eval must give a score that each version can reach | `build-skill` testing reference |
| Trace a new field through each reader of that field | `senior-practice`, self-check |
| Prefer to cut a rule when the same protection exists | `build-skill` writing rules |
| A review with experiments finds more than a review by reading | `build-skill` and `build-agent` review mode |
| A build rehearsal in a copy finds what a review of a design does not find | `CONTRIBUTING.md`, the concept step |
| Run a new rule over real history before you trust it, when the data exists | `build-skill` testing reference |

## Cost

- Two independent agents used about 155,000 and 179,000 tokens. No eval run took place, so the plan spent no eval budget.
- Rounds 3 and 4 used about 209,000 and 266,000 tokens of agents.
- The owner asked for two cycles, and each later round still found serious problems. So the final text still carries some risk.

## State at the end

| Item | State |
|------|-------|
| `design.md`, `evals.md`, `build-notes.md`, `retrospective.md` | Written on the branch `docs/review-flow`, in the worktree `maikelsoft-skills-review-flow`. Not committed. |
| `skillListingBudgetFraction` | Set to `0.02`. Effect not confirmed. |
| Source folder `docs/drive-download-20261030T142542Z-1-001/` | Not changed. Not tracked in git. Do not commit it, because it holds third-party material. |
| Skills, agents, checkers | Not changed |
| Gate G1 of the build | The owner said yes to each decision on 2026-10-31. |
| `dev-team` 0.6.0 | Built on the branch `feat/review-flow-dev-team`, then cut. `dev-stack` stays at 1.2.2. Not pushed. See `comparison.md`. |
| The `dev-flow` parts and Part J | Not built. |
