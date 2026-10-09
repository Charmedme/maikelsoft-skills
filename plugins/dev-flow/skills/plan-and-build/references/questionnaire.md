# Questionnaire

Use a questionnaire when only a third person can answer a question. The human sends it to that person, or uses it in a meeting.

## Steps

1. **Propose the send.** In one message, propose three things, each with your recommended answer. First, who gets the questionnaire. Second, what that person knows that the human does not know. Third, the date for the answers (default: one week from today). Use the language of the conversation, unless the human says otherwise. Do not ask for the name of the sender. Do not ask the human about the subject itself.
2. **List what the human needs back.** Take the "waiting" decisions from `decisions.md`. Add each question that depends on them, so that one questionnaire covers the full subject.
3. **Write the questionnaire.** Use `templates/questionnaire.md`. Write it to `questionnaires/<slug>.md` in the work folder. Put the most important question first. One idea in each question. Give a "Why this matters" line only when a question is easy to misread.

Done when each "waiting" decision has a question.

When the answers come back, add them to `decisions.md` and remove the "waiting" mark. If a question about the subject is still open, do not ask the human for the answer. Ask one question with a recommendation: send a short follow-up questionnaire, or use your recommended answers as assumptions. Then make the next grill round.
