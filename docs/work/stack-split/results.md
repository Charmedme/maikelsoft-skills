# Results: the stack split, phase 1

Phase 1 of `brief.md`: dev-stack 2.0.0, frontend-stack 1.0.0, test-stack 1.0.0, and dev-team 1.1.0. The moved skills (`angular`, `dotnet-testing`, `migrate-fluentassertions`) have no change to their rules, so their earlier results stay valid. Phase 1 has a trigger check only. The behaviour workflow of the brief starts in phase 2.

## Trigger check

Set: `evals/frontend-stack/triggers.json`. Each query ran one time on Sonnet, with frontend-stack and dev-stack loaded, so that `web-security` and `dev-stack:secure-coding` compete.

| Skill | Version of the description | Starts (of 5) | Quiet (of 5) |
|-------|----------------------------|---------------|--------------|
| `web-security` | first | 5 | 5 |
| `modern-web` | first: "Sends HTML, CSS, and client-side JavaScript work to ... Use when ..." | 0 | 5 |
| `modern-web` | second: "Run first for each task that writes or changes HTML, CSS, or client-side JavaScript ..." | 4 | 5 |

- In the first version of `modern-web`, no run called a skill. The description said what the skill does, not when it must run.
- The second version names the task kinds first, as the description of Modern Web Guidance does. The one miss is "Add a confirm dialog before the user deletes a file on this page. Plain HTML and JavaScript."
- The skip queries are back-end, SQL, CI, and test requests. No skip query started a skill of frontend-stack.

## Limits

- One run for each query, on one model.
- The check did not have Modern Web Guidance installed. It does not show what happens when both skills are in the session.
