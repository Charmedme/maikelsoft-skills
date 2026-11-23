# Playwright Test in TypeScript

Playwright Test 1.65 (`@playwright/test`). Use Node.js 22, 24, or 26. The rule numbers refer to `SKILL.md`.

## Contents

- Config with webServer
- Sign-in with a setup project
- Fixtures and page objects
- Locators
- Assertions
- Test data
- Aria snapshots
- Clock
- Route mocks
- Traces and flaky tests
- CI and shards
- Component tests
- Test agents

## Config with webServer

This config starts the app, signs in once, and records a trace on the first retry:

```ts
import { defineConfig, devices } from '@playwright/test';

const baseURL = process.env.BASE_URL ?? 'http://localhost:4200';

export default defineConfig({
  testDir: './e2e',
  fullyParallel: true,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 2 : 0,
  reporter: process.env.CI ? [['html', { open: 'never' }], ['junit', { outputFile: 'test-results/junit.xml' }]] : 'html',
  use: {
    baseURL,
    trace: 'on-first-retry',
  },
  projects: [
    { name: 'setup', testMatch: /.*\.setup\.ts/ },
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'], storageState: 'playwright/.auth/user.json' },
      dependencies: ['setup'],
    },
  ],
  webServer: {
    command: 'npm run start',
    url: baseURL,
    reuseExistingServer: !process.env.CI,
  },
});
```

- `webServer` takes `url` or `port`, not both. With both, the runner stops at start with an error. Prefer `url`: the runner waits until the URL answers.
- `reuseExistingServer: !process.env.CI` uses a running dev server on the machine of the developer, and a fresh server in CI.
- `forbidOnly` stops a CI run that has a `test.only` in it.
- A project with `default: false` stays in the config but is not in a plain `npx playwright test` (1.64). Run all projects with `--project="*"`.

## Sign-in with a setup project

The setup project signs in once and saves the browser state. Each browser project loads it through `storageState` and `dependencies: ['setup']`.

```ts
// e2e/auth.setup.ts
import { test as setup, expect } from '@playwright/test';

const authFile = 'playwright/.auth/user.json';

setup('sign in as a clerk', async ({ page }) => {
  await page.goto('/login');
  await page.getByLabel('Email').fill(process.env.E2E_USER!);
  await page.getByLabel('Password').fill(process.env.E2E_PASSWORD!);
  await page.getByRole('button', { name: 'Sign in' }).click();
  await expect(page.getByRole('heading', { name: 'Orders' })).toBeVisible();
  await page.context().storageState({ path: authFile });
});
```

- Add `playwright/.auth` to `.gitignore`. The file holds live session cookies.
- The user name and the password come from environment variables of a test account. Never put them in the code.
- For more roles, write one setup test and one state file for each role (PW-05). A test that needs another role sets `test.use({ storageState: 'playwright/.auth/admin.json' })`.
- A test that must start without a session sets `test.use({ storageState: { cookies: [], origins: [] } })`.
- Storage state can include passkeys (`credentials`, 1.62) and OPFS (`opfs`, 1.63).

## Fixtures and page objects

Give each test its page objects through a fixture. Then the test body has no setup code.

```ts
// e2e/fixtures.ts
import { test as base, type Locator, type Page } from '@playwright/test';

export class OrdersPage {
  readonly rows: Locator;
  readonly newOrder: Locator;

  constructor(private readonly page: Page) {
    this.rows = page.getByRole('row');
    this.newOrder = page.getByRole('button', { name: 'New order' });
  }

  async goto() {
    await this.page.goto('/orders');
  }

  row(orderNumber: string) {
    return this.rows.filter({ hasText: orderNumber });
  }
}

export const test = base.extend<{ ordersPage: OrdersPage }>({
  ordersPage: async ({ page }, use) => {
    await use(new OrdersPage(page));
  },
});
export { expect } from '@playwright/test';
```

Since 1.65, a class field initializer that reads a constructor parameter property fails at run time (`useDefineForClassFields`). This form breaks:

