# Test plan template

```markdown
# Test plan: <title of the prototype>

## Question

| Rank | Candidate question | Source in ux-design.md | Cost of a wrong answer |
|------|--------------------|------------------------|------------------------|
| 1 | <main question> | <risk, proposed decision, option, or NEEDS EVIDENCE> | <for the user and for the build> |

Main question: <the question of the manifest>
Second question: <a question that the same tasks answer, or "none">

## Hypotheses and decisions

| ID | Hypothesis | Measure | Pass rule | If it passes | If it fails |
|----|------------|---------|-----------|--------------|-------------|
| H1 | If <change>, then <user result>, because <reason>. | <success, time, errors, SEQ, a prediction> | <for example: 4 of 5 participants> | <the design decision> | <the change, or the next test> |

## Tasks

| Task | Goal for the participant (no control labels) | Start | Success rule | Moderator script | What to watch |
|------|-----------------------------------------------|-------|--------------|------------------|---------------|
| T1 | <goal> | <screen/state> | <screen, state, or element> | <what to say before, and the probe after> | <the hesitation or error that matters> |

A failure state that the moderator starts (for example `error-save`): name the task and the moment.

## Order

| Participant | Order of the variants | Data set |
|-------------|-----------------------|----------|
| P01 | A, B | <set 1, set 2> |
| P02 | B, A | <set 1, set 2> |

Use matched data sets, so that the second variant does not get easier tasks.

## Participants

5 for each round, from the user group of the frame. Screening: <behavior that a participant must have>. Not: <who does not count>.

## Session plan

| Minutes | Part |
|---------|------|
| 0-5 | Welcome, consent, "we test the design, not you" |
| 5-40 | Tasks, think aloud, SEQ after each task |
| 40-50 | Preference and the reason, the probes |
| 50-60 | Notes of the moderator |

## Record

- The Results mode of the prototype: success, time, misclicks, SEQ, and preference. Export the JSON after each session.
- A note sheet: task, quote, hesitation, the probe answer.

## Pilot

One session with a colleague before the first participant. Check the tasks, the timing, and each success rule.
```

## Rules

- Each hypothesis has a pass rule and a decision for both results. A test that cannot change a decision is not worth the session.
- Below 20 participants for each variant, the numbers are indicative. The reasons and the observations decide.
- Do not invent participants or results.
