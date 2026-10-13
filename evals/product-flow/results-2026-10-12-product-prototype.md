# Test results: product-prototype

Date: 2026-10-12. Skill: `product-flow/product-prototype`, versions v1, v2, and v3.

## Method

- 4 requests, 3 runs for each variant, each in a separate agent. Nobody could answer questions.
  - **dashboard:** a prototype of the system status dashboard.
  - **search:** search by name, article number, or brand, with contract prices.
  - **login** (Dutch): log in before you use the app.
  - **branded:** the search box of a web shop. The repository has brand tokens in the DTCG format (`design/brand.tokens.json`), but the request does not mention them.
- A script opened each prototype in a headless browser and clicked each button once. All 33 files opened without script errors and without horizontal scroll at phone width.
- One blind grader for each file (Q1 to Q7).

## Results

| Assertion | Baseline | v1 | v2 (branded) | v3 (search) |
|-----------|----------|----|--------------|-------------|
| `protocheck.py` 0 errors | 0/12 | 12/12 | 3/3 | 3/3 |
| Q1 Shows the one question it answers | 0/12 | 12/12 | 3/3 | 3/3 |
| Q2 Variants or flow steps to compare | 1/12 | 12/12 | 3/3 | 3/3 |
| Q3 Empty state and error state | 0/12 | 12/12 | 3/3 | 3/3 |
| Q4 Marked as a prototype | 10/12 | 12/12 | 3/3 | 3/3 |
| Q5 Fake data that looks fake | 4/12 | 12/12 | 3/3 | 3/3 |
| Q6 No functions that nobody asked for | 0/12 | 4/12 | 0/3 | 3/3 |
| Q7 Uses the brand tokens (branded only) | 3/3 | 0/3 | 3/3 | - |
| Asks "Is this what you mean?" with options | 0/12 | 12/12 | 3/3 | 3/3 |

The last row comes from the replies of the agents, not from the grader.

## Triggers

10 queries (5 must trigger, 5 must not), with 15 skills to select from, among them `mattpocock-skills:prototype` and `frontend-design`: 10/10.

## Changes

- **v1 to v2.** In v1, the skill used low fidelity as the default, because the research recommends it, and only one brand colour. The original request of the product owner asks for a prototype in the existing branding and designs. v2 uses all design sources that it finds. The banner and the fake data show that the page is not final. The plain style is only for when there is no design source.
- **v2 to v3.** v1 and v2 added functions that nobody asked for. Examples: a cart, account links, filters, and history. v3 adds a "Scope" section to `build.md`. The agent builds only what the question needs. It asks about other functions in the reply. Tested on the search request: Q6 went from 0/3 to 3/3.
