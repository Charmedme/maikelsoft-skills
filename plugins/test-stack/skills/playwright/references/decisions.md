# Playwright decisions

The team decisions that end-to-end tests and browser checks need. The owner approved PW-01 to PW-04 as catalog defaults (P3-01 to P3-04). PW-05 and PW-06 are recommendations. Ask the human before you apply a recommendation that has no accepted decision (stack rule 4).

## Contents

- PW-01. Browser look by a coding agent
- PW-02. .NET adapter
- PW-03. App under test in a .NET test
- PW-04. Playwright test agents
- PW-05. Sign-in in tests
- PW-06. Browsers and retries in CI

## PW-01. Browser look by a coding agent

- Question: Does a coding agent use Playwright MCP or `playwright-cli` to look at a page?
- Ask when: a new agent setup, or a request to add Playwright MCP.
- Options: `playwright-cli` with its skill: a shell command for each step, fewer tokens, headless by default. Playwright MCP: a browser that stays open, tools in the MCP client, visible by default. Both.
- Recommended: A project that has MCP set up keeps it. In a new setup, `playwright-cli` is the default for a short check, as the MCP README advises. MCP for a long exploratory session, or a client without a shell. Pin the version in each case.
- Choose another option when: the agent runs in a client that has no shell. Then MCP.
- Depends on: none.
- Revisit when: the CLI or the MCP server leaves the 0.x line.
- Check: tool (PW-AGENT-UNPINNED), judge.
- Detect: DET-PW-01.

## PW-02. .NET adapter

- Question: Which Playwright adapter does a .NET test project use?
- Ask when: a new .NET end-to-end test project, or an upgrade of the test framework.
- Options: `Microsoft.Playwright.MSTest.v4` with `PageTest` (MSTest 4). `Microsoft.Playwright.Xunit.v3` (xUnit v3). `Microsoft.Playwright.NUnit`. No adapter and an own browser lifecycle.
- Recommended: The adapter of the test framework of the repository: `.MSTest.v4` for MSTest 4, `.Xunit.v3` for xUnit v3. MSTest 4 is the default framework for a new repository (TEST-01 in `test-stack:dotnet-testing`).
- Choose another option when: the repository uses NUnit. Then `Microsoft.Playwright.NUnit`. Never an own lifecycle: the base classes give each test a new context and page.
- Depends on: TEST-01.
- Revisit when: MSTest 5 ships, or the Playwright docs name the `.v4` package.
- Check: tool (PW-MSTEST-ADAPTER), judge.
- Detect: DET-PW-02.

## PW-03. App under test in a .NET test

- Question: How does a .NET end-to-end test start the app that the browser opens?
- Ask when: a new .NET end-to-end test project.
- Options: `WebApplicationFactory` with `UseKestrel(0)` and `StartServer()` (.NET 10 and later). `Aspire.Hosting.Testing` with the AppHost. A deployed URL. An own Kestrel host in `CreateHost`.
- Recommended: `WebApplicationFactory` with `UseKestrel(0)` and `StartServer()` for an app in the same repository. The test can replace services, and the port is free. `Aspire.Hosting.Testing` when the repository has an AppHost (`dev-stack:aspire`). A deployed URL only for a smoke test.
- Choose another option when: the frontend is a separate app (for example an Angular dev server) that the .NET host does not serve. Then start it as in the TypeScript guide, or through the AppHost.
- Depends on: PW-02.
- Revisit when: the app gets an Aspire AppHost.
- Check: judge.
- Detect: DET-PW-03.

## PW-04. Playwright test agents

- Question: Does the project use the Playwright test agents (planner, generator, healer)?
- Ask when: a request to run `npx playwright init-agents`, or a healer change in a pull request.
- Options: No agents. The agents for new tests only (planner and generator). All 3 agents.
- Recommended: Not by default. When the team uses them, classify each red test before a change (app defect, test defect, or flaky test). A real regression stays red. Never accept a skip or `test.fixme()` from the healer without a review.
- Choose another option when: the team wants a first draft of many tests from a plan. Then the planner and the generator, with a human review of each test.
- Depends on: none.
- Revisit when: the healer stops to mark tests as skipped, or a new Playwright version changes the agents.
- Check: judge.
- Detect: none.

## PW-05. Sign-in in tests

- Question: How do end-to-end tests sign in?
- Ask when: a new suite, or a new role in the app.
- Options: A setup project saves `storageState` once, and each test loads it. Each test signs in through the UI. Each test gets a token from the API and sets the cookie.
- Recommended: A setup project with one state file for each role, from a test account. One sign-in through the UI tests the sign-in page. The other tests start signed in, so they are faster and do not depend on the sign-in page.
- Choose another option when: the session ends in a short time, or each test needs another user. Then a fixture that signs in through the API for each worker.
- Depends on: none.
- Revisit when: the sign-in method changes (for example passkeys).
- Check: judge.
- Detect: none.

## PW-06. Browsers and retries in CI

- Question: Which browsers run in CI, and does CI retry a failed test?
- Ask when: a new CI job for end-to-end tests.
- Options: Chromium only. Chromium on each pull request and all browsers each night. All browsers on each pull request. Retries 0, 1, or 2.
- Recommended: Chromium on each pull request. Firefox and WebKit in a nightly run when the users have them. 2 retries in CI with `trace: 'on-first-retry'`. Each test that passed only on a retry is a flaky test and gets a fix.
- Choose another option when: the product supports Safari users, or a defect showed in one browser only. Then add that browser to the pull request run.
- Depends on: none.
- Revisit when: the CI time is too long, or flaky tests stay.
- Check: judge.
- Detect: none.
