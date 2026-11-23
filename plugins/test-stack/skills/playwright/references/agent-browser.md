# Browser checks by a coding agent

Playwright MCP (`@playwright/mcp` 0.0.86) and the Playwright CLI (`@playwright/cli` 0.1.25, command `playwright-cli`). Both let a coding agent open a page, read its accessibility tree, click, and take a screenshot. The rule numbers refer to `SKILL.md`.

## Contents

- When to use which
- Set up the CLI
- Set up Playwright MCP in Claude Code
- Options
- Security
- Test accounts
- From a look to a test

## When to use which

The decision is PW-01.

| Task | Use |
|------|-----|
| A check that must run again in CI | A Playwright test (`typescript.md` or `dotnet.md` of this skill) |
| A coding agent looks at a page, takes a screenshot, or finds a locator | `playwright-cli` with its skill. Its calls use fewer tokens |
| A long exploratory session with a browser that stays open | Playwright MCP |
| An MCP client without a shell | Playwright MCP |
| A new test from a walk through the app | MCP or the CLI to explore, then a normal test with role locators |

- The README of Playwright MCP recommends the CLI with its skill for a coding agent.
- A project that has Playwright MCP set up keeps it. Do not replace a working setup without a task.
- The CLI is headless by default. The MCP server opens a visible browser by default.

## Set up the CLI

Install a pinned version, then the skill of the CLI:

```sh
npm install -g @playwright/cli@0.1.25
playwright-cli install --skills
```

Playwright 1.62 and later also bundle the CLI and the MCP server in the project: `npx playwright cli` and `npx playwright mcp`. These use the Playwright version of the project, so `package.json` pins the version.

## Set up Playwright MCP in Claude Code

For one person, add the server with a pinned version:

```sh
claude mcp add playwright -- npx @playwright/mcp@0.0.86 --isolated --headless
```

For the team, put the server in `.mcp.json` in the repository:

```json
{
  "mcpServers": {
    "playwright": {
      "command": "npx",
      "args": ["@playwright/mcp@0.0.86", "--isolated", "--headless", "--secrets", ".playwright-mcp.env"]
    }
  }
}
```

- Pin the version (rule 16). The 0.0.x line has about one release each 2 weeks, and `@latest` gives each person another tool set. Update the pin on purpose, and read the release notes.
- `--isolated` keeps the profile in memory. Without it, the server keeps a persistent profile on disk, and one profile can serve one browser only. Parallel clients need `--isolated` or their own `--user-data-dir`.
- Keep the file of `--secrets` out of Git.

## Options

| Option | Effect |
|--------|--------|
| `--headless` | No visible browser |
| `--isolated` | Profile in memory |
| `--storage-state <path>` | Loads cookies and local storage into an isolated context, for example a state file of the setup project |
| `--user-data-dir <path>` | Persistent profile in a known folder |
| `--caps <list>` | Opt-in tool groups: `vision`, `pdf`, `devtools`, `network`, `storage`, `config`, `testing` |
| `--allowed-origins`, `--blocked-origins` | Origin filter. Not a security boundary, and it does not affect redirects |
| `--allowed-hosts` | Hosts that the HTTP transport of the server answers. Default: localhost and loopback |
| `--secrets <path>` | Dotenv file. The server replaces these values in tool output |
| `--redact-url-params <names>` | Replaces tokens in URLs in the output (0.0.84) |
| `--codegen csharp` | Code in tool output in C#. Default TypeScript |
| `--snapshot-mode none`, `--image-responses omit` | Fewer tokens |
| `--allow-unrestricted-file-access` | Off by default: file access stays in the workspace roots. Keep it off |
| `--extension` | Connects to the Chrome or Edge of the user through the Playwright extension |
| `--no-webmcp` | Hides the `webmcp_<tool>` tools of a page |
| `--idle-timeout` | Closes an idle headless browser. Default one hour |

- `--caps=testing` adds `browser_generate_locator` and `browser_verify_*`. `--caps=vision` adds tools that click on coordinates. Use vision only for an element that the accessibility tree does not show.
- `browser_tabs` with `isolatedContext: "<name>"` gives separate sessions in one browser (0.0.84), for example 2 users in one check.
- To turn off the browser sandbox, the environment variable is `PLAYWRIGHT_MCP_SANDBOX=false`. Older README versions named other variables that do nothing. Keep the sandbox on.
- Older versions resolved relative paths in a `--config` file against the working folder of the process, not the config file. Use absolute paths in such a file when the version is older than 0.0.86.

## Security

Playwright MCP is not a security boundary (rule 17). The MCP client decides which actions the agent can do.

- `--allowed-origins` is a filter for convenience. It does not stop redirects, and it does not keep the agent from other sites. In a public test, the task named a company that the model did not know. Agents then called `browser_navigate` on a guessed domain in 96 to 100 percent of trials.
- `--secrets` only replaces the values in the output. The agent can still use the account, and the page can still show the data.
- `browser_run_code_unsafe` runs any JavaScript in the server process. It is equal to remote code execution. Do not allow it for an agent on a page that you do not control (rule 18).
- `--extension` connects to the real browser of the user, with each session and cookie of that user. Use it only when the human asks, on a page that the human names.
- Each page text is input for the model. A page can hold text that tries to steer the agent (prompt injection). Keep the agent on local or test apps that the team controls.
- Use the permissions of the MCP client to allow only the tools that the task needs.

## Test accounts

- Give the agent a test account with test data only. Never a real account, an admin account of production, or a personal account.
- Put the user name and the password in the `--secrets` file or in environment variables, not in `.mcp.json` and not in the prompt.
- Point the agent at a local or test URL. A production URL needs an explicit task from the human.
- Use `--storage-state` with the state file of a setup project. Then the agent does not type a password.

## From a look to a test

1. Explore the flow with the CLI or MCP.
2. Write the test by hand with role locators and web-first assertions (rules 1 to 7). Code from `--codegen` or from a recorded session is a draft only. Review it against rules 1 to 7 before you keep it.
3. Run the test 3 times. Done when it passes 3 times.
