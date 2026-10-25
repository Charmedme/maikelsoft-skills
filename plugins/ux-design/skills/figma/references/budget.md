# Figma MCP call budget

Status on 2026-10-25. Figma can change the limits. Source: the Figma page "Plans, access, and permissions" of the MCP server, as reported on 2026-10-22.

| Seat | Starter | Professional | Organization | Enterprise |
|------|---------|--------------|--------------|------------|
| View, Collab | 20 a month | 20 a month | 20 a month | 20 a month |
| Dev, Full | 20 a month | 200 a day, 10 a minute | 200 a day, 15 a minute | 600 a day, 20 a minute |

- Education plans have the limits of a Dev or Full seat on Professional.
- The limits count the tools that read: `get_metadata`, `get_design_context`, `get_variable_defs`, `get_screenshot`, and the other read tools.
- The tools that write do not count: `use_figma`, `upload_assets`, `generate_figma_design`, `create_new_file`, `add_code_connect_map`. `whoami` does not count. `generate_image` uses Figma AI credits.
- Write to canvas is free in the beta. Figma will make it a paid feature by usage.
- The remote server accepts only the clients in the Figma MCP Catalog. Claude Code is in the catalog.

## Limit for `figmamap.py count`

| Seat in CLAUDE.md | `--limit` |
|-------------------|-----------|
| Dev or Full, Professional or Organization | 180 (keep 20 for the human) |
| Dev or Full, Enterprise | 550 |
| View, Collab, Starter, or unknown | 5, and tell the human that the month has only 20 |
