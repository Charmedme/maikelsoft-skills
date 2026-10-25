# The "## Design" section

## Template

```markdown
## Design
- Design tools: <Claude Design (system "<name>"), Figma (<plan>, <seat> seat), OpenPencil, none>
- Mockup tool: <claude-design | figma | openpencil | html>
- Design-system source: <the master first: a Claude Design System URL, a DTCG tokens file, a CSS tokens file, a Figma library link, a Storybook URL>
- Figma files: <links, or none>
- Brand files: <DESIGN.md, PRODUCT.md, or none>
- UI stack: <read from the project>
- Languages: <codes, with the form of address: nl (je), de (Sie), en>
- Run the app: <command>, <base URL>
- Playwright: <Python | Node | not installed>
```

## How to fill each line

| Line | Fact from the scan | Ask when |
|------|--------------------|----------|
| Design tools | `plugins`, `openpencil.cli`, `figma_links`, the Design Systems of the account | Always confirm. A scan cannot see a Figma plan or a seat. |
| Mockup tool | The rule below | The design tools have more than one candidate. |
| Design-system source | `tokens`, `tailwind`, `storybook`, the Design Systems | More than one source, and none is clearly the master. |
| Figma files | `figma_links` | The links are old, or there are more than 3. |
| Brand files | `brand_files` | None found: offer the `brand-rules` skill. |
| UI stack | `ui_stack` | Nothing found. |
| Languages | `languages` | Always ask for the form of address in Dutch and German. |
| Run the app | `run` | More than one script, or none. Ask for the base URL. |
| Playwright | `playwright` | Not installed: give the install command. |

## Mockup tool rule

1. The product has a Claude Design System, and the session can publish artifacts: `claude-design`.
2. Else the team has Figma with a Dev or Full seat: `figma`. With a View or Collab seat (20 read calls a month), recommend `html`, and use Figma only to read.
3. Else the team uses OpenPencil: `openpencil`.
4. Else `html`.

A headless run (`claude -p`, CI) cannot publish artifacts. In that run, the agent makes HTML mockups and says so.

## Install commands

| Tool | Command |
|------|---------|
| Figma plugin (MCP server and skills) | `claude plugin install figma@claude-plugins-official`, then `/plugin`, select `figma`, and sign in |
| Figma MCP server only | `claude mcp add --transport http figma https://mcp.figma.com/mcp`, then `/mcp` and Authenticate |
| OpenPencil CLI | `brew install zseven-w/openpencil/op` (macOS), or the install script in the OpenPencil README |
| OpenPencil skill | `/plugin marketplace add zseven-w/openpencil-skill`, then `/plugin install openpencil-skill@openpencil-skill` |
| Claude Design | Needs a Pro, Max, Team, or Enterprise plan and a claude.ai login (`/login`). Not on Bedrock, Vertex, or Foundry. |
| Playwright (Python) | `pip install playwright`, then `python3 -m playwright install chromium` |
| axe-core | `npm install axe-core` |
