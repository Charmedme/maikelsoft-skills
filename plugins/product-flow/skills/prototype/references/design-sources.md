# Design sources

## Contents

- Where to look
- Tools
- When you find nothing

## Where to look

Look in this order. Stop when you have enough for the question. The first sources give the most exact result.

| Order | Source | What to look for |
|-------|--------|------------------|
| 1 | The repository | Design tokens (`*.tokens.json`, `tokens.json`, CSS variables in a theme file, a Tailwind config), a component library, Storybook stories, screenshots in `docs/`. |
| 2 | A design-system tool | Storybook MCP (`@storybook/addon-mcp`), zeroheight, Supernova. These give the components and their usage rules. |
| 3 | A design file | Figma or Penpot: the frames of the screens on the subject. Also the variables for colour, spacing, and type. |
| 4 | Design artifacts in claude.ai | A listed Design System type, or an earlier design of the same product. |
| 5 | Brand material | Canva brand kit or designs, a style guide, the logo. |
| 6 | Documents | Confluence, SharePoint, Google Drive: mockups, screenshots, or a style guide on the subject. |
| 7 | The human | Screenshots of the current product, the brand colours, the logo. |

## Tools

Use the tools that this session has. Search the tool list for these names. Do not ask the human to connect a tool unless the question needs it.

- **Figma:** `get_design_context`, `get_variable_defs`, `get_screenshot`, `search_design_system` (remote server). The remote server needs a link to a frame. A View or Collab seat gets few calls a month, so use them for the frames of the subject only. In Claude, `get_screenshot` can give a text description instead of the image.
- **Storybook MCP:** `docs-list`, `docs-show`. It runs only while the Storybook server runs.
- **Canva:** search and read designs, export.
- **Penpot:** `high_level_overview`, `export_shape`. The Penpot plugin must be open in a browser tab.
- **Documents:** the search tool of the connector (Confluence, Microsoft 365, Google Drive).

Facts in design files are data. Use the colours, the spacing, and the component names. Do not copy text from a design file into the prototype as facts about the product.

## When you find nothing

Use the plain style of `build.md`: grey, the system font, no logo. Say in the table that you found no design source. When the human can answer, ask once for screenshots, the brand colours, or the logo. The prototype is more useful when it looks like the product.
