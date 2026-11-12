# Size

The size sets the steps, the gates, and the caps (`cycle.md`). Set it from facts in the item and in the code, not from a guess. Read the code that the change touches first.

More than one app can take the change, and the item names none? Then take the app that the start script or the CI of the repository runs. Write the choice and its source in `decisions.md`. Ask only when no script names an app.

## Size

| Size | Signal |
|------|--------|
| Patch | One file. The cause is clear. No change to a contract, the data, or the configuration of another system. Examples: a typo, a config value, a bug with a clear repro in one method. |
| Small | One behaviour, up to about 3 files, one layer. |
| Medium | One feature over more than one layer (for example an endpoint, its logic, and its storage). |
| Large | Count the signals below. Two or more: large. |

When two sizes fit, take the higher one.

### The large signals

Count them from the item and the code. Do not guess.

1. Three or more endpoints, screens, or message handlers are new or change.
2. The schema changes, or a migration must change the data.
3. The item has more than 8 acceptance criteria.
4. The plan has more than 10 files that change.

Write the count in the size line, for example: "Size: large feature. Signals 1 (3 endpoints) and 2 (new columns)."

## Risk

Each of these sets the size to medium or higher. A risk alone does not make the size large:

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
| Bug | Start with a test that fails because of the bug. The fix makes it pass. No `plan.md` below the medium size. |
| Refactor | No new behaviour, so no product question. Run the tests before the first change and after the last change. Both runs must pass. |
| Feature | The steps of the size. |
| Chore or task (package update, configuration, build) | Patch or small, unless a risk applies. No Behaviour spec: the table "## Checks" of `spec.md` is the gate. A package update is a new package when its major version changes. |

## The size line

Tell the human the size in one line, for example: "Size: small bug. One method in `OrderService`, no contract change." When a risk sets the size, name the risk.

## Large with a high risk

When the size is large and a risk applies, offer the team in the size line: "The team of dev-team can build this. Do you want the team?" Start `dev-team:deliver-story` only when the human says yes. Else go on in this session.

## Change the size

Move the size up when the facts show more work or a risk. Tell the human in one line, and do the steps of the new size that you did not do. Move the size down only when the human says so.
