# Split a story

## Contents

- Before you split
- Patterns
- Select a split

## Before you split

1. Check the story against the rules in `story-rules.md`, except size. A story without user value is a task: combine it with a story, do not split it.
2. Find the core of the story: the part that makes it hard or large.

## Patterns

Try the patterns in this order. Use the first one that gives stories of almost equal size, each with value for the user.

| No | Pattern | Example |
|----|---------|---------|
| P1 | Steps of the workflow | First "submit an order", later "change a submitted order". |
| P2 | Operations (create, read, update, delete) | "See my addresses" before "Delete an address". |
| P3 | Variations of a business rule | One story for each discount type. |
| P4 | Variations of data | Dutch addresses first, then foreign addresses. |
| P5 | Ways to enter data | Type the date first, a date picker later. |
| P6 | Simple case first | The normal path first, the edge cases in later stories. |
| P7 | Interfaces | Web first, the mobile app later. |
| P8 | Defer quality | A working version first, the faster version later. Keep the quality target in the later story. |
| P9 | Spike | The team cannot estimate: a spike story first. |

## Select a split

- Select the split that lets the product owner drop a low-value part.
- Each new story is vertical: a user can see the result.
- Each new story has the same parent as the original story.
- In the report, name the pattern that you used.
