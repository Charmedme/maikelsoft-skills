# Concept: executable specifications in the flow (2026-10-24)

Input: a video of Dave Farley (Modern Software Engineering, 2026-10-21), and the sources below. Title: "The Industry Is Catching Up: The Only Way to Generate AI Code That Works". The transcript of the video was not available: YouTube refuses requests from this workspace. This concept uses the description of the video, the summary on daily.dev, the Aviator podcast with Farley, and his post on X.

## The points of Farley

1. AI does not solve the three hard parts of programming. They are: specify the intent, verify the result, and make progress in small steps.
2. Prompts fail, and specifications in Markdown drift. Prose cannot fail: code can contradict a Markdown spec, and nothing tells you.
3. The specification must be executable: acceptance tests in the words of the problem domain.
4. Four layers keep these tests stable. Layer 1 is the test case in domain words. Layer 2 is a domain-specific language (DSL). Layer 3 is a protocol driver that talks to the real system (API or UI). Layer 4 is the system under test. A rewrite of the implementation does not change the test cases.
5. The agent can change the implementation freely, while the specifications pass. The agent must not change the specifications.
6. Kent Beck and Emily Bache saw agents that delete or weaken a failing test. Keep the acceptance tests outside the scope that the agent can edit. Or require the approval of a human for a change.
7. Gojko Adzic found that the Markdown specs of GitHub Spec Kit missed detail. The real specification ended up in the tests.

Robert C. Martin takes the same line: two test streams. The engineer and the AI write the acceptance tests together in Given/When/Then, in domain words, and the AI does not change them without permission. The AI writes the unit tests. Mutation testing shows if the tests catch bugs. Birgitta Böckeler (martinfowler.com) adds the limit: the process must scale with the size of the change.

## What this means for our skills

| Point | Our skills now | Gap |
|-------|----------------|-----|
| Executable specification | dev-flow copies the acceptance criteria as prose. The agent writes the tests for them, in the same stream as the code | The agent writes and grades its own test. Nothing stops a weak test |
| Domain language | product-flow rule A5 forbids Given/When/Then by default: a checklist only | The criteria are not examples. They need a translation step that can lose intent |
| Tests that the agent cannot change | No rule, no check | A failing acceptance test can be weakened or deleted |
| Small steps | Test first per criterion, one commit per ticket | Covered |
| Scale with the size | Tiers (dev-flow 2.0) | Covered |
| Markdown specs drift | dev-flow 2.0 removed the spec files: the item is the spec | Covered, but the item is prose |

Our own evals show the effect: each arm passes our hidden probes. The probes are executable acceptance tests that a human wrote. The agents never see them. In a real team, nobody writes these probes. So the probe is the part that our skills must produce, with the PO, and then guard.

## Proposal

1. **Examples on the item.** product-flow writes each acceptance criterion with one or more concrete examples (Given/When/Then, or a table of examples). The PO approves the examples on the item. Rule A5 changes its default to scenarios.
2. **Spec first.** dev-flow turns the examples into executable acceptance tests before any production code. These tests fail first (red). The human approves the test cases. This is the gate Spec, and it replaces the gate Plan and `plan.md` for small and medium items. The human reads test cases in domain words, not a plan.
3. **Two streams.** The acceptance tests are the contract of the human. The agent writes the unit tests and the code with TDD until both streams pass.
4. **Guard.** After the gate Spec, a deterministic check refuses a change to the acceptance test files. A hook stops the edit. The checker compares the files with the approved hash. A change needs the approval of the human, and the report names it.
5. **Layers, only when they pay.** The DSL and the protocol driver are made one time for each project, at the first medium item. A patch or a small item adds test cases to the DSL that exists. A bug fix is a failing test that shows the bug: that is the executable spec of a bug.
6. **Mutation testing as an option.** For a medium or large item, run mutation testing on the changed code (Stryker.NET for .NET), and report the score. It is a check, not a gate.

## How we measure it

Our easy stories cannot show a gain: each arm passes. The test needs stories that are ambiguous or have a hidden risk, with hidden probes:

- Arms: baseline, dev-flow 2.0, dev-flow with executable specs. Also each arm on Sonnet.
- Measure: the probe pass rate, the number of tests that the agent weakened or deleted, the cost, and the time.
- Evidence so far for the method: expert reports and examples, no controlled study. Our test is the first measure for our case.

## Sources

- Farley, The Industry Is Catching Up (YouTube, 2026-10-21), and the summary on daily.dev.
- Farley, Engineering discipline in the AI era (Aviator podcast).
- Robert C. Martin, disciplined agentic engineering (summary on noqta.tn), and the `swingerman/atdd` Claude Code plugin based on his approach.
- Böckeler, Understanding Spec-Driven-Development: Kiro, spec-kit, and Tessl (martinfowler.com, 2025-10-15).
