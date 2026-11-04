# Team decisions

Source of truth for defaults in new code. Existing code keeps its style.

| ID | Question | Answer | Status | By | ADR | Check | Date |
|----|----------|--------|--------|----|-----|-------|------|
| FE-01 | UI component library? | None. Plain HTML elements and the CSS of the app. | accepted | human | - | judge | 2026-08-14 |
| FE-02 | State management library? | None (no NgRx). Services with RxJS or signals. | accepted | human | - | judge | 2026-08-14 |
| TEST-01 | Unit test runner? | Vitest through `ng test`. | accepted | human | - | angular.json:test | 2026-08-14 |

A decision that is not in this table has no answer yet. Ask the human before new code depends on it.
