# ux-design 0.4.0: eval results

Date: 2026-10-27. Variant old: ux-design 0.3.1 (origin/main). Variant new: ux-design 0.4.0 (branch `feat/ux-design-0.4.0`, `uxcheck.py` with md5 `d5cc47ac`). Model: `claude-sonnet-5-5` for each child run and each grader. Total cost: about $21 ($18.31 for the runs, $1.98 for the graders).

## Summary

| Case | Result | Verdict |
|------|--------|---------|
| `party-new-direction` | The new variant sets `Mode: new-direction` and writes a Style direction table with 3 directions in 3 of 3 runs. The old variant does this in 0 of 3 runs. No run of the new variant stops for the owner choice (0 of 3). Both variants change the color, the type, and the card look in 3 of 3 runs. | Partly met: the stop fails |
| `party-review-faults` | The new variant names the planted faults 13 of 15 times. The old variant names them 5 of 15 times. In its mockups, the new variant avoids 13 of 18 planted faults. The old variant avoids 1 of 18. The new variant flags no decoy. The old variant flags the decorative card 2 times. | Use |
| Checker on the planted mockups | The new checker finds F3 (2 `MK-REGION`) and F5 (`TELL-21`), and gives `SYS-SURFACE` on the tokens. It does not find F1 and F2 in the mockups (see finding 3). The old checker finds none of the planted faults. Neither checker reports a decoy. | Gap, see finding 3 |

## Method

- Runner: `evals/ux-design/run4.py`. Each run gets a fresh copy of the fixture and a headless `claude -p` session with no permission bypass. The run can use Read, Write, Edit, Glob, Grep, `python3`, and `ls`. Both variants get the same prompt (`evals/ux-design/prompts4/`), and only the plugin path differs. The prompt is the text of `runner.md`: "Nobody can answer questions in this run. Do not stop to ask."
- Variant old: `git archive origin/main plugins/ux-design`, in a scratch folder. Variant new: `plugins/ux-design` of the worktree. The new variant started after `python3 -m unittest tests.test_ux_040` passed (27 tests).
- 3 runs for each variant and each case. All binary assertions are in `evals/ux-design/evals4.json`.
- The `check` assertions: `evals/ux-design/score4.py`. For case 1, it reads `ux-design.md`, the reply, and the frames of the mockups. For case 2, a Playwright probe renders each frame at 390 px and measures the planted faults in the produced mockups. I tested the probe on the planted mockups (each fault present) and on a fixed copy (each fault absent).
- The `judge` assertions: `evals/ux-design/grade4.py`. One grader run for each eval run. The grader sees the context and the output of one run, without the variant name.
- Fixtures: `evals/ux-design/fixtures/party-new-direction/` and `party-review-faults/`. The product is the Dutch party card game Naar de Kloten (Angular, mobile first). The design system is the Gameshow tokens before the contrast fix (`fixtures/field-2026-10-27/gameshow-tokens-before-fix.css`), with invented type, space, and component rules. `fixtures/party-planted/` holds the faulty spec of case 2 as mockups, for the checker assertion.

## Case 1: party-new-direction

The story asks for "3 concepten voor de wachtkamer en het spelscherm" with "een totaal nieuwe aanpak". The design mode and the mockup mode run in one session.

| Assertion | Kind | Old (0.3.1) | New (0.4.0) |
|-----------|------|-------------|-------------|
| C1 `Mode: new-direction` in `ux-design.md` | check | 0 of 3 | 3 of 3 |
| C2 Style direction table with 3 or 4 rows | check | 0 of 3 | 3 of 3 |
| C3 Stop with `NEEDS_HUMAN` before the concepts and the states | check | 0 of 3 | 0 of 3 |
| J1 One direction changes the color, the type, and the card look | judge | 3 of 3 | 3 of 3 |

Details:

- The new variant wrote 3 moodboards (`direction-1.html` to `direction-3.html`), a `Chosen:` line, and an Options table with a Difference column in each run. Then it continued: 19 to 25 frames with states other than default, and a `Selected:` line. Each run wrote the choice as an assumption ("designer recommendation, the owner did not confirm"). The status line was `DONE_WITH_CONCERNS`, not `NEEDS_HUMAN`.
- The old variant made a new foundation in each run (for example Anton and Inter on warm black with an orange or lime accent). The grader found 3 of 3 directions with all 3 changes in each run of both variants. So with this story, Sonnet does not repeat the field failure (bad case 1) with 0.3.1.

