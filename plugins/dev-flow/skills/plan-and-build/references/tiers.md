# Tiers

The tier sets the steps and the gates. Set it from facts in the item and in the code, not from a guess. Read the code that the change touches first.

## Size

| Tier | Signal |
|------|--------|
| Patch | One file. The cause is clear. No change to a contract, the data, or the configuration of another system. Examples: a typo, a config value, a bug with a clear repro in one method. |
| Small | One behaviour, up to about 3 files, one layer. |
| Medium | One feature over more than one layer (for example an endpoint, its logic, and its storage). |
| Large | Count the signals below. Two or more: large. |

When two tiers fit, take the higher one.

### The large signals

Count them from the item and the code. Do not guess.

1. Three or more endpoints, screens, or message handlers are new or change.
2. The schema changes, or the data must be migrated.
3. The item has more than 8 acceptance criteria or examples.
4. The plan has more than 10 tickets.

Write the count in the tier line, for example: "Tier: large feature. Signals 1 (3 endpoints) and 2 (new columns)."

## Risk

Each of these sets the tier to medium or higher, whatever the size:

- A schema change or a data migration.
- Sign-in, authorization, secrets, or file upload.
- A change to a public API or message contract that the item does not ask for. Or a change that breaks existing clients: a removed or renamed field, route, or type. A new endpoint or a new error status that the item asks for is not this risk.
- A new package, or a new external service (a mail server, a payment provider, a queue).
- Money, invoices, or prices.
- The deletion of data.
- Another step that is hard to undo.

## Type

| Type | Change to the steps |
|------|---------------------|
| Bug | Start with a test that fails because of the bug. The fix makes it pass. No plan of more than 5 lines below the medium tier. |
| Refactor | No new behaviour, so no product question. Run the tests before the first change and after the last change. Both runs must pass. |
| Feature | The steps of the tier. |
| Chore (package update, configuration, build) | Patch or small, unless a risk applies. The checks are the gate. A package update is a new package when its major version changes. |

## The tier line

Tell the human the tier in one line, for example: "Tier: small bug. One method in `OrderService`, no contract change." When a risk sets the tier, name the risk.
