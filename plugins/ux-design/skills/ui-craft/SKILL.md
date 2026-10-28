---
name: ui-craft
description: Visual design method for app screens - surface type, hierarchy, grid, type roles, spacing rhythm, color roles, density, and detail, in a fixed order, with a rubric to score concepts and mockups and a list of AI tells. Use when a screen, a concept, or a mockup must be designed or judged on its visual quality, or when UI output looks generic, cluttered, or inconsistent.
---

# UI craft

Good visual design for an app is not decoration. It makes the main task the first thing that the user sees. It uses the design system, so each screen feels like the same product. This skill gives a fixed order of decisions and a rubric, so the same input gives the same result.

The design system wins. When `DESIGN.md`, the tokens, or the components give a value, use it. This skill fills only what the system does not decide, and each such choice becomes a `proposed` decision.

## Steps

0. **Check the design system.** When the project has no design system, read `references/foundation.md` and make the minimal foundation that it describes. When the project has one, read `references/system-audit.md` and audit it. Done when the foundation passes `uxcheck.py system`, or each fault of the system has a row in the issues file.
1. **Set the surface.** Select one: `operate` (an app screen where users do work: lists, forms, dashboards), `read` (long text), or `persuade` (marketing). Most product screens are `operate`. On an `operate` screen, the look comes from the system. Your work is hierarchy, order, and density, not a new style. Done when the surface is in `ux-design.md`.
2. **Make the concepts.** In full mode, make 2 or 3 concepts. They differ in structure: the layout, the order of information, or the main action. Describe each in 2 or 3 lines. In new-direction mode, start from the chosen style direction. At least one concept changes the color, the type, and the shape of the main element. Done when the concepts differ in structure, not only in color or spacing.
3. **Score the concepts.** Use the option rubric (`references/rubrics.md`). When `ux-design.md` has `Mode: new-direction`, use the new-direction variant: it adds Difference and gives less weight to Consistency and Cost. The standard rubric rewards the existing look, so it fails a request for something new. Select the highest total. On a tie, select the one with the fewest new components. Write the scores and the reason in the Options table. Done when the Options table has scores and a "Selected:" line. In new-direction mode, it also has a Difference column with at least one 3.
4. **Decide in order.** For the selected concept, read `references/method.md`. Decide in the order of that file, from hierarchy to detail. Each decision goes into the design spec with its source. Done when the spec rows `layout`, `hierarchy`, `components`, `density`, and `motion` have a value and a source.
5. **Critique the mockups.** After the mockups exist, render them and score each frame with the mockup rubric. Then do the set-level pass: one score for the whole set of screens, with the Element inventory of `ux-design.md`. Fix each score of 1. Max 2 loops. Done when no criterion and no set score is 1, or you listed the open items for the human.
6. **Run the tell check.** Run `uxcheck.py mockup` (in the `ux-practice` skill) on the mockups. Read `references/tells.md` for the tells that need judgment. Done when the checker shows 0 errors.

## Files

| File | Read when |
|------|-----------|
| `references/foundation.md` | Step 0, when the project has no design system. |
| `references/system-audit.md` | Step 0, when the project has a design system. |
| `references/method.md` | Step 4: the rules for each decision in the order. |
| `references/rubrics.md` | Steps 3 and 5: the option rubric with its new-direction variant, and the mockup rubric with its set-level pass. |
| `references/tells.md` | Step 6: the list of AI tells for app screens. |
| `references/language.md` | Before you write text for the human. |
