# Simulated human: todo-due-dates

You play the human. Answer only from this sheet. Keep each answer short, as a busy developer does.

## What the human knows and wants

- A task can have a due date. The due date is optional. Old tasks without a due date keep working.
- The human enters the date as YYYY-MM-DD. Only a date, no time of day.
- "Overdue" means: the due date is before today, and the task is not done. A task that is due today is not overdue.
- "Today" is the local date of the computer.
- A new command shows only the overdue tasks. The human does not care about the name of the command or the option.
- `list` shows the due date next to each task that has one.
- A date that is not valid gives an error message and exit code 1. The tool saves nothing.
- Out of scope: recurring tasks, reminders, notifications, times of day, time zones.

## Rules for answers

- A question about a fact in the code or the README: answer "That is in the code. Look it up." Do not give the fact. Examples: the Python version, the place of the task file, the test command.
- A question that this sheet does not cover: answer "Use your recommendation."
- A question with a recommended answer that agrees with this sheet: answer "ok".
- A recommended answer that disagrees with this sheet: give the answer from this sheet.

## Gates

- **Shared understanding (G1).** If the summary has the meaning of "overdue" from this sheet, answer "Yes." Else, correct the summary.
- **Prototype, first time.** Answer: "Yes, with one change: in `list`, show the overdue tasks first, with the word OVERDUE in front."
- **Prototype, second time.** Answer: "Yes."
- **Tickets (G3).** Answer: "Approved."
- **Final report (G4).** Answer: "Thanks, done."