## Case 2: party-review-faults

The agent has no mode that reviews the mockups of someone else. Review mode checks a built app. So this case is mockup mode on an approved `ux-design.md` that contains the planted faults. The prompt says that the owner gave the OK on the key screens.

| Assertion | Kind | Old (0.3.1) | New (0.4.0) |
|-----------|------|-------------|-------------|
| F1 Tiles and segmented control at 1.25:1, named | judge | 1 of 3 | 3 of 3 |
| F1a Player tile at 1.5:1 or more in the mockup | check | 0 of 3 | 3 of 3 |
| F1b Segmented control track at 3:1 or more in the mockup | check | 0 of 3 | 2 of 3 |
| F2 Input edge at 1.58:1, named | judge | 1 of 3 | 3 of 3 |
| F2 Input edge at 3:1 or more in the mockup | check | 1 of 3 | 3 of 3 |
| F3 Music toggle and leave control in 2 places, named | judge | 0 of 3 | 3 of 3 |
| F3 One place on both screens in the mockup | check | 0 of 3 | 3 of 3 |
| F4 Text toggles for music, chat, and microphone, named | judge | 1 of 3 | 1 of 3 |
| F4 Toggles with a state attribute in the mockup | check | 0 of 3 | 2 of 3 |
| F4 Toggles with a state attribute and an icon in the mockup | check | 0 of 3 | 0 of 3 |
| F5 Icon toggle name changes ("Dempen" or "Dempen opheffen"), named | judge | 2 of 3 | 3 of 3 |
| F5 One fixed name and a state attribute in the mockup | check | 0 of 3 | 2 of 3 |
| D1 Decorative card stack not flagged as a fault | judge | 1 of 3 | 3 of 3 |
| D2 Text button "Spelregels" without a border not flagged | judge | 3 of 3 | 3 of 3 |

Details:

- The new variant changed the edges to `--color-border` (3.16:1) and cited WCAG 1.4.11. It gave each shared element one `data-region`, and it ran the checker rule `MK-REGION`.
- F4: the new variant kept the visible words and added `aria-pressed` or `role=switch` with a fixed name (2 runs). One run kept "Spelgeluid: aan" and "Spelgeluid: uit" with no state attribute, so F5 also failed in that run. No run made icon buttons. The approved spec asked for text toggles. The agent did not see a text toggle with a state as a fault.
- D1: the new variant called the card stack "decoration only" in each run. Two old runs proposed to remove the card stack because of its low contrast.

## Checker on the planted mockups

`uxcheck.py system` on the fixture CSS, and `uxcheck.py mockup` on `fixtures/party-planted/mockups/`.

| Planted item | Old checker | New checker |
|--------------|-------------|-------------|
| F1 Raised surface #2e1a63 on #1a0b3b (tokens) | not found | `SYS-SURFACE` warning |
| F1 Player tiles and segmented control (mockup) | not found | not found |
| F2 Input edge 1.58:1 (tokens and mockup) | not found | not found (found when the stage has no gradient) |
| F3 Music toggle and leave control in 2 regions | not found | `MK-REGION`, 2 errors |
| F4 Text toggles | not found | not found (no rule for this in the build spec) |
| F5 Changing toggle name | not found | `TELL-21` |
| D1, D2 decoys | not reported | not reported |

## Findings

1. Plugin defect, the stop for the owner. File: `plugins/ux-design/agents/ux-designer.md`, design mode step 6 (Style direction) and the mockup pass 1. Both steps say "stop with `NEEDS_HUMAN`". The Rules section tells the agent to take the recommended answer and continue when nobody can answer. The runner prompt also tells the run not to stop. In 3 of 3 runs, the agent followed these two texts. It made all concepts and states with a direction that it chose itself. So in a headless run, the agent does not stop for the style choice. The success criterion is not met. Possible fix: in the Rules line, make an exception for these two stops. Then a run without an owner ends after the moodboards with `NEEDS_HUMAN`.
2. The new variant does the parts that a script can check. These are the mode, the Style direction table, the `Chosen:` line, and the Difference column. The inventory and the `data-region` marks are also correct. It also names the contrast faults with the correct WCAG rule.
3. Checker gap, `uxcheck.py mockup`. The `bgOf()` function returns no color when an ancestor has a gradient background. Then `edge()` returns nothing, and `MK-NONTEXT` and `MK-SURFACE` skip each control on that stage. The Gameshow stage `.gs-podium` of the fixture has a radial gradient, so the checker misses the input edge. When I removed the gradient, the checker found the input (`MK-NONTEXT`, 1.58:1).
4. Checker gap, `MK-SURFACE`. It looks at an element that is interactive or has a class with `chip`, `tile`, or `row`. The fixture tile is `<li class="gs-tegel">` with the fill, and a `<button>` without a fill in it. The field markup also uses Dutch class names (`gs-plek`). The checker does not report these tiles. `MK-NONTEXT` for a segmented control checks only the selected segment, not the track.
5. Checker gap, `SYS-NONTEXT`. On the before-fix tokens, it gives no error. `--color-border` is 3.16:1, and the input border uses `--color-surface-spot`, which is not an edge token. The success criterion "The before-fix tokens give an error for the control edges" is not met with this CSS.
6. Small: `uxcheck.py mockup` stops with a Python traceback when a file does not exist (one run used a wrong folder). A short error message is easier to read for the agent.

