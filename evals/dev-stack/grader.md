# Grader prompt

The orchestrator gives this prompt to one new agent for each run. The grader sees one run only. Do not tell the grader the variant. Remove the variant name from the paths that you give (for example, copy the run to a neutral folder name).

## Prompt

You grade one test run. An agent got a request on a .NET 10 repository. Read these files:

- The final message of the agent: `<run>/final.md`.
- The log of the run, if it exists: `<run>/transcript.md`.
- The repository after the run: `<run>/repo`. For a write case, `git -C <run>/repo diff main` shows the changes of the agent. For a review case, `git -C <run>/repo diff main...feature` shows the code under review, and `git -C <run>/repo status` shows any change of the agent.
- For a review case only: the answer sheet `<planted>`. It lists the planted mistakes with the file, the line, and the severity.

The request was:

> <request>

The human was absent. The agent had to decide by itself and state its assumptions.

Score each assertion below with yes or no. Give one line of evidence for each assertion. The evidence is a `path:line` in the repository, or a quote from the final message (at most 15 words). If an assertion is about the behaviour of the code, you can build or run the code to check it. Use `export PATH=/root/.dotnet:$PATH`. Do not change any file in the repository.

Rules for a review case:

- The review finds a planted mistake when it names the same problem at the same place. The words can differ. A finding at the correct line with the wrong reason does not count.
- Count the found mistakes and list their ids.
- A false positive is a finding that is wrong. A fix that breaks the code is also a false positive. So is a fix with an API that does not exist or that is old. A real problem that is not on the answer sheet is not a false positive. List each false positive with one line of reason.
- The answer sheet can list decoys: code that looks wrong but is correct. A finding that calls a decoy a problem, or that asks to change it, is a false positive. List the ids of these decoys in `decoys_flagged`, and also list each one in `false_positives`.

Rules for a write case:

- "States an assumption" means that the final message says what the agent assumed. Code alone is not enough.
- "Follows the style" means the style of the existing files in `src/` and `tests/`.

Assertions:

<assertions>

Reply with JSON only, in this form:

```json
{"run": "<run>", "found": ["P1", "P3"], "false_positives": ["..."], "decoys_flagged": ["D2"], "results": [{"id": "J1", "pass": true, "evidence": "..."}]}
```

For a write case, leave `found`, `false_positives`, and `decoys_flagged` empty.
