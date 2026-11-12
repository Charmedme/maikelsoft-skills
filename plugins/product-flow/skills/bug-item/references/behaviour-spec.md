# Behaviour spec

Each user story and each bug item has a section "Behaviour spec". It holds the rules of the item and a scenario for each limit, in Gherkin. The build copies this section into `spec.md`, and each rule becomes one test file. So write each scenario with one clear result, in the words of the domain.

## Where it goes

| Tracker | Place |
|---------|-------|
| Jira, GitHub, GitLab, Linear | In the description, under the heading `## Behaviour spec`, after the acceptance criteria. |
| Azure DevOps (Agile, Scrum, CMMI) | In the field Acceptance Criteria: first the criteria, then the heading `Behaviour spec` and the spec. |
| Azure DevOps (Basic) | In the description, under the heading `Behaviour spec`. |
| Item file | Under the heading `## Behaviour spec`. |

Write nothing else in the section. A link or a comment goes outside it, so it does not change the spec.

## Story

```gherkin
Rule: R1 A return within 30 days is free (AC1) (limit: 30 days)

  Scenario: return below the limit
    Given an order that the customer got 29 days ago
    When the customer returns the order
    Then the return is free

  Scenario: return at the limit
    Given an order that the customer got 30 days ago
    When the customer returns the order
    Then the return is free

  Scenario: return above the limit
    Given an order that the customer got 31 days ago
    When the customer returns the order
    Then the shop charges a return fee of 4.95 euro
```

- **Rules.** One rule for each limit, state, "only when", or error of a criterion. Give each rule an ID (`R1`, `R2`) and the criterion that it comes from (`(AC1)`). Each criterion has one or more rules.
- **Limits.** A rule with a number, a time, a count, or a date gets `(limit: <value>)`. It needs a scenario below, at, and above the limit. Use the words "below", "at", and "above" in the scenario names.
- **Scenarios.** Each scenario has a When step and a Then step. A Given step is optional. Use real values: "29 days", "4.95 euro", not "a short time" or "a fee". At most 4 scenarios for each rule. A Scenario Outline with a table of examples counts one scenario for each row.
- **Size.** At most 7 rules. More rules means the story is too large: split it.
- **Words.** Use the words of the users and of the domain, not of the code. No class names, no screen element IDs, no SQL.

## Bug

```gherkin
Rule: R1 A return within 30 days is free (AC1)
  Now: a return on day 30 costs 4.95 euro.

  Scenario: return on the last free day
    Given an order that the customer got 30 days ago
    When the customer returns the order
    Then the return is free

Rule: R2 Unchanged (AC2)

  Scenario: return after the free period
    Given an order that the customer got 31 days ago
    When the customer returns the order
    Then the shop charges a return fee of 4.95 euro
```

- The first rule is the rule that the bug breaks. Its `Now:` line gives the current behaviour. Its scenarios give the expected behaviour. They fail today.
- The rule "Unchanged" holds the behaviour that must not change. Its scenarios pass today and must stay green.
- When no rule exists, the bug is a missing requirement. The product owner decides the rule.

## Dutch

The Dutch keywords also work: `Regel`, `Voorbeeld`, `Gegeven`, `Als`, `Dan`, `En`, `Nu:`, `Ongewijzigd`, `(grens: ...)`, and the case words `onder`, `op`, `boven`.

## Check

Run `python3 ${CLAUDE_SKILL_DIR}/scripts/specsync.py check <file>`. Add `--kind bug` for a bug item. Fix each error. The script checks the IDs, the number of rules and scenarios, the steps, the limit cases, and the parts of a bug.
