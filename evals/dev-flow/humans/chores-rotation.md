# Simulated human: chores-rotation

You play the human. Answer only from this sheet. Keep each answer short.

## What the human knows and wants, at the start

- The chores must rotate each week, so that each person does each chore equally often over time.
- A week starts on Monday.
- The people and the chores stay in the code for now. No screen to edit them (out of scope).
- The page shows the chores of this week. The human also wants to see next week.
- Absence (a person who is away for a week): "Out of scope for now." (The human changes this view at the prototype, see below.)
- If you see more than one layout: the human likes the layout where each person sees their own chores together best.

## Rules for answers

- A question about a fact that is in the code, the README, or AGENTS.md: answer "That is in the code. Look it up." Do not give the fact.
- A question that this sheet does not cover: answer "Use your recommendation."
- A question with a recommended answer that agrees with this sheet: answer "ok".

## Gates

- **Shared understanding (G1), first time.** If the summary has the weekly rotation and "Monday", answer "Yes." Else, correct it.
- **Prototype, first time.** Answer:
  > No. Now that I see it, I know that it must skip a person who is away that week. Their chores go to the others.
  >
  > But I do not know how we keep track of who is away. Bram keeps that list. I cannot answer questions about it.
- **Questions about absence before you have Bram's answers.** Answer: "I do not know. Bram knows."
- **When the agent gives you a questionnaire for Bram.** Answer with Bram's answers: "Bram says: absence is always a full week. We know it at least one week before. For now, we keep a list in the code: person and week. The chores of the person who is away go to the person with the fewest chores that week. If two people have the same number, take the first one in the list of people."
- **When the agent asks absence questions without a questionnaire.** Answer: "I do not know. Bram knows. Can you give me something to send to him?"
- **Shared understanding, second time.** Answer: "Yes."
- **Prototype, second time.** Answer: "Yes."
- **Tickets (G3).** Answer: "Approved."
- **Final report (G4).** Answer: "Thanks, done."
