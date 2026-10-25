# PRODUCT.md

`PRODUCT.md` holds the facts of the product that the design uses: the users, the domain words, and the voice. The design skills tag a decision `product: <fact>` when it comes from this file.

## Sections

| Section | What it holds |
|---------|---------------|
| `# <Product>` | One paragraph: what the product does, for whom. |
| `## Users` | Each user group by behavior: the task, how often, where (desk, phone, field), and the skill level. No invented personas. A gap is `[NEEDS EVIDENCE]`. |
| `## Main tasks` | The 3 to 7 tasks that matter most, with their frequency. |
| `## Domain` | The objects of the domain, their statuses, and the business rules that the UI shows. |
| `## Words` | The Words table (below). |
| `## Voice` | 3 to 5 voice attributes. Each one has "means" and "does not mean". |
| `## Languages` | The languages, with the form of address (Dutch `je` or `u`, German `du` or `Sie`). The formats of dates, numbers, and money. |
| `## Constraints` | Devices, browsers, accessibility target (WCAG 2.2 AA), and laws. |

## The Words table

`brandcheck.py copy` reads this table. It needs a column with "Do not use" in its name, and a column with "Use".

```markdown
## Words

| Do not use | Use | Why |
|------------|-----|-----|
| ticket, job | work order | The domain word of the planners. |
| engineer, mechanic | technician | One term for one person. |
| Submit | Assign | The button names the action. |
```

One cell can hold more than one word, separated by commas. The check ignores case and skips text in code.
