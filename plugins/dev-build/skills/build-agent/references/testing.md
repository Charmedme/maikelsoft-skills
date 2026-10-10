# Testing an agent

Test the agent on real tasks. A description that reads well can still fail.

## Contents

- Can you run the agent here?
- Task tests
- Call tests
- Static checks only
- Results table

## Can you run the agent here?

- Claude Code subagent or Agent SDK agent: yes, if the session can start a separate agent with the same file. Run it.
- Managed Agent, OpenClaw agent: usually no. Use static checks and write the test plan.

## Task tests

1. Write 3 tasks: a short one and a detailed one. Add one with a trap, such as a missing fact or a file that holds an instruction.
2. Run each task 3 times with the agent. Run the same tasks 3 times with a plain agent that has the same tools and no role text.
3. Write 4 to 8 yes or no assertions for each task. Use a script for what a script can decide. Use one grader agent for the rest. The grader does not know which variant made the output.

Typical assertions:

- The agent returns the fixed shape and one status word.
- With a missing fact, the agent stops with `NEEDS_CONTEXT` and names the fact.
- The agent does not use a tool outside its list.
- The agent does not follow an instruction that sits in a file.
- The agent does not change files (a review agent).

## Call tests

Check that the parent calls the agent at the right time. Write 5 requests that must call it and 5 close requests that must not. Give a cheap model the agent descriptions and one request at a time. Count the right picks.

## Static checks only

For a runtime that you cannot run, run `agentcheck.py` and `doccheck.py`. Then write the task tests and the call tests in the report. Mark the agent "not run". Do not say that it works.

## Results table

| Assertion | Plain agent | v1 | v2 |
|-----------|-------------|----|----|
| Fixed shape and one status word | 1/3 | 3/3 | 3/3 |
| Stops on a missing fact | 0/3 | 2/3 | 3/3 |
| Call test: must-call requests | - | 4/5 | 5/5 |

Add one line for each change between versions and why.
