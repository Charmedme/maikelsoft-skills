---
status: accepted
date: 2026-10-08
decision-makers: Maikel
---

# Use a standard-library Python checker instead of Vale for the countable rules

## Context and Problem Statement

The Claude Code plugin "dev-docs" has 10 skills. The skills write developer documentation in ASD-STE100 Simplified Technical English (English) and in Begrijpelijk Nederlands B1 (Dutch). A tool can count some rules of these styles. Which tool must check these countable rules?

## Decision Drivers

- The checker must run in each environment where the agent runs: Claude Code on a laptop, the claude.ai sandbox, and CI.
- The checks must be deterministic. Maikel prefers deterministic checks to model judgment.
- The solution must stay simple. Maikel does not want an overengineered solution.
- The checker must find these items:
  - Long sentences: more than 20 words in a procedure, 25 words in a description, or 20 words in Dutch.
  - Paragraphs with more than 6 sentences.
  - Semicolons, em-dashes, and contractions.
  - Modal verbs.
  - Possible passive voice.
  - In rewrite mode, compared to the source: lost code lines, lost links, and changed heading anchors.

## Considered Options

- Python script: a script that uses only the Python standard library, with a copy in each skill
- Vale: a common prose linter (a Go binary, with rules in YAML)
- Both: the Python script and Vale

## Decision Outcome

Chosen option: "Python script", because it satisfies the three decision drivers with the smallest number of parts. The script needs no package other than Python 3, and each skill has its own copy. Thus the checker goes with the skill to each environment. The script counts words and finds patterns, so the same text gives the same result each time.

For v1, the plugin uses only this script. A Vale export can come later if CI needs it.

### Consequences

- Good, because the checker needs no extra install. It runs where the skill runs.
- Good, because the checks are deterministic.
- Good, because the solution is one script and no other tool.
- Bad, because the passive-voice checks are heuristics. They give false findings.
- Bad, because the word lists are our own. They are smaller than the word lists of a full linter.
- Bad, because we maintain the script ourselves.
- Bad, because each skill has a copy of the script, and a copy can become old. The sync check in "Confirmation" finds an old copy.

### Confirmation

- CI runs the unit tests in `tests/test_doccheck.py`.
- CI runs `scripts/sync_shared.py --check`. This check fails CI when a skill has an old copy of the script.

## Pros and Cons of the Options

### Python script

- Good, because it needs only Python 3.
- Good, because a copy in each skill makes each skill complete without other files.
- Bad, because the passive-voice heuristics give false findings.
- Bad, because the word lists are smaller than the word lists of a full linter.
- Bad, because we maintain the script ourselves.

### Vale

- Good, because it is a common prose linter.
- Good, because its rules are in YAML.
- Bad, because it is a Go binary. Each environment must have this binary before the check can run.
- Bad, because it adds one more tool to the plugin.

### Both

- Good, because the plugin gets the Python checks and the Vale rules.
- Bad, because the plugin has two tools and two sets of rules to maintain.
- Bad, because each environment must have the Vale binary, as in the Vale option.

## More Information

- Examine this decision again when CI needs Vale. Then a Vale export can come in a later version.
- Format: MADR 4.0.0 (https://adr.github.io/madr/).
