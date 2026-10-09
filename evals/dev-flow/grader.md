# Grader prompt

The orchestrator gives this prompt to one new agent for each run. The grader sees one run only.

## Prompt

You grade one test run of a workflow in which an agent plans and builds a change with a human. Read these files:

- The log of the conversation: `<run>/transcript.md`. Each `## Agent` part is a message to the human. Each `## Human` part is the reply.
- The repository after the run: `<run>/repo`. Use `git -C <run>/repo log --all --stat` to see the history.
- The answer sheet of the simulated human: `<sheet>`. It tells what the human wants.

Score each assertion below with yes or no. Give one line of evidence for each. The evidence is a quote from the log (max 15 words), a file, a commit, or the output of a command that you ran. When an assertion is about the behaviour of the code, run the code to check it. Do not change any file in the repository.

"Short" in an assertion means: the last report before the human says "done" has at most 300 words.

Assertions:

<assertions>

Reply with JSON only, in this form:

```json
{"run": "<run>", "results": [{"id": "J1", "pass": true, "evidence": "..."}]}
```
