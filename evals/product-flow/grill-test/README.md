# Grill test

This test measures how well the grill of `idea-to-backlog` finds the need of the customer.

## Contents

- Set-up
- Run
- Score

## Set-up

- `scenarios/a-no-problem/`: a solution definition without a problem statement.
- `scenarios/b-problem-stated/`: the same solution with a short problem statement that hides the real problem.
- `oracle/oracle.py`: a simulated product owner with 5 facts about the need (N1 to N5) and 3 facts about related modules (I1 to I3). A fact comes back only when the question is about its topic. For each choice, the product owner takes the recommended answer.

## Run

1. Copy a scenario to a new run folder. Copy `oracle/oracle.py` to `<run>/.oracle/`.
2. Give an agent the skill and the request. The request is: "Here is our solution definition and the mock-up notes for bulk order upload. Please turn this into a backlog."
3. The agent runs the Start, phase 1, and phase 2 up to G1. It sends each question to `python3 .oracle/oracle.py "<question>"`. It must not read the oracle or its log.

## Score

Run `python3 score.py <run> [<run> ...]`. The table shows, for each hidden fact, the number of the question that found it, or "-".