## Cost and time

| Case | Variant | Cost for each run | Time for each run | Turns |
|------|---------|-------------------|-------------------|-------|
| `party-new-direction` | old | $2.05 to $2.41 | 11 to 13 min | 60 to 69 |
| `party-new-direction` | new | $2.24 to $2.35 | 12 to 13 min | 88 to 115 |
| `party-review-faults` | old | $0.52 to $0.86 | 3 to 4 min | 21 to 39 |
| `party-review-faults` | new | $0.77 to $1.05 | 5 to 6 min | 35 to 44 |

The graders cost $0.10 to $0.24 for each run.

## Limits

- 3 runs for each variant: one run changes a result by 33 points.
- Case 1 cannot show the effect of a stop on the work after the stop, because no run stopped. A run with an answer of the owner (a second prompt) is not part of this test.
- The story of case 1 says what is old ("paarse vlakken, witte kaarten, de Bungee-letter"). The field request was shorter, so this case is easier than the field case.
- In case 2, the prompt says that the owner gave the OK on the key screens. So the case does not test pass 1 of the mockup mode.
- The F4 check asks for an icon. The spec of the fixture asked for text toggles, and an icon toggle with a visible label is also correct. The judge assertion is the better measure here.
- The grader is one Sonnet run for each eval run, with no second grader. For D1 in old run 3, the grader counted "almost invisible, drop it" as a flag. This is a borderline call.
- The fixture CSS has a gradient on the stage. The field mockup uses a separate spotlight element. This makes the checker gap of finding 3 larger in this test than in the field.
- No regression run of the earlier eval cases is in this round.

## Round 2 (after the fix)

Date: 2026-10-27. The fix round changed `agents/ux-designer.md` and `ux-practice/SKILL.md`: the style direction stop and the pass 1 stop are hard gates now. The agent skips a gate only when the task gives the choice or the OK of the owner.

The fix also changed `uxcheck.py` (md5 `d5b3092c`). It averages a gradient background and finds tiles without a class name. It checks the track of a segmented control, and gives a short error for a missing file.

The runner prompt did not change, and it still says "Do not stop to ask". Variant `new2` in `run4.py`, 3 runs for each case, the same model, the same scripts, and the same grader.

### Case 1 in round 2

| Assertion | Kind | Old | New, round 1 | New, round 2 |
|-----------|------|-----|--------------|--------------|
| C1 `Mode: new-direction` | check | 0 of 3 | 3 of 3 | 3 of 3 |
| C2 Style direction table with 3 or 4 rows | check | 0 of 3 | 3 of 3 | 3 of 3 |
| C3 Stop with `NEEDS_HUMAN` before the concepts and the states | check | 0 of 3 | 0 of 3 | 3 of 3 |
| J1 One direction changes the color, the type, and the card look | judge | 3 of 3 | 3 of 3 | 3 of 3 |
| Cost for each run | | $2.05 to $2.41 | $2.24 to $2.35 | $0.58 to $0.67 |
| Time for each run | | 11 to 13 min | 12 to 13 min | 3 min |

Each run of round 2 wrote 3 moodboards and the Style direction table, and then stopped with `NEEDS_HUMAN`. No run wrote a `Selected:` line or a state frame. Each reply asks the owner to choose, and says that a second stop for the key screens follows. A run costs about a quarter of a run that does not stop.

### Case 2 in round 2

