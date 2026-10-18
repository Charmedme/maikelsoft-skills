# Run prompt

The orchestrator gives this prompt to a new agent. Use one agent for each run.

## Before the runs

1. Run `python3 evals/dev-stack/prepare.py <runs>`. The script makes one git repository for each run in `<runs>/<eval>/<variant>/<n>/repo`.
2. A write case starts on `main`. A review case starts on the branch `feature`. The base branch is `main`.
3. Give the run agent the prompt below. Replace the values in angle brackets with the values of the case in `evals.json`.
4. Save the final message of the agent as `<run>/final.md`. Save the full log as `<run>/transcript.md` if the tool can export it.

Do not give the agent `evals.json`, `planted/`, `probes/`, `selftest/`, `grader.md`, or this file.

## Common part

You work in the git repository `<repo>`. It is a .NET 10 solution. The .NET SDK is in `/root/.dotnet`. Run `export PATH=/root/.dotnet:$PATH` before you use `dotnet`. Docker runs on this machine, and the image `postgres:17.6-alpine` is in the local cache.

The human wants this:

> <request>

Nobody can answer questions in this run. Do not stop to ask. Make each decision yourself. In your final message, state each assumption that you made. Also list each open team decision that the work needed, with the answer that you assumed.

The team has these rules:

- MediatR and AutoMapper are not allowed.
- Each new package needs the approval of the human. Add a package only if the request approves it. For other packages, write a proposal in the final message.
- `docs/dev-team/decisions.md` lists the accepted team decisions. Follow them. For new code that needs a decision that is not in the file, ask the human. In this run, nobody answers, so mark it as an assumption.

Sub-agents are not available here. Do all the work yourself. Do not push to a remote. Read only the files in the repository and in the skill folder below.

For a review, do not change the files in the repository. Give the review in your final message.

## Variant: skill

Add this line after the request:

The skill is in `<skill>`. Read `<skill>/SKILL.md` and follow it.

The value of `<skill>` is `plugins/dev-stack/skills/<name>`, with the full path. The name is the `skill` field of the case.

## Variant: baseline

Use the common part with no change. Do not name a skill.

## Trap cases

The cases that end in `-trap` test facts that a model trained before mid-2026 often gets wrong (new package versions, removed APIs, new defaults). The `traps` field of the case lists them. The request does not name them.

- A write case starts from the folder in its `fixture` field. `prepare.py` copies that folder to `main`.
- Some checks add probe files from `probes/<name>/` to a copy of the run and run them. The run folder does not change.
- Run `python3 evals/dev-stack/selftest.py` after you change a check or a fixture. It applies a wrong (`naive`) and a correct (`good`) solution to the fixture and compares each check result with `selftest/expected.json`. It needs Docker.

## After the runs

1. Run `python3 evals/dev-stack/score.py <runs>` for the check assertions. `grep_absent` skips comment lines, unless the assertion has `"comments": true`.
2. Run one grader for each run (`grader.md`). Save its JSON as `<run>/grade.json`.
3. Run `python3 evals/dev-stack/consistency.py <runs>`. It compares the 3 runs of each case and variant on these values:
   - the verdict word,
   - the overlap of the found planted ids,
   - the overlap of the headings,
   - the spread of the word count.
