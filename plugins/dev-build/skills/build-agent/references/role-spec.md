# Role spec

The role spec is the same for each runtime. Write it first. Then render it into the files of the runtime.

## Contents

- Fields
- Example
- Status words

## Fields

| Field | Question | Rule |
|-------|----------|------|
| Name | What is the agent called? | Lowercase, hyphens, 2 to 4 words. A noun for the role: `code-reviewer`. |
| Job | What does it do, in one sentence? | One job. If you need "and", make two agents. |
| Caller | Who gives it work? | A parent agent, a person, or a schedule. |
| Trigger | When is it called? | Real situations in the words of the caller. |
| Input | What does the caller send? | List the facts it receives. It knows nothing else. |
| Output | What does it return? | A fixed shape. The caller sees only the final message. |
| Status | How does it end? | One status word (see below). |
| Tools | What can it use? | The least set. Name each tool and give the reason. |
| Model | Which model? | The smallest model that passes the test. |
| Limits | What must it not do? | Write each limit with its reason. |
| Missing facts | What does it do when it lacks a fact? | Stop and report. It cannot ask the human. |
| Persona | Voice and tone? | Optional. Free text. |

## Example

```
Name:      dependency-auditor
Job:       Find outdated or vulnerable dependencies in one repository.
Caller:    The main agent, after a change to a lock file.
Trigger:   A package file or lock file changed.
Input:     Repository path, the changed files.
Output:    A table: package, current version, problem, fix. Then a status line.
Status:    DONE, DONE_WITH_CONCERNS, NEEDS_CONTEXT, BLOCKED.
Tools:     Read, Grep, Glob, Bash (to run the audit command only).
Model:     sonnet.
Limits:    Do not change files. A change can break the build. The caller decides.
Missing:   No audit command found: stop with NEEDS_CONTEXT and name the file.
```

## Status words

Give the agent a short list of end states. The caller can then act without reading the whole answer.

- `DONE`: the job is complete.
- `DONE_WITH_CONCERNS`: complete, with one or more doubts. The doubts follow.
- `NEEDS_CONTEXT`: a fact is missing. Name the fact.
- `BLOCKED`: the agent cannot continue. Give the reason.

Use these words only when a parent agent reads the result. A person-facing agent needs no status word.