```ts
// Wrong since 1.65
constructor(private readonly page: Page) {}
readonly rows = this.page.getByRole('row');
```

Set each locator field in the constructor, as in the `OrdersPage` example.

## Locators

| Need | Use |
|------|-----|
| A button, a link, a heading, a cell, or a row | `page.getByRole('button', { name: 'Save' })` |
| A form field | `page.getByLabel('Email')` |
| A text on the page | `page.getByText('Order saved')` |
| An element with no role or name | `page.getByTestId('order-total')` |
| One row of a list | `page.getByRole('row').filter({ hasText: orderNumber })` |
| An element inside another | `dialog.getByRole('button', { name: 'Cancel' })`, or `locator.within(parent)` (1.64) |
| Only the visible matches | `page.getByRole('button').visible()` (1.63). Not the CSS `:visible` |
| A role with a description | `getByRole('button', { name: 'Delete', description: 'Order 10045' })` (1.60) |

- A locator that matches more than one element fails in an action (strict mode). Fix it with a better filter, not with `.first()`. `.first()` takes the first match, and that can be data of another test.
- Use `getByTestId` only where role, label, and text do not work. A test ID on each element tests nothing that a user sees.

## Assertions

```ts
await expect(ordersPage.row(orderNumber)).toBeVisible();
await expect(ordersPage.row(orderNumber).getByRole('cell', { name: 'Paid' })).toBeVisible();
await expect(page).toHaveURL(/\/orders\/\d+$/);
await expect(page.getByRole('row')).toHaveCount(3);
```

- Each `expect` on a locator waits and retries until the timeout. `isVisible()`, `textContent()`, and `count()` read once and do not wait.
- `expect.soft(...)` continues the test after a failure, and reports all failures at the end. `expect.soft.poll` (1.61) polls a function.
- `expect.poll(() => fn())` waits for a value that is not on the page, for example a status from the API.

## Test data

- Make the data of each test in the test, through the API. Use the `request` fixture or a fixture that calls the API:

  ```ts
  test('a paid order shows in the list', async ({ request, ordersPage }) => {
    const orderNumber = `E2E-${Date.now()}-${test.info().parallelIndex}`;
    const response = await request.post('/api/orders', { data: { orderNumber, status: 'Paid' } });
    expect(response.ok()).toBeTruthy();

    await ordersPage.goto();
    await expect(ordersPage.row(orderNumber)).toBeVisible();
  });
  ```

- Give each record a unique value and filter on it. Then data of earlier runs or of parallel tests does not change the result.
- `params` in `APIRequestContext` adds to the query of the URL since 1.65. It does not replace it.
- Use test locks when tests share one resource that you cannot split: `{ lock: 'name' }` on a test, or `test.describe.configure({ lock: 'name' })`. `lock: '-exclusive-'` runs the test alone (1.63 to 1.65).
- `--shuffle [seed]` (1.64) finds tests that depend on each other.

## Aria snapshots

An aria snapshot checks the accessibility tree of a part of the page. It finds a missing name or a wrong role.

```ts
await expect(page.getByRole('navigation')).toMatchAriaSnapshot(`
  - link "Orders"
  - link "Customers"
`);
```

- `toMatchAriaSnapshot` works on a locator, and on a page since 1.60.
- Since 1.65, text that the browser does not render is not in the accessible name. Update old snapshots after the upgrade.

## Clock

Control the time in the app with the clock. Do not wait for real time.

```ts
await page.clock.install({ time: new Date('2026-03-01T08:00:00') });
await page.goto('/orders');
await page.clock.runFor('10:00'); // 10 minutes later: the session warning shows
await expect(page.getByRole('alertdialog', { name: 'Session ends soon' })).toBeVisible();
```

- `page.clock.setFixedTime(date)` keeps `Date.now()` fixed, and timers run.
- `page.clock.pauseAt(date)` stops the timers at a time. `runFor` moves the time forward.

## Route mocks

Mock an API only for a state that you cannot make with real data, for example a server error.

