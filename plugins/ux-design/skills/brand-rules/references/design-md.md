# DESIGN.md

The sections follow the order of the DESIGN.md format of Google Labs (alpha), so that tools that read that format find each part. The tokens stay in the DTCG file. `DESIGN.md` names them and does not copy their values.

## Sections

| Section | What it holds |
|---------|---------------|
| `# <Product> design system` | One paragraph: the kind of product, who uses it, and how often. Where the tokens and the components are. |
| `## Overview` | 3 to 5 principles. Each principle has a reason and an example. |
| `## Colors` | The role of each semantic color token. What each color is not for. The contrast pairs. |
| `## Typography` | The families, the type scale (the typography tokens), and where each style goes. |
| `## Layout` | The grid, the spacing scale, the density of each kind of screen, and the widths (390, 820, 1440). |
| `## Elevation & Depth` | The shadow tokens and where each is used. The rule for nested surfaces. |
| `## Shapes` | The radius tokens and where each is used. |
| `## Components` | A table: component, class or name, use. One primary action for each view. |
| `## Do's and Don'ts` | Hard rules first, each marked `hard`, then the soft rules marked `soft`. |

## Example rule lines

```markdown
- hard: Teal (`color.accent`) is only for the primary action, the selection, and the focus.
- hard: Text uses `color.text` or `color.text-muted`. Never a raw hex.
- soft: Empty states say what to do next, not only what is missing.
```
