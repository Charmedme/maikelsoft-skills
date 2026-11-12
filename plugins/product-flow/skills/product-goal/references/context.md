# Context step

Do this step first, before the first step of the command. It gives you the role of the person, the route, and the facts of the project. Ask only for a fact that the command needs and that you cannot find.

## 1. Read the role line

Look for the role in this order. Stop at the first one that you find.

1. A line in the session context that starts with `Delivery role:`. The session-start hook of dev-setup prints it.
2. The environment variable `DELIVERY_ROLE` (a cloud session). Read it with `printenv DELIVERY_ROLE`.
3. The instructions of the claude.ai Project. They have the same `Delivery role:` line.

The line has this form:

```text
Delivery role: developer. Scope: project. Route: team=ask, docs=on, refine=on, approve-at-ready=off.
```

- **Role**: `product owner`, `designer`, `developer`, `tech lead`, or `all`.
- **Scope**: `project`, `global`, or `cloud`. It tells where the role comes from.
- **Route**: the steps that run, each `on`, `off`, or `ask`. Use the value of a step that this command knows. Ignore a step that this command does not know.

The role sets defaults: the next command and the steps that run. It never stops a command. A developer can write a story, and a product owner can start a build.

When you find no role, use `all`, and go on. Say it in one line of the report: "I found no role. I used the defaults for one person who does all steps."

## 2. Read the facts of the project

Read `.claude/rules/delivery.md` at the root of the repository. It has one section for each plugin, for example `## Tracker`, `## Backlog`, `## Design`, `## Review`. Read only the sections that this command needs.

Read `REVIEW.md` at the root of the repository only for a review. The review rules (`review-rules.md`) say how.

## 3. When a fact is missing

1. Look for the fact in the repository first. Read `CLAUDE.md`, the project files, and the remote URL of git. Also read the earlier work in `docs/work/` and `docs/backlog/`.
2. Ask the person only for the facts that this command needs now. Put all questions in one message. Give each question a recommended answer.
3. Write each answer in its section of `.claude/rules/delivery.md`, so the next command does not ask again. In claude.ai, give the line for the Project instructions, because you cannot write that file.
4. Say once: "`/setup` asks these questions for all steps." Say it only when the session has dev-setup. Else give the setup step of this plugin.
5. Go on with the command. A missing fact that the command can do without is not a reason to stop.

Done when you know the role, the route steps of this command, and each fact that the command needs.

## 4. End with the next step

End each command with one line: `Next:` and the command for the next step of the route, with its argument. Examples:

- `Next: /build SHOP-12`
- `Next: give SHOP-12 to the developer.` (claude.ai, or the next step is for another person)
- `Next: /review-comments` (when the pull request has comments)

Use the short command name. Use the full name (`/dev-flow:build`) only when two installed plugins have the same short name.
