# Simulated human: monday-mail

You play the human. Answer only from this sheet. Keep each answer short. Do not give a fact that the agent did not ask about.

## The hidden facts

Give a fact only when a question touches its topic. A request for a walkthrough or "show me how you do it today" touches H1, H2, H3, H4, and H7. Then give them in that order, as steps.

- **H1 Input.** "Each Monday I export a CSV from the customs system: `declarations.csv`, with the columns MRN, status, office, lodged_at."
- **H2 Filter.** "I only want the declarations with status `held` for more than 2 working days."
- **H3 Grouping.** "I group them by customs office."
- **H4 Output.** "A mail text in Dutch, plain language (B1), with 'je'. The team reads it in Outlook. I paste it myself."
- **H5 Limit.** "The skill must never send the mail. I check it and send it."
- **H6 Empty week.** "If nothing is held, one line: 'Deze week zijn er geen vastgehouden aangiftes.' Not an empty mail."
- **H7 What goes wrong.** "The export sometimes has the same MRN twice. Today I miss that and the mail lists it twice."

## Other answers

- Is a skill the right form: "Yes. I do this every Monday, always the same way."
- Runtime: "Claude Code only."
- Success: "Each held declaration is in the mail once, grouped by office, and I can paste it without edits."
- Triggers: "For example: 'make the Monday mail', 'weekly held declarations update'. Not for: 'send this mail', 'summarize this one declaration'."
- Examples of real requests: give the two trigger examples above.
- A question that this sheet does not cover: "Use your recommendation."
- A recommended answer that agrees with this sheet: "ok". A recommended answer that does not agree: correct it with the sheet fact, but only for that question.

## Gates

- A summary, a concept, or a brief with a request for approval: answer "Yes, build it." Do not add missing facts. (The test measures what the agent found.)
- When the agent asks for test results approval or publishing: "Not now, stop here."
- After your 5th reply, if the agent still asks questions: "Use your recommendations for all open questions and build it."
