# Delivery suite

This suite compares delivery flows on stories from easy to very hard. The stories run on pinned real projects. Hidden tests decide whether a story works. The run never sees these tests.

## Sets

| Set | Project | Use |
|-----|---------|-----|
| `conduit` | RealWorld API on ASP.NET Core 10 and SQLite (MIT) | Known solutions. The upstream code and the upstream API tests are the check. |
| `dashboard` | Practical.CleanArchitecture Monolith on .NET 10, PostgreSQL, and Angular 22 (MIT) | The main set: an admin dashboard. |

`fixtures.json` pins each project to one commit. The file `fixtures/dashboard-headless.patch` makes the dashboard run without an identity server and without cloud services.

## Story folder

| File | Seen by the run | Purpose |
|------|-----------------|---------|
| `story.md` | yes | The item |
| `ticket.md` | yes, only in the arms `buildspec` and `flowspec`, in the place of `story.md` | The item with a reviewed Behaviour spec and the status `spec:ready` |
| `answers.md` | yes | The answers of the product owner |
| `meta.json` | no | Size, risk, difficulty, kinds, and the trap |
| `base.patch` | yes, as the start state | Removes a feature or adds project files |
| `reference.patch` | no | The known solution |
| `hidden/` | no | The tests, in four layers |

## Four layers

1. The test cases, in the words of the domain (`hidden/test_*.py`).
2. The DSL (`acceptance/dsl/`).
3. The protocol drivers for HTTP and the browser (`acceptance/drivers/`).
4. The system under test, which `suite.py` starts.

## Commands

```sh
python3 evals/delivery/suite.py setup conduit dashboard
python3 evals/delivery/suite.py verify                   # base fails, reference passes
python3 evals/delivery/suite.py run <runs> --arm baseline --story rw-title-trim
python3 evals/delivery/suite.py score <runs>
```

A story is valid only when its hidden tests fail on the base and pass on the reference. A hard story stays in the suite only when a plain session fails at least one hidden test in a calibration run. An easy story stays as a control for the cost of each flow.
