# Writing rules for skills

Each rule has an ID. A review cites the ID.

## Contents

- Structure
- Words
- Control
- Pruning

## Structure

- **W1 Short body.** `SKILL.md` has the steps and the pointers. Detail goes to `references/`.
- **W2 One level.** `SKILL.md` links to each reference. A reference does not link to another reference.
- **W3 Done when.** Each step ends with a test that the agent can check. Example: "Done when the checker shows 0 errors."
- **W4 Pointer with a reason.** A link says when to read the file. Example: "Read `testing.md` before you run a test."
- **W5 Keep together.** Put a fact next to the step that uses it. Do not make the agent look in two places.
- **W6 Order by need.** Put what the agent needs first at the top. Put rare cases at the end.

## Words

- **W7 Positive.** Say what to do. "Use a comma" works better than "Do not use a dash".
- **W8 Reason.** Give the reason for a rule when it is not obvious. A reason lets the agent handle a case you did not list.
- **W9 No shouting.** Do not use MUST, NEVER, ALWAYS in capitals. Plain words with a reason work better.
- **W10 One name.** Use one word for one thing in the whole skill.
- **W11 Concrete.** Give one example instead of an abstract description. Show good output, not only the rule.
- **W12 Third person description.** The description says what the skill does, not what "I" or "you" do.
- **W13 No time-bound facts.** Do not write "before August 2025". Put old facts in a short "old method" section.

## Control

- **W14 Match the freedom to the risk.** A fragile task (database migration) gets exact steps or a script. An open task (code review) gets goals and criteria.
- **W15 Scripts solve.** A script handles its errors. It does not pass them to the agent. Comment each constant.
- **W16 Feedback loop.** For quality work: do, check with a script or a test, fix, check again.
- **W17 Gate before cost.** Ask the human before any step that is costly or hard to undo. Ask all questions in one message.
- **W18 Safe by default.** A skill with side effects (send, delete, deploy) starts only when a person calls it.

## Pruning

Cut text that does not change the output. Look for:

- **P1 No-op.** A line for something the agent does anyway ("Be helpful", "Read the file first").
- **P2 Sediment.** A line added after one bad run that no test needs.
- **P3 Duplicate.** The same rule in two places. Keep it in one.
- **P4 Sprawl.** A section for a case that never happens. Remove it.
- **P5 Hidden coupling.** A rule that only works with a rule far away. Move them together.

Test each cut: run the test again. If the result does not get worse, the line was not needed.
