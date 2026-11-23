# Gherkin rules

The rules for `.feature` files. The rule numbers refer to `SKILL.md`. The sources are the Cucumber Gherkin reference, "Writing better Gherkin", and the Cucumber anti-patterns page (`sources.md`).

## Contents

- Declarative steps
- One behaviour in each scenario
- Structure: Feature, Rule, Background
- Scenario Outline and Examples
- Failure paths
- Step reuse
- Tags
- Language
- Secrets and test data
- Example before and after

## Declarative steps

A step tells the behaviour and the business result. It does not tell clicks, keys, or fields.

| Imperative (wrong) | Declarative (correct) |
|--------------------|-----------------------|
| `When I click the "Cancel" button` | `When the customer cancels the order` |
| `And I type "100" in the "Amount" field` | `Given an order of 100.00 euro` |
| `Then I see the text "Cancelled" in the status column` | `Then the order has the status "Cancelled"` |

- The UI details (the button, the field, the locator) go into the step definition or a page object.
- A declarative scenario stays correct when the screen changes. Only the step definition changes.
- The product owner can read and approve the scenario.

## One behaviour in each scenario

- One `When`: one action of the user or of the system. Two `When` steps show two behaviours. Split the scenario.
- The order is `Given` (context), `When` (action), `Then` (result). No `Given` after a `When`. No action in a `Then`.
- `And` and `But` continue the keyword before them.
- Do not join two actions in one step ("Given the customer has an account and an open order"). Write two steps.
- A `Then` checks what the user or an external system can see: a status on the page, a message, an email in the test mailbox. A database row is usually not observable.

## Structure: Feature, Rule, Background

- `Feature`: one capability. A short text under the title can give the user story.
- `Rule` (Gherkin 6 and later): groups the scenarios of one business rule. A `Rule` can have its own `Background`. Reqnroll 3.3.4 runs a `Rule`.
- `Background`: only the `Given` steps that each scenario of the feature or rule needs. Keep it short. A detail that is essential for one scenario stays in that scenario.

## Scenario Outline and Examples

- Use a `Scenario Outline` with `Examples` when only the data changes between scenarios.
- Name the columns in business words: `| status | can cancel |`, not `| col1 | col2 |`.
- Each row is one test.
- A plain `Scenario` with an `Examples` block is an error (Reqnroll #822). Use `Scenario Outline` (or `Scenario Template`).
- With MSTest, put tags on the outline, not on the `Examples` block (rule 18).

## Failure paths

Write at least one scenario for each way that a rule refuses an action: a wrong state, a missing right, wrong input, a time limit. Models write the success path and forget the refusals, unless a rule asks for them. An outline with the refused states is often the shortest form.

## Step reuse

- Write a step in domain words, so that more features can use it.
- Use a parameter for each value that changes: `{string}`, `{int}`, `{decimal}`, `{word}`. One step with a parameter replaces many near-copies.
- Do not make one step for each screen ("When I fill the cancel form"). This gives many steps that do almost the same work (step explosion).
- Do not couple a step to one feature with `[Scope(Feature = ...)]`. The Reqnroll docs call feature-coupled steps an anti-pattern. A `[Scope(Tag = "web")]` on a hook is correct.

## Tags

- Tags filter runs and select hooks: `@web`, `@slow`, `@wip`.
- In MSTest, feature tags and scenario tags become `TestCategory` (fixed in 3.2.1). Filter with `dotnet test --no-build --filter "TestCategory=web"`.
- `generator/addNonParallelizableMarkerForTags` in `reqnroll.json` marks the features with these tags as not parallel.

## Language

Use the language of the existing features (RQ-04). Use English in a new repository. Use Dutch when the product owner writes the specs in Dutch.

A Dutch feature starts with this line:

```gherkin
# language: nl
```

The Dutch keywords:

| English | Dutch |
|---------|-------|
| `Feature` | `Functionaliteit` |
| `Rule` | `Regel` |
| `Background` | `Achtergrond` |
| `Scenario` | `Scenario` or `Voorbeeld` |
| `Scenario Outline` | `Abstract Scenario` |
| `Examples` | `Voorbeelden` |
| `Given` | `Gegeven` or `Stel` |
| `When` | `Als` or `Wanneer` |
| `Then` | `Dan` |
| `And`, `But` | `En`, `Maar` |

`"language": { "feature": "nl-NL" }` in `reqnroll.json` sets the default language for each feature file without a header. The header in each file is easier to see. Use one of the two in a repository.

## Secrets and test data

- No password, token, or key in a feature file. The file is in Git and in the HTML report. Write "Given the customer has signed in" and read the secret from the environment in the step definition.
- No real personal data. Use made-up names and addresses.
- Give each scenario its own data with a unique value (for example a new order number), so that scenarios can run in parallel.

## Example before and after

Before: imperative steps, a password, two behaviours, and no failure path.

```gherkin
Feature: Orders

  Scenario: Cancel
    Given I open the login page
    And I type "anna@example.com" in the "Email" field
    And I type "S3cret!pass" in the "Password" field
    And I click the "Sign in" button
    When I click the "Orders" menu
    And I click the "Cancel" button in the first row
    Then I see "Cancelled" in the status column
    When I click the "Undo" button
    Then I see "Open" in the status column
```

After: domain steps, one `When` for each scenario, a `Rule`, and an outline for the refused states.

```gherkin
Feature: Cancel an order

  Rule: A customer can cancel an order until it ships

    Background:
      Given the customer has signed in

    Scenario: The customer cancels an open order
      Given the customer has an open order
      When the customer cancels the order
      Then the order has the status "Cancelled"

    Scenario Outline: The customer cannot cancel an order that is <status>
      Given the customer has an order with the status "<status>"
      When the customer cancels the order
      Then the customer sees that the order cannot be cancelled
      And the order keeps the status "<status>"

      Examples:
        | status    |
        | Shipped   |
        | Delivered |
```

The undo is a separate behaviour. It gets its own scenario with its own `Given`.