```ts
await page.route('**/api/orders', route => route.fulfill({ status: 503, json: { title: 'Service unavailable' } }));
await page.goto('/orders');
await expect(page.getByRole('alert')).toHaveText(/try again/i);
```

- `page.routeFromHAR(file)` plays back a recorded HAR file.
- `context.tracing.startHar()` with `await using` records a HAR file (1.60). HAR files and traces include WebSocket traffic (1.61).
- A test with a mocked API does not prove that the frontend and the backend work together. Keep at least one test for each flow against the real API.

## Traces and flaky tests

- `trace: 'on-first-retry'` records a trace when a test fails the first time. `retain-on-failure-and-retries` keeps the trace of each failed attempt. Video has the same modes since 1.61.
- Open a trace with `npx playwright show-trace <trace.zip>`.
- `retries` with `retryStrategy: 'isolated'` (1.62) runs a retry in a new worker.
- `--fail-on-flaky-tests` fails the run when a test passed only on a retry. Use it in a nightly run to find flaky tests.
- The `failures` reporter and `onlyFailures` for the HTML, JSON, and JUnit reporters (1.65) show only the failed tests.

## CI and shards

A GitHub Actions job for one shard:

```yaml
jobs:
  e2e:
    strategy:
      fail-fast: false
      matrix:
        shard: [1/4, 2/4, 3/4, 4/4]
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v5
      - uses: actions/setup-node@v5
        with:
          node-version: 22
      - run: npm ci
      - run: npx playwright install --with-deps chromium
      - run: npx playwright test --shard=${{ matrix.shard }} --reporter=blob
        env:
          E2E_USER: ${{ secrets.E2E_USER }}
          E2E_PASSWORD: ${{ secrets.E2E_PASSWORD }}
      - uses: actions/upload-artifact@v4
        if: ${{ !cancelled() }}
        with:
          name: blob-report-${{ strategy.job-index }}
          path: blob-report
```

- A next job downloads all `blob-report-*` artifacts and runs `npx playwright merge-reports --reporter html ./all-blob-reports`.
- Since 1.65, a shard without tests fails. Add `--pass-with-no-tests` when a shard can be empty.
- Install only the browsers of the projects that CI runs (PW-06).
- `bunx --bun playwright test` runs under Bun (1.65). Without `--bun` it runs on Node.js.
- Microsoft Playwright Testing (`@azure/microsoft-playwright-testing`) retired on 2026-03-08. The new service is Playwright Workspaces in Azure App Testing (`npm init @azure/playwright@latest`).

## Component tests

- Since 1.62, component tests use stories and galleries with a `mount(storyId)` fixture. The docs have a migration guide.
- `@playwright/experimental-ct-react`, `-react17`, and `-vue` get no updates since 1.63. Do not start new component tests on them.
- Angular component tests are not Playwright tests. Use `frontend-stack:angular`.

## Test agents

`npx playwright init-agents --loop=claude` writes 3 agent definitions:

- planner: walks the app and writes a Markdown plan in `specs/`.
- generator: writes tests from the plan.
- healer: runs the failing tests and repairs them.

The rules for the agents (PW-04):

1. Do not add the agents by default. Ask the human first.
2. Classify each red test before a change. The classes are: a defect in the app, a defect in the test, or a flaky test. A real regression stays red, and the report names it.
3. The healer can mark a test as skipped or `test.fixme()` when it decides that the app has a defect. Do not accept a skip or a `fixme` without a review by a human, with the reason in the commit.
4. Run `init-agents` again after each Playwright update. The agent files change with the version.
5. Review generated tests against rules 1 to 7 of `SKILL.md`, as each other test.

- A maintainer wrote in 2026-10 that an agent that fixes the tests directly is now often better than the healer.
- `npx playwright test --debug=cli` (1.59) lets an agent debug a test through `playwright-cli`.
- `page.ariaSnapshot({ mode: 'ai' })` gives refs such as `e2`, and `page.getByRef('e2')` turns a ref into a locator (1.64). Use these in agent tools only. A test uses role locators.
