# Stack rules

The source of this file is `plugins/dev-stack/shared/stack-rules.md` in the repository github.com/Charmedme/maikelsoft-skills. Each dev-stack skill has a copy. To change the rules, edit the source and run `python3 scripts/sync_shared.py`.

These rules apply to each dev-stack skill. The skill adds the rules of its technology.

## Contents

- 1. Version first
- 2. Current facts
- 3. Existing code wins
- 4. Decisions for new code
- 5. New packages
- 6. Check with tools first
- 7. Output language
- 8. Report

## 1. Version first

1. Read the versions that the project uses: `global.json`, `*.csproj`, `Directory.Build.props`, `Directory.Packages.props`, `package.json`, image tags. Done when you can name the version of each technology that the change touches.
2. Apply the rules for that version. When the skill has a reference for that major version (for example `references/net11.md`), read it.
3. An existing project keeps its versions. Upgrade only when the task asks for it.
4. A new project uses the latest stable versions on the day of the work. Look them up (section 2).

## 2. Current facts

Your trained knowledge is older than the project. The skill text is also older: its `metadata.verified` date says when someone checked its facts.

- When the `verified` date is older than 6 months, look up the version facts of the skill before you use them.
- Look up each API, package, or setting that the skill does not cover. Do not trust memory for these.
- Use the tools in this order:
  1. An MCP tool for the source, for example Microsoft Learn or DeepWiki.
  2. The official documentation page.
  3. The package registry (nuget.org, npmjs.com).
  4. The source repository.
- Write each look-up in your output: "Checked: <fact>, <source>, <date>".

## 3. Existing code wins

Read the code near the change before you write. Follow its style: names, folders, patterns, and libraries. When the project style differs from this skill, the project style wins in existing code. Say so in your output. When `docs/dev-team/conventions.md` exists, read it first.

## 4. Decisions for new code

Each skill has a list of team decisions in `references/decisions.md`, with an ID such as `API-01`.

- When `docs/dev-team/decisions.md` exists, read it. Follow each accepted decision.
- When the new code needs a decision that has no accepted answer, do not decide it yourself. Give the question, the options, and the recommendation from `references/decisions.md`, and ask the human. When nobody can answer, use the recommendation and mark it "assumed" in your output.

## 5. New packages

Before you add a package (NuGet, npm, container image, tool):

1. Check that the package exists in its registry, with the exact name. LLMs invent package names.
2. Tell the human the name, the version, the license, and the reason. Then wait for the approval. This applies to open source licenses too.
3. When the license is not open source, or the license changed in a recent major version, say so first.

These packages are not allowed: MediatR, AutoMapper. Use direct handler classes and manual mapping. For large mappings, use Mapperly.

One pin has standing approval. SqlClient 6.1 and 7.1 and MimeKit 4.15.1 to 4.18.1 pull a vulnerable `System.Security.Cryptography.Pkcs`. Then add a direct `PackageVersion` for Pkcs 10.0.x with `CentralPackageTransitivePinningEnabled`. Name the pin and the reason in the report. Do not ask again.

## 6. Check with tools first

Run the tools before you judge the code yourself:

- The build with the analyzers (`dotnet build -warnaserror`), the formatter, and the tests, when they apply.
- The rule check of this skill:

  ```
  python3 <skill folder>/scripts/stackcheck.py --root <repo>
  ```

  In an existing project, add `--changed <base branch>` to check only the changed files.

Fix each error. For a warning, fix it or give the reason to keep it. Suppress a finding only with a reason on the line: `// stackcheck: ignore <RULE-ID> <reason>`.

Run the checks again after your last change to a file. The Checks table of the report shows the results of that last run only. A check that you did not run after the last change gets the result "not run after the last change".

## 7. Output language

Code, identifiers, and code comments are in English. Write all other output in the rules of `references/language.md`: ASD-STE100 for English, Begrijpelijk Nederlands (B1) for Dutch.

## 8. Report

End each task with one report in the template below. Use the headings as written, in this order, also when a section is empty ("None."). The same headings on each run let the human and the next agent find each fact in the same place. A skill or an agent that gives its own report template (for example the security reviewer) replaces this one.

Keep the report under 700 words. Tables count. Do not repeat the request, the diff, or the skill text. Put each fact in one section only.

### Report after a change

```markdown
## Result
<Status word: DONE | DONE_WITH_CONCERNS | BLOCKED>. <One or two sentences: what works now.>
Cause: <only when the change fixes a problem that is not obvious (a build or test failure, a hidden trap, an old API): the cause in one sentence.>

## Changes
| File | Change |
|------|--------|

## Checks
| Check | Command | Result |
|-------|---------|--------|
<build, tests, format, stackcheck: the summary line of the tool, not your opinion>

## Decisions
| ID | Question | Answer | Status |
|----|----------|--------|--------|
<accepted (from decisions.md) or assumed (nobody answered). Decisions without a catalog ID use "-".>

## Assumptions
1. <One line each. Only assumptions that are not a decision above.>

## Packages
<None. Or one line for each package: name, version, license, reason, "waiting for approval".
For a vulnerable package: name, advisory ID, the fixed version, and how you fixed it (upgrade, pin of the transitive package).>

## Open points
<None. Or one line each: what the human must do or check.>
```

### Report after a review

```markdown
## Verdict
<APPROVED | APPROVED_WITH_COMMENTS | CHANGES_REQUIRED>. <One sentence: the main reason.>

## Checks
| Check | Command | Result |
|-------|---------|--------|

## Findings
| # | Severity | Location | Problem | Fix |
|---|----------|----------|---------|-----|
<Severity: blocking, major, or minor. Location: path:line. One row per problem. Sort by severity. Fix: what to change, how, and why it solves the problem, in one or two sentences.>

## Decisions
| ID | Question | Answer | Status |
|----|----------|--------|--------|

## Assumptions
1. <One line each.>

## Open points
<None. Or one line each.>
```

Each recommendation in a report says what to change, how to change it, and why it solves the problem. This applies to a fix, a setting, a package and a design change.

Use "blocking" for a bug, a security hole, a broken build or test, or a break of an accepted decision. Use "major" for a wrong or outdated API, a missing test of core behavior, or a design that costs the team later. Use "minor" for style and naming. When you are not sure, take the lower level.
