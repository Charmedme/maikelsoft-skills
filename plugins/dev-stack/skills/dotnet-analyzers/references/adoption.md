# Adopt the baseline in an existing repo

## Contents

- Rules
- Steps
- Raise one rule
- Ratchet in CI

## Rules

- The build does not break on the first day. Start with a report only.
- Raise one rule ID (or one small group) per pull request, and fix all its findings in the same pull request.
- Do not change the existing style rules of the repo (stack rule 3). Add the severities of this skill one by one.
- Ask the human before each step that changes the build for the team.

## Steps

1. Measure. Add only the `ErrorLog` property to `Directory.Build.props`, build once, and count the findings per ID:

   ```bash
   dotnet build <solution> --no-incremental -tl:off -v q
   python3 <skill folder>/scripts/sarif.py . --summary --all-levels
   ```

   Done when you have the count per rule ID. Put it in the output.

2. Add the analyzer packages and `AnalysisLevel` with no build break. Keep `TreatWarningsAsErrors` off. The new findings show as warnings and in SARIF only.

3. Turn on `Nullable` per project. Start with the smallest projects. A large project can use `#nullable enable` per file first. Add `nullable` to `WarningsAsErrors` for each project that has 0 nullable warnings.

4. Pick the next rule from the summary. Start with the rules that find bugs and have few findings: CS86xx, VSTHRD100, VSTHRD002, CA2016, CA2200, RCS1075, RS0030. Leave the style rules (IDE0011, IDE0161, IDE1006) for last, because they change many lines.

5. Raise the rule (next section). Fix all findings in one pull request. Done when the build shows 0 findings for that ID.

6. When all chosen rules are at warning with 0 findings, turn on `TreatWarningsAsErrors` in CI.

## Raise one rule

Make the rule an error for the build, while other warnings stay warnings:

```xml
<PropertyGroup>
  <WarningsAsErrors>$(WarningsAsErrors);nullable;CA2016;RS0030</WarningsAsErrors>
</PropertyGroup>
```

Tested: with `WarningsAsErrors` set to `CA2016;RS0030`, these two IDs were errors and CA1822 stayed a warning.

When the repo already has `TreatWarningsAsErrors`, keep a rule that still has findings as a warning:

```xml
<PropertyGroup>
  <WarningsNotAsErrors>$(WarningsNotAsErrors);CA1822;IDE0011</WarningsNotAsErrors>
</PropertyGroup>
```

Tested: with `-warnaserror` and `WarningsNotAsErrors` set to `CA1822;IDE0011`, both stayed warnings and CA2016 was an error. Remove each ID from `WarningsNotAsErrors` when its count is 0.

For an IDE rule, the severity in `.editorconfig` must be `warning` first. Else the build does not report it.

## Ratchet in CI

A count that only goes down keeps the progress:

1. Store the summary of the main branch (`sarif.py . --summary > analyzer-baseline.txt`) in the repo.
2. In CI, compare the summary of the pull request with the stored file. Fail when the count of an ID goes up.
3. Fail when `sarif.py . --suppressed` shows more lines than on the main branch, unless the human approved the new suppression.
