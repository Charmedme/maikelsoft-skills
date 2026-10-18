# Agent loop

## Contents

- The loop
- Read the build log
- Read the SARIF file
- Fixer table
- Fix by hand
- Suppress a false positive
- Stop rules
- Report

## The loop

The loop gives the same result each run, because each step uses a tool and an exit code.

| Step | Command | Pass |
|---|---|---|
| 1. Build | `dotnet build <project.csproj> -warnaserror --no-incremental -tl:off -v q` | Exit 0 |
| 2. Read | `python3 <skill folder>/scripts/sarif.py <repo>` | Exit 0 |
| 3. Fix | The fixer for a known ID (fixer table), else by hand | The finding is gone |
| 4. Repeat | Steps 1 to 3, max 3 rounds | Exit 0 |
| 5. Format | `dotnet format <solution-or-project> --verify-no-changes` | Exit 0 |
| 6. Test | `dotnet test <solution-or-project>` | All pass |

Flags of the build:

- `-warnaserror`: each warning becomes an error, also NuGet and MSBuild warnings. The local build then gives the same result as CI.
- `--no-incremental`: a full compile. An incremental build that has nothing to do prints 0 warnings, also when the code has findings.
- `-tl:off -v q`: plain text lines, one per finding, without the progress display. The quiet log prints each finding two times: once during the build and once in the summary. Count unique lines.

## Read the build log

Each finding is one line:

```text
<file>(<line>,<column>): <error|warning> <ID>: <message> (<help URL>) [<project file>]
```

Example from the test run:

```text
/repo/src/Shop/Bad.cs(37,26): error CA2016: Forward the 'cancellationToken' parameter to the 'ReadToEndAsync' method or pass in 'CancellationToken.None' explicitly to indicate intentionally not propagating the token (https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca2016) [/repo/src/Shop/Shop.csproj]
```

This regex reads one line (tested):

```text
^(?<file>.+)\((?<line>\d+),(?<col>\d+)\): (?<sev>warning|error) (?<id>[A-Z]+\d+): (?<msg>.+?)( \(https?://[^)]*\))? \[(?<proj>.+)\]$
```

The build log does not show Info (`suggestion`) and Hidden (`silent`) findings, and it does not show suppressed findings.

## Read the SARIF file

When `Directory.Build.props` sets `ErrorLog`, the compiler writes `obj/<Project>.sarif` (SARIF 2.1) for each compile. The file has all findings: errors, warnings, notes (Info), and suppressed findings.

Fields of one result:

| Field | Meaning |
|---|---|
| `ruleId` | The rule ID, for example `RS0030` |
| `level` | `error`, `warning`, or `note` (Info). `-warnaserror` does not change the level in SARIF: a warning stays `warning`. |
| `message.text` | The message |
| `locations[0].physicalLocation.artifactLocation.uri` | The file, as a `file:///` URI |
| `locations[0].physicalLocation.region.startLine`, `startColumn` | The position |
| `suppressions[].kind` | `inSource` for `#pragma` or `SuppressMessage`. A result with `suppressions` is a suppressed finding. |
| `runs[0].tool.driver.rules[]` | The rule data by ID: `shortDescription`, `helpUri`, `properties.category` |

Example result (tested, shortened):

```json
{
  "ruleId": "RS0030",
  "level": "warning",
  "message": { "text": "The symbol 'DateTime.UtcNow' is banned in this project: Use TimeProvider.GetUtcNow(), so that tests can control the time." },
  "locations": [ { "physicalLocation": {
    "artifactLocation": { "uri": "file:///repo/src/Shop/Bad.cs" },
    "region": { "startLine": 15, "startColumn": 34, "endLine": 15, "endColumn": 49 } } } ]
}
```

`scripts/sarif.py` reads all `obj/*.sarif` files under a folder:

```bash
python3 <skill folder>/scripts/sarif.py <repo>               # open warnings and errors
python3 <skill folder>/scripts/sarif.py <repo> --summary     # count per rule ID
python3 <skill folder>/scripts/sarif.py <repo> --suppressed  # each #pragma and SuppressMessage
python3 <skill folder>/scripts/sarif.py <repo> --all-levels  # also the Info notes
```