The prompt of case 2 gives the OK of the owner on the key screens. With the new gate, a correct run skips the pass 1 stop. So the expected result of P1 is 0 of 3.

| Assertion | Kind | Old | New, round 1 | New, round 2 |
|-----------|------|-----|--------------|--------------|
| P1 Stopped after pass 1 (expected: no stop) | check | 0 of 3 | 0 of 3 | 0 of 3 |
| F1 Tiles and segmented control, named | judge | 1 of 3 | 3 of 3 | 3 of 3 |
| F1a Player tile at 1.5:1 or more | check | 0 of 3 | 3 of 3 | 3 of 3 |
| F1b Segmented control track at 3:1 or more | check | 0 of 3 | 2 of 3 | 3 of 3 |
| F2 Input edge, named | judge | 1 of 3 | 3 of 3 | 3 of 3 |
| F2 Input edge at 3:1 or more | check | 1 of 3 | 3 of 3 | 3 of 3 |
| F3 Two places, named | judge | 0 of 3 | 3 of 3 | 3 of 3 |
| F3 One place on both screens | check | 0 of 3 | 3 of 3 | 3 of 3 |
| F4 Text toggles, named | judge | 1 of 3 | 1 of 3 | 2 of 3 |
| F4 Toggles with a state attribute | check | 0 of 3 | 2 of 3 | 3 of 3 |
| F4 Toggles with a state attribute and an icon | check | 0 of 3 | 0 of 3 | 1 of 3 |
| F5 Changing toggle name, named | judge | 2 of 3 | 3 of 3 | 3 of 3 |
| F5 One fixed name and a state attribute | check | 0 of 3 | 2 of 3 | 2 of 3 |
| D1 Decorative card stack not flagged | judge | 1 of 3 | 3 of 3 | 3 of 3 |
| D2 "Spelregels" without a border not flagged | judge | 3 of 3 | 3 of 3 | 3 of 3 |
| Faults named (of 15) | judge | 5 | 13 | 14 |
| Faults avoided in the mockups (of 18) | check | 1 | 13 | 15 |
| Cost for each run | | $0.52 to $0.86 | $0.77 to $1.05 | $0.76 to $1.14 |

Details:

- No run of round 2 stopped, and the replies end with `DONE_WITH_CONCERNS`. This is correct, because the prompt gives the OK. This round does not test a pass 1 stop without an OK.
- F5: run 3 used the visible text "Spelgeluid aan" and "Spelgeluid uit" as the name of a switch. The probe counts 2 names, so F5 fails in the mockup. The run named the fault in its notes.
- D1: run 3 left out the decorative card stack because it has no task (TELL-13). It did not call it a contrast fault.

### Checker on the planted mockups in round 2

| Planted item | Round 1 | Round 2 |
|--------------|---------|---------|
| F1 Raised surface (tokens) | `SYS-SURFACE` warning | `SYS-SURFACE` warning |
| F1 Player tiles (mockup) | not found | `MK-SURFACE` warning on lobby and stand |
| F1 Segmented control track (mockup) | not found | `MK-NONTEXT` error |
| F2 Input edge (mockup) | not found | `MK-NONTEXT` error on 2 frames |
| F2 Input edge (tokens) | not found | not found |
| F3 Two regions | `MK-REGION`, 2 errors | `MK-REGION`, 2 errors |
| F5 Changing toggle name | `TELL-21` | `TELL-21` |
| D1, D2 decoys | not reported | not reported |
| Missing file | traceback | `uxcheck: file not found` |

The fix round removed the gaps of findings 3, 4, and 6 (round 1). The gap of finding 5 is still there. `SYS-NONTEXT` gives no error on the CSS of the fixture. Under the gradient, the checker measures against the average color (#2e195d), not against `--color-bg`. So the ratios in the messages (1.02:1 and 1.33:1) are lower than the ratios against the stage (1.25:1 and 1.58:1). The verdicts are the same.

Round 2 cost: $4.83 for the runs and $0.57 for the graders.

## Files

- Fixtures: `evals/ux-design/fixtures/party-new-direction/`, `party-review-faults/`, `party-planted/`
- Prompts: `evals/ux-design/prompts4/`
- Assertions: `evals/ux-design/evals4.json`
- Scripts: `evals/ux-design/run4.py`, `score4.py`, `grade4.py`
- Run folders (scratch, not in the repository): each has `transcript.jsonl`, `final.md`, `meta.json`, `grade.json`, and `shots/`
