# Walk file

The walk file tells `uxcheck.py review` how to reach each state of the built app. It is JSON.

```json
{
  "base_url": "http://localhost:8123",
  "flows": [
    {
      "id": "work-orders",
      "routes": [],
      "states": [
        { "state": "default", "steps": [{ "goto": "/" }, { "wait": "[data-testid=wo-row]" }] },
        { "state": "loading", "routes": [{ "url": "**/api/work-orders", "hold": true }],
          "steps": [{ "goto": "/", "no_wait": true }, { "wait_ms": 300 }] },
        { "state": "error-save", "routes": [{ "url": "**/api/assignments", "method": "POST", "status": 500, "body": "{}" }],
          "steps": [{ "goto": "/" }, { "check": "[data-testid=wo-check]" }, { "click": "[data-testid=assign]" }] }
      ]
    }
  ]
}
```

## Steps

| Step | Value | Job |
|------|-------|-----|
| `goto` | path | Open the path on `base_url`. |
| `click` | selector | Click. Add `"no_wait": true` when the click starts a request that does not end. |
| `fill` | [selector, text] | Type the text into the input. |
| `select` | [selector, value] | Select an option. |
| `check` | selector | Check a checkbox. |
| `press` | [selector, key] | Press a key, for example `Enter` or `Tab`. |
| `wait` | selector | Wait until the element exists. |
| `wait_ms` | number | Wait. Use it only for a loading state. |
| `expect_text` | [selector, text] | Fail when the element does not show the text. |

## Routes

A route gives a fake answer of the server for the requests that match `url` (a glob). The routes of the flow apply to each state. The routes of a state apply to that state only.

| Key | Job |
|-----|-----|
| `url` | The glob, for example `**/api/assignments`. |
| `method` | Only requests with this method, for example `POST`. |
| `status`, `body`, `content_type` | The fake answer. |
| `hold` | The request never ends. Use it for `loading`. |
| `abort` | The request fails as a network error. Use it for `offline` and `error-load`. |

## Make each state

| State | How |
|-------|-----|
| `default` | No route. Go to the start of the main task. |
| `loading` | `hold` on the request that loads the data. |
| `empty` | Read the "User sees" line of the design first. "No results for the filters" needs filter steps. "No data at all" needs a `body` with an empty list. These are different states. |
| `error-load` | `status` 500 on the load request, or `abort`. |
| `error-save` | `status` 500 on the save request, then the steps that save. |
| `partial`, `bulk-result` | A `body` with some items that failed, then the steps of the bulk action. |
| `conflict` | `status` 409 on the save request. |
| `session-expired` | `status` 401 on the next request. |
| `no-permission` | `status` 403, or the data of a role without the right. |
| `overflow` | A `body` with long names and many items. |

## Check each state against the design

End the steps of each state with one or more `expect_text` steps. Take the text from the states table and the copy table of `ux-design.md`. Check the message, the counts, and the label of each recovery action, for example "Try again".

Use a selector inside the element of the state, for example the alert or the empty block. A control that is always on the page proves nothing about the state. A missing text or a missing recovery action then fails as `RV-STEP`. That failure is a finding when the walk is correct.

When a state cannot be made with routes and steps, write `not reachable: <reason>` in the scope table of `ux-review.md`. Do not leave it out without a word.