Output (tested):

```text
src/Shop/Bad.cs:15:34 RS0030 warning The symbol 'DateTime.UtcNow' is banned in this project: Use TimeProvider.GetUtcNow(), so that tests can control the time.
sarif: 31 findings, 24 rule IDs, 1 suppressed not shown | 1 files
```

Exit code: 1 when an open warning or error exists, 2 when no SARIF file exists, else 0.

With `jq`, this command lists the open findings:

```bash
jq -r '.runs[].results[] | select(.suppressions == null and .level != "note") | "\(.locations[0].physicalLocation.artifactLocation.uri):\(.locations[0].physicalLocation.region.startLine) \(.ruleId) \(.message.text)"' src/Shop/obj/Shop.sarif
```

The SARIF file is from the last real compile. After a no-op build it is old. Build with `--no-incremental` before you read it.

## Fixer table

Use a fixer only with the exact IDs. Then build and test again.

| Tool | IDs that it fixed in the test | Notes |
|---|---|---|
| `dotnet format style <proj> --diagnostics <IDs>` | IDE0005, IDE0011, IDE0161 | IDE rules only. IDE1006 (naming) has no fix in `dotnet format`: rename by hand or in the IDE. |
| `dotnet format analyzers <proj> --diagnostics <IDs> --severity info` | CA1510, CA1829, CA2016, RCS1155, RCS1073, RCS1077, VSTHRD100 | Without `--severity info` the command fixed nothing in the test. |
| `roslynator fix <proj> --supported-diagnostics <IDs>` | CA1510, CA1829, CA2016, VSTHRD100 | Prints "Fixed diagnostics", "Unfixed diagnostics", and "Unfixable diagnostics". Exit 0 also when it fixed nothing. |
| `dotnet format whitespace <proj>` | Whitespace, BOM (`CHARSET`) | Safe to run on all files. |
| None | CA1304, CA1305, CA1311, CA1862, CA2254, CS8602, RS0030, RCS1075, VSTHRD002, VSTHRD110, MA0016, MA0048 | Fix by hand: the fix needs a choice (culture, comparison, time source, error handling). |

Fixes that change behavior or API:

- The VSTHRD100 fix changes `async void` to `async Task`. That changes the public API. The callers must await the task.
- The RCS1155 fix picks `StringComparison.OrdinalIgnoreCase` or `Ordinal`. Check that the comparison is what the code needs.
- RCS1154 sorts enum members. Do not run it on enums with stored values.

## Fix by hand

The fix patterns for the common IDs are in `rule-catalog.md`. Read the help URL of the finding for an ID that is not there.

## Suppress a false positive

Suppress only when the finding is a false positive and the human agrees. Use the smallest scope and give the reason on the same line:

```csharp
#pragma warning disable CA1822 // Instance member: the serializer needs an instance method.
public int Version() => 1;
#pragma warning restore CA1822
```

Or on a member:

```csharp
[SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "The serializer needs an instance method.")]
public int Version() => 1;
```

Do not use `NoWarn` in a project file, `<RunAnalyzers>false</RunAnalyzers>`, or a lower severity in `.editorconfig`. Each of these hides the rule for all code, and `NoWarn` leaves no trace in SARIF.

## Stop rules

- Stop after 3 build rounds with open findings. Report them.
- Stop when a fix needs a decision that is not yours (a public API change, a new package, a lower severity). Ask the human.
- Stop when a finding is a false positive. Give the ID, the location, and the reason. Do not suppress it without approval.
- Stop when the same finding comes back after a fix. The fix is wrong, or two rules conflict.

## Report

Write one line per open item in the output:

```text
Open: src/Shop/Bad.cs:24 CA1862 needs a choice: ordinal or culture comparison for user input.
Suppressed: src/Shop/Serializer.cs:12 CA1822 "The serializer needs an instance method." (approved by <human>)
Checks: build 0 warnings, format exit 0, tests 42 passed, stackcheck 0 errors.
```
